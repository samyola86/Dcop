# BAD GUY development harness

Engine: **Unity 6000.3.6f1 + URP**. Code: **C# in VS Code**. Art: **Blender -> FBX -> Unity**.
Repository: https://github.com/samyola86/Dcop

This is a development range and integration foundation, not the finished first level. Colored boxes are deliberate placeholders. All numerical balance remains provisional. No live advertising, payments, finished character art, voice acting or phone-performance claim is included.

## Quick start

Open `C:\Users\samiu\dcop` in VS Code. In a terminal:

```powershell
python tools/harness.py doctor
python tools/harness.py validate
python tools/harness.py open
```

If `python` is not on PATH, use your Python executable. `DCOP_UNITY` and `DCOP_BLENDER` can override tool executable paths.

In Unity use **BAD GUY -> Create or open combat harness**, then press **Play**. Set the Game view to **1280 x 720** or another **16:9 landscape** size. The prototype debug UI is designed for that aspect ratio.

**Close this Unity project before running batch commands** (`validate`, builds). Unity permits only one editor process per project. Other Unity projects can remain open. While the project is open, use the BAD GUY editor menu for rule checks instead.

## What to do in Play mode

1. The range starts with a teal Cop and green phone-carrying Bhabi. Attacks are OFF initially.
2. Click/tap a target. Yellow is the tiny crotch zone, the top cube is the head. Green actors are civilians.
3. Reload using the bottom button. Body, head, limb and crotch zones use the same shared damage rules.
4. Use **Force AI pickup**, then shoot the magenta pickup and activate AI at the bottom. Civilian hits should cause no casualty while active; ammo still decreases.
5. Spawn rock/knife enemies and enable attacks. A white flashing body warns of attack. Orange blades and grey rocks are shootable projectiles. The warning text substitutes for unrecorded voice lines.
6. Use **Empty ammo recovery test**: tap the emergency ammo without spending a round, then reload.
7. Start exterior: after eight enemy defeats, the mock breach sequence leads to a boss with upper support spawns. Or jump straight to the boss.
8. Reset with the same seed to repeat encounter choices. Same seed plus the same inputs reproduces random choices; it does not force identical player-driven timing.

Range spawn controls are debug tools and can deliberately create overlaps; the scripted encounter uses a small active-actor cap and one attack warning/projectile at a time. The full production director and cover AI are future work.

## Everyday workflow

**Edit C# -> Unity compiles -> run rules checks -> Play range -> play a seeded scenario -> build desktop -> smoke test -> commit reviewed source.**

```powershell
python tools/harness.py validate
python tools/harness.py build-windows
python tools/harness.py smoke
```

`validate` generates/opens the scene and runs shared-rule checks. `build-windows` also validates. `smoke` launches the built player, verifies scene wiring, precision ray collision, kill/score, AI immunity, transition lock and boss spawn, captures a screenshot and exits. A failure returns a nonzero exit code. Reports/logs/builds live in ignored `artifacts/`.

Tune `game/BadGuy/Assets/BadGuy/Settings/HarnessTuning.asset` in Unity's Inspector. Changes to an asset during Play mode may persist: keep a known preset in Git and review diffs.

## Blender workflow

```powershell
python tools/harness.py blender-sample
```

This creates `art/blender/cover_crate.blend` and exports `game/BadGuy/Assets/BadGuy/Art/Models/SM_CoverCrate_Prototype.fbx`. Open the `.blend` for editing; export updated FBX with consistent names and meter-scale dimensions. Unity imports FBX into the Project panel. The sample is not automatically placed in the scene; drag it in when testing art. Keep collision/damage logic separate from the replaceable visual mesh.

Use FBX exports rather than relying on automatic `.blend` imports. Commit source art and exports; preserve Unity `.meta` files. Configure Git LFS before adding large production binary assets.

## Android

The discovered Unity installation initially has **no Android Build Support**. In Unity Hub, add to **6000.3.6f1**: Android Build Support, Android SDK & NDK Tools, and OpenJDK. Then:

```powershell
python tools/harness.py doctor
python tools/harness.py build-android
```

The output is a development APK, not a signed store release. Touch uses Unity IMGUI mouse/touch event handling for this prototype; verify it and the layout on the target phone before replacing the harness with the production HUD. Real hardware performance, thermal behavior and device installation remain unverified until tested.

## Ads and transition

The transition has a **mock banner**, a proposed 12-second preparation beat, and a skip-preview button. Combat and gameplay timers stop. There is no AdMob SDK, ad request or paid impression. Do not replace the mock with a clickable gameplay surface. Final timing, replay skip policy and monetization still require design work. Use Google test ads when implementing the real reserved banner view.

## Code map

- `Runtime/CombatRules.cs`: shared damage, hitbox-area math, run resources and civilian protection.
- `Runtime/HarnessTuning.cs`: editable proposed balance.
- `Runtime/HarnessController.cs`: scenarios, seeded choices, UI, shooting, transitions and loot.
- `Runtime/HarnessTarget.cs`: runtime target zones, warnings, projectiles and pickups.
- `Runtime/HarnessSmoke.cs`: opt-in standalone integration checks.
- `Editor/HarnessSetup.cs`: scene/URP setup, rule checks and build entry points.
- `tools/harness.py`: repeatable commands; Python standard library only.

## Git and safety

The folder originally contained an unborn repository with staged files/deletions from `chapriracing`. Those index entries were preserved. The previous origin is retained as `legacy-chapriracing`; `origin` points to Dcop. Do not run `git add .` and commit without reviewing that old index. This task does not push or publish the game. Unity generated caches, local reports and builds are ignored.

## Next production work

Add genuine moving/cover behavior, runner traversal, civilian routes, proper rickshaw loot, boss phases and attack interrupt cooldown; replace debug GUI with a responsive mobile HUD; add native Bangla subtitles/audio; measure on a selected Android phone. The harness establishes shared rules and repeatable validation for that work.
