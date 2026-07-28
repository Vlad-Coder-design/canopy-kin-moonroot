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
- The project enforces a production-asset build contract so an accidentally
  missing rig, clip set, 8K Windows import or 2K WebGL override fails the build.
- The Windows High profile now uses separate underground/surface visibility
  partitions. Invisible vegetation and terrain are not submitted while the
  player is in the nest, without reducing visible scene density.
- Player-build telemetry measures frame time, GPU/CPU timing, batches, SetPass
  calls, triangles, memory and GC rather than relying on estimates.

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
- Ten connected mission beats from waking in the nursery through the forest
  route, gathering, beetle fight, territory hold, colony upgrade, rival defence,
  spider battle and final threat reveal.
- Versioned saving/loading, checkpoints on mission advance, bilingual HUD,
  objective marker, prompts, start/pause/settings UI, effects and audio.

## Verification at this checkpoint

- Unity: `6000.0.78f1`.
- Production asset contract: passed.
- Windows x64 build: passed, `396,902,595` bytes in Unity's build report.
- Windows player: launched into real gameplay; production ant and corrected
  underground material/lighting were visually inspected.
- Measured High profile at 1920x1080 on RTX 3050 Laptop:
  57.00 FPS average, 17.54 ms average frame, 14.30 ms GPU frame,
  17.56 ms CPU frame, 411 batches, 258 SetPass calls, 2.40M triangles,
  151.3 MB allocated memory and 0 B/frame GC during the 20-second sample.

## Remaining production work

- Replace the remaining procedural beetle/spider and coarse root silhouettes with
  authored production meshes and expand the licensed high-resolution material set.
- Refine close camera composition, foot contact, slope posing, animation blending,
  combat alignment and creature audio.
- Split the current ten mission gates into the requested fifteen clearly presented
  beats, then time a complete start-to-finish 15–25 minute playthrough.
- Profile the surface region and combat encounters separately; optimize invisible
  work while preserving the Windows High presentation.
- Rebuild and smoke-test the independently optimized WebGL edition, deploy it,
  open the real public URL in a clean session and verify gameplay before publishing
  0.4.0.
