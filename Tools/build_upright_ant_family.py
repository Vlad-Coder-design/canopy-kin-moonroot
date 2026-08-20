"""Build the 0.6.0 opaque, upright ant family from the verified CC BY worker.

The Sketchfab source is a detailed, segmented and rigged 47.9K-triangle ant,
but it uses 53 bone-parented mesh objects, a heavily scaled hierarchy and
Blender -Y as anatomical forward. This tool:

* bakes the source hierarchy into one identity-transform skinned mesh;
* rebuilds a clean armature with stable production bone names;
* recalculates every disconnected shell's normals outside;
* creates distinct caste morphologies without changing the gameplay root;
* keeps Blender Z up / -Y forward so standard FBX export becomes Unity
  +Y up / +Z forward with no runtime corrective rotation;
* exports a 47.9K close LOD and an approximately 12K distant LOD;
* authors the existing thirteen production animation clips.

Run:
    blender --background --python Tools/build_upright_ant_family.py
    blender --background --python Tools/build_upright_ant_family.py -- Player
"""

from __future__ import annotations

import importlib.util
import math
import sys
from dataclasses import dataclass
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


PROJECT = Path(r"C:\codex-ant-project")
SOURCE = (
    PROJECT
    / "ArtSource/ThirdParty/Sketchfab/GameReadyWorkerAnt/Raw"
    / "GameReadyWorkerAnt.blend"
)
FBX_DIRECTORY = PROJECT / "Assets/Resources/Models/Ant/Family"
BLEND_DIRECTORY = PROJECT / "ArtSource/AntFamily"
LEGACY_BUILDER = PROJECT / "Tools/build_ant_family.py"


@dataclass(frozen=True)
class Caste:
    key: str
    head: tuple[float, float, float]
    thorax: tuple[float, float, float]
    abdomen: tuple[float, float, float]
    leg_length: float
    leg_height: float
    antenna_length: float
    mandible_length: float
    mandible_width: float


CASTES = (
    Caste("Player", (1.04, 1.03, 1.03), (1.0, 1.0, 1.0), (.93, .98, .95), 1.08, 1.03, 1.14, 1.08, .96),
    Caste("Scout", (.98, 1.0, .98), (.96, .98, .96), (.9, .94, .9), 1.12, 1.04, 1.18, 1.0, .9),
    Caste("Worker", (.94, .97, .95), (.98, 1.0, .98), (1.12, 1.09, 1.1), .98, 1.0, 1.0, .92, .88),
    Caste("Nurse", (.91, .95, .93), (.94, .98, .95), (1.08, 1.06, 1.08), .94, .98, .96, .84, .82),
    Caste("LightSoldier", (1.19, 1.08, 1.12), (1.06, 1.04, 1.06), (1.0, 1.0, 1.0), 1.02, 1.02, .96, 1.26, 1.12),
    Caste("HeavySoldier", (1.38, 1.15, 1.22), (1.16, 1.1, 1.14), (1.1, 1.06, 1.1), 1.0, 1.04, .93, 1.5, 1.28),
    Caste("Queen", (1.03, 1.02, 1.05), (1.42, 1.25, 1.38), (1.72, 1.5, 1.62), .9, .96, .9, .94, .96),
    Caste("Rival", (1.24, 1.1, 1.15), (1.09, 1.05, 1.08), (.98, 1.02, 1.0), 1.06, 1.05, 1.05, 1.42, 1.16),
)


DIRECT_BONE_NAMES = {
    "Body.front": "Thorax",
    "Body.back": "Abdomen",
    "Head": "Head",
    "zange.L.001": "Mandible_L",
    "zange.R.001": "Mandible_R",
    "Fühler.L.001": "Antenna_L_1",
    "Fühler.L.002": "Antenna_L_3",
    "Fühler.R.001": "Antenna_R_1",
    "Fühler.R.002": "Antenna_R_3",
}
PAIR_NAMES = {"front": "Front", "center": "Middle", "back": "Rear"}
SEGMENT_NAMES = {
    "001": "Coxa",
    "002": "Femur",
    "003": "Tibia",
    "004": "Tarsus",
    "005": "Tarsus2",
    "006": "Tarsus3",
    "007": "TarsusTip",
}


def production_bone_name(source: str) -> str | None:
    direct = DIRECT_BONE_NAMES.get(source)
    if direct:
        return direct
    parts = source.split(".")
    if len(parts) == 4 and parts[0] == "leg":
        pair = PAIR_NAMES.get(parts[1])
        segment = SEGMENT_NAMES.get(parts[3])
        if pair and segment and parts[2] in {"L", "R"}:
            return f"Leg_{parts[2]}_{pair}_{segment}"
    return None


