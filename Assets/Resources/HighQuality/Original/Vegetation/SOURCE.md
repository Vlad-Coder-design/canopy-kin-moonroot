# Retired Moonroot vegetation image atlases

The project-owned images `moonroot_grass_atlas_v1.png`,
`moonroot_dead_leaf_atlas_v1.png`, and `moonroot_groundcover_atlas_v2.png` were
used by earlier prototypes as alpha-cutout vegetation. Version 0.9.0 deletes all
three PNGs and their Unity metadata. It also deletes the unused
`CanopyKinHeroVegetation.shader` and `CanopyKinHeroLeaf.shader` image-card
shaders.

The current playable scene instead uses `VolumetricVegetationMeshFactory` to
construct opaque closed geometry:

- grass blades have separate upper and lower surfaces plus connecting edge walls;
- stems are tubes and groundcover leaves are individual thick curved meshes;
- fallen leaves have a curved upper surface, underside, perimeter walls, midrib
  and raised secondary-vein detail;
- distant tree crowns use distributed individual solid leaves, not billboard
  planes or photographic cards.

`CanopyKinSolidVegetation.shader` is an opaque back-face-culled PBR surface with
depth writing, wind and contact response. There is no alpha cutout or transparent
vegetation renderer in the 0.9.0 runtime environment.
