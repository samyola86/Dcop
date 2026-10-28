# Level 01: Shopping District

Open Unity 6000.3.6f1 on `game/BadGuy`. Select **BAD GUY > Open Level 01 - Shopping District**, then Play. The separate practice scene remains available through **BAD GUY > Create or open combat harness**.

Click/tap targets in the world. **R** or Reload reloads; **Space** or AI activates a collected charge. Yellow is the unchanged precision region. Green silhouettes are civilians; the phone marks Gulshan Bhabis. Cyan empty rickshaws take three shots and guarantee ammo. If magazine and reserve both reach zero, tap the cyan emergency supply without spending a bullet, then reload. Pause/Resume is at bottom right.

Clear six threats in each of three street sections. Cops duck behind physical cover and stand before shooting. Unarmed cadres throw rocks or stronger, warned blades; shoot projectiles to intercept them. Magenta runners cross the street and can drop biryani if defeated; an escaped runner costs five health but still resolves its encounter. Pedestrians exit naturally. Camera travel locks combat. After the third section, a proposed 12-second breach has a labeled mock banner and Skip Preview. The Patron is a prototype ground-floor boss with balcony support. Results and defeat both offer same-seed restart.

All balance, six-threat waves, boss identity and timing are prototype proposals. Geometry, signage, silhouettes and UI are greybox assets. No real ads, payments, downloads or new packages are used.

## Verification commands

The existing local editor bridge uses `artifacts/level-command.txt` and `artifacts/level-editor-result.json`. It accepts `inspect`, `create`, `build`, `build-practice`; no arbitrary method execution. Only submit one command at a time, after the previous result arrives. It waits during play/compile/import, and never starts another editor. Stop Play before submitting. Build logs are in the active Unity Editor log. Build commands rerun the 16 shared combat-rule checks.

Development player: `artifacts/Level01/BadGuyLevel01.exe`.

Opt-in full integration check (PowerShell):

```powershell
& ./artifacts/Level01/BadGuyLevel01.exe -levelSmoke -harnessReportDir "$PWD/artifacts/level-check" -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile "$PWD/artifacts/level-check/player.log"
```

The verifier drives production scheduling, screen rays and reloads. It replenishes health/reserve during the long wave sweep to isolate progression from balance; separate checks exercise damage/defeat and empty-ammo recovery. Its screenshots are actual rendered player frames. It exits with a nonzero code on failure and writes `level-runtime-report.json`. This is integration evidence, not proof of difficulty balance or phone performance. No test hooks run in ordinary play.

The Python CLI also supports `python tools/harness.py build-level` and `python tools/harness.py smoke-level`. When Python is not on PATH, the installed interpreter is `C:\Users\samiu\AppData\Roaming\uv\python\cpython-3.11.15-windows-x86_64-none\python.exe`. See LEVEL01_VALIDATION.md for final test evidence.
