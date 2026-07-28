"""Build the runtime rhinoceros beetle from the preserved CC0 scan."""

from __future__ import annotations

import importlib.util
import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector


PROJECT = Path(r"C:\codex-ant-project")
SOURCE = (
    PROJECT
    / "ArtSource/ThirdParty/Sketchfab/RhinocerosBeetle/Raw/source"
    / "IKINOSHIMA001-W07-1all-1.gltf"
)
FBX_PATH = PROJECT / "Assets/Resources/Models/Creatures/CanopyKinRhinocerosBeetle.fbx"
BLEND_PATH = PROJECT / "ArtSource/Creatures/CanopyKinRhinocerosBeetle.blend"

common_spec = importlib.util.spec_from_file_location(
    "creature_build_common", Path(__file__).with_name("build_production_spider.py")
)
common = importlib.util.module_from_spec(common_spec)
common_spec.loader.exec_module(common)


def prepare_source():
    bpy.ops.import_scene.gltf(filepath=str(SOURCE))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    beetle = max(meshes, key=lambda obj: len(obj.data.polygons))
    beetle.rotation_euler = (0, 0, 0)
    beetle.data.transform(beetle.matrix_world)
    beetle.parent = None
    beetle.matrix_world = Matrix.Identity(4)
    beetle.data.transform(Matrix.Rotation(math.radians(-90), 4, "Z"))
    for obj in list(bpy.context.scene.objects):
        if obj != beetle:
            bpy.data.objects.remove(obj, do_unlink=True)

    bpy.context.view_layer.objects.active = beetle
    beetle.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.delete_loose(use_verts=True, use_edges=True, use_faces=False)
    bpy.ops.object.mode_set(mode="OBJECT")

    coordinates = [vertex.co.copy() for vertex in beetle.data.vertices]
    low = Vector(tuple(common.quantile([point[axis] for point in coordinates], .001) for axis in range(3)))
    high = Vector(tuple(common.quantile([point[axis] for point in coordinates], .999) for axis in range(3)))
    margin = (high - low) * .08
    mesh = bmesh.new()
    mesh.from_mesh(beetle.data)
    outliers = [
        vertex
        for vertex in mesh.verts
        if any(vertex.co[axis] < low[axis] - margin[axis] or vertex.co[axis] > high[axis] + margin[axis] for axis in range(3))
    ]
    if outliers:
        bmesh.ops.delete(mesh, geom=outliers, context="VERTS")
    mesh.to_mesh(beetle.data)
    mesh.free()
    beetle.data.update()

    coordinates = [vertex.co.copy() for vertex in beetle.data.vertices]
    x_low = common.quantile([point.x for point in coordinates], .001)
    x_high = common.quantile([point.x for point in coordinates], .999)
    y_low = common.quantile([point.y for point in coordinates], .001)
    y_high = common.quantile([point.y for point in coordinates], .999)
    z_low = common.quantile([point.z for point in coordinates], .001)
    scale = 2.25 / max(.001, x_high - x_low)
    center = Vector(((x_low + x_high) * .5, (y_low + y_high) * .5, z_low))
    beetle.data.transform(
        Matrix.Translation(Vector((0, 0, .035)))
        @ Matrix.Scale(scale, 4)
        @ Matrix.Translation(-center)
    )
    beetle.name = "Beetle_LOD0"
    beetle.data.name = "RhinocerosBeetle_LOD0_Mesh"
    for polygon in beetle.data.polygons:
        polygon.use_smooth = True
    return beetle


def add_armature():
    data = bpy.data.armatures.new("CanopyKinBeetleArmature")
    armature = bpy.data.objects.new("CanopyKinBeetleArmature", data)
    bpy.context.collection.objects.link(armature)
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bones = {}
    segments = {}

    def bone(name, head, tail, parent=None):
        item = data.edit_bones.new(name)
        item.head = Vector(head)
        item.tail = Vector(tail)
        item.parent = bones.get(parent)
        bones[name] = item
        segments[name] = (Vector(head), Vector(tail))

    bone("Root", (0, 0, .35), (0, 0, .62))
    bone("Thorax", (0, -.08, .47), (0, .42, .53), "Root")
    bone("Abdomen", (0, -.02, .51), (0, -.86, .57), "Thorax")
    bone("Head", (0, .34, .48), (0, .76, .50), "Thorax")
    bone("Horn", (0, .63, .54), (0, 1.45, .83), "Head")

    pair_data = [
        ("Front", .30, .62, 1.02, 1.02),
        ("Middle", .03, .02, -.05, 1.14),
        ("Rear", -.25, -.48, -.92, 1.08),
    ]
    for side_name, sign in (("L", -1.0), ("R", 1.0)):
        for pair_name, hip_y, knee_y, foot_y, reach in pair_data:
            points = [
                Vector((sign * .20, hip_y, .45)),
                Vector((sign * .48, knee_y, .50)),
                Vector((sign * reach * .78, (knee_y + foot_y) * .5, .24)),
                Vector((sign * reach, foot_y, .045)),
            ]
            parent = "Thorax"
            for index, segment_name in enumerate(("Coxa", "Femur", "Tibia")):
                name = f"Leg_{side_name}_{pair_name}_{segment_name}"
                bone(name, points[index], points[index + 1], parent)
                parent = name
    bpy.ops.object.mode_set(mode="OBJECT")
    return armature, segments


