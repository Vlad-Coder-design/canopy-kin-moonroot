"""Create the runtime Fishing Spider asset from the preserved CC0 scan.

The source remains untouched under ArtSource.  This pipeline removes scan
outliers, creates two useful LODs, adds an anatomical animation rig and exports
one Unity FBX.  The game assigns the retained high-resolution scan texture at
runtime so Windows and WebGL can use independent import overrides.
"""

from __future__ import annotations

import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector


PROJECT = Path(r"C:\codex-ant-project")
SOURCE = (
    PROJECT
    / "ArtSource/ThirdParty/Sketchfab/FishingSpider/Raw/source"
    / "OKINAWA193-W01-1all-3.gltf"
)
FBX_PATH = PROJECT / "Assets/Resources/Models/Creatures/CanopyKinFishingSpider.fbx"
BLEND_PATH = PROJECT / "ArtSource/Creatures/CanopyKinFishingSpider.blend"


def clean_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def quantile(values, percentile):
    ordered = sorted(values)
    return ordered[int((len(ordered) - 1) * percentile)]


def prepare_source():
    bpy.ops.import_scene.gltf(filepath=str(SOURCE))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    spider = max(meshes, key=lambda obj: len(obj.data.polygons))

    # The scan arrives below a nested transform hierarchy.  The inspected pose
    # deliberately removes the extra local rotation, then bakes world space.
    spider.rotation_euler = (0, 0, 0)
    spider.data.transform(spider.matrix_world)
    spider.parent = None
    spider.matrix_world = Matrix.Identity(4)
    spider.data.transform(Matrix.Rotation(math.radians(-90), 4, "Z"))

    for obj in list(bpy.context.scene.objects):
        if obj != spider:
            bpy.data.objects.remove(obj, do_unlink=True)

    # Delete unreferenced scan vertices first.
    bpy.context.view_layer.objects.active = spider
    spider.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.delete_loose(use_verts=True, use_edges=True, use_faces=False)
    bpy.ops.object.mode_set(mode="OBJECT")

    # Photogrammetry occasionally emits a few long spikes outside the visible
    # animal.  Clip only statistical outliers, retaining more than 99.8% of
    # sampled surface vertices and every close-view anatomical feature.
    coordinates = [vertex.co.copy() for vertex in spider.data.vertices]
    low = Vector(tuple(quantile([point[axis] for point in coordinates], .001) for axis in range(3)))
    high = Vector(tuple(quantile([point[axis] for point in coordinates], .999) for axis in range(3)))
    margin = (high - low) * .08
    mesh = bmesh.new()
    mesh.from_mesh(spider.data)
    outliers = [
        vertex
        for vertex in mesh.verts
        if any(vertex.co[axis] < low[axis] - margin[axis] or vertex.co[axis] > high[axis] + margin[axis] for axis in range(3))
    ]
    if outliers:
        bmesh.ops.delete(mesh, geom=outliers, context="VERTS")
    mesh.to_mesh(spider.data)
    mesh.free()
    spider.data.update()

    coordinates = [vertex.co.copy() for vertex in spider.data.vertices]
    x_low = quantile([point.x for point in coordinates], .001)
    x_high = quantile([point.x for point in coordinates], .999)
    y_low = quantile([point.y for point in coordinates], .001)
    y_high = quantile([point.y for point in coordinates], .999)
    z_low = quantile([point.z for point in coordinates], .001)
    scale = 2.65 / max(.001, x_high - x_low)
    center = Vector(((x_low + x_high) * .5, (y_low + y_high) * .5, z_low))
    transform = Matrix.Translation(Vector((0, 0, .035))) @ Matrix.Scale(scale, 4) @ Matrix.Translation(-center)
    spider.data.transform(transform)
    spider.name = "Spider_LOD0"
    spider.data.name = "FishingSpider_LOD0_Mesh"
    for polygon in spider.data.polygons:
        polygon.use_smooth = True
    return spider


