"""Build Canopy Kin's original macro-scale forest-root landmark.

The mesh is authored as irregular tapered sweeps with branching junctions,
elliptical ground contact and continuous UVs.  It deliberately avoids visible
Unity cylinders while remaining inexpensive enough to instance in Windows and
WebGL editions.
"""

from __future__ import annotations

import math
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT = Path(__file__).resolve().parents[1]
BLEND_PATH = PROJECT / "ArtSource" / "Environment" / "CanopyKinRootNetwork.blend"
FBX_PATH = (
    PROJECT
    / "Assets"
    / "Resources"
    / "Models"
    / "Environment"
    / "CanopyKinRootNetwork.fbx"
)


def clean_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.curves, bpy.data.materials):
        for item in list(block):
            if item.users == 0:
                block.remove(item)


def cubic(points: tuple[Vector, Vector, Vector, Vector], t: float) -> Vector:
    a, b, c, d = points
    one = 1.0 - t
    return (
        a * (one**3)
        + b * (3.0 * one * one * t)
        + c * (3.0 * one * t * t)
        + d * (t**3)
    )


def cubic_tangent(points: tuple[Vector, Vector, Vector, Vector], t: float) -> Vector:
    a, b, c, d = points
    one = 1.0 - t
    return (
        (b - a) * (3.0 * one * one)
        + (c - b) * (6.0 * one * t)
        + (d - c) * (3.0 * t * t)
    ).normalized()


def build_root_mesh(name: str, sides: int, step_scale: float) -> bpy.types.Object:
    # Local Z is up.  The network is six metres long and intentionally
    # asymmetric so repeated rotations do not look like cloned cylinders.
    paths = [
        ((-3.1, 0.00, 0.14), (-1.8, 0.10, 0.54), (1.2, -0.16, 0.48), (3.25, 0.18, 0.10), 0.82, 0.19, 66),
        ((-1.05, 0.03, 0.24), (-0.20, 0.66, 0.48), (1.20, 1.70, 0.26), (2.65, 2.45, 0.045), 0.52, 0.08, 48),
        ((-0.82, -0.02, 0.24), (0.00, -0.72, 0.44), (1.05, -1.72, 0.22), (2.45, -2.35, 0.04), 0.46, 0.07, 46),
        ((0.35, -0.05, 0.28), (1.00, 0.62, 0.34), (2.15, 0.80, 0.17), (3.35, 1.18, 0.035), 0.34, 0.055, 40),
        ((-1.68, 0.02, 0.27), (-1.15, -0.74, 0.36), (-0.42, -1.54, 0.15), (0.25, -2.38, 0.03), 0.38, 0.055, 40),
        ((-2.28, 0.02, 0.20), (-2.10, 0.66, 0.28), (-1.62, 1.25, 0.13), (-0.82, 1.72, 0.025), 0.29, 0.04, 34),
        ((1.42, 0.00, 0.24), (1.85, -0.48, 0.29), (2.50, -0.86, 0.12), (3.20, -1.05, 0.025), 0.27, 0.04, 32),
        ((-0.24, 0.00, 0.36), (0.05, 0.12, 0.98), (0.28, -0.08, 1.32), (0.55, 0.04, 1.62), 0.58, 0.18, 34),
    ]

    vertices: list[tuple[float, float, float]] = []
    faces: list[tuple[int, int, int, int]] = []
    uvs: list[tuple[float, float]] = []
    face_uvs: list[tuple[int, int, int, int]] = []

    for path_index, raw in enumerate(paths):
        points = tuple(Vector(point) for point in raw[:4])
        radius_start, radius_end, base_steps = raw[4:]
        steps = max(9, int(base_steps * step_scale))
        start_vertex = len(vertices)
        previous_side = Vector((0.0, 1.0, 0.0))

        for ring in range(steps + 1):
            t = ring / steps
            center = cubic(points, t)
            tangent = cubic_tangent(points, t)
            side = tangent.cross(Vector((0.0, 0.0, 1.0)))
            if side.length_squared < 1e-5:
                side = previous_side
            side.normalize()
            previous_side = side
            up = side.cross(tangent).normalized()
            radius = radius_start * ((1.0 - t) ** 1.15) + radius_end * t
            # Roots flatten where they meet soil and become rounder on the
            # central buttress.  Two non-harmonic ripples break the tube shape.
            vertical_scale = 0.56 + 0.16 * math.exp(-((t - 0.28) * 3.2) ** 2)

            for radial in range(sides):
                angle = radial / sides * math.tau
                bark_ripple = (
                    1.0
                    + math.sin(angle * 5.0 + t * 19.0 + path_index * 1.7) * 0.045
                    + math.sin(angle * 11.0 - t * 31.0) * 0.018
                )
                radial_vector = (
                    side * math.cos(angle)
                    + up * math.sin(angle) * vertical_scale
                )
                point = center + radial_vector * radius * bark_ripple
                # Give buried lower vertices a soft contact shelf instead of a
                # perfect circular silhouette.
                point.z = max(-0.035, point.z)
                vertices.append(tuple(point))
                uvs.append((t * 3.25, radial / sides * 2.0))

        for ring in range(steps):
            for radial in range(sides):
                next_radial = (radial + 1) % sides
                a = start_vertex + ring * sides + radial
                b = start_vertex + ring * sides + next_radial
                c = start_vertex + (ring + 1) * sides + next_radial
                d = start_vertex + (ring + 1) * sides + radial
                faces.append((a, b, c, d))
                face_uvs.append((a, b, c, d))

    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    uv_layer = mesh.uv_layers.new(name="BarkUV")
    for polygon, indices in zip(mesh.polygons, face_uvs):
        for loop_index, vertex_index in zip(polygon.loop_indices, indices):
            uv_layer.data[loop_index].uv = uvs[vertex_index]
        polygon.use_smooth = True

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def export() -> None:
    clean_scene()
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0

    lod0 = build_root_mesh("RootNetwork_LOD0", sides=24, step_scale=1.0)
    lod1 = build_root_mesh("RootNetwork_LOD1", sides=10, step_scale=0.42)
    lod1.hide_render = False

    material = bpy.data.materials.new("CanopyKin Bark Material Slot")
    material.diffuse_color = (0.19, 0.105, 0.045, 1.0)
    lod0.data.materials.append(material)
    lod1.data.materials.append(material)

    BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)
    FBX_PATH.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))

    bpy.ops.object.select_all(action="DESELECT")
    lod0.select_set(True)
    lod1.select_set(True)
    bpy.context.view_layer.objects.active = lod0
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        object_types={"MESH"},
        apply_scale_options="FBX_SCALE_UNITS",
        apply_unit_scale=True,
        bake_space_transform=False,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        embed_textures=False,
    )

    lod0_triangles = sum(len(p.loop_indices) - 2 for p in lod0.data.polygons)
    lod1_triangles = sum(len(p.loop_indices) - 2 for p in lod1.data.polygons)
    print(
        "CANOPY_KIN_ROOT_NETWORK_OK "
        f"blend={BLEND_PATH} fbx={FBX_PATH} "
        f"lod0Triangles={lod0_triangles} lod1Triangles={lod1_triangles}"
    )


if __name__ == "__main__":
    export()
