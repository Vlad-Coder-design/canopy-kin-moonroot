# Tunnel clearance, traffic and camera validation — 0.9.2

Version 0.9.2 replaces the narrow prototype nest connections with one measured,
continuous passage network. The same passage specification now drives modeled
geometry, collision, runtime camera containment and automated validation.

## Measured clearance

The player capsule has a radius of 0.23 m (0.46 m diameter) and a height of
0.68 m. The third-person camera collision radius is 0.19 m. Regular worker ants
occupy approximately 0.36 m across; the largest squad body envelope is 0.46 m.

| Passage | 0.9.1 width × height | 0.9.2 width × height | Player-width clearance |
| --- | ---: | ---: | ---: |
| Queen approach | 1.36 × 1.05 m | 2.10 × 1.65 m | 1.64 m |
| Food approach | 1.24 × 0.94 m | 1.84 × 1.50 m | 1.38 m |
| Nursery / busy route | 1.32 × 1.00 m | 2.20 × 1.70 m | 1.74 m |
| Main entrance / busy route | 1.44 × 1.10 m | 2.70 × 2.00 m | 2.24 m |

The smallest new overhead clearance above the 0.68 m player capsule is 0.82 m.
Busy routes meet the 2.20 m minimum specification and allow two opposing squad
ants plus the player envelope to pass without a hard body-collider deadlock.

## What changed

- Four main tunnels use seven-point smooth paths rather than four angular points.
- Chamber portal openings have individual measured arcs matching each tunnel.
- Collision floors are smooth, continuous and have no hidden vertical outer rim.
- Decorative roots remain visual detail but no longer create invisible choke points.
- A closed inward-facing soil safety envelope prevents views outside the nest.
- The entrance trigger and arch were widened and the squad holding bay moved away
  from the doorway.
- Squad movement uses directional lanes, player yielding, local separation,
  repathing and safe recovery. Squad body colliders are non-blocking triggers, so
  a crowd cannot physically imprison the player.
- The camera boom and containment calculation use the current local passage width
  and height. Orbit input remains independent of player translation.

## Verification in the real build

`Canopy-Kin-Moonroot.exe -tunnel-clearance-qa` completed with 16/16 checks:

- all four configured passage specifications and live clearance markers;
- continuous collision floors and shells, with no orphaned blocking colliders;
- closed nest safety envelope;
- enter, turn and leave traversal;
- camera rotation while player translation is obstructed;
- 16 camera-wall/ceiling containment samples;
- two ants passing in opposite directions;
- minimum NPC separation, congestion recovery and player crossing live traffic;
- camera side-wall and ceiling push tests;
- final player position validity.

Result: `MOONROOT_TUNNEL_CLEARANCE_QA_OK tests=16 failures=0`.

The WebGL 0.9.2 build was then loaded through a real HTTP server. It reached the
underground playable scene, accepted input and transitioned to the forest surface
with `E` without Unity warnings or errors.

## Visual evidence

- Before: [0.9.1 narrow entrance](Screenshots/world-080-tunnel-entrance.png)
- After: [0.9.2 entrance clearance](Screenshots/tunnel-092-main-entrance-clearance.png)
- Collision view: [0.9.2 collider wireframe](Screenshots/tunnel-092-collider-wireframe.png)
- [Enter, turn and leave](Videos/tunnel-092-1-enter-turn-leave.mp4)
- [Two opposing ants pass](Videos/tunnel-092-2-two-ants-pass.mp4)
- [Camera rotates while blocked](Videos/tunnel-092-3-camera-while-blocked.mp4)
- [Camera pushed against walls and ceiling](Videos/tunnel-092-4-camera-surface-test.mp4)

## Known verification limitation

The Codex in-app Chromium runner exposes a Chromium pointer-lock bug after the
canvas is clicked. This does not come from Unity and did not prevent keyboard
input or the nest-to-surface transition. Native pointer-lock/fullscreen behavior
should still be spot-checked in current Chrome or Edge on the public build.
