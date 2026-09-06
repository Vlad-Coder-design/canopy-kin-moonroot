# Canopy Kin: Moonroot 0.9.3 — explorable nest and nursery acceptance

Date: 2026-09-06

## Scope implemented

- Rebuilt the underground home from five prototype chambers and four corridors into nine authored chambers connected by twelve curved, multi-route tunnels.
- Added the great nursery, queen chamber, central crossroads, food/seed store, dedicated egg gallery, larva/pupa gallery, sanitation/refuse chamber, guard/work chamber, and the defensive entrance vestibule.
- Added wide main and service passages with individual portal openings, uneven earthen shells, solid floors, walls and ceilings, and a closed underground safety envelope.
- Kept all structural collision enabled. The movement fix excludes the walkable floor only from the extra horizontal wall probe; the CharacterController continues to collide with floors, walls, ceiling, rocks and terrain.
- Trimmed tunnel collision shells at chamber junctions while retaining the chamber shell, removing the hidden overlapping barriers that previously blocked the player at entrances.
- Increased chamber portal height to clear the player body, antennae and close third-person camera.
- Added separate egg, larva and pupa nursery areas, low non-blocking earthen berms, perimeter brood, storage cargo, queen activity, and biological refuse handling appropriate to a Formica rufa colony.
- Added twelve collision-aware ambient workers. They travel on authored room-to-room routes, yield to the player and each other, cannot form a solid plug, and visibly carry eggs, larvae, pupae, seeds, protein and refuse.
- Added brighter warm/cool indirect lighting and seven shadow-free guide lights so floor shape, tunnel mouths, workers and brood remain readable without losing the underground look.
- Surface-to-nest entry now places the player in the entrance vestibule rather than inside the nursery. The first mission still begins in the nursery as authored.

## Runtime geometry specification

| Metric | Result |
| --- | ---: |
| Chambers | 9 |
| Tunnels | 12 |
| Normal passage minimum | 2.40 m |
| Busy passage minimum | 2.80 m |
| Maximum passage width | 3.60 m |
| Clear passage height | 2.05–2.65 m |
| Player collider diameter | 0.46 m |
| Player collider height | 0.68 m |
| Camera collision radius | 0.19 m |
| Ambient workers | 12 |

## Windows build acceptance

The actual packaged `Builds/Windows/CanopyKin.exe` was launched with `-nest-home-qa`.

- Result: `MOONROOT_NEST_HOME_QA_OK`
- Tests: 28 passed, 0 failed
- Complete-tour waypoints: 117
- Anti-stuck recoveries: 0
- Camera containment in nursery: 24/24 clear samples
- Moving ambient workers: 12/12
- Complete route: entrance → central crossroads → full nursery loop → egg gallery → sanitation → queen → food storage → alternate entrance route → guard chamber → pupa gallery → nursery → central crossroads → surface
- Exit transition returned the player to a valid outdoor position.

The packaged executable was also launched with `-tunnel-clearance-qa`.

- Result: `MOONROOT_TUNNEL_CLEARANCE_QA_OK`
- Tests: 16 passed, 0 failed
- Entry and exit traversal completed without snags.
- A 360-degree camera turn remained outside wall and ceiling collision in 16/16 samples.
- Two ants passed in opposite directions at 0.44 m minimum separation.
- The player crossed active two-way NPC traffic.

Evidence:

- `D:/moonroot-nest-093-runtime-qa-final.log`
- `D:/moonroot-nest-093-tunnel-qa.log`
- `QA/Screenshots/nest-093-great-nursery-gameplay.png`
- `QA/Screenshots/nest-093-great-nursery-collision.png`

## WebGL acceptance

The production WebGL build was served over local HTTP and opened in a clean Codex in-app Chromium tab.

- Loading screen advanced to the Unity canvas.
- The introduction was visible and the Start button entered real gameplay in the rebuilt nursery.
- Keyboard movement changed the player position/view inside the room.
- Runtime log reported `chambers=9 tunnels=12` and `edition=WebOptimized`.
- No Unity exception, missing file, decompression, MIME, WebAssembly, out-of-memory or black-screen error occurred.

Measured WebGL profile (20 seconds):

| Metric | Result |
| --- | ---: |
| Average FPS | 59.70 |
| Average frame | 16.75 ms |
| p95 frame | 17.00 ms |
| Average batches | 418.49 |
| Average SetPass calls | 285.87 |
| Average triangles | 675,890 |
| GC allocation/frame | 0 B |
| Allocated memory | 155,367,886 B |
| Reserved memory | 167,835,043 B |

The in-app Chromium build emits its generic pointer-lock message (`If you see this error we have a bug. Please report this bug to chromium.`) when mouse capture is requested. This is a Chromium host issue rather than a Unity/game exception; keyboard gameplay and rendering continue. Ordinary desktop Chrome is the preferred public play target.
