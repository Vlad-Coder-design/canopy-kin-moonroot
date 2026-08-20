# Opaque/upright ant acceptance — 0.5.1

Date: 2026-07-29
Unity: 6000.0.78f1

## Root-cause diagnosis

The rejected 0.5.0 result had more than one independent defect:

1. Blender topology audit found 124 LOD0 and 12 LOD1 faces whose normals were
   reversed by an outside-normal recalculation. Back-face culling made those
   faces disappear and exposed the environment through the shell.
2. Runtime LODs used dithered animated cross-fade although the ant material
   path did not implement a controlled ant-specific fade. The transition
   pattern punched visible screen-door holes through the dark body.
3. Terrain alignment calculated a world-space `FromToRotation` and assigned it
   directly as a local rotation below a yawed gameplay root. The coordinate
   mismatch could roll the visual sideways.
4. NPC ground probing could select the NPC's own gameplay collider. A side
   normal from that collider could be interpreted as terrain.
5. The old imported family required a repeated 180-degree model correction,
   making the source/runtime forward convention ambiguous.

Changing colour, lighting or culling alone would not correct all five causes.

## Selected model and license

- Selected source: *Game-Ready Worker Ant Model*
- Creator: Msassasa (`@LilCick`)
- Page:
  https://sketchfab.com/3d-models/game-ready-worker-ant-model-b48893d316cc4b2f98ec1d1e37027e6a
- License shown on the source page: CC BY 4.0
- License text: https://creativecommons.org/licenses/by/4.0/
- Creator-provided Blender source:
  https://drive.google.com/file/d/1FZ0JOgfEm71EQQW4SXqalfPg29DdLfIp/view
- Original SHA-256:
  `63EF209CAF1A59C95D79C551E46580F4D098E1ED53E69C10FD33091C489F14CF`

The linked *Ant Rig VFX* candidate by rayray was also verified as downloadable
under CC BY 4.0. Msassasa's model was selected because the actual downloadable
source provides a stronger close-camera worker mesh, a 52-bone armature and a
large authored action library. Full attribution and modifications are recorded
in `THIRD_PARTY_ASSETS.md`.

## Geometry and integration result

- The source's 53 rigged anatomical parts were baked into a single skinned
  mesh at identity transform.
- Nine source segment openings were closed. Every face was triangulated before
  FBX export and all face normals were recalculated outward.
- Blender audit of the player found zero boundary, loose or non-manifold edges,
  zero reversed faces, positive signed volume and determinant `+1`.
- Every caste has a 47,090-triangle source LOD0 and an 11,772-triangle source
  LOD1. Unity's importer retains 47,086 / 11,772 triangles per caste.
- Player, scout, worker, nurse, light soldier, heavy soldier, queen and rival
  each retain two skinned LODs, 52-bone anatomy and 13 gameplay clips.
- FBX axes are baked as Unity `+Y` up and `+Z` forward. Runtime model local
  rotation is identity; the old 180-degree correction is gone.
- Runtime material setup forces alpha 1, opaque render type/queue, depth write,
  one/zero blend factors and back-face culling, while disabling alpha test,
  alpha blend, premultiply and transparent keywords.
- LOD switching is solid (`LODFadeMode.None`) instead of dithered.
- Ground probes reject wall-like normals and an NPC's own colliders. The
  desired world-space slope orientation is converted through the gameplay
  parent's rotation before being assigned locally.
- Build-time and runtime validation reject missing anatomy, backwards forward
  axes, feet-above-thorax orientation or a missing production LOD.

## Windows verification

Production contract:

`CANOPY_KIN_PRODUCTION_ASSETS_OK antCastes=8
antCloseTriangles=376688 antDistantTriangles=94176 antClips=104`

Visible 1600×900 Direct3D 11 run on an RTX 3050 Laptop GPU:

- `MOONROOT_ANT_VISUAL_QA_OK screenshots=10 playerState=Idle queen=Queen
  workers=3`
- Every instantiated caste logged six legs, both LODs, `forwardDot=0.995` and
  `opaque=1`.
- Front, rear, close side, top, uneven-ground and low bright-background views
  were inspected. No shell holes, screen-door fade, inverted body or sideways
  gameplay root was visible.
- Workers, both soldier weights, carrying workers, queen chamber and the live
  player/beetle bite arrangement were also captured in the actual mission.
- Complete mission/state smoke passed:
  `MOONROOT_MISSION_FLOW_SMOKE_OK finalStep=15 activeSoldiers=4 saveLoad=True`.

Measured rendered performance:

- 54.15 FPS average / 18.47 ms average / 33.34 ms p95
- CPU 18.45 ms, GPU 17.60 ms
- 634 batches, 279 SetPass calls, approximately 2.87M triangles
- 0 B/frame GC, 186.7 MB allocated / 276.2 MB reserved

This is a measured High-profile laptop result, not a claim of 60 FPS on the
different requested RTX 3060 hardware.

## Local WebGL verification

- WebGL Optimized build passed: 75,371,459 bytes.
- A new in-app Chromium tab loaded the hash-named gzip build through a plain
  static server and reached the playable nursery.
- The production player, queen, nurses and workers rendered upright. Runtime
  logs again reported six legs, both LODs, `forwardDot=0.995` and `opaque=1`.
- Canvas focus plus `W` and `Space` input were accepted and changed live
  gameplay state.
- No Unity exception, null reference, missing resource, shader or material
  error appeared. Chromium emitted one generic automation `UnknownError` while
  synthesizing input; it did not come from Unity and rendering continued.
- 20-second profile: 59.85 FPS, 16.71 ms average, 19.0 ms p95, 60 batches,
  51 SetPass calls, approximately 164K triangles, 131.3 MB allocated /
  143.1 MB reserved and 0 B/frame GC.

## Evidence

- Actual bright-background opacity proof:
  `QA/Screenshots/ant-051-windows-player-bright-background.png`
- Actual close side:
  `QA/Screenshots/ant-051-windows-player-side-close.png`
- Actual top axis proof:
  `QA/Screenshots/ant-051-windows-player-top.png`
- Actual uneven-ground pose:
  `QA/Screenshots/ant-051-windows-player-uneven-ground.png`
- Actual worker/soldier castes:
  `QA/Screenshots/ant-051-windows-worker-soldiers.png`
- Actual queen:
  `QA/Screenshots/ant-051-windows-queen-chamber.png`
- Actual combat placement:
  `QA/Screenshots/ant-051-windows-player-bite.png`
- Actual local WebGL gameplay:
  `QA/Screenshots/ant-051-webgl-local-gameplay.png`

The rejected 0.5.0 actual-game frames remain in `QA/Screenshots` for comparison.

## Remaining limitations

- The runtime bone driver is responsive and state-aware but is not yet a
  hand-polished Mecanim blend tree using all of the source author's actions.
- Whole-body slope alignment is corrected; full six-foot terrain raycast IK is
  not implemented, so abrupt terrain can still show small foot penetration.
- Antennae react independently but do not yet perform physical contact probing.
- The measured RTX 3050 Laptop run averaged 54.15 FPS rather than the requested
  60 FPS target. GPU/SetPass optimization remains useful.
- A separately timed human 15–25 minute playthrough on an unrelated clean PC
  remains outstanding; the complete automated mission, combat and save state
  paths pass on this machine.
