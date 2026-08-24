# Collision, camera and recovery acceptance — 0.9.1

This record describes the implementation that is present in the packaged Windows
and optimized WebGL editions. The results below come from `Builds/Windows/CanopyKin.exe`,
not from a mock scene or an Editor-only test.

## Player prefab and controller

- Prefab: `Assets/Resources/Prefabs/PlayerScoutAnt.prefab`
- Runtime object: `Player scout ant`
- Controller: Unity `CharacterController`
- Radius: `0.23 m`
- Height: `0.68 m`
- Center: `(0, 0.34, 0)`
- Step offset: `0.22 m`
- Slope limit: `54 degrees`
- Skin width: `0.025 m`
- Overlap recovery and collision detection: enabled
- Recovery probe: a matching disabled `CapsuleCollider`; it is used only for
  overlap/penetration queries and is never a second physical body.

The controller is instantiated from the prefab by `WorldBootstrap`. The old
runtime-only player construction remains solely as a guarded fallback if the
resource is missing, and production validation fails when the prefab is absent.

## Movement and anti-stuck behavior

`PlayerAnt.MoveWithSweptSubsteps` divides long frame movement into as many as 18
short steps. Before each planar step it capsule-casts the body, stops before the
contact skin and projects the remaining velocity onto the hit plane. The real
`CharacterController.Move` then performs the step and collision callbacks provide
the stable blocking normal used on the next frame. This prevents low-frame-rate
tunnelling and lets the ant slide along trunks, roots and nest walls instead of
vibrating against them.

The player records the last valid grounded pose. Persistent input with almost no
progress starts recovery in this order:

1. resolve penetrations using `Physics.ComputePenetration`;
2. try short radial valid positions around the body;
3. restore the last safe grounded position.

Every candidate must pass `WorldBootstrap.IsPlayerPositionValid`, including the
analytic nest chamber/tunnel volume. Teleport, respawn and load all use the same
validation. Save writes `GetValidatedSavePosition`, so a bad overlap cannot become
the next permanent spawn point.

## Solid world inventory

- All 44 modeled trees carry solid trunk/root collision and a
  `SolidWorldGeometry` marker.
- Curved surface roots use overlapping capsule segments following the authored
  path; the traversal audit detects 9 segments in the hero route.
- Hero rocks, mound pebbles, banks and production mesh obstacles are marked solid.
- Five nest chambers and four curved tunnels use collidable floor/wall/ceiling
  shells. Hidden tunnel collision shells trim their end rings, preventing two
  shells from forming an invisible wall at a junction.
- Worker/soldier squad bodies have sphere colliders and use swept movement.
- Ambient route ants and hostile/neutral creatures use swept sphere movement and
  constrained actor positions instead of direct transform translation.

Decorative grass blades, tiny leaves, particles and audio emitters deliberately
remain non-solid. This prevents traversal noise without allowing passage through
gameplay barriers.

## Camera and browser input

The camera sphere-casts independently from the ant's elevated target, ignores
non-solid decorative geometry and shortens its boom before a true obstacle. A
second overlap pass pushes the camera out of nearby solid colliders. Underground,
the camera is constrained to the actual union of chamber ellipses and curved
tunnel volumes and below their ceiling. The look target is not clamped, so the ant
does not appear to jump when the camera corrects itself.

The WebGL shell focuses the Unity canvas on click, requests pointer lock from the
same browser-approved gesture, reports pointer-lock diagnostics, and restores
focus after fullscreen changes. A visible hint appears whenever the browser
releases the mouse. Browser security still requires one user click after loading.

## Packaged-build results

- `QA/Logs/collision-safety-091-final.log`:
  `MOONROOT_COLLISION_SAFETY_QA_OK tests=34 failures=0 solidMarkers=811 solidTrees=44 nestMeshes=18 squadBodies=8 recoveries=1`
- `QA/Logs/camera-containment-091-final.log`:
  `MOONROOT_CAMERA_CONTAINMENT_OK samples=61 failures=0 solidOverlaps=0 tooClose=0`
- `QA/Logs/environment-traversal-091-final.log`:
  `MOONROOT_ENVIRONMENT_TRAVERSAL_OK terrainHits=30/30 rootColliderSegments=9`
- `QA/Logs/mission-flow-091-final.log`:
  `MOONROOT_MISSION_FLOW_SMOKE_OK finalStep=15 activeSoldiers=4 saveLoad=True`

The collision suite covers a tree sprint, branching-root approach, a simulated
10 FPS long step, surface seam, nest wall/corner, curved tunnel, forced invalid
nest position, valid save/load, pause, three nest exit/re-entry cycles, camera
ground/ceiling cases, NPC bodies and a full solid-collider inventory.

## Recorded evidence

The source is an actual Windows player run captured at 960x540 and 15 FPS:

- `QA/Video/collision-091-tree.mp4`
- `QA/Video/collision-091-root.mp4`
- `QA/Video/collision-091-nest-wall.mp4`
- `QA/Video/collision-091-camera-obstruction.mp4`
- `QA/Video/collision-091-anti-stuck.mp4`
- `QA/Video/collision-091-tunnel.mp4`
- `QA/Video/collision-091-proof.mp4` (combined recording)

## Honest limitations

- Browser pointer lock cannot legally be restored without a new user gesture.
- The camera deliberately moves closer in very tight tunnels; this is preferable
  to showing the outside of the nest shell.
- NPC collision uses sphere sweeps for predictable squad movement rather than a
  fully articulated collider on every leg.
- Automated tests cover authored barriers and representative stress paths; they
  cannot mathematically prove every possible player input on every GPU/driver.
