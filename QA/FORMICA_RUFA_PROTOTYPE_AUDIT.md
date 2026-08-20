# Formica rufa maximum-quality prototype audit

Date: 2026-08-20

Status: **Blender prototype accepted for Unity import; Unity validation blocked
before compilation by the local Unity Licensing Client. It has not replaced the
actual gameplay Player yet.**

## Actual-player audit

The current game does not instantiate a Player prefab. The real gameplay chain
is:

1. `WorldBootstrap.BuildPlayerAndSquad()` creates `Player scout ant`.
2. It adds `PlayerAnt`.
3. `PlayerAnt.Awake()` calls `AntVisual.Create(..., AntCaste.Player)`.
4. `AntVisual.TryBuildProductionModel()` loads
   `Resources/Models/Ant/Family/CanopyKinAnt_Player`.

The prototype remains under `Models/Ant/Prototype` and is therefore not a fake
replacement hidden inside the current game. Production integration will happen
only after the separate scene is visibly verified.

## Geometry and anatomy

- Species target: `Formica rufa` worker.
- Unity runtime mesh: 53,394 triangles.
- Editable high-poly bake source: 304,764 triangles.
- One head, mesosoma/thorax, explicit one-node petiolar scale and dark gaster.
- Six legs attached to the thorax, with seven donor leg segments plus two new
  claw bones per foot.
- Two three-bone antennae with rebuilt continuous closed shells.
- Two separately rigged mandibles and source compound-eye geometry.
- 420 deterministic macro setae with slight asymmetry.
- No old prototype mesh has been placed underneath this model.

## Rig

67 bones total:

- `Root`, `Thorax`, `Petiole`, `Abdomen`, `Head`.
- `Mandible_L`, `Mandible_R`.
- Three deform bones for each antenna.
- Coxa, femur, tibia, four tarsal/foot segments per leg.
- Two independently weighted claws on each foot.

The hard cuticle pieces intentionally use rigid dominant weights, while visible
junctions are represented by separate dark arthrodial membrane geometry. This
avoids rubber deformation of the exoskeleton. Unity leg IK and terrain probes
are not yet connected to this prototype and remain an acceptance blocker.

## Genuine action assets

Direct transfers of donor keyframes:

- `ANT_Attack_Primary`
- `ANT_Attack_Secondary`
- `ANT_Bite`
- `ANT_ColonyWork`
- `ANT_Dig`
- `ANT_Drink`
- `ANT_Eat`
- `ANT_FormicAcidDefense`
- `ANT_GrabHeavyBite`
- `ANT_CalmIdle`
- `ANT_Jump`
- `ANT_LayEgg`
- `ANT_StingLargeTarget`
- `ANT_StingAnt`
- `ANT_Trophallaxis`
- `ANT_FeedLarvae`
- `ANT_NormalWalk`

Explicitly marked time-remapped derivatives of genuine donor keyframes:

- `ANT_SlowWalk`
- `ANT_FastRun`
- `ANT_Backward`
- `ANT_AlertIdle`
- `ANT_ExploreAntennae`
- `ANT_TurnLeft`
- `ANT_TurnRight`

These seven are not falsely described as independently motion-captured clips.
Start/stop, fall/landing, dedicated slope/climb, clean-body, pickup and put-down
motions still require distinct authored passes after the basic Unity prototype
is approved.

## Evidence files

All evidence below was rendered from the editable prototype `.blend`, not from
a separate mock ant:

- `QA/AntPrototype/formica-rufa-prototype-front.png`
- `QA/AntPrototype/formica-rufa-prototype-side.png`
- `QA/AntPrototype/formica-rufa-prototype-top.png`
- `QA/AntPrototype/formica-rufa-prototype-bottom.png`
- `QA/AntPrototype/formica-rufa-prototype-wireframe.png`
- `QA/AntPrototype/formica-rufa-prototype-armature.png`
- `QA/AntPrototype/formica-rufa-prototype-skinning-regions.png`
- `QA/AntPrototype/formica-rufa-prototype-dark-light.png`
- `QA/AntPrototype/formica-rufa-prototype-backlight.png`

## Unity scene and validation tooling

- Runtime Game View controller:
  `Assets/Scripts/AntPrototypeShowcase.cs`
- Scene/controller/import validator:
  `Assets/Editor/AntPrototypeSceneBuilder.cs`
- Planned generated scene: `Assets/Scenes/AntPrototype.unity`
- Planned controller: `Assets/Animation/AntPrototype.controller`

The validator requires one SkinnedMeshRenderer, at least 50,000 triangles, all
named anatomical bones, at least 24 clips, and the specific idle/walk/run/turn/
attack clips before it will save the scene.

## Honest blocker and remaining work

Three Unity batch attempts failed before project import because the Licensing
Client either returned `No valid Unity Editor license` (exit code 198) or lost
its IPC entitlement channel. Unity therefore has not compiled these scripts,
imported the final FBX, produced Game View screenshots/video, or built the
evidence player in this checkpoint.

After the user signs in/activates Unity Hub, the next actions are:

1. Run `AntPrototypeSceneBuilder.BuildAndValidate` and fix any import/compiler
   errors.
2. Build and launch the separate Windows evidence player.
3. Capture Game View screenshots and close-up animation videos.
4. Add terrain-aware foot IK and antenna collision response.
5. Only then replace the actual gameplay Player model and connect all gameplay
   animation states.
