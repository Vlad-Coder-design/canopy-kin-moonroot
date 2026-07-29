"""Build the 0.5.0 production ant family from the inspected CC0 rig base.

The source OpenGameArt mesh supplies the legal anatomical proportions, UV
layout and initial skinning.  This script repairs the hierarchy and weights,
creates a much denser smooth close-camera mesh, adds compound eyes and
serrated mandibles, authors biologically distinct castes, and exports two
skinned LODs plus a common thirteen-action animation set per caste.
"""

from __future__ import annotations

import math
from dataclasses import dataclass
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT = Path(r"C:\codex-ant-project")
SOURCE = (
    PROJECT
    / "ArtSource/ThirdParty/OpenGameArt/Ant/Raw/ant.blend"
)
FBX_DIRECTORY = PROJECT / "Assets/Resources/Models/Ant/Family"
BLEND_DIRECTORY = PROJECT / "ArtSource/AntFamily"


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
    wing_scars: bool = False
    dorsal_shield: float = 0.0


CASTES = (
    Caste("Player", (1.04, 1.03, 1.03), (1.0, 1.0, 1.0), (.93, .98, .95), 1.08, 1.03, 1.14, 1.08, .96),
    Caste("Scout", (.98, 1.0, .98), (.96, .98, .96), (.9, .94, .9), 1.12, 1.04, 1.18, 1.0, .9),
    Caste("Worker", (.94, .97, .95), (.98, 1.0, .98), (1.12, 1.09, 1.1), .98, 1.0, 1.0, .92, .88),
    Caste("Nurse", (.91, .95, .93), (.94, .98, .95), (1.08, 1.06, 1.08), .94, .98, .96, .84, .82),
    Caste("LightSoldier", (1.19, 1.08, 1.12), (1.06, 1.04, 1.06), (1.0, 1.0, 1.0), 1.02, 1.02, .96, 1.26, 1.12, dorsal_shield=.55),
    Caste("HeavySoldier", (1.38, 1.15, 1.22), (1.16, 1.1, 1.14), (1.1, 1.06, 1.1), 1.0, 1.04, .93, 1.5, 1.28, dorsal_shield=1.0),
    Caste("Queen", (1.03, 1.02, 1.05), (1.42, 1.25, 1.38), (1.72, 1.5, 1.62), .9, .96, .9, .94, .96, wing_scars=True, dorsal_shield=.35),
    Caste("Rival", (1.24, 1.1, 1.15), (1.09, 1.05, 1.08), (.98, 1.02, 1.0), 1.06, 1.05, 1.05, 1.42, 1.16, dorsal_shield=.72),
)


BONE_NAMES = {
    "Bone": "Thorax",
    "Bone.001": "Neck",
    "Bone.031": "Head",
    "Bone.002": "Abdomen",
    "Bone.003": "Mandible_R",
    "Bone.004": "Mandible_R_Tip",
    "Bone.005": "Mandible_L",
    "Bone.006": "Mandible_L_Tip",
    "Bone.032": "Antenna_R_1",
    "Bone.034": "Antenna_R_2",
    "Bone.036": "Antenna_R_3",
    "Bone.033": "Antenna_L_1",
    "Bone.035": "Antenna_L_2",
    "Bone.037": "Antenna_L_3",
    "Bone.007": "Leg_L_Front_Coxa",
    "Bone.008": "Leg_L_Front_Femur",
    "Bone.009": "Leg_L_Front_Tibia",
    "Bone.010": "Leg_L_Front_Tarsus",
    "Bone.011": "Leg_L_Middle_Coxa",
    "Bone.012": "Leg_L_Middle_Femur",
    "Bone.013": "Leg_L_Middle_Tibia",
    "Bone.014": "Leg_L_Middle_Tarsus",
    "Bone.015": "Leg_L_Rear_Coxa",
    "Bone.016": "Leg_L_Rear_Femur",
    "Bone.017": "Leg_L_Rear_Tibia",
    "Bone.018": "Leg_L_Rear_Tarsus",
    "Bone.019": "Leg_R_Front_Coxa",
    "Bone.020": "Leg_R_Front_Femur",
    "Bone.021": "Leg_R_Front_Tibia",
    "Bone.022": "Leg_R_Front_Tarsus",
    "Bone.027": "Leg_R_Middle_Coxa",
    "Bone.028": "Leg_R_Middle_Femur",
    "Bone.029": "Leg_R_Middle_Tibia",
    "Bone.030": "Leg_R_Middle_Tarsus",
    "Bone.023": "Leg_R_Rear_Coxa",
    "Bone.024": "Leg_R_Rear_Femur",
    "Bone.025": "Leg_R_Rear_Tibia",
    "Bone.026": "Leg_R_Rear_Tarsus",
}

