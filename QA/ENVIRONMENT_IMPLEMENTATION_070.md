# Environment implementation record — 0.7.0

Date: 2026-08-21

This record covers the production game scene and builds. The QA modes reuse the
actual mission bootstrap, player ant, camera, lighting, terrain, vegetation,
collision and material code; they do not load a separate mock scene.

## Implemented

- Replaced the uniform surface around `(9.1, 16.1)` with a 12×10 m high-density
  playable microhabitat containing layered displaced soil, leaf litter, moss,
  wetness and an irregular physical puddle.
- Added three collidable feeder roots, eight faceted stones, eight moss cushions,
  27 individually curled/damaged leaves and 58 close-detail grass tufts.
- Added independent wind response, reactive near-player vegetation and a camera-
  to-player visibility corridor so tall blades do not hide the playable ant.
- Reused the new root bark, stone, grass, leaf and water standards throughout the
  mission map while retaining Windows/WebGL-specific density and LOD profiles.
- Increased full-quality terrain density and retained chunked vegetation, shared
  materials, LODs, texture platform overrides and the optimized WebGL profile.

## Verified Windows build

- Build marker: `CANOPY_KIN_WINDOWS_BUILD_OK`
- Build size reported by Unity: 760,750,451 bytes
- Runtime GPU used for testing: NVIDIA GeForce RTX 3050 Laptop GPU, 3,964 MB VRAM
- Runtime engine: Unity 6000.0.78f1, Direct3D 11
- Environment construction: 1,367 renderers, 106 vegetation chunks, 760 broad-map
  tufts, 395,200 high-LOD and 22,800 low-LOD grass triangles
- Traversal smoke: 30/30 sampled rays reached physical layered soil, three feeder
  roots had mesh colliders, soil and shallow-water movement surfaces were active,
  local height range was 0.924 m and the controller advanced 2.392 m along the
  central route (`MOONROOT_ENVIRONMENT_TRAVERSAL_OK`).
- Mission smoke: all 15 mission steps completed, four soldiers active, save/load
  round-trip successful (`MOONROOT_MISSION_FLOW_SMOKE_OK`).
- Contact video: 90 actual gameplay frames at 15 fps, encoded to
  `QA/Videos/environment-070-contact.mp4`.

## Performance sample

The hidden automated Windows run sampled 13,404 frames over 20 seconds: average
CPU frame interval 1.492 ms and p95 2.145 ms, with 152,363,047 allocated bytes and
227,954,688 reserved bytes. Unity did not return valid GPU timing, batch or triangle
counters from this hidden player run (all were zero), so these figures are CPU and
memory evidence only and are not presented as a measured 1080p GPU benchmark.

## Verified WebGL build

- Unity build marker: `CANOPY_KIN_WEBGL_BUILD_OK`
- Unity-reported build size: 80,268,814 bytes; committed payload: 80,269,006
  bytes across seven files, with a 72,071,163-byte largest file.
- Local HTTP load reached the real underground mission in approximately 15
  seconds. Automated keyboard input moved the ant to the nest exit, `E` loaded
  the forest surface, and further movement input worked in the surface scene.
- No Unity, shader, asset, memory or missing-file warning/error appeared. The
  Codex in-app verification webview emitted one browser-level
  `WrongDocumentError` when Unity requested pointer lock; keyboard gameplay
  continued. This webview does not expose ordinary Chrome pointer lock, so the
  deployed URL must also be checked in a normal browser.

## Honest visual limitations

- The focused hero microhabitat is substantially more detailed than distant parts
  of the map. Large background tree silhouettes and some secondary props remain
  more repetitive and lower-detail than the close region.
- The puddle uses a lightweight real-time surface shader rather than planar or ray-
  traced reflections/refraction.
- Grass visibility fading prioritizes playability; blades near the camera-player
  sightline visibly thin out instead of using per-pixel temporal dither.
- A controlled RTX 3060 1920×1080 GPU benchmark is still required because the
  available verification computer contains an RTX 3050 Laptop GPU.
