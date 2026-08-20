"""Build and render the separate maximum-quality Formica rufa player prototype.

The original CC BY worker-ant file remains untouched.  This tool converts its
segmented exoskeleton and dense authored action curves to a clean Unity rig,
adds the Formica one-node petiole, paired pretarsal claws and macro setae,
configures project-owned 4K PBR cuticle materials, preserves a subdivided bake
source, exports the skinned runtime FBX, and renders visual-audit stills.

Run with Blender 4.5 LTS or newer:
    blender --background --python Tools/build_formica_rufa_prototype.py
"""

from __future__ import annotations

import importlib.util
import math
import random
import re
import sys
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT = Path(__file__).resolve().parents[1]
SOURCE = (
    PROJECT
    / "ArtSource/ThirdParty/Sketchfab/GameReadyWorkerAnt/Raw"
    / "GameReadyWorkerAnt.blend"
)
BLEND_PATH = (
    PROJECT
    / "ArtSource/AntPrototype/CanopyKin_FormicaRufa_Player_Prototype.blend"
)
FBX_PATH = (
    PROJECT
    / "Assets/Resources/Models/Ant/Prototype"
    / "CanopyKin_FormicaRufa_Player_Prototype.fbx"
)
QA_DIRECTORY = PROJECT / "QA/AntPrototype"
UPRIGHT_BUILDER = PROJECT / "Tools/build_upright_ant_family.py"
TEXTURE_DIRECTORY = PROJECT / "Assets/Resources/HighQuality/Original/Ant"
SPECIES = "Formica rufa"


def load_upright_builder():
    spec = importlib.util.spec_from_file_location("upright_ant_builder", UPRIGHT_BUILDER)
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def image_node(nodes, path: Path, name: str, non_color: bool = False):
    node = nodes.new("ShaderNodeTexImage")
    node.name = name
    node.label = name
    node.image = bpy.data.images.load(str(path), check_existing=True)
    if non_color:
        node.image.colorspace_settings.name = "Non-Color"
    return node


def principled_input(shader, name: str):
    return shader.inputs.get(name)


def cuticle_material(
    name: str,
    tint: tuple[float, float, float, float],
    roughness: float,
    micro_strength: float,
):
    material = bpy.data.materials.new(name)
    material.diffuse_color = tint
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()

    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    shader.location = (720, 80)
    output.location = (980, 80)
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])

    base = image_node(
        nodes,
        TEXTURE_DIRECTORY / "ant_exoskeleton_diff_4k.jpg",
        "Project 4K cuticle albedo",
    )
    ao = image_node(
        nodes,
        TEXTURE_DIRECTORY / "ant_exoskeleton_ao_4k.png",
        "Project 4K cuticle AO",
        True,
    )
    rough = image_node(
        nodes,
        TEXTURE_DIRECTORY / "ant_exoskeleton_rough_4k.png",
        "Project 4K cuticle roughness",
        True,
    )
    normal = image_node(
        nodes,
        TEXTURE_DIRECTORY / "ant_exoskeleton_nor_dx_4k.png",
        "Project 4K cuticle normal (DirectX)",
        True,
    )
    base.location = (-1050, 250)
    ao.location = (-1050, 10)
    rough.location = (-560, -260)
    normal.location = (-1050, -520)

    tint_node = nodes.new("ShaderNodeRGB")
    tint_node.outputs[0].default_value = tint
    tint_node.location = (-1050, 430)
    tint_mix = nodes.new("ShaderNodeMixRGB")
    tint_mix.blend_type = "MULTIPLY"
    tint_mix.inputs[0].default_value = 1
    tint_mix.location = (-760, 300)
    links.new(base.outputs["Color"], tint_mix.inputs[1])
    links.new(tint_node.outputs[0], tint_mix.inputs[2])
    ao_mix = nodes.new("ShaderNodeMixRGB")
    ao_mix.blend_type = "MULTIPLY"
    ao_mix.inputs[0].default_value = .58
    ao_mix.location = (-470, 230)
    links.new(tint_mix.outputs[0], ao_mix.inputs[1])
    links.new(ao.outputs["Color"], ao_mix.inputs[2])
    links.new(ao_mix.outputs[0], principled_input(shader, "Base Color"))

    rough_mix = nodes.new("ShaderNodeMix")
    rough_mix.data_type = "FLOAT"
    rough_mix.inputs[0].default_value = .72
    rough_mix.inputs[2].default_value = roughness
    links.new(rough.outputs["Color"], rough_mix.inputs[3])
    links.new(rough_mix.outputs[0], principled_input(shader, "Roughness"))

    separate = nodes.new("ShaderNodeSeparateColor")
    invert_green = nodes.new("ShaderNodeMath")
    invert_green.operation = "SUBTRACT"
    invert_green.inputs[0].default_value = 1
    combine = nodes.new("ShaderNodeCombineColor")
    normal_map = nodes.new("ShaderNodeNormalMap")
    normal_map.inputs["Strength"].default_value = .72
    separate.location = (-770, -520)
    invert_green.location = (-540, -560)
    combine.location = (-300, -500)
    normal_map.location = (-60, -470)
    links.new(normal.outputs["Color"], separate.inputs["Color"])
    links.new(separate.outputs["Red"], combine.inputs["Red"])
    links.new(separate.outputs["Green"], invert_green.inputs[1])
    links.new(invert_green.outputs[0], combine.inputs["Green"])
    links.new(separate.outputs["Blue"], combine.inputs["Blue"])
    links.new(combine.outputs["Color"], normal_map.inputs["Color"])

    micro = nodes.new("ShaderNodeTexNoise")
    micro.noise_dimensions = "3D"
    micro.inputs["Scale"].default_value = 145
    micro.inputs["Detail"].default_value = 5.5
    micro.inputs["Roughness"].default_value = .68
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = micro_strength
    bump.inputs["Distance"].default_value = .018
    micro.location = (-330, -760)
    bump.location = (210, -430)
    links.new(micro.outputs["Fac"], bump.inputs["Height"])
    links.new(normal_map.outputs["Normal"], bump.inputs["Normal"])
    links.new(bump.outputs["Normal"], principled_input(shader, "Normal"))

    metallic = principled_input(shader, "Metallic")
    if metallic:
        metallic.default_value = .0
    ior = principled_input(shader, "IOR")
    if ior:
        ior.default_value = 1.47
    coat = principled_input(shader, "Coat Weight")
    if coat:
        coat.default_value = .12
    coat_roughness = principled_input(shader, "Coat Roughness")
    if coat_roughness:
        coat_roughness.default_value = .24
    return material


