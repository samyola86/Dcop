# Windows collaborator setup. Run from any directory; paths are relative to this script.
[CmdletBinding()]
param(
    [switch]$Install,
    [switch]$Open,
    [switch]$Build,
    [switch]$Test,
    [switch]$Android,
    [switch]$ArtTools,
    [switch]$CheckOnly,
    [string]$UnityPath = $env:DCOP_UNITY,
    [string]$HubPath = $env:DCOP_HUB
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$projectRoot = Join-Path $repoRoot 'game/BadGuy'
$outputRoot = Join-Path $repoRoot 'artifacts'
$versionText = Get-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') -Raw
if ($versionText -notmatch '(?m)^m_EditorVersion:\s*(\S+)') { throw 'Unity version is missing from ProjectVersion.txt.' }
$editorVersion = $Matches[1]
if ($versionText -notmatch 'm_EditorVersionWithRevision:.*\(([a-f0-9]+)\)') { throw 'Unity changeset is missing from ProjectVersion.txt.' }
$editorChangeset = $Matches[1]

function First-Existing([string[]]$Candidates) {
    foreach ($candidate in $Candidates) { if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) { return (Resolve-Path -LiteralPath $candidate).Path } }
    return $null
}
function Find-Unity {
    if ($UnityPath) {
        if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) { throw "Unity override does not exist: $UnityPath" }
        return (Resolve-Path -LiteralPath $UnityPath).Path
    }
    return First-Existing @("$env:ProgramFiles/Unity/Hub/Editor/$editorVersion/Editor/Unity.exe", "$env:LOCALAPPDATA/Programs/Unity/Hub/Editor/$editorVersion/Editor/Unity.exe")
}
function Find-Hub {
    if ($HubPath) {
        if (-not (Test-Path -LiteralPath $HubPath -PathType Leaf)) { throw "Hub override does not exist: $HubPath" }
        return (Resolve-Path -LiteralPath $HubPath).Path
    }
    $classic = First-Existing @("$env:ProgramFiles/Unity Hub/Unity Hub.exe", "$env:LOCALAPPDATA/Programs/Unity Hub/Unity Hub.exe")
    if ($classic) { return $classic }
    # Recent WinGet releases may install Hub as MSIX rather than in Program Files.
    if (Get-Command Get-AppxPackage -ErrorAction SilentlyContinue) {
        foreach ($package in @(Get-AppxPackage | Where-Object { $_.Name -match 'Unity.*Hub' })) {
            if ($package.InstallLocation) {
                $exe = @(Get-ChildItem -LiteralPath $package.InstallLocation -Filter 'Unity Hub.exe' -File -Recurse -ErrorAction SilentlyContinue)
                if ($exe.Count -gt 0) { return $exe[0].FullName }
            }
        }
    }
    return $null
}
function Find-Code {
    $command = Get-Command code -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    return First-Existing @("$env:LOCALAPPDATA/Programs/Microsoft VS Code/bin/code.cmd", "$env:ProgramFiles/Microsoft VS Code/bin/code.cmd")
}
function Find-Blender {
    $command = Get-Command blender -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    $base = "$env:ProgramFiles/Blender Foundation"
    if (Test-Path -LiteralPath $base) {
        $files = @(Get-ChildItem -LiteralPath $base -Filter blender.exe -Recurse | Sort-Object FullName -Descending)
        if ($files.Count -gt 0) { return $files[0].FullName }
    }
    return $null
}
function Install-Package([string]$Id) {
    $winget = Get-Command winget -ErrorAction SilentlyContinue
    if (-not $winget) { throw 'WinGet is missing. Install/update App Installer from Microsoft Store, then rerun. Manual links are in README.md.' }
    Write-Host "Installing $Id through WinGet (installer/UAC prompts may appear)..."
    & $winget.Source install --id $Id --exact --source winget --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -ne 0) { throw "WinGet failed for $Id (exit $LASTEXITCODE). Install it manually, then rerun; see README.md." }
}
function Invoke-Hub([string[]]$HubArguments) {
    Write-Host "Unity Hub: $($HubArguments -join ' ')"
    # Piping a Windows GUI executable makes PowerShell wait for its headless process.
    & $script:hub '--' '--headless' @HubArguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Unity Hub failed (exit $LASTEXITCODE). Sign in and install $editorVersion via Hub; see README.md." }
}
function Assert-EditorVersion([string]$Editor) {
    $fileVersion = (Get-Item -LiteralPath $Editor).VersionInfo.ProductVersion
    if ($fileVersion -notlike "$editorVersion*") { throw "Expected Unity $editorVersion, found '$fileVersion' at $Editor. Use the project's pinned version." }
}
function Invoke-UnityBatch([string]$Method, [string]$LogName) {
    if (Test-Path -LiteralPath (Join-Path $projectRoot 'Temp/UnityLockfile')) { throw 'This project is open in Unity. Save and close it before batch build/test. Use BAD GUY menu commands while editing.' }
    New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
    $logPath = Join-Path $outputRoot $LogName
    Write-Host "Running $Method. First import can take several minutes. Log: $logPath"
    & $script:editor '-batchmode' '-nographics' '-quit' '-projectPath' $projectRoot '-executeMethod' $Method '-logFile' $logPath | Out-Host
    if ($LASTEXITCODE -ne 0) {
        if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Tail 35 | Out-Host }
        throw "Unity failed (exit $LASTEXITCODE). Check $logPath. First-time licensing requires Unity Hub sign-in and license activation."
    }
}

