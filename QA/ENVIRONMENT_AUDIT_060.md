# Environment quality audit — public build 0.6.0 / 73000a6

Audit target: the actual `Moonroot` playable scene and the public build at commit `73000a622306e89d9979999b9a85e9861be3c673`. Baseline camera position is the real player-ant QA location `(9.1, GroundHeight, 16.1)`; no separate mock scene is used.

## Baseline evidence

- `QA/Screenshots/ant-060-windows-player-side-close.png`
- `QA/Screenshots/ant-060-windows-player-uneven-ground.tga`
- `QA/Screenshots/ant-060-windows-player-bright-background.tga`
- Public WebGL: <https://vlad-coder-design.github.io/canopy-kin-moonroot/?build=73000a6>

## Real-scene findings

| Area | 0.6.0 implementation | Visible problem at ant scale | 0.7 slice acceptance |
|---|---|---|---|
| Ground | 82 m runtime mesh, 192×192 on Windows, one 8K Poly Haven forest-floor PBR material | Strong scan texture but large triangles and uniform material response; no close geometric clods or ecological transitions | 12×10 m high-density overlay, layered soil/moss/litter/wetness, navigable collider, no seams in the close camera |
| Grass | Eight five-segment flat ribbons per tuft; moss albedo reused as foliage | Rectangular dark blades, repeated silhouette, poor close-up surface detail | Original four-leaf alpha atlas, 10 length segments, curved blade sections, per-blade phase, close LOD and reactive bending |
| Fallen leaves | 18 vertices; shadow casting disabled | Flat decorative strips with weak contact | 175-vertex curled/damaged leaf, raised midrib, contact shadow and deliberate accumulation zones |
| Roots | Authored root-network FBX plus procedural 12-sided tubes | Good landmark scale but little feeder-root hierarchy or sheltered microhabitat | Three collidable feeder roots tied to moss, stones, litter and damp soil |
| Stones | One smoothed insect-body silhouette deformed by noise | Rounded blobs without fracture language | 360-vertex irregular fractured stones, partially buried and compositionally placed |
| Moss | Material applied to stones; no volumetric close-up patch | Reads as color/texture, not growth | Layered moss-cushion meshes concentrated beside sheltered roots and damp stone faces |
| Water | Thin box with transparent water shader | Rectangular silhouette, no natural shore | Irregular physical puddle mesh in a geometric depression with a wet ground mask |
| Lighting | Two directional lights, trilight ambient and fog | Readable but static and evenly exposed | Soft moving procedural canopy cookie while preserving bright playable values |
| Placement | Most grass, leaf and debris positions use uniform `Random.insideUnitCircle` | Distribution feels scattered rather than caused by moisture, shelter and traffic | Bare traversal lane; grass at light margins; litter under shelter; moss near roots; water in a depression |

## Performance boundaries retained

The quality increase is concentrated around the close-camera playable region. Reusable meshes, shared materials, LOD groups, instancing on the broad forest, platform-specific terrain density and the separate WebGL profile remain in place. No padding or duplicate quality assets are introduced.

## Verification gate

The slice is not accepted from source code alone. It must compile, load the real game, capture the same player-side camera plus ground/grass/root/puddle close-ups, retain collision and traversal, and show visible motion between two grass captures.
