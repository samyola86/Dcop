using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BadGuy
{
    // Development-player opt-in. Walks the production scheduler and uses production screen rays.
    public sealed class ShoppingLevelSmoke : MonoBehaviour
    {
        HarnessController combat;
        ShoppingDistrictLevel level;
        readonly List<string> checks=new List<string>();
        string directory,error;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-levelSmoke")>=0)new GameObject("Shopping level integration verifier").AddComponent<ShoppingLevelSmoke>();
        }
        void Expect(bool condition,string name)
        {
            if(!condition)throw new Exception(name);
            checks.Add(name);Debug.Log("LEVEL_PASS "+name);
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;
            directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../../"));
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-harnessReportDir");
            if(index>=0 && index+1<args.Length)directory=args[index+1];
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(1);yield return null;
            var routine=Run();float deadline=Time.realtimeSinceStartup+150;
            while(true)
            {
                object current=null;bool next=false;
                try {if(Time.realtimeSinceStartup>deadline)throw new Exception("Integration watchdog");next=routine.MoveNext();if(next)current=routine.Current;}
                catch(Exception ex){error=ex.ToString();Debug.LogException(ex);break;}
                if(!next)break;yield return current;
            }
            Time.timeScale=1;
            File.WriteAllText(Path.Combine(directory,"level-runtime-report.json"),JsonUtility.ToJson(new Report{success=error==null,error=error??"",checks=checks.ToArray(),width=Screen.width,height=Screen.height},true));
            Application.Quit(error==null?0:1);
        }
        HarnessTarget[] Targets()=>FindObjectsByType<HarnessTarget>(FindObjectsSortMode.None);
        bool VisibleRay(Vector3 world,out RaycastHit hit)
        {
            Physics.SyncTransforms();var point=combat.View.WorldToScreenPoint(world);
            if(point.z<=0||point.x<0||point.x>Screen.width||point.y<0||point.y>Screen.height){hit=default;return false;}
            return Physics.Raycast(combat.View.ScreenPointToRay(point),out hit,100);
        }
        void FireAt(Vector3 world) {Physics.SyncTransforms();combat.Shoot(combat.View.WorldToScreenPoint(world));}
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));yield return new WaitForSecondsRealtime(.3f);
        }
        IEnumerator Run()
        {
            combat=FindFirstObjectByType<HarnessController>();level=FindFirstObjectByType<ShoppingDistrictLevel>();
            Expect(combat!=null&&level!=null&&combat.LevelMode,"Separate ShoppingDistrict scene wiring");
            Expect(combat.Stage=="Exterior"&&level.Stop==0,"Starts in outdoor section one");
            Time.timeScale=3;
            // Full depletion with unrelated loot present must still yield reachable, free emergency ammo.
            combat.Drop(PickupKind.Biryani,new Vector3(1,1,5),25);
            combat.State.Magazine=combat.State.Reserve=0;combat.EnsureAmmoRecovery();yield return null;
            HarnessPickup emergency=null;
            foreach(var pickup in FindObjectsByType<HarnessPickup>(FindObjectsSortMode.None))if(pickup.Emergency)emergency=pickup;
            Expect(emergency!=null&&VisibleRay(emergency.transform.position,out var emergencyHit)&&emergencyHit.collider.GetComponent<HarnessPickup>()==emergency,"Emergency ammo visible despite unrelated pickups");
            FireAt(emergency.transform.position);
            Expect(combat.State.Reserve>0&&combat.State.Magazine==0,"Empty-empty pickup costs no round");
            combat.Reload();yield return new WaitForSeconds(combat.Tuning.ReloadSeconds+.1f);
            Expect(combat.State.Magazine>0,"Emergency ammo reload completes");
            // Physical cover test on a deliberately isolated enemy socket.
            combat.NewRun();combat.AttacksEnabled=false;combat.Spawn(ActorKind.Cop,new Vector3(-3,0,7));yield return new WaitForSeconds(.5f);
            HarnessTarget cop=null;foreach(var t in Targets())if(t.Kind==ActorKind.Cop)cop=t;
            Expect(cop!=null&&cop.GetComponent<LevelActorMotion>().Hidden,"Cop crouches behind physical cover");
            Expect(VisibleRay(new Vector3(-3,.7f,7),out var coverHit)&&coverHit.collider.name=="Cop cover","Cover intercepts incoming player ray");
            combat.AttacksEnabled=true;float until=Time.time+6;
            while(cop.ReleaseAt<0&&Time.time<until)yield return null;
            yield return new WaitForSeconds(.15f);
            Expect(cop.ReleaseAt>0&&cop.GetComponent<LevelActorMotion>().ReadyToFire&&VisibleRay(cop.transform.position+Vector3.up*2,out var copHit)&&copHit.collider.GetComponent<HitRegion>()?.Owner==cop,"Cop visibly emerges before firing");
            var precision=cop.PrecisionRegion.localScale;
            Expect(Mathf.Abs(precision.x-.2f)<.001f&&Mathf.Abs(precision.y-.16f)<.001f,"Precision dimensions preserved");
            combat.NewRun();combat.ClearEncounter();combat.AttacksEnabled=true;
            combat.Spawn(ActorKind.Knife,new Vector3(0,0,7));yield return null;
            HarnessTarget knife=Targets()[0];knife.NextAttack=Time.time;
            until=Time.time+4;while(FindObjectsByType<HarnessProjectile>(FindObjectsSortMode.None).Length==0&&Time.time<until)yield return null;
            var projectiles=FindObjectsByType<HarnessProjectile>(FindObjectsSortMode.None);
            Expect(projectiles.Length==1,"Blade warning releases one interceptable projectile");
            int score=combat.State.Score;FireAt(projectiles[0].transform.position);yield return null;
            Expect(combat.State.Score==score+50,"Screen shot intercepts incoming blade");
            // Natural pedestrian lifecycle; no direct cleanup calls in this interval.
            combat.NewRun();combat.AttacksEnabled=false;
            combat.Spawn(ActorKind.Civilian,new Vector3(-7,0,3));combat.Spawn(ActorKind.Bhabi,new Vector3(-7,0,3));
            var walkers=new List<HarnessTarget>();foreach(var t in Targets())if(t.Civilian)walkers.Add(t);
            yield return new WaitForSeconds(12);
            bool left=true;foreach(var t in walkers)left &= t==null;
            Expect(left,"Civilian and phone-carrying Bhabi leave naturally");
            Expect(Array.FindAll(Targets(),t=>t.Civilian).Length<=2,"Pedestrian population remains bounded");
            combat.State.AIReady=true;combat.State.Activate(8);combat.Spawn(ActorKind.Bhabi,new Vector3(0,0,4));
            foreach(var t in Targets())if(t.Kind==ActorKind.Bhabi)t.TakeHit(HitZone.Body);
            Expect(combat.State.CiviliansHit==0,"Shared AI Overdrive protects civilians in level");
            // Seed replay includes actor types AND socket choices.
            combat.NewRun();yield return new WaitForSeconds(1.6f);var first=Targets()[0];Vector3 firstHome=first.GetComponent<LevelActorMotion>().Home;ActorKind firstKind=first.Kind;
            combat.NewRun();yield return new WaitForSeconds(1.6f);first=Targets()[0];
            Expect(first.Kind==firstKind&&first.GetComponent<LevelActorMotion>().Home==firstHome,"Seed reproduces encounter socket and type");
            // Actual production waves. No defeat-counter or stage shortcuts.
            combat.NewRun();Time.timeScale=3;int visited=-1;bool travelChecked=false;int travelCount=0;int runnerEscapes=0;
            bool populationSafe=true,socketsSafe=true,attacksSafe=true;
            until=Time.time+220;
            while(combat.Stage=="Exterior"&&Time.time<until)
            {
                combat.State.Health=100;combat.State.Reserve=120;
                var live=Targets();int hostiles=0,civilians=0,warnings=0;
                for(int a=0;a<live.Length;a++)
                {
                    if(live[a].Dead)continue;
                    if(live[a].Civilian)civilians++;else hostiles++;
                    if(live[a].ReleaseAt>0)warnings++;
                    if(live[a].Civilian||live[a].Kind==ActorKind.Runner)continue;
                    for(int b=a+1;b<live.Length;b++)
                        if(!live[b].Dead&&!live[b].Civilian&&live[b].Kind!=ActorKind.Runner)
                            socketsSafe &= Vector3.Distance(live[a].GetComponent<LevelActorMotion>().Home,live[b].GetComponent<LevelActorMotion>().Home)>=1.5f;
                }
                populationSafe &= hostiles<=3&&civilians<=2;
                attacksSafe &= warnings<=1&&FindObjectsByType<HarnessProjectile>(FindObjectsSortMode.None).Length<=1;
                if(level.Travelling)
                {
                    if(!travelChecked)
                    {
                        int mag=combat.State.Magazine;float health=combat.State.Health;
                        combat.Shoot(new Vector2(Screen.width/2,Screen.height/2));
                        foreach(var t in Targets())combat.Attack(t);
                        Expect(!combat.AttacksEnabled&&!combat.CanShoot&&combat.State.Magazine==mag&&combat.State.Health==health,"Travel "+(++travelCount)+" locks shooting and attacks");travelChecked=true;
                    }
                    yield return null;continue;
                }
                travelChecked=false;
                if(visited!=level.Stop)
                {
                    visited=level.Stop;
                    Expect(Vector3.Distance(combat.View.transform.position,level.CameraPosition(visited))<.01f,"Distinct camera position for street "+(visited+1));
                    yield return new WaitForSeconds(4.8f);
                    yield return Capture("outdoor-"+(visited+1));
                    if(visited==0)
                    {
                        LevelLootRickshaw rick=null;foreach(var r in FindObjectsByType<LevelLootRickshaw>(FindObjectsSortMode.None))if(r.transform.position.z<5)rick=r;
                        Expect(rick!=null&&VisibleRay(rick.transform.position+Vector3.up*.6f,out var rh)&&rh.collider.GetComponentInParent<LevelLootRickshaw>()==rick,"Marked empty rickshaw has clear shooting lane");
                        for(int n=0;n<3;n++){FireAt(rick.transform.position+Vector3.up*.6f);yield return new WaitForSeconds(.2f);}
                        Expect(!rick.gameObject.activeSelf,"Rickshaw destroyed by three real shots");
                        bool ammo=false;foreach(var p in FindObjectsByType<HarnessPickup>(FindObjectsSortMode.None))ammo |= p.Kind==PickupKind.Ammo&&p.Amount==24;
                        Expect(ammo,"Destroyed empty rickshaw guarantees ammo");
                    }
                }
                if(combat.State.Magazine==0){combat.Reload();yield return new WaitForSeconds(combat.Tuning.ReloadSeconds+.05f);continue;}
                foreach(var target in Targets())
                {
                    if(target.Civilian||target.Dead)continue;
                    if(target.Kind==ActorKind.Runner){runnerEscapes++;continue;} // Exercise escape path and progression, not just perfect kills.
                    Vector3 point=target.transform.position+Vector3.up*2;
                    if(VisibleRay(point,out var hit)&&hit.collider.GetComponent<HitRegion>()?.Owner==target){FireAt(point);break;}
                }
                yield return new WaitForSeconds(.18f);
            }
            Expect(combat.Stage=="Transition"&&level.Defeats==18&&visited==2&&travelCount==2&&runnerEscapes>0,"All three street waves complete without deadlock including escaped runners");
            Expect(populationSafe&&socketsSafe,"Encounter caps and occupied sockets hold throughout all outdoor sections");
            Expect(attacksSafe,"At most one attack warning or projectile is scheduled throughout outdoor combat");
            int transitionAmmo=combat.State.Magazine;combat.Shoot(new Vector2(Screen.width/2,Screen.height/2));
            Expect(!combat.IsCombat&&combat.State.Magazine==transitionAmmo,"Breach locks shooting and clears enemies");
            yield return Capture("breach-mock-banner");
            until=Time.time+13;while(combat.Stage=="Transition"&&Time.time<until)yield return null;
            Expect(combat.Stage=="Interior","Timed twelve-second preparation enters atrium");
            yield return new WaitForSeconds(2.2f);
            HarnessTarget boss=null;bool balcony=false;
            foreach(var t in Targets()){if(t.Kind==ActorKind.Boss)boss=t;else if(!t.Civilian)balcony |= t.transform.position.y>=3;}
            Expect(boss!=null&&boss.transform.position.y==0&&balcony,"Ground-floor boss and upper-balcony support spawn");
            Expect(VisibleRay(boss.transform.position+Vector3.up*2,out var bh)&&bh.collider.GetComponent<HitRegion>()?.Owner==boss,"Boss silhouette has unobstructed shooting lane");
            yield return Capture("indoor-atrium");
            until=Time.time+60;
            while(combat.Stage=="Interior"&&Time.time<until)
            {
                combat.State.Health=100;combat.State.Reserve=120;
                if(combat.State.Magazine==0){combat.Reload();yield return new WaitForSeconds(combat.Tuning.ReloadSeconds+.05f);continue;}
                FireAt(boss.transform.position+Vector3.up*2);yield return new WaitForSeconds(.18f);
            }
            Expect(combat.Stage=="Results"&&!combat.IsCombat,"Boss defeated by screen shots reaches results");
            yield return Capture("results");
            combat.NewRun();yield return null;
            Expect(combat.Stage=="Exterior"&&level.Stop==0&&level.Defeats==0&&combat.State.Health==100&&combat.State.Score==0&&combat.State.Magazine==combat.Tuning.MagazineSize,"Results restart resets level state");
            combat.State.Health=1;combat.Spawn(ActorKind.Cop,new Vector3(0,0,8));
            foreach(var t in Targets())if(t.Kind==ActorKind.Cop){combat.Attack(t);break;}
            Expect(combat.State.Health==0&&!combat.CanShoot,"Defeat disables combat");yield return Capture("defeat");
            combat.NewRun();yield return null;
            Expect(combat.IsCombat&&level.Defeats==0,"Defeat restart is playable");
            combat.BeginTransition();combat.SkipTransition();yield return null;
            Expect(combat.Stage=="Interior","Skip-preview action enters atrium");
            combat.NewRun();
        }
        [Serializable]class Report {public bool success;public string error;public string[] checks;public int width,height;}
    }
}
