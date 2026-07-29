"""Print an objective topology, rig, animation and material report.

Run with Blender after opening a candidate file:

    blender --background candidate.blend --python Tools/inspect_ant_candidate.py
"""

from __future__ import annotations

import json
from collections import Counter

import bpy


def mesh_report(obj: bpy.types.Object) -> dict:
    mesh = obj.data
    mesh.calc_loop_triangles()
    assigned = [False] * len(mesh.vertices)
    influence_counts = [0] * len(mesh.vertices)
    group_names = {group.index: group.name for group in obj.vertex_groups}
    for vertex in mesh.vertices:
        for assignment in vertex.groups:
            if assignment.weight <= 0.0001:
                continue
            assigned[vertex.index] = True
            influence_counts[vertex.index] += 1

    return {
        "name": obj.name,
        "vertices": len(mesh.vertices),
        "edges": len(mesh.edges),
        "polygons": len(mesh.polygons),
        "triangles": len(mesh.loop_triangles),
        "uv_layers": [layer.name for layer in mesh.uv_layers],
        "materials": [
            material.name if material else None for material in mesh.materials
        ],
        "vertex_groups": len(obj.vertex_groups),
        "zero_weight_vertices": assigned.count(False) if obj.vertex_groups else None,
        "max_influences": max(influence_counts, default=0),
        "influences_over_four": sum(count > 4 for count in influence_counts),
        "group_sample": list(group_names.values())[:30],
        "modifiers": [
            {
                "name": modifier.name,
                "type": modifier.type,
                "target": getattr(getattr(modifier, "object", None), "name", None),
            }
            for modifier in obj.modifiers
        ],
        "dimensions": [round(value, 6) for value in obj.dimensions],
    }


def armature_report(obj: bpy.types.Object) -> dict:
    bones = list(obj.data.bones)
    return {
        "name": obj.name,
        "bones": len(bones),
        "deform_bones": sum(bone.use_deform for bone in bones),
        "roots": [bone.name for bone in bones if bone.parent is None],
        "bone_names": [bone.name for bone in bones],
        "bone_layout": [
            {
                "name": bone.name,
                "parent": bone.parent.name if bone.parent else None,
                "head": [round(value, 5) for value in bone.head_local],
                "tail": [round(value, 5) for value in bone.tail_local],
            }
            for bone in bones
        ],
    }


def action_report(action: bpy.types.Action) -> dict:
    slots = getattr(action, "slots", ())
    return {
        "name": action.name,
        "frame_range": [round(value, 3) for value in action.frame_range],
        "fcurves": len(getattr(action, "fcurves", ())),
        "slots": len(slots),
    }


def main() -> None:
    objects = Counter(obj.type for obj in bpy.data.objects)
    report = {
        "blender": bpy.app.version_string,
        "file": bpy.data.filepath,
        "object_types": dict(objects),
        "meshes": [
            mesh_report(obj) for obj in bpy.data.objects if obj.type == "MESH"
        ],
        "armatures": [
            armature_report(obj)
            for obj in bpy.data.objects
            if obj.type == "ARMATURE"
        ],
        "actions": [action_report(action) for action in bpy.data.actions],
        "materials": [
            {
                "name": material.name,
                "nodes": material.use_nodes,
                "node_names": [
                    node.name for node in material.node_tree.nodes
                ] if material.use_nodes and material.node_tree else [],
            }
            for material in bpy.data.materials
        ],
        "images": [
            {
                "name": image.name,
                "size": list(image.size),
                "packed": image.packed_file is not None,
                "filepath": image.filepath,
            }
            for image in bpy.data.images
        ],
    }
    print("CANOPY_KIN_ANT_CANDIDATE_REPORT_BEGIN")
    print(json.dumps(report, indent=2, ensure_ascii=False))
    print("CANOPY_KIN_ANT_CANDIDATE_REPORT_END")


if __name__ == "__main__":
    main()
