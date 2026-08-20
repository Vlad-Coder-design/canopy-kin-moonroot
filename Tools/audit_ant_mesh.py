"""Audit ant mesh closure, normals, transforms, materials and rig axes.

Run with:
    blender --background ant.blend --python Tools/audit_ant_mesh.py
"""

from __future__ import annotations

import json

import bmesh
import bpy
from mathutils import Vector


def rounded(values):
    return [round(float(value), 6) for value in values]


def mesh_topology(obj: bpy.types.Object) -> dict:
    mesh = obj.data
    mesh.calc_loop_triangles()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.faces.ensure_lookup_table()
    original_normals = [face.normal.copy() for face in bm.faces]
    boundary_edges = sum(len(edge.link_faces) == 1 for edge in bm.edges)
    loose_edges = sum(len(edge.link_faces) == 0 for edge in bm.edges)
    non_manifold_edges = sum(not edge.is_manifold for edge in bm.edges)
    signed_volume = bm.calc_volume(signed=True) if bm.faces else 0.0
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    flipped_faces = sum(
        original.dot(face.normal) < 0
        for original, face in zip(original_normals, bm.faces)
    )
    bm.free()

    alpha = []
    render_methods = []
    for material in mesh.materials:
        if not material:
            continue
        alpha.append(float(material.diffuse_color[3]))
        render_methods.append(
            getattr(material, "surface_render_method", "DITHERED_OR_OPAQUE")
        )

    return {
        "name": obj.name,
        "parent": obj.parent.name if obj.parent else None,
        "hidden_render": obj.hide_render,
        "vertices": len(mesh.vertices),
        "triangles": len(mesh.loop_triangles),
        "boundary_edges": boundary_edges,
        "loose_edges": loose_edges,
        "non_manifold_edges": non_manifold_edges,
        "faces_reversed_by_recalculate_outside": flipped_faces,
        "signed_volume": round(float(signed_volume), 9),
        "location": rounded(obj.location),
        "rotation_degrees": rounded(
            angle * 57.295779513 for angle in obj.rotation_euler
        ),
        "scale": rounded(obj.scale),
        "dimensions": rounded(obj.dimensions),
        "determinant": round(float(obj.matrix_world.to_3x3().determinant()), 6),
        "materials": [
            material.name if material else None for material in mesh.materials
        ],
        "material_alpha": alpha,
        "surface_render_methods": render_methods,
        "armature_targets": [
            modifier.object.name
            for modifier in obj.modifiers
            if modifier.type == "ARMATURE" and modifier.object
        ],
    }


def armature_axes(obj: bpy.types.Object) -> dict:
    roots = [bone for bone in obj.data.bones if bone.parent is None]
    return {
        "name": obj.name,
        "location": rounded(obj.location),
        "rotation_degrees": rounded(
            angle * 57.295779513 for angle in obj.rotation_euler
        ),
        "scale": rounded(obj.scale),
        "determinant": round(float(obj.matrix_world.to_3x3().determinant()), 6),
        "roots": [
            {
                "name": bone.name,
                "head": rounded(bone.head_local),
                "tail": rounded(bone.tail_local),
                "direction": rounded(
                    (bone.tail_local - bone.head_local).normalized()
                    if (bone.tail_local - bone.head_local).length > 0
                    else Vector((0, 0, 0))
                ),
            }
            for bone in roots
        ],
    }


def main() -> None:
    report = {
        "file": bpy.data.filepath,
        "scene_units": {
            "system": bpy.context.scene.unit_settings.system,
            "scale_length": bpy.context.scene.unit_settings.scale_length,
        },
        "meshes": [
            mesh_topology(obj)
            for obj in bpy.data.objects
            if obj.type == "MESH"
        ],
        "armatures": [
            armature_axes(obj)
            for obj in bpy.data.objects
            if obj.type == "ARMATURE"
        ],
    }
    print("CANOPY_KIN_ANT_MESH_AUDIT_BEGIN")
    print(json.dumps(report, indent=2))
    print("CANOPY_KIN_ANT_MESH_AUDIT_END")


if __name__ == "__main__":
    main()
