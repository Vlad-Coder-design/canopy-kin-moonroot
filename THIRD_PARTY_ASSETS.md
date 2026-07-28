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
