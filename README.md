# Canopy Kin: Moonroot

An original Unity 6 ant-scale action-strategy vertical slice. The player controls an
articulated ant, explores a rolling forest-floor region and an underground nursery,
gathers and carries resources, commands worker and soldier squads, fights multiple
insect castes, upgrades the colony, and completes the Moonroot tutorial mission.

The production slice uses an original Blender-authored skinned ant, custom organic
environment meshes, full-resolution licensed and original PBR texture sets,
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
The project uses uncompressed, hash-named WebGL artifacts so GitHub Pages supplies the
correct MIME types and browsers do not reuse an incompatible old build.

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

The ant FBX, procedural creature/environment geometry, generated source textures,
gameplay, mission content, audio, and interface layout are original project work.
The outdoor terrain also uses a properly recorded CC0 Poly Haven scan. See
`THIRD_PARTY_ASSETS.md` for exact asset sources and licenses.
