# Moonroot grass atlas v1

- File: `moonroot_grass_atlas_v1.png`
- Ownership: original project asset generated for Canopy Kin: Moonroot; not a third-party download.
- Generation method: OpenAI built-in image generation, 2026-08-20.
- Production intent: alpha-cutout albedo atlas for close-camera woodland grass in the playable hero microhabitat.
- Content: four isolated temperate grass-leaf variants (fresh, curved, edge-damaged, broad young leaf), arranged as a 2×2 atlas.
- Modifications: imported into Unity with clamped sampling; tint, alpha cutoff, wind phase and translucency response are applied by `CanopyKinHeroVegetation.shader`.
- Restrictions used during generation: no text, watermark, trademarks, soil, props, insects, baked cast shadows or background.

Final prompt:

> Use case: stylized-concept. Asset type: production game texture atlas for realistic macro-scale forest-floor vegetation. Create a square 2-by-2 atlas containing exactly four separate full-length temperate woodland grass leaves, one per quadrant, entirely visible from base to pointed tip, on a genuinely transparent background. Photorealistic PBR-ready albedo source, orthographic flat capture, diffuse neutral light, fine veins, microscopic speckling, realistic edge wear, coherent natural greens. No soil, moss, roots, flowers, insects, droplets, labels, text, grid lines, logos or watermark.

## Moonroot dead-leaf atlas v1

- File: `moonroot_dead_leaf_atlas_v1.png`
- Ownership: original project asset generated for Canopy Kin: Moonroot; not a third-party download.
- Generation method: OpenAI built-in image generation, 2026-08-20.
- Production intent: alpha-cutout albedo atlas for individually modeled dead leaves in sheltered accumulation zones.
- Content: four isolated temperate leaf variants with different species silhouettes, veins, edge damage, insect holes and decay states.
- Modifications: mapped per quadrant to curled 175-vertex leaf geometry; micro normal, roughness, two-sided lighting and contact shadows are applied in Unity.

Final prompt:

> Use case: stylized-concept. Asset type: production game texture atlas for realistic dead leaves on an ant-scale forest floor. Create a square 2-by-2 atlas with exactly four separate complete fallen temperate leaves, one per quadrant, on a genuinely transparent background. Include a curled torn brown leaf, ochre oval leaf with insect holes, reddish narrow leaf with chipped tip, and mottled yellow-brown broad leaf. Orthographic top-down, diffuse neutral light, detailed veins and midrib, dirt, moisture variation, tears and decay. No soil, grass, moss, twigs, insects, droplets, labels, text, grid lines, logos or watermark.

## Moonroot groundcover atlas v2

- File: `moonroot_groundcover_atlas_v2.png`
- Ownership: original project asset generated for Canopy Kin: Moonroot; not a third-party download.
- Generation method: OpenAI built-in image generation plus built-in background-extraction edit, 2026-08-21.
- Production intent: replace the grass-only surface silhouette with four differentiated forest-floor species.
- Content: sedge, wood sorrel, serrated woodland seedling and creeping groundcover, arranged as a 2×2 atlas.
- Modifications: the first generation was rejected because it contained a photographic background; a targeted edit removed only that background. The final PNG contains real alpha (transparent corner pixels verified before import).

Final generation prompt:

> Use case: stylized-concept. Asset type: transparent cutout texture atlas for realistic Unity forest-floor vegetation. Exactly four isolated temperate forest groundcover specimens in a clean 2×2 atlas: young sedge tuft, wood-sorrel cluster, serrated woodland seedling, and damp creeping groundcover sprig. Photorealistic botanical macro cutouts, soft neutral light, natural green variation, visible veins, curled edges and small imperfections; no soil, pots, text, logos or watermark.

Final edit prompt:

> Use case: background-extraction. Remove only the entire studio background and replace it with genuine transparent alpha. Preserve all four plants exactly, including every leaf, stem, water droplet, serrated edge, placement, scale and color. Transparent pixels between and around every specimen; no halo, crop, restyling, added elements, text or watermark.
