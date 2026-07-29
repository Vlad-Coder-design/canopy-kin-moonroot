# Ant replacement acceptance — 0.5.0

Date: 2026-07-29
Unity: 6000.0.78f1

## Result

The former shared 0.4.1 ant presentation is no longer a visible runtime
fallback. Version 0.5.0 loads a dedicated skinned FBX for each important caste:

| Runtime caste | Production resource |
| --- | --- |
| Player | `Models/Ant/Family/CanopyKinAnt_Player` |
| Scout | `Models/Ant/Family/CanopyKinAnt_Scout` |
| Worker | `Models/Ant/Family/CanopyKinAnt_Worker` |
| Nurse | `Models/Ant/Family/CanopyKinAnt_Nurse` |
| Light soldier | `Models/Ant/Family/CanopyKinAnt_LightSoldier` |
| Heavy soldier | `Models/Ant/Family/CanopyKinAnt_HeavySoldier` |
| Queen | `Models/Ant/Family/CanopyKinAnt_Queen` |
| Rival | `Models/Ant/Family/CanopyKinAnt_Rival` |

Each close LOD is approximately 105K triangles and each distant LOD is 6,592
triangles. The family uses a common compatible rig while retaining role-specific
geometry, including distinct head, mandible, abdomen, thorax/pronotum and queen
wing-scar proportions.

## Source and material provenance

- Selected base: OpenGameArt, *Ant 3D Model + Rigging + Animated
  (Low-poly-ish)*, mujtaba-io, CC0 1.0.
- Original inspected file: 542 vertices, 1,080 triangles, one UV layer,
  38-bone armature, two actions and no usable packed texture.
- The source is retained at
  `ArtSource/ThirdParty/OpenGameArt/Ant/Raw/ant.blend`; SHA-256
  `6F4DE1451333964F8FFFFE6CA9D15AAF6EF01FD089A8185B0268BD0D032FB662`.
- Exact URL, license and modifications are in `THIRD_PARTY_ASSETS.md`.
- A project-specific tileable ant-cuticle source was generated with Codex's
  built-in image generation mode from a prompt requesting biologically
  plausible dark reddish-brown ant chitin, micro-pores and subtle scratches,
  without reptile scales, text or watermark. The retained source is
  `ArtSource/Textures/Ant/ant_cuticle_base_generated_20260729.png`; local tools
  derived 4K albedo, DirectX normal, roughness and AO maps.

## Animation and integration

All eight FBXs contain 13 imported clips: Idle, Walk, Run, StartMove, StopMove,
TurnLeft, TurnRight, Attack, Carry, Interact, Climb, Stagger and Death.

`AntVisual` drives a velocity-scaled alternating tripod gait and controls six
segmented legs, both three-segment antennae and both mandibles. Carrying,
interaction and combat states are read from the actual actors. Damage remains
timed by the combat system while visible mandible closure uses the same attack
phase. Existing gameplay objects retain their colliders, navigation, health,
squad commands, save identifiers and mission logic.

## Verification

### Build and asset contract

- `CANOPY_KIN_PRODUCTION_ASSETS_OK antCastes=8
  antCloseTriangles=839320 antDistantTriangles=52736 antClips=104`
- `CANOPY_KIN_VALIDATION_OK`
- Windows x64: passed, 780,559,607 bytes in the final build report.
- WebGL Optimized: passed, 89,732,654 bytes in the build manifest.
- Windows archive:
  `Releases/CanopyKin-Moonroot-Windows-x64-v0.5.0.zip`,
  638,069,609 bytes, SHA-256
  `5C52CA2FCB6A4CC79563D249AD0BF11293E2D08A995A5A03F14DE1C4FBD2E2AE`.
- The archive was extracted into a new clean directory and its copied
  executable independently passed the complete mission/save-load smoke:
  `MOONROOT_MISSION_FLOW_SMOKE_OK finalStep=15 activeSoldiers=4
  saveLoad=True`.

### Built Windows executable

- Visual QA in the real mission scene passed:
  `MOONROOT_ANT_VISUAL_QA_OK screenshots=8 playerState=Idle queen=Queen
  workers=3`.
- The player was inspected from front, close side and rear, plus on uneven
  ground. Workers, both soldier weights, carrying workers, the queen chamber
  and the player bite pose were captured from the same playable scene.
- Fifteen-stage mission/save-load smoke passed:
  `MOONROOT_MISSION_FLOW_SMOKE_OK finalStep=15 activeSoldiers=4 saveLoad=True`.
  Rival ants also instantiated their dedicated production model during the raid.
- Player versus production beetle passed after a real telegraphed enemy attack:
  `MOONROOT_BEETLE_COMBAT_SMOKE_OK elapsed=2.6 damageEvents=5
  attackEvents=1 hits=1 mission=6`.
- Soldier squad versus production spider passed after a real telegraphed
  predator attack:
  `MOONROOT_SPIDER_COMBAT_SMOKE_OK elapsed=7.0 damageEvents=23
  attackEvents=1 hits=1 mission=8`.