def decimate(obj, target_triangles):
    current = max(1, sum(max(1, len(poly.vertices) - 2) for poly in obj.data.polygons))
    if current <= target_triangles:
        return current
    modifier = obj.modifiers.new("Quality-preserving game topology", "DECIMATE")
    modifier.decimate_type = "COLLAPSE"
    modifier.ratio = target_triangles / current
    modifier.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    return sum(max(1, len(poly.vertices) - 2) for poly in obj.data.polygons)


def add_armature():
    data = bpy.data.armatures.new("CanopyKinSpiderArmature")
    armature = bpy.data.objects.new("CanopyKinSpiderArmature", data)
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

    bone("Root", (0, 0, .38), (0, 0, .68))
    bone("Thorax", (0, -.12, .52), (0, .36, .55), "Root")
    bone("Abdomen", (0, -.04, .53), (0, -.82, .60), "Thorax")
    bone("Head", (0, .28, .50), (0, .76, .45), "Thorax")

    pair_data = [
        ("Front", .28, .62, 1.12, 1.24),
        ("FrontMid", .12, .30, .52, 1.34),
        ("RearMid", -.04, -.28, -.52, 1.28),
        ("Rear", -.20, -.58, -1.10, 1.20),
    ]
    for side_name, sign in (("L", -1.0), ("R", 1.0)):
        for pair_name, hip_y, knee_y, foot_y, reach in pair_data:
            points = [
                Vector((sign * .19, hip_y, .52)),
                Vector((sign * .52, knee_y, .61)),
                Vector((sign * reach * .78, (knee_y + foot_y) * .5, .30)),
                Vector((sign * reach, foot_y, .055)),
            ]
            parent = "Thorax"
            for index, segment_name in enumerate(("Coxa", "Femur", "Tibia")):
                name = f"Leg_{side_name}_{pair_name}_{segment_name}"
                bone(name, points[index], points[index + 1], parent)
                parent = name

    bpy.ops.object.mode_set(mode="OBJECT")
    return armature, segments


def point_segment_distance_squared(point, start, end):
    delta = end - start
    length_squared = delta.length_squared
    if length_squared <= 1e-8:
        return (point - start).length_squared
    factor = max(0.0, min(1.0, (point - start).dot(delta) / length_squared))
    return (point - (start + delta * factor)).length_squared


def skin_mesh(obj, armature, segments):
    groups = {name: obj.vertex_groups.new(name=name) for name in segments}
    body_names = ("Abdomen", "Thorax", "Head")
    left_leg_names = [name for name in segments if name.startswith("Leg_L_")]
    right_leg_names = [name for name in segments if name.startswith("Leg_R_")]

    assignments = {name: [] for name in segments}
    for vertex in obj.data.vertices:
        point = vertex.co
        if abs(point.x) < .34:
            candidates = body_names
        else:
            candidates = (left_leg_names if point.x < 0 else right_leg_names) + list(body_names)
        nearest = min(candidates, key=lambda name: point_segment_distance_squared(point, *segments[name]))
        assignments[nearest].append(vertex.index)

    for name, indices in assignments.items():
        if indices:
            groups[name].add(indices, 1.0, "REPLACE")

    modifier = obj.modifiers.new("Anatomical spider armature", "ARMATURE")
    modifier.object = armature
    obj.parent = armature