def simple_material(
    name: str,
    color: tuple[float, float, float, float],
    roughness: float,
    emission_strength: float = 0,
):
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = color
    shader.inputs["Roughness"].default_value = roughness
    if emission_strength:
        emission_color = principled_input(shader, "Emission Color")
        emission = principled_input(shader, "Emission Strength")
        if emission_color:
            emission_color.default_value = color
        if emission:
            emission.default_value = emission_strength
    return material


def add_skin_group(obj: bpy.types.Object, bone: str) -> None:
    group = obj.vertex_groups.new(name=bone)
    group.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")


def weighted_icosphere(
    name: str,
    center: Vector,
    scale: Vector,
    bone: str,
    material,
    subdivisions: int = 4,
):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=True)
    # A tiny deterministic offset keeps the silhouette from looking mirrored by a machine.
    for vertex in obj.data.vertices:
        direction = vertex.co - center
        vertex.co.x += .0012 * math.sin(direction.y * 83 + direction.z * 47)
    obj.data.materials.append(material)
    add_skin_group(obj, bone)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return obj


def polyline_tube(
    name: str,
    points: list[Vector],
    radius: float,
    bone: str,
    material,
    sides: int = 10,
    end_taper: float = .72,
):
    vertices = []
    for index, point in enumerate(points):
        if index == 0:
            tangent = (points[1] - point).normalized()
        elif index == len(points) - 1:
            tangent = (point - points[index - 1]).normalized()
        else:
            tangent = (points[index + 1] - points[index - 1]).normalized()
        helper = Vector((0, 0, 1))
        if abs(tangent.dot(helper)) > .92:
            helper = Vector((0, 1, 0))
        axis_a = tangent.cross(helper).normalized()
        axis_b = tangent.cross(axis_a).normalized()
        taper = 1 - end_taper * index / max(1, len(points) - 1)
        for side in range(sides):
            angle = math.tau * side / sides
            vertices.append(
                point + (axis_a * math.cos(angle) + axis_b * math.sin(angle))
                * radius * taper
            )
    faces = []
    for ring in range(len(points) - 1):
        for side in range(sides):
            nxt = (side + 1) % sides
            a = ring * sides + side
            b = ring * sides + nxt
            c = (ring + 1) * sides + nxt
            d = (ring + 1) * sides + side
            faces.append((a, b, c, d))
    faces.append(tuple(reversed(range(sides))))
    last = (len(points) - 1) * sides
    faces.append(tuple(last + side for side in range(sides)))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update(calc_edges=True)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    add_skin_group(obj, bone)
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    return obj


