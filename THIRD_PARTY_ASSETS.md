# Third-party assets

## Noto Sans Regular

- File: `Assets/Resources/Fonts/NotoSans-Regular.ttf`
- Author: The Noto Project Authors
- Source: https://github.com/notofonts/noto-fonts
- License: SIL Open Font License 1.1
- Local license copy: `Assets/Resources/Fonts/Noto-OFL.txt`
- Use: Latin and Cyrillic runtime interface text.

All game models, procedural environment geometry, interface layout, gameplay code
and original generated texture sources are project-owned work. Third-party material
sets are listed individually below.

## Original generated texture sources

The soil, bark, moss, leaf-litter, and ant-exoskeleton texture sources were generated
specifically for this project and converted locally into albedo, normal, and
roughness maps. They are project-owned source assets and do not import third-party
game content.

## Poly Haven — Forest Floor

- Asset: `Forest Floor`
- Creator: eye-candy.xyz
- Provider: Poly Haven
- Source: https://polyhaven.com/a/forest_floor
- License: CC0 1.0 / public domain dedication
- Imported files: 8K diffuse, DirectX normal, roughness, ambient occlusion and
  displacement maps
- Modifications: Unity mip generation, BC compression and platform-specific
  maximum resolution; Standalone retains 8K while WebGL imports a 2K copy
- Use: primary Moonroot forest-floor terrain material

Poly Haven confirms that all assets on the site are CC0 and may be used,
redistributed and included in commercial products:
https://polyhaven.com/license

## Poly Haven — Dead Tree Trunk

- Asset: `Dead Tree Trunk`
- Creator: Rob Tuytel
- Provider: Poly Haven
- Source: https://polyhaven.com/a/dead_tree_trunk
- License: CC0 1.0 / public domain dedication
- Imported files: 4K FBX geometry, diffuse, DirectX normal and packed
  AO/roughness/metallic maps
- Modifications: Unity material reconstruction, mip streaming, BC compression,
  4K Standalone import and independent 2K WebGL texture override
- Use: production fallen-log landmark, natural barrier and elevated traversal
  route in the Moonroot forest region; its 4K bark maps are also applied to the
  project-authored branching root-network landmarks

## Sketchfab — CC0 Fishing Spider (Dolomedes orion)

- Asset: `CC0 Fishing Spider (Dolomedes orion)`
- Creator: ffish.asia / floraZia.com
- Provider: Sketchfab
- Source: https://sketchfab.com/3d-models/cc0-fishing-spider-dolomedes-orion-320e77ebe2e049dcbb759dd79ee03a8c
- License: CC0 1.0 / public domain dedication
- Imported files: original glTF scan and original 8K-class photographic
  base-colour atlas
- Modifications: scan-outlier cleanup; close-view LOD0 reduced from 437,892
  to 111,999 triangles; 29,999-triangle LOD1; original 28-bone anatomical rig;
  idle, walk, run, telegraph, attack, stagger, death and retreat clips; Unity
  material reconstruction; BC compression and mip streaming; 8K Standalone
  import and independent 2K WebGL texture override
- Use: Ashback fishing-spider predator and mission boss in the Moonroot
  forest region

The downloaded model is explicitly published as CC0. Credit is retained here
for provenance even though attribution is not required.

## Sketchfab — CC0 Japanese Rhinoceros Beetle

- Asset: `CC0 Japanese Rhinoceros Beetle`
- Creator: ffish.asia / floraZia.com
- Provider: Sketchfab
- Source: https://sketchfab.com/3d-models/cc0-japanese-rhinoceros-beetle-6395f798f7d243e19975a55b76608a8b
- License: CC0 1.0 / public domain dedication
- Imported files: original glTF scan and original high-resolution photographic
  base-colour atlas
- Modifications: scan-outlier cleanup; close-view LOD0 reduced from 319,517
  to 92,000 triangles; 24,000-triangle LOD1; original 23-bone anatomical rig;
  idle, walk, run, charge telegraph, charge, stagger, death and retreat clips;
  Unity material reconstruction; BC compression and mip streaming; 8K
  Standalone import and independent 2K WebGL texture override
- Use: Barkshield rhinoceros-beetle enemy and weak-point combat encounter

The downloaded model is explicitly published as CC0. Credit is retained here
for provenance even though attribution is not required.
