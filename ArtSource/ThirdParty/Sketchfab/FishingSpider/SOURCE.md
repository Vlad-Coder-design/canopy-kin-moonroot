# CC0 Fishing Spider source record

- Asset: `CC0 Fishing Spider (Dolomedes orion)`
- Creator: ffish.asia / floraZia.com
- Source URL: https://sketchfab.com/3d-models/cc0-fishing-spider-dolomedes-orion-320e77ebe2e049dcbb759dd79ee03a8c
- License: CC0 1.0 / public domain dedication
- Downloaded format: original glTF archive
- Download archive MD5: `82528e552655afdfefa79105bfb0ec32`
- Original glTF MD5: `c1f9906a6f019df4c735fa4b68440373`
- Original photographic atlas MD5: `03702dc5ffc4356c5b2f9a2969a3c9e3`

The source is a static 437,892-triangle photogrammetry scan with no animation.
`Tools/build_production_spider.py` rebuilds from the extracted original glTF,
removes only scan outliers, produces 111,999- and 29,999-triangle gameplay LODs, builds a
28-bone anatomical armature, creates eight animation clips and exports
`Assets/Resources/Models/Creatures/CanopyKinFishingSpider.fbx`.

The downloaded ZIP was removed after checksum recording and verified extraction
so the repository does not retain a meaningless duplicate of the same glTF and
texture files.

The derived FBX MD5 at integration time is
`a43d27e55bf604bdf4252cddd0a691c4`.
