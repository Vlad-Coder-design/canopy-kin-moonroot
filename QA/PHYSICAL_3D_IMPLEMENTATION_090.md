# Canopy Kin: Moonroot 0.9.0 implementation record

Date: 2026-08-23

## Implemented

- Removed the 0.8.0 photographic forest cylinder, its PNG and dedicated shader.
- Removed all grass, groundcover and fallen-leaf atlas images and their
  dedicated image-card shaders.
- Added opaque closed grass, groundcover, fallen-leaf and individual canopy-leaf
  meshes with full and reduced LODs.
- Added layered 3D forest enclosure with real trunks, branches, buttress roots,
  terrain ridges, stones and understory.
- Rebuilt the underground level as five connected modeled chambers and four
  curved tunnels with collidable floors, walls and ceilings.
- Corrected the central-to-queen portal orientation, narrowed portal cuts to
  the tunnel cross-section, made chamber floors closed/thick, and wound tunnel
  wall triangles toward the playable interior for correct lighting/collision.
- Added camera sphere collision, surface/nest containment and transition
  correction.
- Corrected the camera against the rendered terrain rather than only the
  analytic height function, added penetration recovery for non-convex roots and
  stones, and added an elevated shoulder fallback so a compressed camera boom
  cannot enter the visible ant body.
- Added runtime photographic-background/transparent-vegetation rejection,
  geometry-count audit, runtime wireframe captures, camera containment smoke and
  a surface/nest/exit walkthrough capture mode.
- Fixed fractional-power endpoint precision that produced non-finite vegetation
  vertices; mesh construction now fails immediately if any vertex is non-finite.

## Required release evidence

The final release record must include these fresh 0.9.0 artifacts generated from
the rebuilt Windows player:

- `QA/Screenshots/physical-090-forest-no-photo-wall.png`
- `QA/Screenshots/physical-090-forest-wireframe.png`
- `QA/Screenshots/physical-090-solid-grass-wireframe.png`
- `QA/Screenshots/physical-090-solid-plants-wireframe.png`
- `QA/Screenshots/physical-090-tree-roots-wireframe.png`
- `QA/Screenshots/physical-090-queen-chamber-gameplay.png`
- `QA/Screenshots/physical-090-queen-chamber-wireframe.png`
- `QA/Screenshots/physical-090-nest-tunnel-wireframe.png`
- `QA/Screenshots/physical-090-resources-brood-wireframe.png`
- `QA/Video/physical-090-walkthrough.mp4`

## Final verification

- Unity validation: `CANOPY_KIN_VALIDATION_OK`.
- Windows x64 build: `CANOPY_KIN_WINDOWS_BUILD_OK`, 760,746,087 bytes reported
  by Unity; 760,747,117 bytes including manifest and README.
- Windows ZIP: `Releases/Canopy-Kin-Moonroot-0.9.0-Windows-x64.zip`,
  629,931,986 bytes.
- Physical-world audit: `backdrops=0`, `modeledTrees=44`,
  `solidGrassMeshes=164`, `solidLeafMeshes=468`,
  `transparentVegetation=0`, `chamberShells=5`, `tunnelShells=4`.
- Camera containment: 61 samples, 0 failures, 0 solid overlaps and 0 samples
  closer than the safe visual distance.
- Traversal: 30/30 terrain samples, three root colliders, soil and puddle
  surfaces, 0.924 m displacement and 2.379 m route progress.
- Mission flow: final step 15, four active soldiers, save/load passed.
- Windows High profile on the available NVIDIA RTX 3050 Laptop GPU:
  59.94 average FPS, 16.68 ms average frame, 16.77 ms p95, 134,112,767 bytes
  allocated and 194,064,384 bytes reserved. Standalone runtime stats did not
  expose reliable batch/triangle counters, so those values are not claimed.
- WebGL build: `CANOPY_KIN_WEBGL_BUILD_OK`, 80,179,429 bytes reported by Unity;
  largest deployed file 72,620,358 bytes.
- Local clean browser smoke: WebGL 2 and PhysX initialized, the actual mission
  world reported `MOONROOT_SLICE_READY`, the loading shell hid, the responsive
  1600x900 canvas rendered inside a 1280x720 viewport, and no console errors
  were present. The WebGL sample averaged 56.56 FPS with 153,360 average
  triangles on the browser test surface.
- The Unity 6 decompression-fallback path can start the runtime before its
  JavaScript promise resolves. The loader now treats the real
  `MOONROOT_SLICE_READY` runtime marker as authoritative, preventing an infinite
  `Starting colony simulation...` overlay.
- The first speed-optimized WebGL link crashed Unity Binaryen `wasm-opt` with
  Windows status 0xC0000409. The WebGL-only IL2CPP preset now uses
  `OptimizeSize`; the retry completed successfully without changing Windows
  assets or quality.

## Honest limitations

- Environment and nest meshes are project-authored procedural geometry, not
  photogrammetry scans. They are physically modeled and inspectable but are not
  film-quality botanical scans.
- Runtime wireframe captures demonstrate mesh topology in the shipped scene;
  they are not editor Scene View captures.
- The walkthrough is scripted QA traversal of the actual runtime player and its
  collision-resolved gameplay camera, not a recording of human keyboard input.
- WebGL uses lower counts and LOD thresholds than the Windows High preset to fit
  browser memory and GPU constraints; the mission and physical geometry types
  remain the same.