- Pause/resume, quick-save/quick-load and the full state restore are exercised
  by the runtime smoke harness. Hardware-input automation for the standalone
  executable is limited by Unity Raw Input, so ordinary keyboard movement was
  visually checked at launch but not measured by the automated driver.

### Local WebGL

- A fresh in-app Chromium tab loaded the hash-named gzip build through a plain
  static server using Unity's JavaScript decompression fallback.
- The nursery HUD and production player/worker family rendered; canvas focus,
  forward input and vault input changed the live gameplay state.
- Unity logged every required visible caste used at the opening with six legs
  and both LODs. No Unity exception, missing shader, pink material or missing
  resource error appeared.
- The only browser console issue was a Chromium automation
  `UnknownError` emitted while synthesizing canvas input; it is not a Unity
  runtime exception and did not stop input or rendering.
- Twenty-second WebGL profile: 60.00 FPS, 16.67 ms average, 18.0 ms p95,
  84 batches, 51 SetPass calls, approximately 101K triangles, 156.0 MB
  allocated / 166.7 MB reserved and 0 B/frame GC.

### Public GitHub Pages

- Commit `c50143b` deployed successfully through GitHub Actions run
  `30441042200`.
- A new browser tab opened the real cache-busted URL
  `https://vlad-coder-design.github.io/canopy-kin-moonroot/?build=c50143b`.
- A no-cache request to the public manifest returned version 0.5.0, WebGL
  Optimized, 89,732,654 build bytes and Unity 6000.0.78f1.
- The public 83,282,967-byte data payload returned HTTP 200 and
  `application/vnd.unity`. GitHub Pages does not add `Content-Encoding`, so
  the verified Unity JavaScript decompression fallback is required and worked.
- The live page reached the nursery, rendered the production player and opening
  castes, accepted forward/vault input, opened pause, and resumed gameplay.
- There were no Unity exceptions, null references, missing resources or shader
  errors. The browser automation layer emitted two generic Chromium
  `UnknownError` messages while synthesizing canvas input; rendering and input
  continued, and those messages contain no Unity stack.
- Public 20-second browser profile: 39.35 FPS, 25.41 ms average, 43.0 ms p95,
  83 batches, 50 SetPass calls, approximately 100K triangles, 156.0 MB
  allocated / 166.7 MB reserved and 0 B/frame GC. This is materially below the
  local 60 FPS sample and is reported as measured, not normalized away.

## Evidence images

- Neutral Blender reference: `QA/Screenshots/ant-050-player-front.png`,
  `ant-050-player-side.png`, `ant-050-player-top.png`,
  `ant-050-queen-front.png`, `ant-050-queen-side.png` and
  `ant-050-queen-top.png`.
- Actual Windows scene: `ant-050-windows-player-front.png`,
  `ant-050-windows-player-side-close.png`,
  `ant-050-windows-player-rear.png`,
  `ant-050-windows-player-uneven-ground.png`,
  `ant-050-windows-worker-soldiers.png`,
  `ant-050-windows-workers-carrying.png`,
  `ant-050-windows-queen-chamber.png` and
  `ant-050-windows-player-bite.png`.
- Actual WebGL scene: `ant-050-webgl-local-gameplay.png`.
- Public WebGL scene: `ant-050-webgl-public-c50143b.png`,
  `ant-050-webgl-public-c50143b-pause.png` and
  `ant-050-webgl-public-c50143b-resumed.png`.

The before state is documented in `ANT_REPLACEMENT_AUDIT_0.5.0.md` and in the
0.4.1 movement screenshots. That build used one 36,713-triangle analytic
ellipsoid/tube mesh for every caste; the queen was a scaled heavy soldier.

## Measured Windows performance

The rendered 1920×1080 Full-quality visual-QA scene on an RTX 3050 Laptop
averaged approximately 38.33 FPS with 33.35 ms p95 frame time, 695 batches,
288 SetPass calls, 4.49M triangles, 211.5 MB allocated and 293 MB reserved.
This does not meet the requested 60 FPS target and must not be represented as
meeting it. The measured GPU is also below the requested RTX 3060 target.

## Honest remaining limitations

- The free CC0 source is a low-poly rigging base; the close meshes, morphologies,
  materials and clips are a substantial rebuild, not a purchased scan-quality
  ant asset.
- Runtime presentation uses the imported rig plus a procedural bone driver. It
  does not yet use a hand-polished Mecanim blend tree with animation events for
  every transition.
- Body slope alignment is implemented, but full per-foot terrain raycast IK is
  not. Small foot penetration or sliding can still appear on abrupt terrain.
- Antenna motion is independent and state-aware, but does not yet raycast
  against every nearby surface.
- The 105K close LOD is deliberately expensive and the Windows Full-quality
  profile requires further GPU/SetPass optimization to reach 60 FPS on the
  measured laptop.
- Automated state-flow and encounter tests cover the complete mission logic,
  but a separately timed 15–25 minute human playthrough on an unrelated clean
  PC remains outstanding.
