# Canopy Kin: Moonroot

An original Unity 6 ant-scale action-strategy vertical slice. The player controls an
articulated ant, explores a rolling forest-floor region and an underground nursery,
gathers and carries resources, commands worker and soldier squads, fights multiple
insect castes, upgrades the colony, and completes the Moonroot tutorial mission.

Version 0.8.0 gives the player a separately authored close-camera Formica rufa
model derived from Msassasa's legally verified CC BY 4.0 Game-Ready Worker Ant.
The production export has 53,390 triangles, 67 anatomical/animation bones and
24 named gameplay clips; the other seven castes keep the optimized upright LOD
family. The slice also includes custom organic environment meshes,
full-resolution licensed and original PBR texture sets, LOD vegetation,
positional effects, an original procedural soundtrack, bilingual UI,
settings, save/load, and a complete WebGL loading shell.

Version 0.8.0 also replaces the repeated brood/resource placeholders, rebuilds
the underground chamber dressing, corrects twisted procedural roots, adds mixed
woodland groundcover and closes the outdoor map with a fog-integrated forest LOD
behind the playable geometry. Windows and WebGL retain separate quality and
density profiles.

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
Version 0.8.0 uses hash-named gzip WebGL artifacts with Unity's JavaScript
decompression fallback. This keeps the data file below GitHub's per-file limit
while still loading correctly from GitHub Pages without custom response headers.

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
