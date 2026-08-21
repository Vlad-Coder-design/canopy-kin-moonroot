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

The 0.5.0 ant cuticle source is retained at
`ArtSource/Textures/Ant/ant_cuticle_base_generated_20260729.png`. It was
generated specifically for this project, corrected into a seamless continuous
insect-cuticle surface, and locally derived into 4K albedo, DirectX normal,
roughness and ambient-occlusion maps. Windows imports the 4K source maps while
WebGL receives independent 2K platform copies.

## Sketchfab — Game-Ready Worker Ant Model

- Asset: `Game-Ready Worker Ant Model`
- Creator: Msassasa (`@LilCick`)
- Provider: Sketchfab
- Source:
  https://sketchfab.com/3d-models/game-ready-worker-ant-model-b48893d316cc4b2f98ec1d1e37027e6a
- Creator-provided original:
  https://drive.google.com/file/d/1FZ0JOgfEm71EQQW4SXqalfPg29DdLfIp/view
- License: Creative Commons Attribution 4.0 International (CC BY 4.0)
- License URL: https://creativecommons.org/licenses/by/4.0/
- Download date: 2026-07-29
- Original format: Blender 4.0 `.blend`
- Original file:
  `ArtSource/ThirdParty/Sketchfab/GameReadyWorkerAnt/Raw/GameReadyWorkerAnt.blend`
- Original SHA-256:
  `63EF209CAF1A59C95D79C551E46580F4D098E1ED53E69C10FD33091C489F14CF`
- Inspected original: 55 meshes, two armatures, 52-bone worker rig, 47,900
  triangles and a large set of authored actions
- Modifications: baked 53 anatomical pieces into a single skinned mesh;
  preserved detailed head, thorax, abdomen, mandibles, eyes, antennae and all
  six segmented legs; closed nine source openings; triangulated every face;
  recalculated outward normals; normalized transforms; limited weights for
  Unity; added the project animation set; authored eight caste morphologies;
  produced 47,090-triangle close and 11,772-triangle distant LODs; replaced
  the procedural source surface with the project-owned opaque 4K cuticle PBR
  material; baked Unity `+Y` up / `+Z` forward axes
- Use: visible model and rig source for player, scout, worker, nurse, light
  soldier, heavy soldier, queen and rival ants from version 0.5.1 onward

Version 0.6.0 also uses this same licensed source for the separate maximum-
quality `Formica rufa` player prototype. That derivative preserves the donor's
17 dense authored actions, adds a Formicinae petiolar scale, paired pretarsal
claws, rebuilt closed antenna/tarsus shells, arthrodial membranes, macro setae,
67 production bones, project-owned 4K PBR cuticle materials and a 304,764-
triangle editable bake source. Its game-ready Unity export contains 53,390
triangles and 24 named gameplay clips. Exact derivative paths and hashes are
recorded in `ArtSource/AntPrototype/SOURCE.md`.

Attribution is required by CC BY 4.0 and is retained here and in the
redistributed source record.

## OpenGameArt — Ant 3D Model + Rigging + Animated (Low-poly-ish)

- Asset: `Ant 3D Model + Rigging + Animated (Low-poly-ish)`
- Creator: mujtaba-io
- Provider: OpenGameArt
- Source:
  https://opengameart.org/content/ant-3d-model-rigging-animated-low-poly-ish
- Direct original file:
  https://opengameart.org/sites/default/files/ant.blend
- License: CC0 1.0 / public-domain dedication
- License URL: https://creativecommons.org/publicdomain/zero/1.0/
- Download date: 2026-07-29
- Original format: Blender `.blend`
- Original file: `ArtSource/ThirdParty/OpenGameArt/Ant/Raw/ant.blend`
- Original SHA-256:
  `6F4DE1451333964F8FFFFE6CA9D15AAF6EF01FD089A8185B0268BD0D032FB662`
- Inspected original: 542 vertices, 1,080 triangles, one UV layer, 38-bone
  armature, two actions, no available packed texture
- Modifications: repaired and named bone hierarchy; four-influence skin-weight
  limit; Catmull-Clark close-camera topology; separate 6.6K distant LOD;
  integrated compound-eye geometry; new thick serrated mandibles; separate
  worker, nurse, scout, player, light-soldier, heavy-soldier, queen and rival
  morphologies; queen wing-scar plates; soldier pronotum plates; 13 authored
  animation clips; new project-owned 4K PBR material set
- Use: historical anatomical and skinning base for the version 0.5.0 ant
  family. It is retained for provenance but is no longer instantiated as a
  visible model from version 0.5.1 onward.

The source is explicitly published as CC0. Credit is retained here for
provenance even though attribution is not required.

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

## Project-owned generated environment textures (not third-party)

- Assets: `moonroot_packed_soil_albedo_v1.png`,
  `moonroot_groundcover_atlas_v2.png`,
  `moonroot_forest_horizon_panorama_v1.png`, and
  `moonroot_weathered_stone_albedo_v1.png`
- Creator: OpenAI image generation, directed and integrated by the Canopy Kin
  development team on 2026-08-21
- Source/license: original project-owned generated assets; no commercial-game
  material or third-party asset was copied
- Modifications: transparent-background extraction for the groundcover atlas;
  Unity mip generation and platform compression; cylindrical fog integration
  for the panorama; existing project normal/roughness detail combined with the
  stone base colour
- Use: packed underground soil, mixed botanical groundcover, distant forest
  closure, and forest stones
- Exact prompts and original-output paths:
  `Assets/Resources/HighQuality/Original/Nest/SOURCE.md`,
  `Assets/Resources/HighQuality/Original/Vegetation/SOURCE.md`, and
  `Assets/Resources/HighQuality/Original/Environment/SOURCE.md`

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
