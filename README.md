# Canopy Kin: Moonroot

An original Unity 6 ant-scale action-strategy vertical slice. The player controls an
articulated ant, explores a rolling forest-floor region and an underground nursery,
gathers and carries resources, commands worker and soldier squads, fights multiple
insect castes, upgrades the colony, and completes the Moonroot tutorial mission.

The production slice uses custom procedural meshes, authored PBR texture sets,
articulated insect animation, LOD vegetation, positional effects, an original
procedural soundtrack, bilingual UI, settings, save/load, and a complete WebGL
loading shell.

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
- `Escape` — pause
- `F5` / `F9` — quick save / load

The mission starts automatically after the introduction. Click the canvas once if the
browser has not captured the mouse.

## Browser build

Public build: https://vlad-coder-design.github.io/canopy-kin-moonroot/

`Builds/WebGL` is deployed by `.github/workflows/pages.yml` on pushes to `main`.
The project uses uncompressed, hash-named WebGL artifacts so GitHub Pages supplies the
correct MIME types and browsers do not reuse an incompatible old build.

## Development

Unity version: `6000.0.78f1`.

- `Canopy Kin > Build WebGL`
- `Canopy Kin > Build Windows`

The reliable low-memory batch build uses `-job-worker-count 1
--burst-disable-compilation`.

All environment geometry, insect models, generated source textures, gameplay,
mission content, audio, and interface layout are original project work. See
`THIRD_PARTY_ASSETS.md` for the bundled interface font and license.