HEAD_GROUPS = {"Bone.001", "Bone.031"}
THORAX_GROUPS = {"Bone"}
ABDOMEN_GROUPS = {"Bone.002"}
MANDIBLE_GROUPS = {"Bone.003", "Bone.004", "Bone.005", "Bone.006"}
ANTENNA_GROUPS = {"Bone.032", "Bone.034", "Bone.036", "Bone.033", "Bone.035", "Bone.037"}
LEG_GROUPS = {name for name in BONE_NAMES if name.startswith("Bone.") and BONE_NAMES[name].startswith("Leg_")}


def material(name: str, color: tuple[float, float, float, float], roughness: float):
    result = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    result.diffuse_color = color
    result.metallic = 0.04 if "Eye" not in name else 0.12
    result.roughness = roughness
    return result


def dominant_group(obj: bpy.types.Object, vertex: bpy.types.MeshVertex) -> str:
    if not vertex.groups:
        return ""
    strongest = max(vertex.groups, key=lambda assignment: assignment.weight)
    return obj.vertex_groups[strongest.group].name


def scale_about(point: Vector, center: Vector, scale: tuple[float, float, float]) -> Vector:
    relative = point - center
    return center + Vector(
        (relative.x * scale[0], relative.y * scale[1], relative.z * scale[2])
    )


def morph_base(obj: bpy.types.Object, caste: Caste) -> None:
    head_center = Vector((0, .225, .17))
    thorax_center = Vector((0, -.055, .105))
    abdomen_center = Vector((0, -.365, .055))
    for vertex in obj.data.vertices:
        group = dominant_group(obj, vertex)
        point = vertex.co.copy()
        if group in HEAD_GROUPS:
            point = scale_about(point, head_center, caste.head)
        elif group in THORAX_GROUPS:
            point = scale_about(point, thorax_center, caste.thorax)
        elif group in ABDOMEN_GROUPS:
            point = scale_about(point, abdomen_center, caste.abdomen)
        elif group in MANDIBLE_GROUPS:
            point = scale_about(point, head_center, caste.head)
            base = Vector((0, .25, .155))
            relative = point - base
            relative.y *= caste.mandible_length
            relative.x *= caste.mandible_width
            point = base + relative
        elif group in ANTENNA_GROUPS:
            point = scale_about(point, head_center, caste.head)
            base = Vector((0, .17, .19))
            point = base + (point - base) * caste.antenna_length
        elif group in LEG_GROUPS:
            point.x *= caste.leg_length
            point.z = thorax_center.z + (point.z - thorax_center.z) * caste.leg_height
        vertex.co = point

    for polygon in obj.data.polygons:
        polygon.use_smooth = True


def rename_rig(mesh: bpy.types.Object, armature: bpy.types.Object) -> None:
    for original, production in BONE_NAMES.items():
        bone = armature.data.bones.get(original)
        if bone:
            bone.name = production

    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    edit_bones = armature.data.edit_bones
    root = edit_bones.new("Root")
    root.head = Vector((0, -.08, -.03))
    root.tail = Vector((0, -.08, .12))
    root.use_deform = False
    thorax = edit_bones["Thorax"]
    thorax.parent = root
    edit_bones["Abdomen"].parent = thorax
    for bone in edit_bones:
        if bone.name.startswith("Leg_") and bone.name.endswith("_Coxa"):
            bone.parent = thorax
    bpy.ops.object.mode_set(mode="OBJECT")


def add_uv(mesh: bpy.types.Mesh) -> None:
    layer = mesh.uv_layers.new(name="UVMap")
    bounds_x = max((abs(vertex.co.x) for vertex in mesh.vertices), default=1)
    bounds_y = max((abs(vertex.co.y) for vertex in mesh.vertices), default=1)
    for polygon in mesh.polygons:
        for loop_index in polygon.loop_indices:
            vertex = mesh.vertices[mesh.loops[loop_index].vertex_index]
            layer.data[loop_index].uv = (
                .5 + vertex.co.x / max(.001, bounds_x * 2),
                .5 + vertex.co.y / max(.001, bounds_y * 2),
            )


