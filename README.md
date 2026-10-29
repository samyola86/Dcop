# BAD GUY: The People's Outlaw

An Android-first, Bangladeshi arcade shooter. This repository contains a playable **Shopping District** greybox level and a separate combat practice harness.

**Stack:** Unity **6000.3.6f1**, URP, C#, VS Code and Blender. Use this exact Unity version; do not accept an automatic editor upgrade when opening the project.

## Fastest setup for a collaborator (Windows 10/11, x64)

Install [Git for Windows](https://git-scm.com/downloads/win) if `git` is unavailable. In PowerShell:

```powershell
git clone https://github.com/samyola86/Dcop.git
cd Dcop
powershell -NoProfile -ExecutionPolicy Bypass -File .\setup.ps1 -Install -Open
```

The setup script locates or installs **Unity Hub**, the pinned **Unity editor**, **VS Code**, and Microsoft's **Unity extension**. It then opens the workspace and Unity level. `-Install` accepts the package/source agreements for software it installs. It uses WinGet and the official Hub CLI; internet and possibly Windows administrator approval are required. It does not upgrade a tool it already finds.

**One manual first-time step:** sign in to Unity Hub and activate an appropriate Unity license (Personal if eligible, otherwise your organization's license). Credentials and licensing cannot be supplied by this repo. If setup stops before that is complete, finish it in Hub and run the same command again. There is no fully unattended first-time licensing promise.

The execution-policy option applies only to that PowerShell process; it does not change your machine's permanent policy. If your organization blocks scripts, use the manual instructions below.

### Include Android and Blender

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\setup.ps1 -Install -Android -ArtTools -Open
```

`-Android` adds Android Build Support, SDK/NDK and OpenJDK for the pinned editor. `-ArtTools` installs Blender if missing. Blender is **not required to play or edit code**: exported FBX assets are committed. Blender source was last exported using **5.2.3 LTS**; WinGet installs its available Blender release, so check version compatibility before resaving source art. For that exact version, use the official [Blender releases](https://www.blender.org/download/previous-versions/).

Unity/editor and Android downloads are large. Allow several GB of free disk space, plus room for Unity's generated Library cache; the first import can take several minutes. A graphics-capable Windows PC is required for the editor and rendered player tests.

## Open and play

1. In Unity select **BAD GUY > Open Level 01 - Shopping District**.
2. Press **Play** at the top and select the **Game** tab. Start with a **16:9 landscape** Game view.
3. Click/tap enemies. **R** reloads; **Space** activates a collected AI charge. HUD buttons provide the same actions.
4. Protect green civilians. Shoot incoming rocks/blades to intercept them. Cyan empty rickshaws yield ammo after three hits.
5. Clear three street encounters, pass the breach transition, then defeat the boss and balcony attackers.

For isolated mechanic testing, use **BAD GUY > Create or open combat harness**. Assets are placeholders; balance and transition timing are provisional. There are no live ads or payments. Android performance has not been certified by the desktop tests.

**Correct Unity project folder:** `game/BadGuy` inside your clone, not the repository root. In Unity Hub use **Add > Add project from disk** and choose that folder. If using **Import projects**, choose its parent `game` folder. Never create a new project over the checkout.

## Setup command reference

Run from the repository root (the script also works by absolute path):

| Command | Action |
|---|---|
| `.\setup.ps1 -CheckOnly` | Locate tools and check editor version; no installs, builds or launches |
| `.\setup.ps1 -Install -Open` | Install missing core tools and open the workspace/game |
| `.\setup.ps1 -Install -Android -ArtTools -Open` | Also install Android modules and Blender |
| `.\setup.ps1 -Open` | Open with existing tools; no installations |
| `.\setup.ps1 -Build` | Build the Windows level and run 16 shared-rule checks |
| `.\setup.ps1 -Test` | Build, then run the standalone level integration check |

If PowerShell blocks a direct invocation, prepend `powershell -NoProfile -ExecutionPolicy Bypass -File` as shown above. Python is **not required** for these commands.

**Save and close this Unity project before `-Build` / `-Test`**. The script refuses to start a second editor on a locked project. Afterward, run `-Open` to resume editing. Runtime checks open a temporary game window and write a fresh report; they exercise progression with test assistance and do not prove final difficulty balance.

Outputs (ignored by Git):
- Windows game: `artifacts/Level01/BadGuyLevel01.exe`
- Build log: `artifacts/collaborator-build.log`
- Test report, screenshots and player log: `artifacts/collaborator-check/`

Custom tool locations:

```powershell
.\setup.ps1 -Open -UnityPath 'D:\Unity\6000.3.6f1\Editor\Unity.exe'
.\setup.ps1 -Install -HubPath 'D:\Apps\Unity Hub\Unity Hub.exe'
```

Environment variables `DCOP_UNITY` and `DCOP_HUB` provide the same overrides. If Hub installs editors to a custom location, provide `-UnityPath` after installation. Setup deliberately does not change your global Hub install directory.

## Manual installation / troubleshooting

- **No WinGet:** install/update [App Installer](https://apps.microsoft.com/detail/9nblggh4nns1) from Microsoft Store, or install tools manually.
- **Unity Hub:** [download](https://unity.com/download). Sign in, activate a license, then install **6000.3.6f1** from the [Unity archive](https://unity.com/releases/editor/archive). Changeset: `bbb010bdb8a3`.
- **Android:** in Hub > Installs > this editor's menu > Add modules, select Android Build Support, Android SDK & NDK Tools, and OpenJDK. Use Hub-matched tools rather than installing random SDK versions.
- **VS Code:** [download](https://code.visualstudio.com/download), install Microsoft's **Unity** extension, and select VS Code in Unity Preferences > External Tools > External Script Editor.
- **Hub CLI fails or requests interaction:** finish the editor/module installation through Hub, then rerun setup. The official Hub CLI is experimental; the GUI is the fallback.
- **Packages fail to restore:** confirm internet access to Unity's package registry. Commit and retain both `Packages/manifest.json` and `Packages/packages-lock.json`; do not upgrade packages as an onboarding fix.
- **Blank default scene:** open **BAD GUY > Open Level 01 - Shopping District**. The environment is generated at runtime, so press Play.
- **Compiler errors:** inspect Unity's Console first. Record the first error and exact editor version before changing dependencies.
- **Project locked:** save and close its existing editor, then retry. Do not delete a live project's lock file.
- **No access to push:** the owner must grant you repository write access. Read/clone access does not grant permission to push.

macOS/Linux collaborators can install the pinned Unity version manually and open `game/BadGuy`; this bootstrap and Windows build workflow are Windows-only and have not been validated on those platforms.

## Work together without breaking assets

Start a branch before editing:

```powershell
git pull --ff-only
git switch -c feature/your-change
```

Use your own branch name. Coordinate who edits each scene/prefab and each `.blend` file; binary art cannot be merged like code. Commit Unity **`.meta` files together with their assets**. Move assets inside Unity so references retain their IDs. Never commit `Library`, `Temp`, local logs, builds or test outputs.

Before sharing:

```powershell
# Save and close Unity first
powershell -NoProfile -ExecutionPolicy Bypass -File .\setup.ps1 -Test
git status
git add <only-the-files-you-intend-to-share>
git commit -m "Describe your change"
git push -u origin HEAD
```

Open a pull request and include the test result. Do not run `git add .` blindly. Before pulling scene changes, save work and leave Play mode; close Unity if imports or package changes are involved. Add Git LFS as a coordinated repository change before introducing large production art; it is not required by the current sample assets.

## Files and optional Python workflow

| Path | Purpose |
|---|---|
| `game/BadGuy/Assets/BadGuy/Runtime` | Shared combat, level director, UI and runtime checks |
| `game/BadGuy/Assets/BadGuy/Scenes` | Shopping District and practice scenes |
| `game/BadGuy/Assets/BadGuy/Settings` | Proposed tuning and render settings |
| `art/blender` | Blender source assets |
| `tools/blender/export_sample.py` | Sample Blender-to-FBX export; overwrites the named sample |
| `dcop.code-workspace` | VS Code workspace and developer tasks |
| `LEVEL01_WORKFLOW.md` | Detailed level controls and editor-bridge workflow |
| `HARNESS_WORKFLOW.md` | Practice harness details |

For the existing Python CLI/VS Code tasks, install **Python 3.11+** separately and ensure `python` works in a new terminal. No pip packages are needed:

```powershell
python tools/harness.py doctor
python tools/harness.py open
python tools/harness.py build-level
python tools/harness.py smoke-level
```

The editor bridge waits while Play is active; stop Play before submitting commands and do not queue duplicates. `build-android` in the Python CLI currently builds the **practice harness**, not the full shopping level or a signed store release.

## Official setup references

**Verification (29 October 2026):** the Windows PowerShell 5.1 setup command was run against a fresh GitHub clone on a workstation with Unity already installed. The Windows build, 16 shared-rule checks and all 35 level integration checks passed. Tool discovery and the missing-editor error path were also checked. Installing software on a completely bare Windows machine, first-time licensing and Android device builds were not exercised; those paths include manual fallbacks above.

- [Unity Hub CLI](https://docs.unity.com/en-us/hub/use-hub-cli)
- [WinGet installation command](https://learn.microsoft.com/en-us/windows/package-manager/winget/install)
- [VS Code for Unity](https://code.visualstudio.com/docs/other/unity)

Setup and game code are shared for repository collaboration. Third-party tools/assets retain their respective licenses; this README does not grant a separate public game license.
