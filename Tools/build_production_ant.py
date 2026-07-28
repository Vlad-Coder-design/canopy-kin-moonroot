"""Build the original production ant as a rigged Blender asset and Unity FBX.

The resulting model is a single skinned mesh, not a hierarchy of Unity
primitives. Rigid exoskeleton islands are weighted to an anatomical armature,
which is appropriate for insect joints and keeps the renderer count low.
"""

from __future__ import annotations

import math
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


PROJECT = Path(r"C:\codex-ant-project")
FBX_PATH = PROJECT / "Assets/Resources/Models/Ant/CanopyKinProductionAnt.fbx"
BLEND_PATH = PROJECT / "ArtSource/Ant/CanopyKinProductionAnt.blend"


def clean_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in (bpy.data.meshes, bpy.data.curves, bpy.data.armatures, bpy.data.materials):
        for item in list(collection):
            if item.users == 0:
                collection.remove(item)


def material(name: str, color: tuple[float, float, float, float], metallic: float, roughness: float):
    result = bpy.data.materials.new(name)
    result.diffuse_color = color
    result.metallic = metallic
    result.roughness = roughness
    return result


def finalize_mesh(name: str, vertices, faces, uvs=None, mat=None):
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update(calc_edges=True)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    if mat is not None:
        obj.data.materials.append(mat)
    if uvs is not None:
        uv_layer = mesh.uv_layers.new(name="UVMap")
        for polygon in mesh.polygons:
            for loop_index in polygon.loop_indices:
                uv_layer.data[loop_index].uv = uvs[mesh.loops[loop_index].vertex_index]
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    return obj


def ellipsoid(
    name: str,
    center: Vector,
    scale: Vector,
    mat,
    kind: str,
    segments: int = 64,
    rings: int = 40,
):
    vertices = []
    uvs = []
    for ring in range(rings + 1):
        v = ring / rings
        phi = math.pi * v
        axial = math.cos(phi)
        radial = math.sin(phi)
        for segment in range(segments + 1):
            u = segment / segments
            theta = math.tau * u
            local = Vector((math.cos(theta) * radial, axial, math.sin(theta) * radial))

            if kind == "abdomen":
                band = 1.0 + 0.035 * math.sin((axial + 1.0) * math.pi * 5.5)
                taper = 0.82 + (axial + 1.0) * 0.08
                local.x *= band * taper
                local.z *= band
                local.z -= max(0.0, -local.z) * 0.10
            elif kind == "thorax":
                lobe = 1.0 + 0.055 * math.cos(axial * math.pi * 3.0)
                local.x *= lobe
                local.z *= 1.0 + 0.035 * math.sin(theta * 3.0)
            elif kind == "head":
                front = max(0.0, axial)
                local.x *= 1.0 + front * 0.10
                local.z *= 0.96 + front * 0.09
            elif kind == "eye":
                local *= 1.0 + 0.025 * math.sin(theta * 12.0) * math.sin(phi * 9.0)

            point = center + Vector((local.x * scale.x, local.y * scale.y, local.z * scale.z))
            vertices.append(tuple(point))
            uvs.append((u, 1.0 - v))

    faces = []
    row = segments + 1
    for ring in range(rings):
        for segment in range(segments):
            a = ring * row + segment
            b = a + 1
            c = a + row
            d = c + 1
            faces.append((a, c, d, b))
    return finalize_mesh(name, vertices, faces, uvs, mat)


def tube(name: str, points: list[Vector], radii: list[float], mat, sides: int = 14):
    vertices = []
    uvs = []
    for point_index, point in enumerate(points):
        if point_index == 0:
            tangent = (points[1] - point).normalized()
        elif point_index == len(points) - 1:
            tangent = (point - points[point_index - 1]).normalized()
        else:
            tangent = (points[point_index + 1] - points[point_index - 1]).normalized()
        reference = Vector((0, 0, 1))
        if abs(tangent.dot(reference)) > 0.92:
            reference = Vector((0, 1, 0))
        normal = tangent.cross(reference).normalized()
        bitangent = tangent.cross(normal).normalized()
        for side in range(sides + 1):
            u = side / sides
            angle = math.tau * u
            offset = (normal * math.cos(angle) + bitangent * math.sin(angle)) * radii[point_index]
            vertices.append(tuple(point + offset))
            uvs.append((u, point_index / max(1, len(points) - 1)))

    faces = []
    row = sides + 1
    for point_index in range(len(points) - 1):
        for side in range(sides):
            a = point_index * row + side
            b = a + 1
            c = a + row
            d = c + 1
            faces.append((a, c, d, b))
    return finalize_mesh(name, vertices, faces, uvs, mat)


