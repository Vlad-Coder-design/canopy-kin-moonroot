# Moonroot original environment textures

These textures are original project assets generated for **Canopy Kin: Moonroot** with OpenAI image generation on 2026-08-21. They are not third-party commercial-game assets.

## `moonroot_forest_horizon_panorama_v1.png`

- Creator: OpenAI image generation, directed and integrated by the Canopy Kin development team.
- License/source: project-owned generated asset; original output preserved at `C:\Users\Asus\.codex\generated_images\019f6be3-5052-7081-8add-bd71f4dc4024\exec-bab6e275-3164-4acd-ab2f-40f73218579f.png`.
- Modifications: imported as an opaque, fog-compatible cylindrical distant LOD backdrop; Unity performs platform texture compression.
- Use: closes the playable forest perimeter behind the real terrain, trees and traversal geometry.
- Prompt:

> Create a production-ready wide equirectangular environmental backdrop texture for a realistic ant-scale temperate forest floor game. Aspect ratio 2:1 panoramic strip, seamless left-to-right composition. Viewpoint only 3 centimeters above the soil, looking toward a dense distant woodland boundary. Show layered dark tree trunks extending beyond the top frame, root flares, mossy soil banks, ferns, sedges, broad woodland leaves, small branches, leaf litter, and soft mist filling all gaps. Warm filtered daylight, natural brown and deep green palette, realistic macro-photography detail, atmospheric depth, no empty sky, no horizon line, no open blank areas, no animals, no insects, no buildings, no text, no logos, no watermark. The image must work as a distant cylindrical Unity backdrop: important shapes should not be cut by left or right edges, lighting should be even, and the far boundary must be fully opaque and visually continuous.

## `moonroot_weathered_stone_albedo_v1.png`

- Creator: OpenAI image generation, directed and integrated by the Canopy Kin development team.
- License/source: project-owned generated asset; original output preserved at `C:\Users\Asus\.codex\generated_images\019f6be3-5052-7081-8add-bd71f4dc4024\exec-45ef3c3c-31a0-44ed-84d4-27a1891eb189.png`.
- Modifications: imported as repeating base color and combined in Unity with the existing fine surface normal and roughness maps.
- Use: weathered and moss-tinted stones throughout the surface level.
- Prompt:

> Create a production-ready seamless square PBR albedo texture for a damp weathered temperate-forest stone at ant-scale. Top-down orthographic surface scan look, neutral diffuse lighting with no cast shadow and no baked specular highlight. Fine gray-brown mineral grain, subtle lichen flecks, tiny cracks, softened water-worn variation, restrained natural color, uniformly detailed from edge to edge. Perfectly tileable on all four edges. No perspective, no separate rocks, no soil, no leaves, no plants, no text, no logos, no watermark. This is the base-color/albedo map only.
