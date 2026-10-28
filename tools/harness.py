"""BAD GUY developer harness. Python standard library only; run from any folder."""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / 'game' / 'BadGuy'
ARTIFACTS = ROOT / 'artifacts'
UNITY_VERSION = '6000.3.6f1'

def unity():
    value = os.environ.get('DCOP_UNITY')
    return Path(value) if value else Path('C:/Program Files/Unity/Hub/Editor') / UNITY_VERSION / 'Editor/Unity.exe'

def blender():
    value = os.environ.get('DCOP_BLENDER') or shutil.which('blender')
    if value:
        return Path(value)
    candidates = sorted(Path('C:/Program Files/Blender Foundation').glob('Blender */blender.exe'))
    return candidates[-1] if candidates else Path('blender.exe')

def doctor():
    editor = unity()
    android = editor.parent / 'Data/PlaybackEngines/AndroidPlayer'
    report = {
        'root': str(ROOT), 'project': str(PROJECT),
        'unity': str(editor), 'unity_available': editor.is_file(),
        'blender': str(blender()), 'blender_available': blender().is_file(),
        'vscode_available': bool(shutil.which('code') or shutil.which('code.cmd')),
        'android_module_available': android.is_dir(),
        'note': 'Android requires matching SDK/NDK and OpenJDK too. No live ads installed.'
    }
    print(json.dumps(report, indent=2))
    return 0 if report['unity_available'] else 1

def editor_open():
    return (PROJECT / 'Temp/UnityLockfile').exists()

def editor_command(command):
    ARTIFACTS.mkdir(exist_ok=True)
    request = ARTIFACTS / 'level-command.txt'
    receipt = ARTIFACTS / 'level-editor-result.json'
    if request.exists() or (ARTIFACTS / 'level-editor-busy.json').exists():
        print('An editor command is already pending/running. Inspect its result before retrying.', file=sys.stderr)
        return 1
    previous = receipt.stat().st_mtime_ns if receipt.exists() else 0
    with request.open('x') as stream:
        stream.write(command)
    print('Submitted to the existing editor. Stop Play if active; no second editor is launched.', flush=True)
    deadline = time.monotonic() + 180
    while time.monotonic() < deadline:
        if receipt.exists() and receipt.stat().st_mtime_ns != previous:
            result = json.loads(receipt.read_text())
            print(json.dumps(result, indent=2))
            return 0 if result.get('success') else 1
        time.sleep(.5)
    print('Still pending: inspect the editor and artifacts/level-command.txt before retrying.', file=sys.stderr)
    return 1

def batch(method, name, owner='HarnessSetup'):
    if editor_open():
        print('Project is open in Unity. Use the BAD GUY menu or build-level bridge; do not start a second editor.', file=sys.stderr)
        return 1
    if not unity().is_file():
        print('Unity not found. Set DCOP_UNITY to Unity.exe.', file=sys.stderr)
        return 1
    ARTIFACTS.mkdir(exist_ok=True)
    log = ARTIFACTS / (name + '.log')
    cmd = [str(unity()), '-batchmode', '-nographics', '-quit', '-projectPath', str(PROJECT),
           '-executeMethod', owner + '.' + method, '-logFile', str(log)]
    print('Unity', method, '\nLog:', log, flush=True)
    result = subprocess.run(cmd)
    if log.exists():
        lines = log.read_text(encoding='utf-8', errors='replace').splitlines()
        for line in lines:
            if any(token in line for token in ('BADGUY_', 'error CS', 'Exception:', 'error:')):
                print(line)
    return result.returncode

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['doctor','open','validate','build-windows','build-android','smoke','blender-sample','build-level','smoke-level'])
    args = parser.parse_args()
    if args.command == 'doctor': return doctor()
    if args.command == 'open':
        if editor_open():
            print('Project already open. BAD GUY > Open Level 01 - Shopping District, then Play.')
            return 0
        subprocess.Popen([str(unity()), '-projectPath', str(PROJECT)])
        return 0
    if args.command == 'build-level':
        return editor_command('build') if editor_open() else batch('BuildLevel', 'build-level', 'ShoppingLevelSetup')
    if args.command == 'smoke-level':
        exe = ARTIFACTS / 'Level01/BadGuyLevel01.exe'
        if not exe.exists():
            print('Run build-level first.', file=sys.stderr); return 1
        output = ARTIFACTS / 'level-check'
        output.mkdir(exist_ok=True)
        return subprocess.run([str(exe), '-levelSmoke', '-harnessReportDir', str(output),
                               '-screen-width','1280','-screen-height','720','-screen-fullscreen','0',
                               '-logFile',str(output/'player.log')], timeout=180).returncode
    if args.command == 'validate': return batch('SetupAndValidate', 'validate')
    if args.command == 'build-windows': return batch('BuildWindows', 'build-windows')
    if args.command == 'build-android': return batch('BuildAndroid', 'build-android')
    if args.command == 'smoke':
        exe = ARTIFACTS / 'Windows/BadGuyHarness.exe'
        if not exe.exists():
            print('Run build-windows first.', file=sys.stderr); return 1
        ARTIFACTS.mkdir(exist_ok=True)
        result = subprocess.run([str(exe), '-harnessSmoke', '-harnessReportDir', str(ARTIFACTS),
                                 '-screen-width', '1280', '-screen-height', '720', '-logFile', str(ARTIFACTS/'runtime.log')], timeout=120)
        report = ARTIFACTS / 'runtime-report.json'
        if report.exists(): print(report.read_text())
        return result.returncode
    if args.command == 'blender-sample':
        return subprocess.run([str(blender()), '--background', '--factory-startup', '--python', str(ROOT/'tools/blender/export_sample.py')]).returncode

if __name__ == '__main__':
    raise SystemExit(main())
