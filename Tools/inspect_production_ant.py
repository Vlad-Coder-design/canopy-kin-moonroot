"""Compact geometry/rig audit for a generated Canopy Kin ant source file."""

from __future__ import annotations

import json

import bpy


armature = next(obj for obj in bpy.data.objects if obj.type == "ARMATURE")
meshes = [obj for obj in bpy.data.objects if obj.type == "MESH"]
for mesh in meshes:
    mesh.data.calc_loop_triangles()

print("CANOPY_KIN_PRODUCTION_AUDIT=" + json.dumps({
    "armature": armature.name,
    "bones": {
        bone.name: {
            "head": list(bone.head_local),
            "tail": list(bone.tail_local),
            "parent": bone.parent.name if bone.parent else None,
        }
        for bone in armature.data.bones
    },
    "meshes": [
        {
            "name": obj.name,
            "vertices": len(obj.data.vertices),
            "triangles": len(obj.data.loop_triangles),
            "bounds": [
                [min(vertex.co[axis] for vertex in obj.data.vertices) for axis in range(3)],
                [max(vertex.co[axis] for vertex in obj.data.vertices) for axis in range(3)],
            ],
            "groups": [group.name for group in obj.vertex_groups],
        }
        for obj in meshes
    ],
    "actions": [action.name for action in bpy.data.actions],
}, ensure_ascii=False))
