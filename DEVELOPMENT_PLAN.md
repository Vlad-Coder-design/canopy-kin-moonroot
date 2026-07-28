# Vertical Slice Progress

## 0.4.0 production checkpoint

- Two independent runtime/build profiles now exist. Windows Full Quality retains
  8K source maps, Ultra settings, HDR, 8x MSAA, long shadows and dense scenery;
  WebGL uses independent 2K texture overrides and lower scalable density.
- The former runtime-built ant is replaced in normal operation by an original
  Blender-authored FBX: 20,534 vertices, 36,713 triangles, one skinned mesh,
  27 driven rig bones and nine imported clips (idle, walk, run, turn, attack,
  carry, climb, stagger and death). The old mesh is only an import-failure
  fallback.
- Poly Haven's 8K CC0 Forest Floor diffuse, DirectX normal, roughness, AO and
  displacement maps are integrated as the outdoor terrain surface. Packed
  underground soil keeps a separate material and lighting treatment.
- Poly Haven's 101,802-triangle Dead Tree Trunk scan and 4K PBR maps form the
  principal fallen-log landmark, barrier and traversal route. Its real mesh is
  used for collision; the build forces a synchronous readable import so an old
  Library cache cannot silently replace it with invalid collision.
- Three major surface-route silhouettes now use a project-authored branching
  root network instead of swept runtime tubes. Each instance has an irregular,
  UV-mapped 16,320-triangle close LOD, 2,780-triangle distant LOD, shared 4K
  Poly Haven bark PBR material and a low-detail static mesh collider. The long
  ridge, elevated shortcut and storm-branch fork use separate transforms.
- Dense grass retains 706 placed tufts but is grouped into 124 spatial high/low
  LOD chunks (56,480 close and 8,472 distant triangles), with shared materials,
  wind and shadow policy. This removes per-tuft Renderer overhead without
  thinning the authored forest.
- The project enforces a production-asset build contract so an accidentally
  missing rig, clip set, 8K Windows import, 2K WebGL override or landmark mesh
  fails the build.
- The Ashback predator now uses the CC0 fishing-spider scan by ffish.asia /
  floraZia.com instead of the procedural fallback: 111,999-triangle close LOD,
  29,999-triangle distant LOD, 28-bone rig, eight animation clips and the
  original high-resolution photographic atlas. Windows retains 8K while WebGL
  imports an independent 2K copy.
- The Barkshield encounter now uses the CC0 Japanese rhinoceros-beetle scan by
  ffish.asia / floraZia.com instead of the procedural fallback: 92,000-triangle
  close LOD, 24,000-triangle distant LOD, 23-bone rig, eight animation clips and
  the original high-resolution photographic atlas. Windows retains 8K while
  WebGL imports an independent 2K copy. The visual heading was corrected to
  agree with gameplay forward, so charging, damage-side tests and the horn all
  use the same direction.
- The Windows High profile uses separate underground/surface visibility
  partitions. Invisible vegetation and terrain are not submitted while the
  player is in the nest, without reducing visible scene density.
- Player-build telemetry measures frame time, GPU/CPU timing, batches, SetPass
  calls, triangles, memory and GC rather than relying on estimates.
- WebGL now uses gzip output with Unity's JavaScript decompression fallback, so
  GitHub Pages can serve the build without custom `Content-Encoding` headers.
  The responsive production loader reports real download progress, has a
  120-second failure watchdog, retry guidance and fullscreen support.
- WebGL managed stripping was reduced from High to Low after a release-only
  regression was reproduced: High removed runtime-reached Input System/UI paths
  and caused a per-frame `NullReferenceException`. Low retains useful stripping
  while preserving the reflection-driven runtime code.

## Gameplay already connected

- Third-person movement, orbit camera with collision, sprint, stamina, vault,
  interaction, bite combat, pause and pointer capture.
- Enterable underground colony with queen chamber, nursery, storage, worker and
  soldier areas, upgrade station, population and persistent resources.
- Workers physically navigate to resources, gather, carry cargo and deposit it.
- Worker/soldier commands: gather, attack, follow, defend, patrol, retreat and
  return to nest, with selection filters and stuck recovery.
- Beetle, rival ants and Ashback spider with different activation, pursuit,
  telegraphs, weak-point/reaction behavior, combat rewards and mission gates.
- Fifteen connected playable mission stages plus completion: queen briefing,
  nest exit, movement trail, worker command, physical delivery, beetle weak
  point, soldier unlock, spider hunt, territory hold, colony return, visible
  nursery upgrade, defence order, rival raid, overlook and cinematic threat
  reveal.
- Soldiers are genuinely inactive before the Barkshield reward; worker,
  soldier and defence command objectives advance only when the corresponding
  selected squad receives a real order.
- Versioned saving/loading, checkpoints on mission advance, bilingual HUD,
  objective marker, prompts, start/pause/settings UI, effects and audio.

## Verification at this checkpoint

- Unity: `6000.0.78f1`.
- Production asset contract: passed.
- Windows x64 build: passed, `667,058,663` bytes in Unity's build report.
- Distributable archive:
  `Releases/CanopyKin-Moonroot-Windows-x64-v0.4.0.zip`,
  `568,664,867` bytes, SHA-256
  `C57F23D610F1A8C9ED97128008CE00DEA96B1F21009D3064697595CF401F632A`.
- The archive was extracted into a new temporary directory and the executable
  was launched from that clean copy. Its mission-flow smoke reached final step
  15, confirmed four unlocked soldiers and completed an isolated save/load
  round trip.
- WebGL 0.4.0 release build: passed, `55,558,369` bytes. A fresh local-browser
  session loaded through a plain static server (therefore exercising the
  JavaScript gzip fallback), displayed the HUD and accepted gameplay input with
  zero console warnings or errors.