def add_petiole_and_claw_bones(armature: bpy.types.Object) -> None:
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bones = armature.data.edit_bones
    thorax = bones["Thorax"]
    abdomen = bones["Abdomen"]
    petiole = bones.new("Petiole")
    petiole.head = Vector((0, .082, .176))
    petiole.tail = Vector((0, .172, .174))
    petiole.parent = thorax
    petiole.use_connect = False
    abdomen.parent = petiole
    abdomen.use_connect = False

    for side_name in ("L", "R"):
        side = 1 if side_name == "L" else -1
        for pair in ("Front", "Middle", "Rear"):
            tip = bones[f"Leg_{side_name}_{pair}_TarsusTip"]
            base = tip.tail.copy()
            travel = (tip.tail - tip.head).normalized()
            for claw_name, lateral in (("Inner", -1), ("Outer", 1)):
                claw = bones.new(f"Leg_{side_name}_{pair}_Claw_{claw_name}")
                claw.head = base
                claw.tail = base + travel * .013 + Vector((side * lateral * .010, -.003, .004))
                claw.parent = tip
                claw.use_connect = False

    bpy.ops.object.mode_set(mode="OBJECT")


def create_anatomical_details(armature: bpy.types.Object, shell_red, joint):
    details = []
    petiole = armature.data.bones["Petiole"]
    center = petiole.head_local.lerp(petiole.tail_local, .52)
    details.append(
        weighted_icosphere(
            "Formica one-node petiole",
            center + Vector((.0012, 0, .031)),
            # Formicinae have one narrow waist segment expressed as a high,
            # laterally broad scale rather than a round Myrmicinae node.
            Vector((.071, .027, .088)),
            "Petiole",
            shell_red,
        )
    )

    for side_name in ("L", "R"):
        side = 1 if side_name == "L" else -1
        for pair in ("Front", "Middle", "Rear"):
            tip = armature.data.bones[f"Leg_{side_name}_{pair}_TarsusTip"]
            base = tip.tail_local.copy()
            travel = (tip.tail_local - tip.head_local).normalized()
            for claw_name, lateral in (("Inner", -1), ("Outer", 1)):
                bone_name = f"Leg_{side_name}_{pair}_Claw_{claw_name}"
                end = base + travel * .017 + Vector((side * lateral * .013, -.004, .005))
                points = [
                    base,
                    base.lerp(end, .42) + Vector((0, 0, .004)),
                    base.lerp(end, .76) + Vector((0, 0, .003)),
                    end,
                ]
                details.append(
                    polyline_tube(
                        f"{pair} {side_name} {claw_name} pretarsal claw",
                        points,
                        .0026,
                        bone_name,
                        joint,
                        9,
                        .72,
                    )
                )
    return details


def delete_deformed_source_segments(mesh: bpy.types.Object) -> int:
    """Remove donor antennae/tarsi whose rigid source pieces do not meet in bind pose."""
    removed = 0
    for vertex in mesh.data.vertices:
        name = dominant_deform_group(mesh, vertex)
        selected = name.startswith("Antenna_") or (
            name.startswith("Leg_") and "Tarsus" in name
        )
        vertex.select = selected
        removed += int(selected)
    bpy.context.view_layer.objects.active = mesh
    mesh.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_mode(type="VERT")
    bpy.ops.mesh.delete(type="VERT")
    bpy.ops.object.mode_set(mode="OBJECT")
    mesh.data.update(calc_edges=True)
    return removed


def smooth_bone_tube(
    name: str,
    bone,
    radius: float,
    material,
    end_taper: float,
    overlap: float,
):
    direction = (bone.tail_local - bone.head_local).normalized()
    start = bone.head_local - direction * overlap
    end = bone.tail_local + direction * overlap
    side = direction.cross(Vector((0, 0, 1)))
    if side.length < .1:
        side = direction.cross(Vector((0, 1, 0)))
    side.normalize()
    bow = side * (.0014 if "Antenna" in name else .0005)
    points = [
        start,
        start.lerp(end, .34) + bow,
        start.lerp(end, .68) + bow * .65,
        end,
    ]
    return polyline_tube(
        name,
        points,
        radius,
        bone.name,
        material,
        14,
        end_taper,
    )


def create_rebuilt_distal_anatomy(armature: bpy.types.Object, joint):
    """Continuous skinned antennae and tarsi with closed, overlapping joints."""
    result = []
    for side in ("L", "R"):
        for segment, radius, taper in (
            (1, .0105, .12),
            (2, .0082, .2),
            (3, .0062, .52),
        ):
            bone = armature.data.bones[f"Antenna_{side}_{segment}"]
            result.append(
                smooth_bone_tube(
                    f"Rebuilt {bone.name} closed cuticle",
                    bone,
                    radius,
                    joint,
                    taper,
                    .004,
                )
            )

        for pair in ("Front", "Middle", "Rear"):
            for segment, radius, taper in (
                ("Tarsus", .0062, .08),
                ("Tarsus2", .0052, .12),
                ("Tarsus3", .0043, .18),
                ("TarsusTip", .0036, .34),
            ):
                bone = armature.data.bones[f"Leg_{side}_{pair}_{segment}"]
                result.append(
                    smooth_bone_tube(
                        f"Rebuilt {bone.name} closed cuticle",
                        bone,
                        radius,
                        joint,
                        taper,
                        .0022,
                    )
                )
    return result