def material(name: str, color: tuple[float, float, float, float], roughness: float):
    result = bpy.data.materials.new(name)
    result.diffuse_color = color
    result.metallic = 0.04 if name != "CompoundEye" else 0.12
    result.roughness = roughness
    return result


def recalculate_outside(mesh: bpy.types.Mesh) -> tuple[int, int]:
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.faces.ensure_lookup_table()
    boundary = [
        edge for edge in bm.edges
        if len(edge.link_faces) == 1
    ]
    filled = 0
    if boundary:
        result = bmesh.ops.holes_fill(bm, edges=boundary, sides=0)
        filled = len(result.get("faces", ()))
        bm.faces.ensure_lookup_table()
    original = [face.normal.copy() for face in bm.faces]
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    corrected = sum(
        before.dot(face.normal) < 0
        for before, face in zip(original, bm.faces)
    )
    # Unity's FBX importer rejects self-intersecting n-gons.  The downloaded
    # source has nine open segment caps; holes_fill closes them correctly, but
    # a few of those caps are concave.  Triangulate inside Blender so Unity
    # receives only deterministic, non-self-intersecting faces.
    bmesh.ops.triangulate(
        bm,
        faces=list(bm.faces),
        quad_method="BEAUTY",
        ngon_method="BEAUTY",
    )
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(mesh)
    bm.free()
    mesh.validate(clean_customdata=False)
    mesh.update(calc_edges=True)
    return corrected, filled


def create_clean_armature(source: bpy.types.Object) -> bpy.types.Object:
    data = bpy.data.armatures.new("CanopyKin upright ant armature")
    result = bpy.data.objects.new("CanopyKin upright ant armature", data)
    bpy.context.collection.objects.link(result)
    result.matrix_world.identity()

    source_matrix = source.matrix_world.copy()
    source_roll_matrix = source_matrix.to_3x3()
    bpy.context.view_layer.objects.active = result
    result.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")

    root = data.edit_bones.new("Root")
    root.head = Vector((0, 0, 0))
    root.tail = Vector((0, 0, .18))
    root.use_deform = False

    created = {}
    for source_bone in source.data.bones:
        name = production_bone_name(source_bone.name)
        if not name:
            continue
        bone = data.edit_bones.new(name)
        bone.head = source_matrix @ source_bone.head_local
        bone.tail = source_matrix @ source_bone.tail_local
        if (bone.tail - bone.head).length < .0001:
            bone.tail = bone.head + Vector((0, 0, .01))
        roll_axis = (
            source_roll_matrix
            @ source_bone.matrix_local.to_3x3()
            @ Vector((0, 0, 1))
        )
        if roll_axis.length > .0001:
            bone.align_roll(roll_axis.normalized())
        created[source_bone.name] = bone

    for source_bone in source.data.bones:
        bone = created.get(source_bone.name)
        if not bone:
            continue
        parent = created.get(source_bone.parent.name) if source_bone.parent else None
        bone.parent = parent or root
        bone.use_connect = False

    # The source has two visible antenna sections. Insert a middle control bone
    # so the existing three-stage procedural antenna motion remains compatible.
    for side in ("L", "R"):
        base = data.edit_bones[f"Antenna_{side}_1"]
        tip = data.edit_bones[f"Antenna_{side}_3"]
        start = tip.head.copy()
        end = tip.tail.copy()
        middle_point = start.lerp(end, .5)
        middle = data.edit_bones.new(f"Antenna_{side}_2")
        middle.head = start
        middle.tail = middle_point
        middle.roll = tip.roll
        middle.parent = base
        middle.use_connect = False
        tip.head = middle_point
        tip.parent = middle
        tip.use_connect = False

    bpy.ops.object.mode_set(mode="OBJECT")
    return result


def prepare_source_meshes(
    source_armature: bpy.types.Object,
    clean_armature: bpy.types.Object,
    shell_material,
    eye_material,
) -> tuple[list[bpy.types.Object], int, int]:
    prepared = []
    corrected_normals = 0
    filled_holes = 0
    source_armature.data.pose_position = "REST"
    bpy.context.scene.frame_set(0)
    bpy.context.view_layer.update()

    for source in list(bpy.context.scene.objects):
        if (
            source.type != "MESH"
            or source.parent != source_armature
            or source.name == "Sphere"
            or source.parent_type != "BONE"
        ):
            continue
        bone_name = production_bone_name(source.parent_bone)
        if not bone_name:
            continue
        mesh = source.data.copy()
        mesh.name = source.name + "_production"
        mesh.transform(source.matrix_world)
        corrected, filled = recalculate_outside(mesh)
        corrected_normals += corrected
        filled_holes += filled

        obj = bpy.data.objects.new(source.name + "_production", mesh)
        bpy.context.collection.objects.link(obj)
        obj.data.materials.clear()
        obj.data.materials.append(shell_material)
        obj.data.materials.append(eye_material)
        for polygon in obj.data.polygons:
            polygon.material_index = min(polygon.material_index, 1)
            polygon.use_smooth = True
        group = obj.vertex_groups.new(name=bone_name)
        group.add(list(range(len(mesh.vertices))), 1.0, "REPLACE")
        prepared.append(obj)

    if not prepared:
        raise RuntimeError("The worker source produced no ant mesh parts.")

    bpy.ops.object.select_all(action="DESELECT")
    for obj in prepared:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = prepared[0]
    bpy.ops.object.join()
    joined = prepared[0]
    joined.name = "CanopyKinAnt_Source"
    joined.data.name = "CanopyKinAnt_Source_Mesh"
    joined.parent = clean_armature
    modifier = joined.modifiers.new("Anatomical armature", "ARMATURE")
    modifier.object = clean_armature
    return [joined], corrected_normals, filled_holes