def add_armature():
    armature_data = bpy.data.armatures.new("CanopyKinAntArmature")
    armature = bpy.data.objects.new("CanopyKinAntArmature", armature_data)
    bpy.context.collection.objects.link(armature)
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")

    bones = {}

    def bone(name: str, head: Vector, tail: Vector, parent: str | None = None):
        edit_bone = armature_data.edit_bones.new(name)
        edit_bone.head = head
        edit_bone.tail = tail if (tail - head).length > 0.01 else head + Vector((0, 0.05, 0))
        if parent:
            edit_bone.parent = bones[parent]
        bones[name] = edit_bone

    bone("Root", Vector((0, 0, 0.12)), Vector((0, 0, 0.34)))
    bone("Thorax", Vector((0, -0.10, 0.31)), Vector((0, 0.28, 0.34)), "Root")
    bone("Abdomen", Vector((0, -0.12, 0.31)), Vector((0, -0.62, 0.36)), "Thorax")
    bone("Head", Vector((0, 0.28, 0.34)), Vector((0, 0.68, 0.35)), "Thorax")
    bone("Mandible_L", Vector((-0.10, 0.82, 0.30)), Vector((-0.20, 1.10, 0.27)), "Head")
    bone("Mandible_R", Vector((0.10, 0.82, 0.30)), Vector((0.20, 1.10, 0.27)), "Head")

    antenna_points = {
        "L": [Vector((-0.16, 0.79, 0.49)), Vector((-0.29, 1.02, 0.66)), Vector((-0.40, 1.25, 0.63)), Vector((-0.53, 1.45, 0.56))],
        "R": [Vector((0.16, 0.79, 0.49)), Vector((0.29, 1.02, 0.66)), Vector((0.40, 1.25, 0.63)), Vector((0.53, 1.45, 0.56))],
    }
    for side, points in antenna_points.items():
        parent = "Head"
        for index in range(3):
            name = f"Antenna_{side}_{index + 1}"
            bone(name, points[index], points[index + 1], parent)
            parent = name

    leg_points = {}
    pair_data = [
        ("Front", 0.23, 0.32, 0.54),
        ("Middle", 0.01, 0.02, 0.06),
        ("Rear", -0.22, -0.28, -0.52),
    ]
    for pair_name, anchor_y, knee_y, foot_y in pair_data:
        for side_name, sign in (("L", -1.0), ("R", 1.0)):
            points = [
                Vector((sign * 0.19, anchor_y, 0.31)),
                Vector((sign * 0.38, anchor_y + 0.01, 0.25)),
                Vector((sign * 0.75, knee_y, 0.20)),
                Vector((sign * 1.06, foot_y * 0.78, 0.09)),
                Vector((sign * 1.27, foot_y, 0.025)),
            ]
            leg_points[(pair_name, side_name)] = points
            parent = "Thorax"
            for index, segment_name in enumerate(("Coxa", "Femur", "Tibia", "Tarsus")):
                name = f"Leg_{side_name}_{pair_name}_{segment_name}"
                bone(name, points[index], points[index + 1], parent)
                parent = name

    bpy.ops.object.mode_set(mode="OBJECT")
    return armature, leg_points, antenna_points


def assign_bone(obj, bone_name: str):
    group = obj.vertex_groups.new(name=bone_name)
    group.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    obj["bone_name"] = bone_name
    return obj


