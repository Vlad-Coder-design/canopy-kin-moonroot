# Ant replacement audit — 0.5.0

Date: 2026-07-29

## Current visible model coverage

The 0.4.1 build does not contain separate production models for the ant castes.
Every visible ant below is instantiated by `AntVisual` from the same
`Assets/Resources/Models/Ant/CanopyKinProductionAnt.fbx` file:

| Visible entity | Runtime source | 0.4.1 presentation | Required 0.5.0 replacement |
| --- | --- | --- | --- |
| Player | `PlayerAnt` | Scout tint and scale | Close-camera scout/player mesh |
| Worker squad | `Actor` / `GameDefinitions` | Worker tint, smaller scale | Worker morphology |
| Light soldier squad | `Actor` / `GameDefinitions` | Enlarged head and scale | Light-soldier morphology |
| Heavy soldier squad | `Actor` / `GameDefinitions` | Further enlarged head and scale | Heavy-soldier morphology |
| Queen | `WorldBootstrap.BuildQueenChamber` | Heavy soldier scaled to 155% | Unique queen morphology |
| Rival ants | `Creature` | Rival tint and enlarged head | Rival morphology and markings |
| Veteran scout | `WorldBootstrap.BuildMissionLocations` | Scout tint and scale | Scout morphology |
| Ambient route foragers | `WorldBootstrap.BuildTrail` | Worker tint and scale | Worker morphology |

The 0.4.1 caste differences are made after import by scaling the `Head` and
`Abdomen` bones and replacing materials. The queen is explicitly instantiated
as `AntCaste.HeavySoldier`. This is the main defect addressed by this milestone.

## 0.4.1 source asset inspection

- Source: `ArtSource/Ant/CanopyKinProductionAnt.blend`
- Runtime FBX: `Assets/Resources/Models/Ant/CanopyKinProductionAnt.fbx`
- Geometry reported by the Unity build validator: 20,534 vertices / 36,713
  triangles.
- One skinned mesh is shared by all castes.
- The mesh was generated from analytic ellipsoids and tubes in
  `Tools/build_production_ant.py`.
- The rig exposes six four-segment legs, two three-segment antennae, two
  mandibles, head, thorax, abdomen and root bones.
- Nine authored actions exist, while runtime presentation is driven
  procedurally by `AntVisual`.
- Materials are replaced at runtime by one generated exoskeleton material,
  one joint material and one eye material.

Although the source is a skinned FBX rather than Unity primitive GameObjects,
the analytic ellipsoid/tube silhouette is visibly too close to a technical
prototype. It is not retained as a visible fallback in 0.5.0.

## Candidate comparison

| Candidate | License/access | File facts | Decision |
| --- | --- | --- | --- |
| BlenderKit — *Ant Odontomachus davidsoni Hoenle*, Joachim Bornemann | BlenderKit Royalty Free; free plan, but an account is required to obtain the source | 32,055 faces, 1.8 MiB, 126-bone rig | Strongest free catalogue candidate, not used because downloading requires account registration |
| BlenderKit — *Hercules ant rigged v1.2*, Joachim Bornemann | BlenderKit Royalty Free; account required | 23,505 faces, rigged | Good secondary candidate, not used because downloading requires account registration |
| Sketchfab — *Rigged Ant*, ecation | CC BY; download account required | 27.6k triangles, 13.9k vertices, rigged; no verified PBR set | Rejected: weaker provenance package and no verified texture/animation set |
| Sketchfab — *Rigged Ant*, tmarsland | CC BY; download account required | 3.6k triangles, untextured, simple animation | Rejected: game-jam quality is insufficient for the close player camera |
| OpenGameArt — *Ant 3D Model + Rigging + Animated (Low-poly-ish)*, mujtaba-io | CC0 1.0; direct legal download without an account | Original Blender source, 717,296 bytes, rigged with authored actions | Selected only as the legal rig/topology reference base; geometry, weights, materials, castes and animations are rebuilt for 0.5.0 |

## Integration invariants

- Existing gameplay transforms, colliders, navigation, health, resource
  gathering, squad orders, combat and save data remain on their current
  GameObjects.
- `AntVisual` owns only the visible rig and presentation.
- Bite timing continues to be driven by the combat system; visible mandible
  closure is synchronized to the same normalized attack phase.
- Carrying continues to use the existing cargo state and physical resource
  objects.
- Windows retains the highest-detail meshes and textures. WebGL uses model LOD
and texture import overrides, not a different primitive replacement.
