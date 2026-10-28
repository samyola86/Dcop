using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace BadGuy
{
    // Opt-in standalone runtime verification: no automation hooks in ordinary play.
    public sealed class HarnessSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-harnessSmoke")>=0) new GameObject("Smoke verifier").AddComponent<HarnessSmoke>();
        }
        IEnumerator Start()
        {
            yield return null; yield return null;
            string dir=Path.Combine(Application.dataPath,"../../");
            var args=Environment.GetCommandLineArgs();
            int index=Array.IndexOf(args,"-harnessReportDir");
            if(index>=0 && index+1<args.Length) dir=args[index+1];
            Directory.CreateDirectory(dir);
            string error=null; int checks=0;
            try
            {
                var harness=FindFirstObjectByType<HarnessController>();
                if(harness==null) throw new Exception("Missing harness"); checks++;
                var cop=FindFirstObjectByType<HarnessTarget>();
                foreach(var t in FindObjectsByType<HarnessTarget>(FindObjectsSortMode.None)) if(t.Kind==ActorKind.Cop) cop=t;
                Vector3 screen=harness.View.WorldToScreenPoint(cop.PrecisionRegion.position);
                if(!Physics.Raycast(harness.View.ScreenPointToRay(screen),out RaycastHit hit,100) || hit.collider.GetComponent<HitRegion>()?.Zone!=HitZone.Crotch) throw new Exception("Precision ray did not resolve crotch"); checks++;
                cop.TakeHit(HitZone.Crotch);
                if(harness.State.Kills!=1 || harness.State.Score!=400) throw new Exception("Kill/score resolution failed"); checks++;
                harness.State.AIReady=true; harness.State.Activate(8);
                foreach(var t in FindObjectsByType<HarnessTarget>(FindObjectsSortMode.None)) if(t.Kind==ActorKind.Bhabi) t.TakeHit(HitZone.Body);
                if(harness.State.CiviliansHit!=0) throw new Exception("Civilian protection failed"); checks++;
                harness.NewRun(); harness.BeginTransition();
                if(harness.IsCombat) throw new Exception("Transition permits combat"); checks++;
                harness.LoadStage("Interior");
                bool boss=false; foreach(var t in FindObjectsByType<HarnessTarget>(FindObjectsSortMode.None)) boss |= t.Kind==ActorKind.Boss;
                if(!boss) throw new Exception("Boss missing"); checks++;
                harness.NewRun();
            }
            catch(Exception ex) { error=ex.ToString(); Debug.LogException(ex); }
            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"harness-smoke.png"));
            yield return new WaitForSecondsRealtime(2);
            File.WriteAllText(Path.Combine(dir,"runtime-report.json"),JsonUtility.ToJson(new Report{passed=checks,error=error??"",success=error==null},true));
            Application.Quit(error==null?0:1);
        }
        [Serializable] class Report { public int passed; public string error; public bool success; }
    }
}