def weighted_object(
    name: str,
    vertices: list[tuple[float, float, float]],
    faces: list[tuple[int, ...]],
    bone: str,
    target_material,
) -> bpy.types.Object:
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update(calc_edges=True)
    add_uv(mesh)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(target_material)
    group = obj.vertex_groups.new(name=bone)
    group.add(list(range(len(mesh.vertices))), 1.0, "REPLACE")
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    return obj


def compound_eye(
    name: str,
    side: float,
    caste: Caste,
    subdivisions: int,
    eye_material,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1)
    obj = bpy.context.object
    obj.name = name
    head_center = Vector((0, .225, .17))
    position = scale_about(
        Vector((side * .072, .235, .19)),
        head_center,
        caste.head,
    )
    obj.location = position
    obj.scale = Vector((.026 * caste.head[0], .043 * caste.head[1], .034 * caste.head[2]))
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=True)
    for vertex in obj.data.vertices:
        normal = vertex.co.normalized()
        # Shallow deterministic facets break the perfect primitive silhouette.
        vertex.co *= 1.0 + .035 * math.sin(normal.x * 31 + normal.y * 23 + normal.z * 19)
    obj.data.materials.append(eye_material)
    group = obj.vertex_groups.new(name="Head")
    group.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    return obj


def serrated_mandible(
    name: str,
    side: float,
    caste: Caste,
    joint_material,
    lod0: bool,
) -> bpy.types.Object:
    base_y = .255
    length = .155 * caste.mandible_length
    width = .052 * caste.mandible_width
    inner = .019 * caste.mandible_width
    outline = [
        (side * inner, base_y),
        (side * width, base_y + length * .12),
        (side * width * 1.16, base_y + length * .55),
        (side * width * .72, base_y + length),
        (side * inner * .62, base_y + length * .88),
        (side * inner * 1.28, base_y + length * .70),
        (side * inner * .68, base_y + length * .60),
        (side * inner * 1.5, base_y + length * .48),
        (side * inner * .76, base_y + length * .37),
        (side * inner * 1.58, base_y + length * .25),
    ]
    z_low, z_high = .139, .16
    vertices = [(x, y, z_low) for x, y in outline] + [
        (x, y, z_high) for x, y in outline
    ]
    count = len(outline)
    faces: list[tuple[int, ...]] = [
        tuple(range(count)),
        tuple(range(count, count * 2))[::-1],
    ]
    for index in range(count):
        nxt = (index + 1) % count
        faces.append((index, nxt, nxt + count, index + count))
    obj = weighted_object(
        name,
        vertices,
        faces,
        "Mandible_L" if side < 0 else "Mandible_R",
        joint_material,
    )
    if lod0:
        bevel = obj.modifiers.new("Mandible bevel", "BEVEL")
        bevel.width = .006
        bevel.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=bevel.name)
    return obj


def dorsal_plate(
    name: str,
    center: Vector,
    radius_x: float,
    radius_y: float,
    height: float,
    shell_material,
) -> bpy.types.Object:
    rings = 3
    segments = 20
    vertices: list[tuple[float, float, float]] = [
        (center.x, center.y, center.z + height)
    ]
    for ring in range(1, rings + 1):
        distance = ring / rings
        for segment in range(segments):
            angle = math.tau * segment / segments
            falloff = 1 - distance * distance
            vertices.append(
                (
                    center.x + math.cos(angle) * radius_x * distance,
                    center.y + math.sin(angle) * radius_y * distance,
                    center.z + height * falloff,
                )
            )
    faces = []
    for segment in range(segments):
        nxt = (segment + 1) % segments
        faces.append((0, 1 + segment, 1 + nxt))
    for ring in range(1, rings):
        for segment in range(segments):
            nxt = (segment + 1) % segments
            a = 1 + (ring - 1) * segments + segment
            b = 1 + (ring - 1) * segments + nxt
            c = 1 + ring * segments + nxt
            d = 1 + ring * segments + segment
            faces.append((a, b, c, d))
    return weighted_object(name, vertices, faces, "Thorax", shell_material)