- Measured WebGL opening at 1600x900 over a 20-second sample: 55.33 FPS,
  18.07 ms average frame, 24.0 ms p95, 84 batches, 62 SetPass calls,
  396K triangles, 117.9 MB allocated / 128.8 MB reserved and 0 B/frame GC.
  Evidence: `QA/Screenshots/webgl-local-0.4.0-20260728.jpg`.
- GitHub Pages workflow run `30384132474` completed successfully for commit
  `b819264`. A cache-busted fresh tab opened the real public URL, received the
  0.4.0 manifest, rendered the mission HUD, accepted gameplay input and remained
  free of console warnings/errors. Its 20-second public sample measured
  51.02 FPS, 19.60 ms average frame, 28.0 ms p95, 84 batches, 62 SetPass calls,
  396K triangles, 117.9 MB allocated / 128.8 MB reserved and 0 B/frame GC.
  Evidence: `QA/Screenshots/webgl-public-0.4.0-b819264-20260728.jpg`.
- Windows player: ordinary nursery start and optimized surface smoke both
  launched; queen objective, worker-only opening squad, production ant, licensed
  trunk, restored vegetation and corrected location materials were visually
  inspected.
- Runtime mission-flow smoke in the built executable passed all fifteen state
  transitions, verified zero active soldiers at the start, four after unlock,
  and restored final step 15 through an isolated temporary save/load slot.
- Production-spider close-view QA passed in the built executable: two skinned
  LODs, 141,998 combined triangles, approximately 3.2 x 2.17 x 3.49 m runtime
  bounds and no console errors. The evidence frame is
  `QA/Screenshots/spider-production-windows-20260728.jpg`.
- Automated squad combat against the production spider passed in the built
  executable: mission stage 7 advanced to 8 in 4.8 seconds after 19 real damage
  events; the predator completed a telegraphed hit and its death sequence.
- Production-beetle close-view QA passed in the built executable: two skinned
  LODs, 116,000 combined triangles, approximately 2.75 x 2.43 x 3.45 m runtime
  bounds, correct forward orientation and no console errors. The evidence frame
  is `QA/Screenshots/beetle-production-windows-20260728.jpg`.
- Automated player combat against the production beetle passed in the built
  executable: mission stage 5 advanced to 6 in 6.4 seconds after 11 real bite
  damage events; Barkshield completed 10 telegraphed attacks, 10 successful
  hits and its production death sequence.
- Production-root QA passed in the built executable: three actual mission
  instances, 57,300 combined LOD triangles and three imported mesh colliders.
  The unobstructed evidence frame is
  `QA/Screenshots/root-network-windows-20260728.jpg`.
- Measured High profile at 1920x1080 on RTX 3050 Laptop, 20-second warm samples:
  - underground opening: 59.00 FPS, 16.95 ms frame, 10.89 ms GPU,
    358 batches, 254 SetPass calls, 1.76M triangles, 160.4 MB allocated,
    0 B/frame GC;
  - forest-floor trail: 52.95 FPS, 18.89 ms frame, 17.91 ms GPU,
    1,749 batches, 850 SetPass calls, 4.58M triangles, 152.3 MB allocated,
    0 B/frame GC.
  - isolated production-spider arena view: 56.35 FPS, 17.75 ms frame,
    16.77 ms GPU, 314 batches, 190 SetPass calls, 2.36M triangles,
    171.0 MB allocated, 0 B/frame GC.
  - isolated production-root route: 58.14 FPS, 17.20 ms frame,
    5.40 ms GPU, 511 batches, 178 SetPass calls, 1.35M triangles,
    169.7 MB allocated, 0 B/frame GC.

## Remaining production work

- Replace the remaining distant-enclosure and underground procedural root
  tubes, and expand the licensed high-resolution material set.
- Refine close camera composition, foot contact, slope posing, animation
  blending, combat alignment and creature audio.
- Time a complete manual start-to-finish playthrough and tune travel/combat
  pacing into the requested 15-25 minute range; the automated state-flow test
  is not a substitute for that playthrough.
- Run the packaged Windows build on a separate physical clean machine; the
  current clean-extraction test verifies packaging and runtime dependencies on
  this development machine, not an unrelated PC.
- Profile the surface region and combat encounters separately; optimize
  invisible work while preserving the Windows High presentation.
- Complete a manual start-to-finish public WebGL playthrough in addition to the
  verified startup/input/profile pass and automated 15-stage mission-flow test.

## 0.4.1 movement and route correction

- Reproduced the public no-movement defect: the player build enabled only the
  legacy Input Manager while gameplay read the new Input System.
- Enabled the correct input backend, buffered short browser key events and
  rebuilt camera-relative movement, grounding, obstacle handling, slope posing,
  focus recovery and distance-driven gait.
- Added a physical nest-mouth-to-first-resource route with bark, soil banks,
  stones, wet ground, a leaf bridge, reactive vegetation, pushable debris,
  pheromone guidance and ambient patrol ants.
- Forced clean target builds after an incremental WebGL build reproduced a
  per-frame runtime null reference.
- Clean WebGL build passed at 55,615,623 bytes. Manual canvas testing verified
  underground movement, nest exit, the outdoor route, pause/resume,
  fullscreen/refocus and continued movement to the first resource prompt.
- Clean Windows x64 build passed at 667,078,743 bytes, launched, entered
  gameplay and produced no runtime exception. The Windows automation driver's
  synthetic keyboard events are rejected by Unity Raw Input, so hardware-key
  movement on Windows remains an explicitly unverified manual acceptance item.
- Detailed evidence and exact positions are recorded in
  `QA/MOVEMENT_ACCEPTANCE_0.4.1.md`.