def create_model(armature, leg_points, antenna_points):
    shell = material("AntShell", (0.12, 0.025, 0.009, 1), 0.05, 0.24)
    joint = material("AntJoint", (0.025, 0.009, 0.004, 1), 0.0, 0.34)
    eye = material("CompoundEye", (0.006, 0.018, 0.013, 1), 0.18, 0.08)
    pieces = []

    pieces.append(assign_bone(ellipsoid("Abdomen_Segmented", Vector((0, -0.55, 0.39)), Vector((0.42, 0.67, 0.34)), shell, "abdomen"), "Abdomen"))
    pieces.append(assign_bone(ellipsoid("Thorax_Armoured", Vector((0, 0.03, 0.38)), Vector((0.34, 0.39, 0.31)), shell, "thorax"), "Thorax"))
    pieces.append(assign_bone(ellipsoid("Head_Anatomical", Vector((0, 0.57, 0.39)), Vector((0.38, 0.36, 0.31)), shell, "head"), "Head"))
    pieces.append(assign_bone(ellipsoid("Clypeus_FacePlate", Vector((0, 0.84, 0.36)), Vector((0.30, 0.12, 0.20)), shell, "head", 48, 28), "Head"))
    pieces.append(assign_bone(ellipsoid("Petiole_Node", Vector((0, -0.21, 0.36)), Vector((0.14, 0.18, 0.15)), joint, "thorax", 40, 24), "Thorax"))

    for side_name, sign in (("L", -1.0), ("R", 1.0)):
        pieces.append(assign_bone(
            ellipsoid(
                f"CompoundEye_{side_name}",
                Vector((sign * 0.31, 0.66, 0.45)),
                Vector((0.10, 0.16, 0.15)),
                eye,
                "eye",
                40,
                26,
            ),
            "Head",
        ))

        jaw_points = [
            Vector((sign * 0.10, 0.83, 0.30)),
            Vector((sign * 0.20, 1.02, 0.25)),
            Vector((sign * 0.28, 1.19, 0.20)),
            Vector((sign * 0.18, 1.31, 0.18)),
        ]
        jaw_name = f"Mandible_{side_name}"
        pieces.append(assign_bone(tube(f"Mandible_Serrated_{side_name}", jaw_points, [0.075, 0.067, 0.052, 0.018], joint, 16), jaw_name))
        for tooth_index in range(3):
            base = Vector((sign * (0.18 + tooth_index * 0.035), 1.04 + tooth_index * 0.075, 0.22))
            tip = base + Vector((-sign * 0.07, 0.04, -0.035))
            pieces.append(assign_bone(tube(f"MandibleTooth_{side_name}_{tooth_index}", [base, tip], [0.025, 0.004], joint, 10), jaw_name))

        points = antenna_points[side_name]
        for index in range(3):
            bone_name = f"Antenna_{side_name}_{index + 1}"
            pieces.append(assign_bone(
                tube(
                    f"Antenna_{side_name}_Segment_{index + 1}",
                    [points[index], points[index + 1]],
                    [0.026 - index * 0.004, 0.020 - index * 0.004],
                    joint,
                    12,
                ),
                bone_name,
            ))

    segment_names = ("Coxa", "Femur", "Tibia", "Tarsus")
    segment_radii = ((0.070, 0.060), (0.065, 0.048), (0.050, 0.031), (0.030, 0.010))
    for (pair_name, side_name), points in leg_points.items():
        for index, segment_name in enumerate(segment_names):
            bone_name = f"Leg_{side_name}_{pair_name}_{segment_name}"
            pieces.append(assign_bone(
                tube(
                    bone_name + "_Exoskeleton",
                    [points[index], Vector.lerp(points[index], points[index + 1], 0.52), points[index + 1]],
                    [segment_radii[index][0], segment_radii[index][0] * 0.92, segment_radii[index][1]],
                    shell if index in (1, 2) else joint,
                    14,
                ),
                bone_name,
            ))
            if index < 3:
                pieces.append(assign_bone(
                    ellipsoid(
                        bone_name + "_Joint",
                        points[index + 1],
                        Vector((segment_radii[index][1] * 1.15,) * 3),
                        joint,
                        "eye",
                        24,
                        14,
                    ),
                    bone_name,
                ))

        tibia_bone = f"Leg_{side_name}_{pair_name}_Tibia"
        sign = -1.0 if side_name == "L" else 1.0
        for spine_index in range(4):
            base = Vector.lerp(points[2], points[3], 0.22 + spine_index * 0.18)
            tip = base + Vector((sign * 0.045, 0.015, 0.055))
            pieces.append(assign_bone(
                tube(f"{tibia_bone}_SensorySpine_{spine_index}", [base, tip], [0.010, 0.0018], joint, 8),
                tibia_bone,
            ))

    for seam_index in range(5):
        y = -0.25 - seam_index * 0.145
        radius_x = 0.39 * (1.0 - seam_index * 0.035)
        points = []
        for segment in range(33):
            angle = math.tau * segment / 32
            points.append(Vector((math.cos(angle) * radius_x, y, 0.39 + math.sin(angle) * 0.315)))
        pieces.append(assign_bone(
            tube(f"Abdomen_PlateSeam_{seam_index}", points, [0.008] * len(points), joint, 8),
            "Abdomen",
        ))

    bpy.ops.object.select_all(action="DESELECT")
    for obj in pieces:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = pieces[0]
    bpy.ops.object.join()
    mesh = bpy.context.active_object
    mesh.name = "CanopyKin_Ant_SkinnedMesh"
    mesh.data.name = "CanopyKin_Ant_ProductionMesh"
    modifier = mesh.modifiers.new("AnatomicalArmature", "ARMATURE")
    modifier.object = armature
    mesh.parent = armature

    decimate = mesh.modifiers.new("WebSafeTopology", "DECIMATE")
    decimate.decimate_type = "DISSOLVE"
    decimate.angle_limit = math.radians(2.0)

    return mesh, (shell, joint, eye)


def key_rotation(pose_bone, frame: int, rotation):
    pose_bone.rotation_mode = "XYZ"
    pose_bone.rotation_euler = rotation
    pose_bone.keyframe_insert(data_path="rotation_euler", frame=frame, group=pose_bone.name)


