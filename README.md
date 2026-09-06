# Canopy Kin: Moonroot

An original Unity 6 ant-scale action-strategy vertical slice. The player controls an
articulated ant, explores a rolling forest-floor region and an underground nursery,
gathers and carries resources, commands worker and soldier squads, fights multiple
insect castes, upgrades the colony, and completes the Moonroot tutorial mission.

Version 0.9.2 rebuilds the underground traffic network around measured player,
NPC and camera envelopes. Main passages are now 1.84-2.70 m wide and
1.50-2.00 m high, have smooth continuous collision floors, widened organic
junctions and a closed safety shell. Squad ants use two-way lanes, yield to the
player, separate from one another and recover from congestion without blocking
the player. The close camera is constrained to the active passage and can keep
orbiting while movement is obstructed. The packaged build passes all 16 live
tunnel/camera/traffic tests; four evidence videos and exact measurements are in
`QA/TUNNEL_CLEARANCE_IMPLEMENTATION_092.md`.

Version 0.9.1 hardens the complete playable slice against geometry tunnelling and
camera clipping. The player is now loaded from
`Assets/Resources/Prefabs/PlayerScoutAnt.prefab`, moves through swept substeps,
slides along collision planes, records safe grounded positions and recovers from
invalid overlaps. Roots use overlapping compound capsule colliders, every modeled
tree is solid, nest tunnel collision shells are trimmed at junctions, NPC ants use
swept movement, and saving records a validated position. The packaged Windows
build passes 34 collision cases, 61 camera-containment samples, all 30 terrain
probes and the full 15-step mission smoke test. See
`QA/COLLISION_CAMERA_IMPLEMENTATION_091.md` and `QA/Video` for exact evidence.

Version 0.9.0 gives the player a separately authored close-camera Formica rufa
model derived from Msassasa's legally verified CC BY 4.0 Game-Ready Worker Ant.
The production export has 53,390 triangles, 67 anatomical/animation bones and
24 named gameplay clips; the other seven castes keep the optimized upright LOD
family. The slice also includes custom organic environment meshes,
full-resolution licensed and original PBR texture sets, LOD vegetation,
positional effects, an original procedural soundtrack, bilingual UI,
settings, save/load, and a complete WebGL loading shell.

Version 0.9.0 removes the photographic horizon cylinder and every vegetation
image card from the playable scene. The forest boundary is now layered physical
3D terrain with modeled trunks, branches, spreading roots and volumetric
individual-leaf canopies. Grass, groundcover and fallen leaves are opaque closed
meshes with real thickness. The nest is a connected set of five organic chambers
and four curved tunnels with collidable uneven floors, walls and ceilings.
Windows and WebGL retain separate quality and density profiles.

## Controls

- `WASD` — move
- Mouse — orbit camera
- `Shift` — sprint
- `Space` — vault
- `E` — interact, gather, enter the nest, or upgrade
- Left mouse button — bite
- `1` — workers gather
- `2` — soldiers attack
- `3` — squad follows
- `4` — squad defends Moonroot
- `5` — squad patrols
- `6` — squad retreats
- `7` — squad returns to the nest
- `Z` / `X` / `C` — select all / workers / soldiers
- `Escape` — pause
- `F5` / `F9` — quick save / load

The mission starts automatically after the introduction. Click the canvas once if the
browser has not captured the mouse.

## Browser build

Public build: https://vlad-coder-design.github.io/canopy-kin-moonroot/

`Builds/WebGL` is deployed by `.github/workflows/pages.yml` on pushes to `main`.
Version 0.9.2 uses hash-named gzip WebGL artifacts with Unity's JavaScript
decompression fallback. This keeps the data file below GitHub's per-file limit
while still loading correctly from GitHub Pages without custom response headers.
The page restores keyboard focus and requests pointer lock from a click or
fullscreen gesture; if the browser releases the mouse, an on-page prompt explains
how to capture it again.

## Development

Unity version: `6000.0.78f1`.

The project now maintains two independent production profiles:

- Windows Full Quality: 8K source materials, dense vegetation, extended view and
  shadow distances, HDR, 8x MSAA and a 2 GB texture-streaming budget on High.
- WebGL Optimized: the same gameplay and source assets with 2K platform texture
  overrides, reduced simulation density and browser-oriented memory settings.

- `Canopy Kin > Build WebGL`
- `Canopy Kin > Build Windows`

The reliable low-memory batch build uses `-job-worker-count 1
--burst-disable-compilation`.

The final ant topology corrections, eight caste morphologies, 13-clip animation set,
procedural creature/environment geometry, generated source textures, gameplay,
mission content, audio, and interface layout are project work. The current ant
family uses a recorded CC BY 4.0 Sketchfab base by Msassasa and the outdoor
terrain uses recorded CC0 Poly Haven scans. See `THIRD_PARTY_ASSETS.md` for
exact sources, attribution, licenses and modifications.
