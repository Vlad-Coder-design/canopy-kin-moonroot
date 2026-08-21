# World asset implementation record — 0.8.0

Date: 2026-08-21

This record covers changes made to the actual `Moonroot` gameplay scene. The QA
modes instantiate the same runtime world, player, materials, colliders, mission
logic and lighting as the distributable builds; no separate screenshot scene is
used.

## Visible replacements

- Replaced identical brood placeholders with separate egg, larva and pupa
  meshes, varied scale/orientation and a readable nursery layout.
- Replaced identical resource blobs with seed, resin and protein cargo meshes;
  workers still physically collect, carry and deposit the corresponding cargo.
- Added compacted-soil chamber material, chamber berms, embedded structural
  roots and a continuous curved tunnel collar to the playable underground nest.
- Rebuilt procedural root tubes with parallel-transport frames, removing the
  twists and spikes visible at bends in the previous build.
- Replaced the grass-only surface wall with four-species transparent botanical
  groundcover, reduced uniform grass density and retained a clear ant route.
- Replaced faceted black stones with shared-normal weathered meshes and a
  dedicated mineral albedo.
- Replaced the flat olive map boundary with an opaque, fog-integrated forest
  panorama behind real terrain, vegetation and collidable landmarks.
- Replaced floating imported root-network placement with continuous terrain-
  following branch and feeder-root paths.

## Windows verification

- Clean Unity 6000.0.78f1 build: `CANOPY_KIN_WINDOWS_BUILD_OK`, 769,175,495
  bytes reported by Unity.
- Mission smoke: step 15 reached, four soldiers active and save/load round-trip
  succeeded (`MOONROOT_MISSION_FLOW_SMOKE_OK`).
- Traversal smoke: 30/30 terrain samples hit, three hero-root colliders and both
  soil/water movement surfaces found, 0.924 m local height range and 2.377 m
  controller progress (`MOONROOT_ENVIRONMENT_TRAVERSAL_OK`).
- Runtime tested on NVIDIA GeForce RTX 3050 Laptop GPU / Direct3D 11.
- Twenty-second hidden-player sample: 8,687 frames, 434.28 average FPS,
  2.303 ms mean frame interval, 3.859 ms p95, 136,477,751 allocated bytes and
  227,950,592 reserved bytes. GPU timing, batches and triangle counters were not
  available from this hidden player and remain explicitly unmeasured.
- Evidence logs: `QA/Logs/windows-mission-080.log`,
  `QA/Logs/windows-traversal-080.log`, and
  `QA/Logs/windows-profile-080.log`.
- Surface evidence: `QA/Screenshots/environment-080-*.png`.
- Colony evidence: `QA/Screenshots/world-080-*.png`.

## WebGL verification

- Production build marker: `CANOPY_KIN_WEBGL_BUILD_OK`, 83,715,602 bytes
  reported by Unity; committed payload is 83,715,794 bytes across seven files,
  with a 75,507,757-byte largest file.
- Local HTTP clean load reached the real underground mission in the in-app
  Chromium WebGL 2 canvas with no Unity, shader, missing-asset or memory error.
- Repeated focused `W` input moved the ant to the physical exit and `E`
  transitioned to the forest surface. A first run exposed a camera collision
  against the mound; the exit orientation was corrected, rebuilt and retested.
  The accepted run shows the player, squad, entrance and forest immediately
  after transition instead of a camera-inside-soil frame.
- The in-app browser reports `WrongDocumentError` when Unity asks this embedded
  document for pointer lock. Keyboard gameplay and transitions continue; this is
  retained as a browser-host limitation, not hidden as a successful pointer-lock
  test.

## Distribution

- Windows archive: `Releases/Canopy-Kin-Moonroot-0.8.0-Windows-x64.zip`
- Archive size: 635,264,060 bytes
- SHA-256: `1DDE4144E0D76211B5AAC99F4421E6EF54133EB753E7D6FAC94C99CD511E0F17`

## Honest limitations after this milestone

- This is a visibly improved vertical slice, not an AAA-equivalent final asset
  pass. Procedural roots and the cave shell remain less detailed than the
  photogrammetry assets, and several distant objects repeat.
- The panoramic forest is a distant LOD plate. It provides continuous depth but
  is not traversable geometry; all reachable foreground remains real geometry.
- The player has 24 authored/derived gameplay clips, while non-player ant castes
  currently use the optimized shared procedural animation driver.
- The available verification GPU is an RTX 3050 Laptop, not the requested RTX
  3060. A controlled 1920×1080 GPU benchmark is therefore still outstanding.