def create_detail_objects(
    caste: Caste,
    lod0: bool,
    shell_material,
    joint_material,
    eye_material,
) -> list[bpy.types.Object]:
    result = [
        compound_eye(
            "CompoundEye_L",
            -1,
            caste,
            3 if lod0 else 1,
            eye_material,
        ),
        compound_eye(
            "CompoundEye_R",
            1,
            caste,
            3 if lod0 else 1,
            eye_material,
        ),
        serrated_mandible(
            "SerratedMandible_L",
            -1,
            caste,
            joint_material,
            lod0,
        ),
        serrated_mandible(
            "SerratedMandible_R",
            1,
            caste,
            joint_material,
            lod0,
        ),
    ]
    if caste.dorsal_shield > 0 and lod0:
        result.append(
            dorsal_plate(
                "Dorsal pronotum shield",
                Vector((0, -.045, .202)),
                .14 * caste.thorax[0] * caste.dorsal_shield,
                .105 * caste.thorax[1],
                .025 * caste.dorsal_shield,
                shell_material,
            )
        )
    if caste.wing_scars and lod0:
        result.extend(
            (
                dorsal_plate(
                    "Left wing scar",
                    Vector((-.075, -.045, .222)),
                    .031,
                    .062,
                    .007,
                    joint_material,
                ),
                dorsal_plate(
                    "Right wing scar",
                    Vector((.075, -.045, .222)),
                    .031,
                    .062,
                    .007,
                    joint_material,
                ),
            )
        )
    return result


def apply_subdivision(obj: bpy.types.Object, levels: int) -> None:
    modifier = obj.modifiers.new(
        "Close camera repaired topology" if levels > 1 else "Distant repaired topology",
        "SUBSURF",
    )
    modifier.subdivision_type = "CATMULL_CLARK"
    modifier.levels = levels
    modifier.render_levels = levels
    bpy.context.view_layer.objects.active = obj
    modifier_index = len(obj.modifiers) - 1
    if modifier_index > 0:
        bpy.ops.object.modifier_move_up(modifier=modifier.name)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True


def join_lod(
    base: bpy.types.Object,
    details: list[bpy.types.Object],
    armature: bpy.types.Object,
    name: str,
) -> bpy.types.Object:
    bpy.ops.object.select_all(action="DESELECT")
    base.select_set(True)
    for detail in details:
        detail.select_set(True)
    bpy.context.view_layer.objects.active = base
    bpy.ops.object.join()
    base.name = name
    base.data.name = name + "_Mesh"
    base.parent = armature
    for modifier in list(base.modifiers):
        if modifier.type == "ARMATURE":
            modifier.object = armature
    if not any(modifier.type == "ARMATURE" for modifier in base.modifiers):
        modifier = base.modifiers.new("Anatomical armature", "ARMATURE")
        modifier.object = armature
    return base


def shift_to_ground(meshes: list[bpy.types.Object], armature: bpy.types.Object) -> None:
    minimum = min(
        vertex.co.z
        for obj in meshes
        for vertex in obj.data.vertices
    )
    offset = -.002 - minimum
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


def limit_weights(obj: bpy.types.Object, maximum: int = 4) -> None:
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="WEIGHT_PAINT")
    bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=maximum)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode="ALL", lock_active=False)
    bpy.ops.object.mode_set(mode="OBJECT")


def key_rotation(pose_bone, frame: int, rotation) -> None:
    pose_bone.rotation_mode = "XYZ"
    pose_bone.rotation_euler = rotation
    pose_bone.keyframe_insert(
        data_path="rotation_euler",
        frame=frame,
        group=pose_bone.name,
    )