def scale_family_source(
    mesh: bpy.types.Object,
    armature: bpy.types.Object,
) -> float:
    y_values = [vertex.co.y for vertex in mesh.data.vertices]
    factor = 1.12 / max(.0001, max(y_values) - min(y_values))
    for vertex in mesh.data.vertices:
        vertex.co *= factor
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    for bone in armature.data.edit_bones:
        bone.head *= factor
        bone.tail *= factor
    bpy.ops.object.mode_set(mode="OBJECT")
    return factor


def scale_about(point: Vector, center: Vector, scale) -> Vector:
    relative = point - center
    return center + Vector(
        (relative.x * scale[0], relative.y * scale[1], relative.z * scale[2])
    )


def dominant_group(obj: bpy.types.Object, vertex: bpy.types.MeshVertex) -> str:
    if not vertex.groups:
        return ""
    strongest = max(vertex.groups, key=lambda assignment: assignment.weight)
    return obj.vertex_groups[strongest.group].name


def morph_point(
    name: str,
    point: Vector,
    caste: Caste,
    head_center: Vector,
    thorax_center: Vector,
    abdomen_center: Vector,
) -> Vector:
    if name == "Head":
        return scale_about(point, head_center, caste.head)
    if name.startswith("Mandible_"):
        result = scale_about(point, head_center, caste.head)
        relative = result - head_center
        relative.y *= caste.mandible_length
        relative.x *= caste.mandible_width
        return head_center + relative
    if name.startswith("Antenna_"):
        result = scale_about(point, head_center, caste.head)
        return head_center + (result - head_center) * caste.antenna_length
    if name == "Thorax":
        return scale_about(point, thorax_center, caste.thorax)
    if name == "Abdomen":
        return scale_about(point, abdomen_center, caste.abdomen)
    if name.startswith("Leg_"):
        result = point.copy()
        result.x *= caste.leg_length
        result.z = thorax_center.z + (
            result.z - thorax_center.z
        ) * caste.leg_height
        return result
    return point


def morph_caste(
    mesh: bpy.types.Object,
    armature: bpy.types.Object,
    caste: Caste,
) -> None:
    bones = armature.data.bones
    head_center = bones["Head"].head_local.copy()
    thorax_center = bones["Thorax"].head_local.copy()
    abdomen_center = bones["Abdomen"].head_local.copy()

    for vertex in mesh.data.vertices:
        group = dominant_group(mesh, vertex)
        vertex.co = morph_point(
            group,
            vertex.co,
            caste,
            head_center,
            thorax_center,
            abdomen_center,
        )

    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    for bone in armature.data.edit_bones:
        bone.head = morph_point(
            bone.name,
            bone.head,
            caste,
            head_center,
            thorax_center,
            abdomen_center,
        )
        bone.tail = morph_point(
            bone.name,
            bone.tail,
            caste,
            head_center,
            thorax_center,
            abdomen_center,
        )
    bpy.ops.object.mode_set(mode="OBJECT")


def shift_to_ground(
    meshes: list[bpy.types.Object],
    armature: bpy.types.Object,
) -> float:
    minimum = min(
        vertex.co.z
        for obj in meshes
        for vertex in obj.data.vertices
    )
    offset = .004 - minimum
    for obj in meshes:
        for vertex in obj.data.vertices:
            vertex.co.z += offset
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    for bone in armature.data.edit_bones:
        bone.head.z += offset
        bone.tail.z += offset
    bpy.ops.object.mode_set(mode="OBJECT")
    return offset


