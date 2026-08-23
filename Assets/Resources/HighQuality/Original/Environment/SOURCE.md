# Moonroot original environment textures

These textures are original project assets generated for **Canopy Kin: Moonroot**
with OpenAI image generation on 2026-08-21. They are not third-party
commercial-game assets.

## Active: `moonroot_weathered_stone_albedo_v1.png`

- Creator: OpenAI image generation, directed and integrated by the Canopy Kin
  development team.
- License/source: project-owned generated asset; original output preserved at
  `C:\Users\Asus\.codex\generated_images\019f6be3-5052-7081-8add-bd71f4dc4024\exec-45ef3c3c-31a0-44ed-84d4-27a1891eb189.png`.
- Modifications: imported as repeating base colour and combined in Unity with
  the existing fine surface normal and roughness maps.
- Use: weathered and moss-tinted physical stones throughout the surface level.

## Retired and deleted in 0.9.0

`moonroot_forest_horizon_panorama_v1.png` was previously displayed on the
runtime object `Forest horizon enclosure/Fogged photographic forest horizon`
through `CanopyKinForestBackdrop.shader`. The image, its `.meta`, that shader
and its `.meta` were deleted. `WorldBootstrap.BuildDistantEnclosure` now creates
only modeled terrain ridges, irregular trunks, branching limbs, spreading roots,
individual solid leaf canopies and volumetric understory. No panorama, sky card,
or photographic environment plane is loaded by the current scene.