def create_actions(armature: bpy.types.Object) -> None:
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)
    bpy.context.view_layer.objects.active = armature
    armature.animation_data_create()
    bones = armature.pose.bones
    tripod = [
        ("Front", "L", 0),
        ("Front", "R", math.pi),
        ("Middle", "L", math.pi),
        ("Middle", "R", 0),
        ("Rear", "L", 0),
        ("Rear", "R", math.pi),
    ]

    def new_action(name: str, end: int):
        action = bpy.data.actions.new(name)
        action.use_fake_user = True
        armature.animation_data.action = action
        for pose_bone in bones:
            pose_bone.rotation_mode = "XYZ"
            pose_bone.rotation_euler = (0, 0, 0)
            pose_bone.location = (0, 0, 0)
        bpy.context.scene.frame_start = 1
        bpy.context.scene.frame_end = end
        return action

    new_action("ANT_Idle", 60)
    for frame in (1, 16, 31, 46, 60):
        wave = math.sin((frame - 1) / 59 * math.tau)
        key_rotation(bones["Abdomen"], frame, (wave * .025, 0, wave * .012))
        key_rotation(bones["Head"], frame, (-wave * .012, 0, -wave * .015))
        for side_index, side in enumerate(("L", "R")):
            key_rotation(
                bones[f"Antenna_{side}_1"],
                frame,
                (wave * .08, (-1 if side == "L" else 1) * (.1 + wave * .08), 0),
            )

    for name, stride, lift, end in (
        ("ANT_Walk", .33, .24, 24),
        ("ANT_Run", .53, .36, 16),
    ):
        new_action(name, end)
        for frame in range(1, end + 1, max(1, end // 4)):
            normalized = (frame - 1) / max(1, end - 1)
            for pair, side, phase in tripod:
                cycle = math.sin(normalized * math.tau + phase)
                raised = max(0.0, cycle)
                sign = -1 if side == "L" else 1
                key_rotation(
                    bones[f"Leg_{side}_{pair}_Coxa"],
                    frame,
                    (-raised * lift, cycle * stride * sign, cycle * .08 * sign),
                )
                key_rotation(
                    bones[f"Leg_{side}_{pair}_Femur"],
                    frame,
                    (raised * lift * 1.15, -cycle * stride * .28 * sign, 0),
                )
                key_rotation(
                    bones[f"Leg_{side}_{pair}_Tibia"],
                    frame,
                    (-raised * lift * 1.35, cycle * stride * .18 * sign, 0),
                )
                key_rotation(
                    bones[f"Leg_{side}_{pair}_Tarsus"],
                    frame,
                    (raised * lift * .6, 0, -cycle * .05 * sign),
                )
            key_rotation(
                bones["Abdomen"],
                frame,
                (math.sin(normalized * math.tau * 2) * .025, 0, 0),
            )

    for name, end, amount in (
        ("ANT_StartMove", 12, .22),
        ("ANT_StopMove", 14, -.18),
    ):
        new_action(name, end)
        for frame, blend in ((1, 0), (end // 2, amount), (end, 0)):
            key_rotation(bones["Thorax"], frame, (blend, 0, 0))
            key_rotation(bones["Head"], frame, (-blend * .55, 0, 0))

    for name, direction in (("ANT_TurnLeft", -1), ("ANT_TurnRight", 1)):
        new_action(name, 20)
        for frame, curve in ((1, 0), (10, direction * .24), (20, 0)):
            key_rotation(bones["Head"], frame, (0, 0, curve))
            key_rotation(bones["Abdomen"], frame, (0, 0, -curve * .72))
            for pair in ("Front", "Rear"):
                for side in ("L", "R"):
                    key_rotation(
                        bones[f"Leg_{side}_{pair}_Coxa"],
                        frame,
                        (0, curve * (-1 if side == "L" else 1), 0),
                    )

    new_action("ANT_Attack", 22)
    for frame, bite in ((1, 0), (6, -.22), (9, .42), (13, -.05), (22, 0)):
        key_rotation(bones["Mandible_L"], frame, (0, 0, bite))
        key_rotation(bones["Mandible_R"], frame, (0, 0, -bite))
        key_rotation(bones["Head"], frame, (-abs(bite) * .32, 0, 0))

    new_action("ANT_Carry", 32)
    for frame, lift in ((1, .08), (16, .14), (32, .08)):
        key_rotation(bones["Head"], frame, (-lift, 0, 0))
        key_rotation(bones["Abdomen"], frame, (-lift * .6, 0, 0))

    new_action("ANT_Interact", 30)
    for frame, pitch in ((1, 0), (9, -.22), (18, .08), (30, 0)):
        key_rotation(bones["Head"], frame, (pitch, 0, 0))
        for side in ("L", "R"):
            sign = -1 if side == "L" else 1
            key_rotation(bones[f"Antenna_{side}_1"], frame, (pitch * -.8, sign * .18, 0))

    new_action("ANT_Climb", 24)
    for frame, reach in ((1, -.36), (12, .36), (24, -.36)):
        for side in ("L", "R"):
            sign = -1 if side == "L" else 1
            key_rotation(bones[f"Leg_{side}_Front_Coxa"], frame, (reach, sign * .1, 0))
            key_rotation(bones[f"Leg_{side}_Rear_Coxa"], frame, (-reach * .55, 0, 0))

    new_action("ANT_Stagger", 18)
    for frame, roll in ((1, 0), (5, .32), (10, -.22), (18, 0)):
        key_rotation(bones["Thorax"], frame, (0, roll * .3, roll))
        key_rotation(bones["Head"], frame, (-abs(roll) * .2, 0, -roll * .5))

    new_action("ANT_Death", 40)
    for frame, roll in ((1, 0), (12, .4), (25, 1.18), (40, 1.5)):
        key_rotation(bones["Thorax"], frame, (.12 * roll, 0, roll))
        for pair in ("Front", "Middle", "Rear"):
            for side in ("L", "R"):
                key_rotation(
                    bones[f"Leg_{side}_{pair}_Coxa"],
                    frame,
                    (-roll * .25, 0, (-1 if side == "L" else 1) * roll * .18),
                )

    armature.animation_data.action = None
    for pose_bone in bones:
        pose_bone.rotation_mode = "XYZ"
        pose_bone.rotation_euler = (0, 0, 0)
        pose_bone.location = (0, 0, 0)
        pose_bone.scale = (1, 1, 1)
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()


def export_variant(caste: Caste) -> None:
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    mesh = next(obj for obj in bpy.data.objects if obj.type == "MESH")
    armature = next(obj for obj in bpy.data.objects if obj.type == "ARMATURE")
    mesh.name = f"CanopyKin_{caste.key}_Source"
    armature.name = f"CanopyKin_{caste.key}_Armature"
    morph_base(mesh, caste)
    rename_rig(mesh, armature)

    shell_color = (
        (.2, .025, .008, 1)
        if caste.key == "Rival"
        else (.105, .018, .006, 1)
    )
    if caste.key == "Nurse":
        shell_color = (.19, .055, .018, 1)
    shell = material("AntShell", shell_color, .29)
    joint = material("AntJoint", (.018, .006, .002, 1), .39)
    eye = material("CompoundEye", (.003, .012, .008, 1), .08)
    mesh.data.materials.clear()
    mesh.data.materials.append(shell)

    lod0 = mesh
    lod1 = mesh.copy()
    lod1.data = mesh.data.copy()
    bpy.context.collection.objects.link(lod1)
    apply_subdivision(lod0, 3)
    apply_subdivision(lod1, 1)
    lod0 = join_lod(
        lod0,
        create_detail_objects(caste, True, shell, joint, eye),
        armature,
        f"CanopyKin_{caste.key}_LOD0",
    )
    lod1 = join_lod(
        lod1,
        create_detail_objects(caste, False, shell, joint, eye),
        armature,
        f"CanopyKin_{caste.key}_LOD1",
    )
    shift_to_ground([lod0, lod1], armature)
    limit_weights(lod0)
    limit_weights(lod1)
    create_actions(armature)

    lod0["lod_level"] = 0
    lod1["lod_level"] = 1
    lod0["caste"] = caste.key
    lod1["caste"] = caste.key

    FBX_DIRECTORY.mkdir(parents=True, exist_ok=True)
    BLEND_DIRECTORY.mkdir(parents=True, exist_ok=True)
    blend_path = BLEND_DIRECTORY / f"CanopyKinAnt_{caste.key}.blend"
    fbx_path = FBX_DIRECTORY / f"CanopyKinAnt_{caste.key}.fbx"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path), compress=True)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in (armature, lod0, lod1):
        obj.select_set(True)
    bpy.context.view_layer.objects.active = armature
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
        f"CANOPY_KIN_ANT_FAMILY_OK caste={caste.key} "
        f"fbx={fbx_path} triangles={triangles} actions={len(bpy.data.actions)}"
    )


def main() -> None:
    for caste in CASTES:
        export_variant(caste)


if __name__ == "__main__":
    main()
