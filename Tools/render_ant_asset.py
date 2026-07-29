"""Render neutral front, side and top inspection images from an opened blend."""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def arguments() -> tuple[Path, str]:
    raw = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    if len(raw) != 2:
        raise SystemExit("Expected: -- <output-directory> <filename-prefix>")
    return Path(raw[0]), raw[1]


def look_at(obj: bpy.types.Object, point: Vector) -> None:
    obj.rotation_euler = (point - obj.location).to_track_quat("-Z", "Y").to_euler()


def main() -> None:
    output, prefix = arguments()
    output.mkdir(parents=True, exist_ok=True)
    meshes = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH" and "LOD1" not in obj.name
    ]
    if not meshes:
        raise RuntimeError("The opened scene contains no mesh.")

    points = []
    for obj in meshes:
        points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
        obj.hide_render = False
    minimum = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    maximum = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    center = (minimum + maximum) * 0.5
    extent = maximum - minimum
    radius = max(extent) * 1.55

    for obj in list(bpy.context.scene.objects):
        if obj.type in {"CAMERA", "LIGHT"}:
            bpy.data.objects.remove(obj, do_unlink=True)
        elif obj.type == "ARMATURE":
            obj.hide_render = True
        elif obj.type == "MESH" and obj not in meshes:
            obj.hide_render = True

    material = bpy.data.materials.get("Inspection chitin") or bpy.data.materials.new(
        "Inspection chitin"
    )
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    principled.inputs["Base Color"].default_value = (0.18, 0.025, 0.008, 1)
    principled.inputs["Metallic"].default_value = 0.05
    principled.inputs["Roughness"].default_value = 0.3
    for obj in meshes:
        obj.data.materials.clear()
        obj.data.materials.append(material)

    camera_data = bpy.data.cameras.new("Inspection camera")
    camera = bpy.data.objects.new("Inspection camera", camera_data)
    bpy.context.collection.objects.link(camera)
    bpy.context.scene.camera = camera
    camera_data.lens = 58

    key_data = bpy.data.lights.new("Inspection key", "AREA")
    key_data.energy = 900
    key_data.shape = "DISK"
    key_data.size = radius * 1.6
    key = bpy.data.objects.new("Inspection key", key_data)
    bpy.context.collection.objects.link(key)
    key.location = center + Vector((-radius, radius, radius * 1.6))
    look_at(key, center)

    fill_data = bpy.data.lights.new("Inspection fill", "AREA")
    fill_data.energy = 520
    fill_data.size = radius * 1.2
    fill = bpy.data.objects.new("Inspection fill", fill_data)
    bpy.context.collection.objects.link(fill)
    fill.location = center + Vector((radius, -radius * 0.7, radius))
    look_at(fill, center)

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.display.shading.cavity_type = "WORLD"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world.color = (0.025, 0.025, 0.025)

    views = {
        "front": center + Vector((0, radius, radius * 0.18)),
        "side": center + Vector((-radius, 0, radius * 0.18)),
        "top": center + Vector((0, 0, radius)),
    }
    for name, position in views.items():
        camera.location = position
        look_at(camera, center)
        scene.render.filepath = str(output / f"{prefix}-{name}.png")
        bpy.ops.render.render(write_still=True)
        print(f"CANOPY_KIN_ANT_INSPECTION_RENDER {scene.render.filepath}")


if __name__ == "__main__":
    main()