def create_joint_membranes(armature: bpy.types.Object, joint):
    """Close visible segment ends with small flexible arthrodial membranes."""
    result = []
    leg_radii = {
        "Coxa": .0115,
        "Femur": .0095,
        "Tibia": .0078,
        "Tarsus": .0048,
        "Tarsus2": .0039,
        "Tarsus3": .0033,
        "TarsusTip": .0028,
    }
    for side in ("L", "R"):
        for pair in ("Front", "Middle", "Rear"):
            for segment, radius in leg_radii.items():
                bone_name = f"Leg_{side}_{pair}_{segment}"
                bone = armature.data.bones[bone_name]
                point = bone.head_local
                direction = (bone.tail_local - bone.head_local).normalized()
                result.append(
                    polyline_tube(
                        f"{bone_name} arthrodial membrane",
                        [point - direction * radius * .72, point + direction * radius * .72],
                        radius,
                        bone_name,
                        joint,
                        12,
                        0,
                    )
                )
            tip_name = f"Leg_{side}_{pair}_TarsusTip"
            tip = armature.data.bones[tip_name]
            result.append(
                weighted_icosphere(
                    f"{tip_name} terminal membrane",
                    tip.tail_local,
                    Vector((.0042, .0042, .0038)),
                    tip_name,
                    joint,
                    2,
                )
            )

        for segment in (1, 2, 3):
            bone_name = f"Antenna_{side}_{segment}"
            bone = armature.data.bones[bone_name]
            point = bone.head_local
            direction = (bone.tail_local - bone.head_local).normalized()
            result.append(
                polyline_tube(
                    f"{bone_name} flexible socket",
                    [point - direction * .007, point + direction * .007],
                    .0082,
                    bone_name,
                    joint,
                    12,
                    0,
                )
            )
        tip_name = f"Antenna_{side}_3"
        tip = armature.data.bones[tip_name]
        result.append(
            weighted_icosphere(
                f"{tip_name} closed sensory tip",
                tip.tail_local,
                Vector((.011, .011, .011)),
                tip_name,
                joint,
                2,
            )
        )

        mandible_name = f"Mandible_{side}"
        mandible = armature.data.bones[mandible_name]
        point = mandible.head_local
        direction = (mandible.tail_local - mandible.head_local).normalized()
        result.append(
            polyline_tube(
                f"{mandible_name} condyle",
                [point - direction * .009, point + direction * .009],
                .0105,
                mandible_name,
                joint,
                14,
                0,
            )
        )
    return result


