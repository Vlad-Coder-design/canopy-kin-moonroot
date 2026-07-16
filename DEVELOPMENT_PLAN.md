# Vertical Slice Progress

- [x] Unity 6 URP project foundation and modular runtime assembly
- [x] Third-person ant movement, camera collision, sprint/stamina, interaction and bite combat
- [x] Original procedural forest-floor region, nest mound, underground chamber, landmarks and resource sites
- [x] Seed/resin gathering, persistent colony resources and constructed nursery upgrade
- [x] Worker/soldier squad formations and four command modes
- [x] Beetle, rival ant and spider threats with pursuit, attacks, health and defeat states
- [x] Guided five-stage mission: forage, resin, predator, rival scout, nursery upgrade, threat reveal
- [x] HUD, controls legend, save/load with version and corruption guards
- [ ] Production art: rigged six-leg animation/IK, authored terrain, audio, localization tables and full settings UI
- [ ] Profiled RTX 3060 release-quality lighting/vegetation pass
- [x] Windows x64 development build, 2/2 edit-mode tests, and 10-second headless runtime smoke test without exceptions

## Verification record (2026-07-16)

- Unity: 6000.0.78f1
- Automated tests: 2 passed, 0 failed (`test-results.xml`)
- Build: `Builds/Windows/CanopyKin.exe`
- Runtime: world bootstrap and procedural material creation validated in a headless player run
- Toolchain note: Burst native compilation crashed the local Unity editor once and exhausted the page file. The project does not use Burst jobs, so verified test/build commands use `--burst-disable-compilation`.