def create_actions(armature):
    bpy.context.view_layer.objects.active = armature
    armature.animation_data_create()
    bones = armature.pose.bones
    leg_names = [name for name in bones.keys() if name.startswith("Leg_") and name.endswith("_Coxa")]
    antenna_names = [name for name in bones.keys() if name.startswith("Antenna_") and name.endswith("_1")]

    def new_action(name: str, end: int = 24):
        action = bpy.data.actions.new(name)
        action.use_fake_user = True
        action.frame_range = (1, end)
        armature.animation_data.action = action
        for pose_bone in bones:
            pose_bone.rotation_mode = "XYZ"
            pose_bone.rotation_euler = (0, 0, 0)
        return action

    new_action("ANT_Idle", 48)
    for frame in (1, 13, 25, 37, 48):
        breath = math.sin(frame / 48 * math.tau) * 0.025
        key_rotation(bones["Abdomen"], frame, (breath, 0, 0))
        key_rotation(bones["Head"], frame, (-breath * 0.5, 0, 0))
        for index, name in enumerate(antenna_names):
            key_rotation(bones[name], frame, (0, math.sin(frame * 0.17 + index) * 0.12, (-1 if index == 0 else 1) * 0.08))

    for action_name, stride, lift, end in (("ANT_Walk", 0.34, 0.22, 24), ("ANT_Run", 0.52, 0.34, 16)):
        new_action(action_name, end)
        frames = (1, end // 2 + 1, end)
        for frame_index, frame in enumerate(frames):
            for leg_index, coxa_name in enumerate(leg_names):
                phase = 1 if leg_index % 2 == 0 else -1
                value = phase * (stride if frame_index != 1 else -stride)
                if frame_index == 2:
                    value *= -1
                key_rotation(bones[coxa_name], frame, (0, value, phase * lift))
                femur_name = coxa_name.replace("_Coxa", "_Femur")
                tibia_name = coxa_name.replace("_Coxa", "_Tibia")
                key_rotation(bones[femur_name], frame, (0, -value * 0.45, -phase * lift * 0.7))
                key_rotation(bones[tibia_name], frame, (0, value * 0.25, phase * lift * 0.45))
            key_rotation(bones["Abdomen"], frame, (0.03 * math.sin(frame_index * math.pi), 0, 0))

    new_action("ANT_Turn", 24)
    for frame, yaw in ((1, -0.18), (12, 0.18), (24, -0.18)):
        key_rotation(bones["Head"], frame, (0, 0, yaw))
        key_rotation(bones["Abdomen"], frame, (0, 0, -yaw * 0.7))

    new_action("ANT_Attack", 18)
    for frame, bite in ((1, 0.0), (6, 0.55), (10, -0.08), (18, 0.0)):
        key_rotation(bones["Mandible_L"], frame, (0, 0, bite))
        key_rotation(bones["Mandible_R"], frame, (0, 0, -bite))
        key_rotation(bones["Head"], frame, (-abs(bite) * 0.28, 0, 0))

    new_action("ANT_Carry", 36)
    for frame, raise_value in ((1, -0.10), (18, -0.18), (36, -0.10)):
        key_rotation(bones["Abdomen"], frame, (raise_value, 0, 0))
        key_rotation(bones["Head"], frame, (0.12, 0, 0))

    new_action("ANT_Climb", 24)
    for frame, reach in ((1, -0.35), (12, 0.35), (24, -0.35)):
        for name in leg_names:
            if "_Front_" in name:
                key_rotation(bones[name], frame, (reach, 0.1 if "_L_" in name else -0.1, 0))

    new_action("ANT_Stagger", 18)
    for frame, roll in ((1, 0), (5, 0.30), (10, -0.22), (18, 0)):
        key_rotation(bones["Root"], frame, (0, roll * 0.3, roll))

    new_action("ANT_Death", 36)
    for frame, roll in ((1, 0), (12, 0.45), (24, 1.2), (36, 1.48)):
        key_rotation(bones["Root"], frame, (0.2 * roll, 0, roll))

    armature.animation_data.action = None


def export(mesh, armature):
    FBX_PATH.parent.mkdir(parents=True, exist_ok=True)
    BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))

    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    mesh.select_set(True)
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
        use_armature_deform_only=False,
        bake_anim=True,
        bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,
        path_mode="AUTO",
        embed_textures=False,
    )


def main():
    clean_scene()
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    armature, leg_points, antenna_points = add_armature()
    mesh, _ = create_model(armature, leg_points, antenna_points)
    create_actions(armature)
    export(mesh, armature)
    print(f"CANOPY_KIN_ANT_FBX_OK path={FBX_PATH} vertices={len(mesh.data.vertices)} polygons={len(mesh.data.polygons)} actions={len(bpy.data.actions)}")


if __name__ == "__main__":
    main()