def create_setae(source: bpy.types.Object, shell_red, shell_dark, joint):
    rng = random.Random(3701761)
    vertices = []
    faces = []
    groups: dict[str, list[int]] = {}
    material_indices = []
    allowed = {"Head", "Thorax", "Abdomen"}
    candidates = [
        vertex
        for vertex in source.data.vertices
        if dominant_deform_group(source, vertex) in allowed
    ]
    rng.shuffle(candidates)
    for vertex in candidates[:420]:
        bone_name = dominant_deform_group(source, vertex)
        if bone_name not in allowed:
            continue
        normal = vertex.normal.normalized()
        if normal.length < .5:
            continue
        tangent = normal.cross(Vector((0, 0, 1)))
        if tangent.length < .1:
            tangent = normal.cross(Vector((0, 1, 0)))
        tangent.normalize()
        bitangent = normal.cross(tangent).normalized()
        start = vertex.co + normal * .0008
        length = rng.uniform(.008, .016)
        lean = tangent * rng.uniform(-.003, .003) + bitangent * rng.uniform(-.002, .002)
        tip = start + normal * length + lean
        radius = rng.uniform(.0007, .0012)
        base_index = len(vertices)
        vertices.extend(
            (
                start + tangent * radius,
                start - tangent * radius * .5 + bitangent * radius * .86,
                start - tangent * radius * .5 - bitangent * radius * .86,
                tip,
            )
        )
        faces.extend(
            (
                (base_index, base_index + 1, base_index + 2),
                (base_index, base_index + 3, base_index + 1),
                (base_index + 1, base_index + 3, base_index + 2),
                (base_index + 2, base_index + 3, base_index),
            )
        )
        groups.setdefault(bone_name, []).extend(range(base_index, base_index + 4))
        material_indices.extend([1 if bone_name == "Abdomen" else 0] * 4)

    mesh = bpy.data.meshes.new("Macro setae mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update(calc_edges=True)
    obj = bpy.data.objects.new("Natural asymmetric macro setae", mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(shell_red)
    obj.data.materials.append(shell_dark)
    obj.data.materials.append(joint)
    for bone, indices in groups.items():
        group = obj.vertex_groups.new(name=bone)
        group.add(indices, 1.0, "REPLACE")
    for polygon in mesh.polygons:
        first = polygon.vertices[0] // 4
        polygon.material_index = material_indices[first * 4] if material_indices else 0
    return obj


def mark_compound_eye_vertices(mesh: bpy.types.Object) -> None:
    eye_vertices = {
        vertex_index
        for polygon in mesh.data.polygons
        if polygon.material_index == 1
        for vertex_index in polygon.vertices
    }
    if eye_vertices:
        group = mesh.vertex_groups.new(name="CompoundEyeSurface")
        group.add(sorted(eye_vertices), 1.0, "REPLACE")


def join_details(base: bpy.types.Object, details: list[bpy.types.Object], armature):
    bpy.ops.object.select_all(action="DESELECT")
    base.select_set(True)
    for detail in details:
        detail.select_set(True)
    bpy.context.view_layer.objects.active = base
    bpy.ops.object.join()
    base.parent = armature
    modifier = next((item for item in base.modifiers if item.type == "ARMATURE"), None)
    if modifier is None:
        modifier = base.modifiers.new("Anatomical armature", "ARMATURE")
    modifier.object = armature
    return base


def vertex_in_group(obj, vertex, group_name: str) -> bool:
    group = obj.vertex_groups.get(group_name)
    return bool(group) and any(item.group == group.index and item.weight > .5 for item in vertex.groups)


def dominant_deform_group(obj, vertex) -> str:
    candidates = [
        item for item in vertex.groups
        if obj.vertex_groups[item.group].name != "CompoundEyeSurface"
    ]
    if not candidates:
        return ""
    return obj.vertex_groups[max(candidates, key=lambda item: item.weight).group].name


def assign_formica_materials(mesh, shell_red, shell_dark, joint, eye):
    mesh.data.materials.clear()
    for material in (shell_red, shell_dark, joint, eye):
        mesh.data.materials.append(material)
    for polygon in mesh.data.polygons:
        vertices = [mesh.data.vertices[index] for index in polygon.vertices]
        if any(vertex_in_group(mesh, vertex, "CompoundEyeSurface") for vertex in vertices):
            polygon.material_index = 3
            continue
        names = [dominant_deform_group(mesh, vertex) for vertex in vertices]
        name = max(set(names), key=names.count) if names else ""
        if name == "Abdomen":
            polygon.material_index = 1
        elif name.startswith("Leg_") or name.startswith("Antenna_"):
            polygon.material_index = 2
        else:
            polygon.material_index = 0


BONE_PATH = re.compile(r'^pose\.bones\["([^"]+)"\](.*)$')


def remap_path(builder, path: str) -> str | None:
    match = BONE_PATH.match(path)
    if not match:
        return None
    source_bone, suffix = match.groups()
    target_bone = builder.production_bone_name(source_bone)
    if target_bone is None:
        return None
    return f'pose.bones["{target_bone}"]{suffix}'


def copy_action(
    builder,
    source_action,
    target_name: str,
    scale_factor: float,
    frame_scale: float = 1,
    reverse: bool = False,
):
    target = bpy.data.actions.new(target_name)
    target.use_fake_user = True
    target["source_action"] = source_action.name
    target["source_license"] = "CC BY 4.0 — Msassasa (@LilCick)"
    target["motion_provenance"] = (
        "time-remapped source keyframes" if frame_scale != 1 or reverse
        else "direct source keyframe transfer"
    )
    start, end = source_action.frame_range
    for curve in source_action.fcurves:
        target_path = remap_path(builder, curve.data_path)
        if target_path is None:
            continue
        group_name = curve.group.name if curve.group else None
        target_curve = target.fcurves.new(
            target_path,
            index=curve.array_index,
            action_group=group_name,
        )
        target_curve.extrapolation = curve.extrapolation
        target_curve.keyframe_points.add(len(curve.keyframe_points))
        location_curve = target_path.endswith(".location")
        for source_point, target_point in zip(
            curve.keyframe_points,
            target_curve.keyframe_points,
        ):
            source_frame = source_point.co.x
            if reverse:
                target_frame = (end - source_frame) * frame_scale
            else:
                target_frame = (source_frame - start) * frame_scale
            value_scale = scale_factor if location_curve else 1
            target_point.co = (target_frame, source_point.co.y * value_scale)
            target_point.interpolation = source_point.interpolation
            target_point.easing = source_point.easing
            target_point.handle_left_type = "AUTO_CLAMPED"
            target_point.handle_right_type = "AUTO_CLAMPED"
        target_curve.update()
    target.frame_start = 0
    target.frame_end = (end - start) * frame_scale
    return target


def add_turn_overlay(action, amount: float) -> None:
    path = 'pose.bones["Root"].rotation_euler'
    curve = action.fcurves.new(path, index=2, action_group="Root")
    curve.keyframe_points.add(3)
    end = max(1.0, action.frame_end)
    for point, value in zip(
        curve.keyframe_points,
        ((0, 0), (end * .5, amount), (end, 0)),
    ):
        point.co = value
        point.interpolation = "BEZIER"
        point.handle_left_type = "AUTO_CLAMPED"
        point.handle_right_type = "AUTO_CLAMPED"
    curve.update()


def transfer_authored_actions(builder, armature, scale_factor: float):
    source_actions = {
        action.name: action
        for action in list(bpy.data.actions)
        if any(BONE_PATH.match(curve.data_path) for curve in action.fcurves)
    }
    mapping = {
        "Attack1": "ANT_Attack_Primary",
        "Attack2": "ANT_Attack_Secondary",
        "bite": "ANT_Bite",
        "build": "ANT_ColonyWork",
        "dig": "ANT_Dig",
        "drink": "ANT_Drink",
        "eat": "ANT_Eat",
        "Formic Acid": "ANT_FormicAcidDefense",
        "grab/heavy bite": "ANT_GrabHeavyBite",
        "idle": "ANT_CalmIdle",
        "Jump": "ANT_Jump",
        "Lay egg": "ANT_LayEgg",
        "Sting(human)": "ANT_StingLargeTarget",
        "Sting(other ant)": "ANT_StingAnt",
        "Trophalaxis": "ANT_Trophallaxis",
        "Trophalaxis(Feed Larvae)": "ANT_FeedLarvae",
        "Walk": "ANT_NormalWalk",
    }
    created = []
    for source_name, target_name in mapping.items():
        source = source_actions.get(source_name)
        if source:
            created.append(copy_action(builder, source, target_name, scale_factor))

    walk = source_actions["Walk"]
    idle = source_actions["idle"]
    created.extend(
        (
            copy_action(builder, walk, "ANT_SlowWalk", scale_factor, 1.45),
            copy_action(builder, walk, "ANT_FastRun", scale_factor, .56),
            copy_action(builder, walk, "ANT_Backward", scale_factor, 1.12, True),
            copy_action(builder, idle, "ANT_AlertIdle", scale_factor, .72),
            copy_action(builder, idle, "ANT_ExploreAntennae", scale_factor, 1.35, True),
            copy_action(builder, walk, "ANT_TurnLeft", scale_factor, .68),
            copy_action(builder, walk, "ANT_TurnRight", scale_factor, .68),
        )
    )
    add_turn_overlay(created[-2], math.radians(-21))
    add_turn_overlay(created[-1], math.radians(21))

    for source in source_actions.values():
        if source not in created:
            bpy.data.actions.remove(source)
    armature.animation_data_create()
    armature.animation_data.action = bpy.data.actions.get("ANT_CalmIdle")
    return created


def create_bake_source(runtime_mesh, armature):
    high = runtime_mesh.copy()
    high.data = runtime_mesh.data.copy()
    bpy.context.collection.objects.link(high)
    high.name = "CanopyKin_FormicaRufa_HighPoly_BakeSource"
    high.data.name = high.name + "_Mesh"
    high.parent = armature
    for modifier in high.modifiers:
        if modifier.type == "ARMATURE":
            modifier.object = armature
    subdivision = high.modifiers.new("Editable high-poly cuticle source", "SUBSURF")
    subdivision.subdivision_type = "CATMULL_CLARK"
    subdivision.levels = 1
    subdivision.render_levels = 1
    bpy.context.view_layer.objects.active = high
    high.select_set(True)
    bpy.ops.object.modifier_apply(modifier=subdivision.name)
    high["purpose"] = "editable high-poly bake source; not exported to Unity"
    high.hide_render = True
    high.hide_viewport = True
    return high


def look_at(obj, target: Vector) -> None:
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def create_cylinder_between(name, start, end, radius, material):
    vector = end - start
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=10,
        radius=radius,
        depth=vector.length,
        location=start.lerp(end, .5),
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(vector.normalized())
    obj.data.materials.append(material)
    return obj


def render_still(scene, camera, path: Path) -> None:
    scene.camera = camera
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def render_qa(scene, runtime_mesh, armature) -> None:
    QA_DIRECTORY.mkdir(parents=True, exist_ok=True)
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world.use_nodes = False
    scene.world.color = (.006, .009, .007)
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.view_settings.exposure = -.7

    ground_material = simple_material("Prototype neutral soil", (.055, .04, .025, 1), .78)
    bpy.ops.mesh.primitive_plane_add(size=5, location=(0, 0, 0))
    ground = bpy.context.object
    ground.name = "Prototype neutral ground"
    ground.data.materials.append(ground_material)

    camera_data = bpy.data.cameras.new("Prototype evidence camera")
    camera = bpy.data.objects.new("Prototype evidence camera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.data.lens = 62

    sun_data = bpy.data.lights.new("Macro sunlight", "SUN")
    sun_data.energy = .72
    sun_data.angle = math.radians(18)
    sun = bpy.data.objects.new("Macro sunlight", sun_data)
    bpy.context.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(32), math.radians(-18), math.radians(-28))

    key_data = bpy.data.lights.new("Large softbox", "AREA")
    key_data.energy = 260
    key_data.shape = "DISK"
    key_data.size = 4
    key = bpy.data.objects.new("Large softbox", key_data)
    bpy.context.collection.objects.link(key)
    key.location = (1.4, -1.2, 2.4)
    look_at(key, Vector((0, 0, .18)))

    rim_data = bpy.data.lights.new("Back rim", "AREA")
    rim_data.energy = 420
    rim_data.size = 2.2
    rim = bpy.data.objects.new("Back rim", rim_data)
    bpy.context.collection.objects.link(rim)
    rim.location = (-1.2, 1.4, 1.5)
    look_at(rim, Vector((0, 0, .2)))

    target = Vector((0, -.04, .2))
    views = {
        "front": Vector((0, -1.65, .30)),
        "side": Vector((2.08, -.02, .38)),
        "top": Vector((0, 0, 1.72)),
        "bottom": Vector((0, 0, -1.35)),
    }
    for name, location in views.items():
        camera.location = location
        look_at(camera, target)
        ground.hide_render = name == "bottom"
        render_still(scene, camera, QA_DIRECTORY / f"formica-rufa-prototype-{name}.png")
    ground.hide_render = False

    camera.location = views["side"]
    look_at(camera, target)
    wire = runtime_mesh.copy()
    wire.data = runtime_mesh.data.copy()
    bpy.context.collection.objects.link(wire)
    wire.name = "Temporary anatomical wireframe"
    wire.data.materials.clear()
    wire.data.materials.append(simple_material("Wire cyan", (.01, .9, 1, 1), .25, 3))
    modifier = wire.modifiers.new("Visible topology", "WIREFRAME")
    modifier.thickness = .00125
    modifier.use_replace = True
    render_still(scene, camera, QA_DIRECTORY / "formica-rufa-prototype-wireframe.png")
    bpy.data.objects.remove(wire, do_unlink=True)

    rig_material = simple_material("Armature orange", (1, .16, .015, 1), .26, 2.5)
    rig_geometry = []
    for bone in armature.data.bones:
        rig_geometry.append(
            create_cylinder_between(
                "Rig " + bone.name,
                bone.head_local,
                bone.tail_local,
                .0038 if bone.name != "Root" else .006,
                rig_material,
            )
        )
    render_still(scene, camera, QA_DIRECTORY / "formica-rufa-prototype-armature.png")
    for obj in rig_geometry:
        bpy.data.objects.remove(obj, do_unlink=True)

    weight_copy = runtime_mesh.copy()
    weight_copy.data = runtime_mesh.data.copy()
    bpy.context.collection.objects.link(weight_copy)
    weight_copy.name = "Temporary skinning regions"
    palette = [
        simple_material("Weight head", (.95, .06, .04, 1), .45),
        simple_material("Weight thorax", (1, .62, .02, 1), .45),
        simple_material("Weight petiole", (1, .03, .62, 1), .45),
        simple_material("Weight abdomen", (.08, .28, 1, 1), .45),
        simple_material("Weight left legs", (.08, .9, .22, 1), .45),
        simple_material("Weight right legs", (.1, .75, .92, 1), .45),
        simple_material("Weight antennae", (.72, .08, 1, 1), .45),
        simple_material("Weight mandible-claw", (1, 1, .08, 1), .45),
    ]
    weight_copy.data.materials.clear()
    for material in palette:
        weight_copy.data.materials.append(material)
    for polygon in weight_copy.data.polygons:
        names = [dominant_deform_group(weight_copy, weight_copy.data.vertices[index]) for index in polygon.vertices]
        name = max(set(names), key=names.count) if names else ""
        if name == "Head": index = 0
        elif name == "Thorax": index = 1
        elif name == "Petiole": index = 2
        elif name == "Abdomen": index = 3
        elif name.startswith("Leg_L") and "Claw" not in name: index = 4
        elif name.startswith("Leg_R") and "Claw" not in name: index = 5
        elif name.startswith("Antenna"): index = 6
        else: index = 7
        polygon.material_index = index
    runtime_mesh.hide_render = True
    render_still(scene, camera, QA_DIRECTORY / "formica-rufa-prototype-skinning-regions.png")
    runtime_mesh.hide_render = False
    bpy.data.objects.remove(weight_copy, do_unlink=True)

    key_data.energy = 34
    sun_data.energy = .08
    rim_data.energy = 42
    render_still(scene, camera, QA_DIRECTORY / "formica-rufa-prototype-dark-light.png")
    key_data.energy = 28
    sun_data.energy = .04
    rim_data.energy = 760
    render_still(scene, camera, QA_DIRECTORY / "formica-rufa-prototype-backlight.png")
    key_data.energy = 260
    sun_data.energy = .72
    rim_data.energy = 420


def export_fbx(armature, runtime_mesh) -> None:
    FBX_PATH.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    runtime_mesh.select_set(True)
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


def main() -> None:
    builder = load_upright_builder()
    print("FORMICA_STAGE=open_source", flush=True)
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    source_armature = bpy.data.objects.get("Worker ant armature")
    if not source_armature:
        raise RuntimeError("Verified CC BY worker armature is missing")

    print("FORMICA_STAGE=materials", flush=True)
    shell_red = cuticle_material("FormicaRufa_RedMesosoma_4K", (.92, .22, .065, 1), .34, .16)
    shell_dark = cuticle_material("FormicaRufa_DarkGaster_4K", (.16, .045, .022, 1), .28, .2)
    joint = cuticle_material("FormicaRufa_DarkJoint_4K", (.29, .07, .025, 1), .38, .12)
    eye = simple_material("FormicaRufa_CompoundEye", (.002, .006, .004, 1), .1)

    print("FORMICA_STAGE=clean_rig", flush=True)
    armature = builder.create_clean_armature(source_armature)
    prepared, corrected_normals, filled_holes = builder.prepare_source_meshes(
        source_armature,
        armature,
        shell_red,
        eye,
    )
    print("FORMICA_STAGE=prepared_mesh", flush=True)
    runtime_mesh = prepared[0]
    mark_compound_eye_vertices(runtime_mesh)
    scale_factor = builder.scale_family_source(runtime_mesh, armature)
    player = next(caste for caste in builder.CASTES if caste.key == "Player")
    builder.morph_caste(runtime_mesh, armature, player)
    add_petiole_and_claw_bones(armature)
    removed_source_vertices = delete_deformed_source_segments(runtime_mesh)
    print("FORMICA_STAGE=rebuilt_distal_anatomy", flush=True)
    details = create_anatomical_details(armature, shell_red, joint)
    details.extend(create_rebuilt_distal_anatomy(armature, joint))
    details.extend(create_joint_membranes(armature, joint))
    details.append(create_setae(runtime_mesh, shell_red, shell_dark, joint))
    runtime_mesh = join_details(runtime_mesh, details, armature)
    runtime_mesh.name = "CanopyKin_FormicaRufa_Player_Prototype_LOD0"
    runtime_mesh.data.name = runtime_mesh.name + "_Mesh"
    assign_formica_materials(runtime_mesh, shell_red, shell_dark, joint, eye)
    builder.shift_to_ground([runtime_mesh], armature)

    print("FORMICA_STAGE=transfer_actions", flush=True)
    actions = transfer_authored_actions(builder, armature, scale_factor)
    armature.name = "CanopyKin_FormicaRufa_Player_Prototype_Armature"
    runtime_mesh["species"] = SPECIES
    runtime_mesh["source_license"] = "CC BY 4.0 — Msassasa (@LilCick)"
    runtime_mesh["anatomy"] = "head thorax one-node petiole gaster six legs paired claws antennae mandibles"
    runtime_mesh["prototype_only"] = True
    armature["coordinate_convention"] = "Blender Z-up/-Y-forward; Unity +Y-up/+Z-forward"
    print("FORMICA_STAGE=high_poly", flush=True)
    high_poly = create_bake_source(runtime_mesh, armature)

    print("FORMICA_STAGE=remove_source", flush=True)
    builder.remove_source_objects({armature, runtime_mesh, high_poly})
    print("FORMICA_STAGE=export", flush=True)
    export_fbx(armature, runtime_mesh)
    print("FORMICA_STAGE=render", flush=True)
    render_qa(bpy.context.scene, runtime_mesh, armature)

    BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH), compress=True)
    runtime_mesh.data.calc_loop_triangles()
    high_poly.data.calc_loop_triangles()
    print(
        "CANOPY_KIN_FORMICA_RUFA_PROTOTYPE_OK "
        f"species={SPECIES!r} bones={len(armature.data.bones)} "
        f"runtimeTriangles={len(runtime_mesh.data.loop_triangles)} "
        f"highPolyTriangles={len(high_poly.data.loop_triangles)} "
        f"actions={len(actions)} scale={scale_factor:.6f} "
        f"replacedDonorVertices={removed_source_vertices} "
        f"correctedNormals={corrected_normals} filledHoles={filled_holes} "
        f"blend={BLEND_PATH} fbx={FBX_PATH} qa={QA_DIRECTORY}"
    )


if __name__ == "__main__":
    main()
