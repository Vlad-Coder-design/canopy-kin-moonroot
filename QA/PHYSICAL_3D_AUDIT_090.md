# Canopy Kin: Moonroot 0.9.0 physical-3D audit

Date: 2026-08-23

This audit covers the actual `Assets/Scenes/Moonroot.unity` runtime assembled by
`WorldBootstrap`, not a separate mock scene.

## Removed photographic environment

| Previous item | Exact path or runtime name | 0.9.0 status |
| --- | --- | --- |
| Forest panorama image | `Assets/Resources/HighQuality/Original/Environment/moonroot_forest_horizon_panorama_v1.png` and `.meta` | Deleted |
| Panorama shader | `Assets/Resources/CanopyKinForestBackdrop.shader` and `.meta` | Deleted |
| Runtime parent | `Forest horizon enclosure` | No longer created |
| Runtime renderer | `Fogged photographic forest horizon` | No longer created |
| Runtime factory | `VisualFactory.ForestHorizonBackdrop` | Removed |
| Old image-mapped horizon mesh | `EnvironmentMeshFactory.ForestHorizonBank` | Removed |

`BuildDistantEnclosure` now creates `Layered modeled forest enclosure`: 3D
terrain ridges, multiple rings of trees, irregular trunks, branching limbs,
spreading buttress roots, individual solid-leaf canopy volumes, stones and
volumetric understory. The runtime automated audit rejects renderer names or
shader names containing `photographic` or `backdrop`.

## Removed flat/image-card vegetation

Deleted source images and Unity metadata:

- `moonroot_grass_atlas_v1.png`
- `moonroot_dead_leaf_atlas_v1.png`
- `moonroot_groundcover_atlas_v2.png`

Deleted image-card shaders and Unity metadata:

- `Assets/Resources/CanopyKinHeroVegetation.shader`
- `Assets/Resources/CanopyKinHeroLeaf.shader`

Removed unused flat generators:

- `EnvironmentMeshFactory.GroundcoverCluster`
- `EnvironmentMeshFactory.HeroGrassCluster`
- `EnvironmentMeshFactory.HeroFallenLeaf`

Active replacements are `VolumetricVegetationMeshFactory.GrassCluster`,
`GroundcoverCluster`, `FallenLeaf`, and `CanopyCluster`. Every silhouette is
geometry. Grass and leaves have upper surfaces, undersides and connecting edge
walls. Stems are tubes. The active `CanopyKinSolidVegetation` shader is opaque,
back-face culled and depth-writing; it does not sample an atlas or alpha clip.

## Underground nest replacement

The former broad terrain/cave-shell dressing was replaced by geometry from
`NestGeometryFactory`:

- 5 organic chamber shells with portal openings;
- 5 collidable uneven chamber floors;
- 4 curved tunnel wall/ceiling shells;
- 4 solid collidable tunnel floors;
- embedded soil clods, stones and penetrating roots;
- visible queen, brood stages, workers, food storage and carried resource
  meshes retained inside the connected network.

The runtime audit requires at least five chamber shells and four tunnel shells.

## Camera containment

`PlayerAnt.ResolveCameraPlacement` uses a 0.19 m sphere cast, 0.22 m obstacle
padding, penetration recovery, a narrow-space elevated shoulder fallback and
world-location clamping before and after smoothing. The camera near clip is
0.018 m. `WorldBootstrap.ConstrainCameraPosition` samples the actual terrain
collider rather than relying only on the analytic height function, prevents a
surface camera from crossing below terrain, and keeps an underground camera
inside the excavated nest volume.

Automated Windows test marker:

`MOONROOT_CAMERA_CONTAINMENT_OK samples=61 failures=0 solidOverlaps=0 tooClose=0`

It covers six surface positions in eight viewing directions, twelve
underground directions, a deliberately invalid underground camera position and
the enter/exit transition.

## Proof naming

Files named `*-wireframe.*` are runtime wireframe captures of the exact
production scene. They are not claimed to be Unity Scene View screenshots.
The scripted walkthrough video also uses the production player, environment,
nest and transitions.
