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
- Dense grass retains 706 placed tufts but is grouped into 124 spatial high/low
  LOD chunks (56,480 close and 8,472 distant triangles), with shared materials,
  wind and shadow policy. This removes per-tuft Renderer overhead without
  thinning the authored forest.
- The project enforces a production-asset build contract so an accidentally
  missing rig, clip set, 8K Windows import, 2K WebGL override or landmark mesh
  fails the build.
- The Windows High profile uses separate underground/surface visibility
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
- Windows x64 build: passed, `467,153,835` bytes in Unity's build report.
- Windows player: ordinary nursery start and optimized surface smoke both
  launched; queen objective, worker-only opening squad, production ant, licensed
  trunk, restored vegetation and corrected location materials were visually
  inspected.
- Runtime mission-flow smoke in the built executable passed all fifteen state
  transitions, verified zero active soldiers at the start, four after unlock,
  and restored final step 15 through an isolated temporary save/load slot.
- Measured High profile at 1920x1080 on RTX 3050 Laptop, 20-second warm samples:
  - underground opening: 59.00 FPS, 16.95 ms frame, 10.89 ms GPU,
    358 batches, 254 SetPass calls, 1.76M triangles, 160.4 MB allocated,
    0 B/frame GC;
  - forest-floor trail: 52.95 FPS, 18.89 ms frame, 17.91 ms GPU,
    1,749 batches, 850 SetPass calls, 4.58M triangles, 152.3 MB allocated,
    0 B/frame GC.

## Remaining production work

- Replace the remaining procedural beetle/spider and coarse root silhouettes
  with authored production meshes and expand the licensed high-resolution
  material set.
- Refine close camera composition, foot contact, slope posing, animation
  blending, combat alignment and creature audio.
- Time a complete manual start-to-finish playthrough and tune travel/combat
  pacing into the requested 15-25 minute range; the automated state-flow test
  is not a substitute for that playthrough.
- Profile the surface region and combat encounters separately; optimize
  invisible work while preserving the Windows High presentation.
- Rebuild and smoke-test the independently optimized WebGL edition, deploy it,
  open the real public URL in a clean session and verify gameplay before
  publishing 0.4.0.