def skin(obj, armature, segments):
    groups = {name: obj.vertex_groups.new(name=name) for name in segments}
    body_names = ("Abdomen", "Thorax", "Head", "Horn")
    left_names = [name for name in segments if name.startswith("Leg_L_")]
    right_names = [name for name in segments if name.startswith("Leg_R_")]
    assignments = {name: [] for name in segments}
    for vertex in obj.data.vertices:
        point = vertex.co
        if abs(point.x) < .36:
            candidates = body_names
        else:
            candidates = (left_names if point.x < 0 else right_names) + list(body_names)
        nearest = min(
            candidates,
            key=lambda name: common.point_segment_distance_squared(point, *segments[name]),
        )
        assignments[nearest].append(vertex.index)
    for name, indices in assignments.items():
        if indices:
            groups[name].add(indices, 1, "REPLACE")
    modifier = obj.modifiers.new("Anatomical beetle armature", "ARMATURE")
    modifier.object = armature
    obj.parent = armature


def create_actions(armature):
    armature.animation_data_create()
    bones = armature.pose.bones
    coxae = [name for name in bones.keys() if name.startswith("Leg_") and name.endswith("_Coxa")]

    def action(name, end):
        created = bpy.data.actions.new(name)
        created.use_fake_user = True
        created.frame_range = (1, end)
        armature.animation_data.action = created
        for pose_bone in bones:
            pose_bone.rotation_mode = "XYZ"
            pose_bone.rotation_euler = (0, 0, 0)

    action("BEETLE_Idle", 48)
    for frame in (1, 13, 25, 37, 48):
        breath = math.sin(frame / 48 * math.tau)
        common.key_rotation(bones["Abdomen"], frame, (breath * .025, 0, 0))
        common.key_rotation(bones["Horn"], frame, (-breath * .018, 0, 0))

    for name, stride, lift, end in (("BEETLE_Walk", .25, .16, 28), ("BEETLE_Run", .42, .25, 18)):
        action(name, end)
        for frame_index, frame in enumerate((1, end // 2 + 1, end)):
            direction = -1 if frame_index == 1 else 1
            if frame_index == 2:
                direction = -1
            for index, coxa_name in enumerate(coxae):
                phase = 1 if index % 2 == 0 else -1
                swing = direction * phase * stride
                common.key_rotation(bones[coxa_name], frame, (swing * .2, swing, phase * lift))
                common.key_rotation(
                    bones[coxa_name.replace("_Coxa", "_Femur")],
                    frame,
                    (-swing * .2, -swing * .45, -phase * lift * .7),
                )
                common.key_rotation(
                    bones[coxa_name.replace("_Coxa", "_Tibia")],
                    frame,
                    (swing * .16, swing * .22, phase * lift * .45),
                )

    action("BEETLE_ChargeTelegraph", 22)
    for frame, lower in ((1, 0), (12, .38), (22, .26)):
        common.key_rotation(bones["Head"], frame, (lower, 0, 0))
        common.key_rotation(bones["Horn"], frame, (lower * .55, 0, 0))

    action("BEETLE_Charge", 18)
    for frame, lunge in ((1, 0), (6, -.42), (11, .12), (18, 0)):
        common.key_rotation(bones["Thorax"], frame, (lunge, 0, 0))
        common.key_rotation(bones["Head"], frame, (lunge * .6, 0, 0))

    action("BEETLE_Stagger", 18)
    for frame, roll in ((1, 0), (5, .28), (10, -.20), (18, 0)):
        common.key_rotation(bones["Root"], frame, (0, roll * .25, roll))

    action("BEETLE_Death", 40)
    for frame, roll in ((1, 0), (14, .40), (28, 1.18), (40, 1.52)):
        common.key_rotation(bones["Root"], frame, (roll * .12, 0, roll))
        for name in coxae:
            common.key_rotation(
                bones[name],
                frame,
                (roll * .24, 0, (-.24 if "_L_" in name else .24) * roll),
            )

    action("BEETLE_Retreat", 26)
    for frame_index, frame in enumerate((1, 14, 26)):
        direction = -1 if frame_index != 1 else 1
        for index, name in enumerate(coxae):
            phase = 1 if index % 2 == 0 else -1
            common.key_rotation(bones[name], frame, (0, direction * phase * .30, phase * .16))
    armature.animation_data.action = None


def export():
    common.clean_scene()
    bpy.context.scene.unit_settings.system = "METRIC"
    lod0 = prepare_source()
    lod0_triangles = common.decimate(lod0, 92000)
    lod1 = lod0.copy()
    lod1.data = lod0.data.copy()
    bpy.context.collection.objects.link(lod1)
    lod1.name = "Beetle_LOD1"
    lod1.data.name = "RhinocerosBeetle_LOD1_Mesh"
    lod1_triangles = common.decimate(lod1, 24000)

    armature, segments = add_armature()
    skin(lod0, armature, segments)
    skin(lod1, armature, segments)
    create_actions(armature)
    armature["source_url"] = (
        "https://sketchfab.com/3d-models/"
        "cc0-japanese-rhinoceros-beetle-6395f798f7d243e19975a55b76608a8b"
    )
    armature["source_creator"] = "ffish.asia / floraZia.com"
    armature["source_license"] = "CC0 1.0 Public Domain"

    FBX_PATH.parent.mkdir(parents=True, exist_ok=True)
    BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    bpy.ops.object.select_all(action="DESELECT")
    for obj in (armature, lod0, lod1):
        obj.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_scale_options="FBX_SCALE_UNITS",
        apply_unit_scale=True,
        bake_space_transform=False,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,
        path_mode="AUTO",
        embed_textures=False,
    )
    print(
        "CANOPY_KIN_BEETLE_FBX_OK "
        f"path={FBX_PATH} lod0Triangles={lod0_triangles} "
        f"lod1Triangles={lod1_triangles} bones={len(armature.data.bones)} "
        f"actions={len(bpy.data.actions)}"
    )


if __name__ == "__main__":
    export()
