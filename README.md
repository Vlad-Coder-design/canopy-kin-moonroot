# Canopy Kin: Moonroot

An original Unity 6 ant-scale action-strategy vertical slice. All current art is procedurally assembled from Unity primitives and authored colors; no third-party game assets are included.

Open with Unity `6000.0.78f1`. Use **Canopy Kin > Build Windows** or the batch build method `CanopyKin.Editor.BuildGame.BuildWindows`.

Controls: WASD movement, mouse camera, Shift sprint, Space vault, E interact/upgrade, left mouse bite, 1 gather, 2 attack, 3 follow, 4 defend, F5 save, F9 load, Escape cursor.

## Play in a browser

Public WebGL build: https://vlad-coder-design.github.io/canopy-kin-moonroot/

The committed `Builds/WebGL` artifact is deployed automatically by `.github/workflows/pages.yml` on every push to `main`. The verified low-memory local build command uses `-job-worker-count 1 --burst-disable-compilation` with `CanopyKin.Editor.BuildGame.BuildWebGL`.
