# Harness validation - 29 October 2026

Validated locally with Unity 6000.3.6f1 on Windows.

| Check | Result |
|---|---|
| Unity import and C# compilation | Passed |
| Shared-rule checks | 16 passed, 0 failed |
| Windows development build | Succeeded |
| Standalone runtime integration checks | 6 passed, 0 failed |
| Captured range screenshot | Inspected: targets and controls visible |
| Blender 5.2.3 LTS source save and FBX export | Passed |
| Android build/device test | Not run: Android module not installed |
| Live AdMob | Not integrated; mock banner only |

Rule checks cover precision and Overdrive damage, Bhabi area math and precedence, AI eligibility, ammo conservation, empty shots, charge stacking, civilian immunity/penalty, resource caps and seeded random choices.

Runtime checks cover scene controller, precision ray collision, defeat/score, AI civilian immunity, transition combat lock and interior boss creation. These are representative integration checks, not exhaustive gameplay tests.

Reports and logs are in ignored `artifacts/`: `rules-report.json`, `runtime-report.json`, `validate.log`, `build-windows.log`, `runtime.log`, `harness-smoke.png`. Windows executable: `artifacts/Windows/BadGuyHarness.exe`.

Existing Git index entries were preserved. `origin` now points to `samyola86/Dcop`; the previous remote remains `legacy-chapriracing`. No commit or push was made. Review the old staged entries before making the first commit.

Known prototype limits: placeholder art, stationary targets, text instead of VO, debug GUI, basic boss/support behavior, no finished cover AI/civilian routes/rickshaw loot, no verified phone performance. Numerical balance and transition timing remain proposals.
