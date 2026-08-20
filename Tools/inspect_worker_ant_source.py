"""Print the editable CC BY worker-ant source structure for production audits."""

from __future__ import annotations

import json

import bpy


def action_summary(action: bpy.types.Action) -> dict:
    paths = sorted({curve.data_path for curve in action.fcurves})
    frames = [point.co.x for curve in action.fcurves for point in curve.keyframe_points]
    return {
        "name": action.name,
        "frame_range": [min(frames), max(frames)] if frames else [0, 0],
        "fcurves": len(action.fcurves),
        "keyframes": sum(len(curve.keyframe_points) for curve in action.fcurves),
        "bone_paths": paths[:12],
    }


compact_actions = [action_summary(action) for action in bpy.data.actions]
print("CANOPY_KIN_WORKER_SOURCE_COMPACT=" + json.dumps(
    {
        "actions": compact_actions,
        "armatures": [
            {
                "name": obj.name,
                "bones": len(obj.data.bones),
                "bone_names": [bone.name for bone in obj.data.bones],
            }
            for obj in bpy.data.objects
            if obj.type == "ARMATURE"
        ],
    },
    ensure_ascii=False,
))


armatures = []
for obj in bpy.data.objects:
    if obj.type != "ARMATURE":
        continue
    armatures.append(
        {
            "name": obj.name,
            "bones": len(obj.data.bones),
            "bone_names": [bone.name for bone in obj.data.bones],
            "actions": [
                action_summary(action)
                for action in bpy.data.actions
                if any('pose.bones["' in curve.data_path for curve in action.fcurves)
            ],
        }
    )

meshes = []
for obj in bpy.data.objects:
    if obj.type != "MESH":
        continue
    obj.data.calc_loop_triangles()
    meshes.append(
        {
            "name": obj.name,
            "parent": obj.parent.name if obj.parent else None,
            "parent_type": obj.parent_type,
            "parent_bone": obj.parent_bone,
            "vertices": len(obj.data.vertices),
            "triangles": len(obj.data.loop_triangles),
            "materials": [slot.name for slot in obj.data.materials if slot],
        }
    )

print("CANOPY_KIN_WORKER_SOURCE_AUDIT_BEGIN")
print(json.dumps({"armatures": armatures, "meshes": meshes}, indent=2, ensure_ascii=False))
print("CANOPY_KIN_WORKER_SOURCE_AUDIT_END")
