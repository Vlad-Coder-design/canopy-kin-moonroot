# CC0 Japanese Rhinoceros Beetle source record

- Asset: `CC0 Japanese Rhinoceros Beetle`
- Creator: ffish.asia / floraZia.com
- Source URL: https://sketchfab.com/3d-models/cc0-japanese-rhinoceros-beetle-6395f798f7d243e19975a55b76608a8b
- License: CC0 1.0 / public domain dedication
- Downloaded format: original glTF archive
- Download archive MD5: `6507ba88dd6a31c8daa49d7157308cd4`
- Original glTF MD5: `909f4cd03c2da8efe2a78df374cf9392`
- Original photographic atlas MD5: `d69c98bd8f2a56c84edb8a75c061e9c8`

The source is a static 319,517-triangle photogrammetry scan with no animation.
`Tools/build_production_beetle.py` rebuilds from the extracted original glTF,
removes only scan outliers, produces 92,000- and 24,000-triangle gameplay LODs,
builds a 23-bone anatomical armature, creates eight animation clips and exports
`Assets/Resources/Models/Creatures/CanopyKinRhinocerosBeetle.fbx`.

The downloaded ZIP was not copied into the repository after checksum recording
and verified extraction, avoiding a duplicate of the same glTF and texture.
The derived FBX MD5 at integration time is
`d48024263cdabf0248c99130d71e2e5d`.
