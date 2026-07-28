"""Render and report the downloaded CC0 rhinoceros-beetle scan."""

import json
import os

import bpy
from mathutils import Vector


ROOT = os.getcwd()
SOURCE = os.path.join(
    ROOT,
    "ArtSource",
    "ThirdParty",
    "Sketchfab",
    "RhinocerosBeetle",
    "Raw",
    "source",
    "IKINOSHIMA001-W07-1all-1.gltf",
)
OUTPUT_DIR = os.path.join(
    ROOT, "ArtSource", "ThirdParty", "Sketchfab", "RhinocerosBeetle"
)


def bounds(obj):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    low = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
    high = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
    return low, high


def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SOURCE)
meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
details = [
    {
        "name": obj.name,
        "vertices": len(obj.data.vertices),
        "triangles": sum(max(1, len(poly.vertices) - 2) for poly in obj.data.polygons),
        "materials": [slot.material.name if slot.material else None for slot in obj.material_slots],
        "dimensions": list(obj.dimensions),
        "location": list(obj.location),
        "parent": obj.parent.name if obj.parent else None,
    }
    for obj in meshes
]
beetle = max(meshes, key=lambda obj: len(obj.data.polygons))
beetle.rotation_euler = (0, 0, 0)
for obj in list(bpy.context.scene.objects):
    if obj != beetle and obj.type in {"MESH", "CAMERA", "LIGHT"}:
        bpy.data.objects.remove(obj, do_unlink=True)

low, high = bounds(beetle)
center = (low + high) * .5
size = high - low

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1200
scene.render.resolution_y = 800
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.world = scene.world or bpy.data.worlds.new("Inspection World")
scene.world.color = (.025, .032, .024)

bpy.ops.mesh.primitive_plane_add(size=max(size) * 5)
ground = bpy.context.object
ground.location = (center.x, center.y, low.z - size.z * .02)
ground_material = bpy.data.materials.new("Neutral inspection ground")
ground_material.diffuse_color = (.12, .095, .065, 1)
ground.data.materials.append(ground_material)

for location, energy, area in (
    (center + Vector((size.x * 1.2, -size.y * 1.4, size.z * 3)), 1400, max(size) * 2),
    (center + Vector((-size.x * 1.5, size.y, size.z * 1.4)), 800, max(size) * 2.5),
):
    bpy.ops.object.light_add(type="AREA")
    light = bpy.context.object
    light.data.energy = energy
    light.data.size = area
    light.location = location
    point_at(light, center)

bpy.ops.object.camera_add()
camera = bpy.context.object
camera.data.lens = 62
scene.camera = camera

camera.location = center + Vector((size.x * 1.45, -size.y * 1.65, size.z * 1.25))
point_at(camera, center)
scene.render.filepath = os.path.join(OUTPUT_DIR, "inspection.png")
bpy.ops.render.render(write_still=True)

camera.data.type = "ORTHO"
camera.data.ortho_scale = max(size.x, size.y) * 1.15
camera.location = center + Vector((0, 0, max(size) * 1.8))
point_at(camera, center)
scene.render.filepath = os.path.join(OUTPUT_DIR, "inspection_top.png")
bpy.ops.render.render(write_still=True)

camera.data.ortho_scale = max(size.x, size.z) * 1.15
camera.location = center + Vector((0, -max(size) * 1.8, size.z * .1))
point_at(camera, center)
scene.render.filepath = os.path.join(OUTPUT_DIR, "inspection_side.png")
bpy.ops.render.render(write_still=True)

report = {
    "source": SOURCE,
    "objects": details,
    "selected": beetle.name,
    "bounds_min": list(low),
    "bounds_max": list(high),
    "size": list(size),
}
with open(os.path.join(OUTPUT_DIR, "inspection.json"), "w", encoding="utf-8") as handle:
    json.dump(report, handle, indent=2, ensure_ascii=False)
print("CANOPY_KIN_BEETLE_INSPECTION", json.dumps(report, ensure_ascii=False))