def key_rotation(bone, frame, rotation):
    bone.rotation_mode = "XYZ"
    bone.rotation_euler = rotation
    bone.keyframe_insert(data_path="rotation_euler", frame=frame, group=bone.name)


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
        return created

    action("SPIDER_Idle", 48)
    for frame in (1, 13, 25, 37, 48):
        breath = math.sin(frame / 48 * math.tau)
        key_rotation(bones["Abdomen"], frame, (breath * .035, 0, breath * .012))
        key_rotation(bones["Head"], frame, (-breath * .018, 0, 0))
        for index, name in enumerate(coxae):
            key_rotation(bones[name], frame, (0, 0, math.sin(frame * .12 + index) * .018))

    for name, stride, lift, end in (("SPIDER_Walk", .30, .18, 24), ("SPIDER_Run", .48, .28, 16)):
        action(name, end)
        for frame_index, frame in enumerate((1, end // 2 + 1, end)):
            direction = -1 if frame_index == 1 else 1
            if frame_index == 2:
                direction = -1
            for index, coxa_name in enumerate(coxae):
                phase = 1 if index % 2 == 0 else -1
                swing = direction * phase * stride
                key_rotation(bones[coxa_name], frame, (swing * .32, swing, phase * lift))
                femur = coxa_name.replace("_Coxa", "_Femur")
                tibia = coxa_name.replace("_Coxa", "_Tibia")
                key_rotation(bones[femur], frame, (-swing * .26, -swing * .42, -phase * lift * .7))
                key_rotation(bones[tibia], frame, (swing * .18, swing * .24, phase * lift * .48))

    action("SPIDER_Telegraph", 20)
    for frame, raise_value in ((1, 0), (10, -.48), (20, -.30)):
        key_rotation(bones["Head"], frame, (raise_value, 0, 0))
        for name in coxae:
            if "_Front" in name:
                key_rotation(bones[name], frame, (-raise_value * .85, 0, .12 if "_L_" in name else -.12))

    action("SPIDER_Attack", 18)
    for frame, lunge in ((1, 0), (7, .55), (11, -.16), (18, 0)):
        key_rotation(bones["Head"], frame, (-lunge, 0, 0))
        key_rotation(bones["Thorax"], frame, (-lunge * .18, 0, 0))

    action("SPIDER_Stagger", 18)
    for frame, roll in ((1, 0), (5, .32), (10, -.22), (18, 0)):
        key_rotation(bones["Root"], frame, (0, roll * .28, roll))

    action("SPIDER_Death", 40)
    for frame, roll in ((1, 0), (14, .48), (28, 1.22), (40, 1.52)):
        key_rotation(bones["Root"], frame, (roll * .18, 0, roll))
        for name in coxae:
            key_rotation(bones[name], frame, (roll * .35, 0, (-.28 if "_L_" in name else .28) * roll))

    action("SPIDER_Retreat", 24)
    for frame_index, frame in enumerate((1, 13, 24)):
        direction = -1 if frame_index != 1 else 1
        for index, name in enumerate(coxae):
            phase = 1 if index % 2 == 0 else -1
            key_rotation(bones[name], frame, (0, direction * phase * .36, phase * .20))
    armature.animation_data.action = None


def export():
    clean_scene()
    bpy.context.scene.unit_settings.system = "METRIC"
    source = prepare_source()
    lod0_triangles = decimate(source, 112000)

    lod1 = source.copy()
    lod1.data = source.data.copy()
    bpy.context.collection.objects.link(lod1)
    lod1.name = "Spider_LOD1"
    lod1.data.name = "FishingSpider_LOD1_Mesh"
    lod1_triangles = decimate(lod1, 30000)

    armature, segments = add_armature()
    skin_mesh(source, armature, segments)
    skin_mesh(lod1, armature, segments)
    create_actions(armature)

    armature["source_url"] = (
        "https://sketchfab.com/3d-models/"
        "cc0-fishing-spider-dolomedes-orion-320e77ebe2e049dcbb759dd79ee03a8c"
    )
    armature["source_creator"] = "ffish.asia / floraZia.com"
    armature["source_license"] = "CC0 1.0 Public Domain"

    FBX_PATH.parent.mkdir(parents=True, exist_ok=True)
    BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))

    bpy.ops.object.select_all(action="DESELECT")
    for obj in (armature, source, lod1):
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
        "CANOPY_KIN_SPIDER_FBX_OK "
        f"path={FBX_PATH} lod0Triangles={lod0_triangles} "
        f"lod1Triangles={lod1_triangles} bones={len(armature.data.bones)} "
        f"actions={len(bpy.data.actions)}"
    )


if __name__ == "__main__":
    export()
