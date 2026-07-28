import json
import math
import os

import bpy
from mathutils import Vector


ROOT = os.getcwd()
SOURCE = os.path.join(
    ROOT,
    "ArtSource",
    "ThirdParty",
    "Sketchfab",
    "FishingSpider",
    "Raw",
    "source",
    "OKINAWA193-W01-1all-3.gltf",
)
OUTPUT_DIR = os.path.join(
    ROOT,
    "ArtSource",
    "ThirdParty",
    "Sketchfab",
    "FishingSpider",
)


def world_bounds(objects):
    points = []
    for obj in objects:
        points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    minimum = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    maximum = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return minimum, maximum


def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SOURCE)

meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
details = []
for obj in meshes:
    polygon_count = len(obj.data.polygons)
    vertex_count = len(obj.data.vertices)
    details.append(
        {
            "name": obj.name,
            "vertices": vertex_count,
            "triangles": sum(max(1, len(poly.vertices) - 2) for poly in obj.data.polygons),
            "materials": [slot.material.name if slot.material else None for slot in obj.material_slots],
            "dimensions": list(obj.dimensions),
            "location": list(obj.location),
        }
    )

spider = max(meshes, key=lambda obj: len(obj.data.polygons))
for obj in list(bpy.context.scene.objects):
    if obj != spider and obj.type in {"MESH", "CAMERA", "LIGHT"}:
        bpy.data.objects.remove(obj, do_unlink=True)

spider.rotation_euler = (0, 0, 0)
minimum, maximum = world_bounds([spider])
center = (minimum + maximum) * 0.5
size = maximum - minimum

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1200
scene.render.resolution_y = 800
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
if scene.world is None:
    scene.world = bpy.data.worlds.new("Inspection World")
scene.world.color = (0.025, 0.032, 0.024)

bpy.ops.mesh.primitive_plane_add(size=max(size.x, size.y, size.z) * 5.0)
ground = bpy.context.object
ground.location = (center.x, center.y, minimum.z - size.z * 0.025)
ground_material = bpy.data.materials.new("Neutral inspection ground")
ground_material.diffuse_color = (0.12, 0.095, 0.065, 1.0)
ground.data.materials.append(ground_material)

bpy.ops.object.light_add(type="AREA")
key = bpy.context.object
key.data.energy = 1200
key.data.shape = "DISK"
key.data.size = max(size) * 2.0
key.location = center + Vector((size.x * 1.2, -size.y * 1.4, size.z * 3.0))
point_at(key, center)

bpy.ops.object.light_add(type="AREA")
fill = bpy.context.object
fill.data.energy = 700
fill.data.size = max(size) * 2.5
fill.location = center + Vector((-size.x * 1.6, size.y * 0.8, size.z * 1.4))
point_at(fill, center)

bpy.ops.object.camera_add()
camera = bpy.context.object
camera.data.lens = 62
camera.location = center + Vector((size.x * 1.45, -size.y * 1.65, size.z * 1.2))
point_at(camera, center)
scene.camera = camera

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
    "selected": spider.name,
    "bounds_min": list(minimum),
    "bounds_max": list(maximum),
    "size": list(size),
}
world_vertices = [spider.matrix_world @ vertex.co for vertex in spider.data.vertices]
quantile_indices = [int((len(world_vertices) - 1) * percentile) for percentile in (0.001, 0.01, 0.05, 0.5, 0.95, 0.99, 0.999)]
report["vertex_quantiles"] = {
    axis_name: [sorted(point[axis_index] for point in world_vertices)[index] for index in quantile_indices]
    for axis_index, axis_name in enumerate(("x", "y", "z"))
}
with open(os.path.join(OUTPUT_DIR, "inspection.json"), "w", encoding="utf-8") as handle:
    json.dump(report, handle, indent=2, ensure_ascii=False)

print("CANOPY_KIN_SPIDER_INSPECTION", json.dumps(report, ensure_ascii=False))