def create_lod(
    source: bpy.types.Object,
    armature: bpy.types.Object,
    caste: Caste,
) -> tuple[bpy.types.Object, bpy.types.Object]:
    lod0 = source
    lod0.name = f"CanopyKin_{caste.key}_LOD0"
    lod0.data.name = lod0.name + "_Mesh"

    lod1 = lod0.copy()
    lod1.data = lod0.data.copy()
    bpy.context.collection.objects.link(lod1)
    lod1.name = f"CanopyKin_{caste.key}_LOD1"
    lod1.data.name = lod1.name + "_Mesh"
    for modifier in lod1.modifiers:
        if modifier.type == "ARMATURE":
            modifier.object = armature
    decimate = lod1.modifiers.new("Performance-conscious distant LOD", "DECIMATE")
    decimate.ratio = .25
    decimate.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = lod1
    lod1.select_set(True)
    while lod1.modifiers.find(decimate.name) > 0:
        bpy.ops.object.modifier_move_up(modifier=decimate.name)
    bpy.ops.object.modifier_apply(modifier=decimate.name)
    recalculate_outside(lod0.data)
    recalculate_outside(lod1.data)
    return lod0, lod1


def remove_source_objects(keep: set[bpy.types.Object]) -> None:
    for obj in list(bpy.data.objects):
        if obj not in keep:
            bpy.data.objects.remove(obj, do_unlink=True)


def add_actions(armature: bpy.types.Object) -> None:
    spec = importlib.util.spec_from_file_location(
        "legacy_ant_builder",
        LEGACY_BUILDER,
    )
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    module.create_actions(armature)


def export_variant(caste: Caste) -> None:
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    source_armature = bpy.data.objects.get("Worker ant armature")
    if not source_armature or source_armature.type != "ARMATURE":
        raise RuntimeError("The verified worker source armature is missing.")

    shell_color = (
        (.20, .025, .008, 1)
        if caste.key == "Rival"
        else (.105, .018, .006, 1)
    )
    if caste.key == "Nurse":
        shell_color = (.19, .055, .018, 1)
    shell = material("AntShell", shell_color, .29)
    eye = material("CompoundEye", (.003, .012, .008, 1), .08)
    clean_armature = create_clean_armature(source_armature)
    prepared, corrected_normals, filled_holes = prepare_source_meshes(
        source_armature,
        clean_armature,
        shell,
        eye,
    )
    source_mesh = prepared[0]
    scale_factor = scale_family_source(source_mesh, clean_armature)
    morph_caste(source_mesh, clean_armature, caste)
    shift_to_ground([source_mesh], clean_armature)
    lod0, lod1 = create_lod(source_mesh, clean_armature, caste)
    add_actions(clean_armature)

    clean_armature.name = f"CanopyKin_{caste.key}_Armature"
    lod0["lod_level"] = 0
    lod1["lod_level"] = 1
    lod0["caste"] = caste.key
    lod1["caste"] = caste.key
    clean_armature["coordinate_convention"] = (
        "Blender Z-up/-Y-forward; Unity +Y-up/+Z-forward"
    )
    remove_source_objects({clean_armature, lod0, lod1})

    FBX_DIRECTORY.mkdir(parents=True, exist_ok=True)
    BLEND_DIRECTORY.mkdir(parents=True, exist_ok=True)
    blend_path = BLEND_DIRECTORY / f"CanopyKinAnt_{caste.key}.blend"
    fbx_path = FBX_DIRECTORY / f"CanopyKinAnt_{caste.key}.fbx"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path), compress=True)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in (clean_armature, lod0, lod1):
        obj.select_set(True)
    bpy.context.view_layer.objects.active = clean_armature
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_scale_options="FBX_SCALE_UNITS",
        apply_unit_scale=True,
        bake_space_transform=False,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        use_armature_deform_only=False,
        bake_anim=True,
        bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,
        path_mode="AUTO",
        embed_textures=False,
    )
    triangles = {}
    for obj in (lod0, lod1):
        obj.data.calc_loop_triangles()
        triangles[obj.name] = len(obj.data.loop_triangles)
    print(
        f"CANOPY_KIN_UPRIGHT_ANT_OK caste={caste.key} "
        f"scale={scale_factor:.6f} correctedNormals={corrected_normals} "
        f"filledHoles={filled_holes} "
        f"triangles={triangles} actions={len(bpy.data.actions)}"
    )


def requested_castes() -> tuple[Caste, ...]:
    raw = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    if not raw:
        return CASTES
    wanted = {value.casefold() for value in raw}
    selected = tuple(caste for caste in CASTES if caste.key.casefold() in wanted)
    if len(selected) != len(wanted):
        known = ", ".join(caste.key for caste in CASTES)
        raise SystemExit(f"Unknown caste. Expected one or more of: {known}")
    return selected


def main() -> None:
    for caste in requested_castes():
        export_variant(caste)


if __name__ == "__main__":
    main()