try {
    $script:editor = Find-Unity
    $script:hub = Find-Hub
    $code = Find-Code
    $blender = Find-Blender
    Write-Host "BAD GUY / collaborator setup / Unity $editorVersion"
    Write-Host "Project: $projectRoot"
    if ($CheckOnly) {
        $androidReady = $false
        if ($editor) { $androidReady = Test-Path -LiteralPath (Join-Path (Split-Path $editor) 'Data/PlaybackEngines/AndroidPlayer') }
        [pscustomobject]@{ Unity = $editor; UnityHub = $hub; VSCode = $code; Blender = $blender; AndroidModule = $androidReady; ExpectedVersion = $editorVersion } | Format-List | Out-Host
        if (-not $editor) { Write-Host 'Unity missing. Run .\setup.ps1 -Install -Open (see README for first-time sign-in).' ; exit 1 }
        Assert-EditorVersion $editor
        Write-Host 'Editor found. Licensing, compilation and device compatibility are not checked by -CheckOnly.'
        exit 0
    }
    if ($Install) {
        Write-Host 'Install mode accepts package/source agreements. Downloads require internet; Unity is a large download.'
        if (-not $hub) { Install-Package 'Unity.UnityHub'; $script:hub = Find-Hub }
        if (-not $hub) { throw 'Unity Hub installed in a custom folder. Rerun with -HubPath pointing to Unity Hub.exe.' }
        if (-not $code) { Install-Package 'Microsoft.VisualStudioCode'; $code = Find-Code }
        if ($ArtTools -and -not $blender) { Install-Package 'BlenderFoundation.Blender'; $blender = Find-Blender }
        if (-not $editor) {
            $installArgs = @('install', '--version', $editorVersion, '--changeset', $editorChangeset)
            if ($Android) { $installArgs += @('--module', 'android', 'android-sdk-ndk-tools', 'android-open-jdk') }
            Invoke-Hub $installArgs
            $script:editor = Find-Unity
        }
        if (-not $editor) { throw "Unity is not in the default location. Locate $editorVersion in Hub and rerun with -UnityPath 'D:\path\Editor\Unity.exe'." }
        if ($Android) {
            $module = Join-Path (Split-Path $editor) 'Data/PlaybackEngines/AndroidPlayer'
            $sdk = Join-Path $module 'SDK'
            $ndk = Join-Path $module 'NDK'
            $jdk = Join-Path $module 'OpenJDK'
            if (-not ((Test-Path -LiteralPath $sdk) -and (Test-Path -LiteralPath $ndk) -and (Test-Path -LiteralPath $jdk))) {
                Invoke-Hub @('install-modules', '--version', $editorVersion, '--module', 'android', 'android-sdk-ndk-tools', 'android-open-jdk')
            }
            foreach ($component in @($sdk,$ndk,$jdk)) { if (-not (Test-Path -LiteralPath $component)) { throw "Android component missing: $component. Finish Add modules in Hub, then rerun." } }
        }
        if ($code) {
            & $code '--install-extension' 'visualstudiotoolsforunity.vstuc' | Out-Host
            if ($LASTEXITCODE -ne 0) { Write-Warning 'VS Code Unity extension was not installed. Install the Microsoft Unity extension from VS Code Extensions.' }
        }
    }
    if (-not $editor) { throw 'Unity not found. Run .\setup.ps1 -Install -Open or supply -UnityPath. See README.md.' }
    Assert-EditorVersion $editor
    if ($Build -or $Test) { Invoke-UnityBatch 'ShoppingLevelSetup.BuildLevel' 'collaborator-build.log' }
    if ($Test) {
        $player = Join-Path $outputRoot 'Level01/BadGuyLevel01.exe'
        if (-not (Test-Path -LiteralPath $player)) { throw 'Build returned without creating the level executable.' }
        $reports = Join-Path $outputRoot 'collaborator-check'
        New-Item -ItemType Directory -Force -Path $reports | Out-Null
        $reportPath = Join-Path $reports 'level-runtime-report.json'
        if (Test-Path -LiteralPath $reportPath) { Remove-Item -LiteralPath $reportPath }
        Write-Host 'Running the standalone integration check; a game window appears briefly.'
        & $player '-levelSmoke' '-harnessReportDir' $reports '-screen-width' '1280' '-screen-height' '720' '-screen-fullscreen' '0' '-logFile' (Join-Path $reports 'player.log') | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Level integration check failed (exit $LASTEXITCODE). Inspect $reports." }
        if (-not (Test-Path -LiteralPath $reportPath)) { throw "No fresh integration report was written: $reportPath" }
        $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
        $report | ConvertTo-Json -Depth 8 | Out-Host
        if (-not $report.success) { throw 'Integration report did not pass.' }
    }
    if ($Open) {
        if ($code) { & $code (Join-Path $repoRoot 'dcop.code-workspace') | Out-Host }
        if (Test-Path -LiteralPath (Join-Path $projectRoot 'Temp/UnityLockfile')) { Write-Host 'Project is already open. Use BAD GUY > Open Level 01 - Shopping District.' }
        else {
            $scene = Join-Path $projectRoot 'Assets/BadGuy/Scenes/ShoppingDistrict.unity'
            $arguments = '-projectPath "{0}" -openfile "{1}"' -f $projectRoot,$scene
            # -Open explicitly requests the interactive editor, so show its window.
            Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Normal | Out-Null
            Write-Host 'Unity launched. Complete Unity Hub sign-in/license activation if requested, then press Play in the Game tab.'
        }
    }
    Write-Host 'Done. Game: BAD GUY > Open Level 01 - Shopping District, then Play.'
} catch {
    Write-Error $_ -ErrorAction Continue
    exit 1
}
