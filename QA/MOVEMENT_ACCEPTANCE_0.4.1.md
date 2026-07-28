# Movement and first-route acceptance — 0.4.1

Date: 2026-07-29  
Unity: 6000.0.78f1

## Reproduced cause

The published project had `activeInputHandler: 0` (legacy Input Manager only),
while `PlayerAnt` read movement from `UnityEngine.InputSystem.Keyboard.current`.
In built players `Keyboard.current` was null, so the update returned before
processing WASD. This reproduced the completely static player seen in the
published 0.4.0 build.

Secondary defects found during the same audit:

- very short browser key events could begin and end between Unity frames;
- pointer capture and pause recovery were WebGL-specific and did not recover
  consistently after focus/fullscreen changes;
- traversal and camera casts could hit the player or squad;
- automatic traversal could issue a second `CharacterController.Move` in one
  frame;
- requested velocity, rather than measured displacement, drove rotation and
  gait, causing sliding and unstable animation near obstacles;
- incremental target switching could publish mismatched WebGL script/player
  artifacts and a per-frame `NullReferenceException`.

## Implemented correction

- Enabled the Input System in player settings and added an 85 ms low-level
  event buffer for short WASD/Shift events.
- Added camera-relative normalized movement, acceleration/deceleration,
  measured-velocity rotation, filtered ground/obstacle casts and a single
  controller move per frame.
- Made gait phase distance-driven, added slope alignment and surface-aware
  speed, footsteps and particles.
- Restored pointer capture after focus, pause and fullscreen changes on both
  platforms.
- Made the WebGL canvas focusable and added input-acceptance telemetry.
- Forced clean build caches for both targets and clean output directories.
- Added a physical nest-to-resource route: bark mouth/arch, soil banks,
  collidable stones, wet-soil section, leaf bridge, reactive grass, pushable
  pebbles, pheromone trail and patrol ants.

## Local WebGL manual result

The clean 0.4.1 WebGL player was served by a plain local static server, opened
in a fresh browser tab and controlled through the actual canvas:

1. Underground start: `(0.00, -5.37, -6.10)`.
2. Eighteen short `W` inputs: `(-0.03, -5.35, -3.04)`.
3. `E` at the real nest mouth moved the player to the surface:
   `(0.00, 0.37, -4.19)`.
4. Eighteen more `W` inputs reached `(0.00, 0.34, 0.14)`.
5. `Escape` opened pause; a second `Escape` restored locked-pointer control.
6. Eight `W` inputs after resume reached `(0.00, 0.41, 2.05)`.
7. Fullscreen was entered, the canvas was refocused, and eight `W` inputs
   reached `(0.07, 0.29, 4.08)` at the first seed prompt.

No Unity exception, `NullReferenceException`, or WebGL warning was recorded.
The browser automation layer emitted its own Chromium `UnknownError` after
synthetic key bursts; it did not originate in the game, interrupt rendering or
prevent the verified position changes above.

Evidence:

- `QA/Screenshots/movement-webgl-v041-clean-local-start.png`
- `QA/Screenshots/movement-webgl-v041-clean-local-after-w.png`
- `QA/Screenshots/movement-webgl-v041-clean-local-surface.png`
- `QA/Screenshots/movement-webgl-v041-clean-local-route.png`
- `QA/Screenshots/movement-webgl-v041-clean-local-pause.png`
- `QA/Screenshots/movement-webgl-v041-clean-local-resume-move.png`
- `QA/Screenshots/movement-webgl-v041-clean-local-fullscreen-move.png`

## Windows result

The clean Windows x64 player launched in a 1280x720 window, displayed the start
panel, entered the underground gameplay scene, initialized all production ant
visuals and reported `MOONROOT_SLICE_READY`. `Player.log` contained no
exception, null reference or runtime error.

The Windows automation helper's SendInput-based keyboard injection is rejected
by Unity's Raw Input path, so this environment could not honestly certify a
hardware-keyboard movement pass in the Windows executable. The exact same
`PlayerAnt` movement code is compiled into both targets, and displacement is
manually verified above in WebGL, but a physical-keyboard Windows playtest
remains a separate acceptance item.

## Build results

- WebGL optimized: `55,615,623` bytes in Unity's build report.
- Windows full quality: `667,078,743` bytes in Unity's build report.
- Windows archive: `Releases/CanopyKin-Moonroot-Windows-x64-v0.4.1.zip`,
  `561,912,048` bytes, SHA-256
  `8D6161F890CAD41302994517D51FCED5B0E6ED8AAAE683788B48A319E3176B71`.

## Remaining acceptance work

- Complete a timed manual start-to-finish mission playthrough on both targets.
- Verify the Windows archive on an unrelated clean physical PC with a hardware
  keyboard.
- Continue tuning squad screen composition, close camera framing, terrain foot
  contact and surface-region performance.
- Re-measure warmed Windows performance after the movement/camera changes; the
  launch-inclusive telemetry sample is not a valid steady-state benchmark.
