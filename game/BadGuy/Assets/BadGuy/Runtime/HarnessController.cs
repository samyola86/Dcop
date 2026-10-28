using System;
using System.Collections;
using UnityEngine;

namespace BadGuy
{
    public sealed class HarnessController : MonoBehaviour
    {
        public HarnessTuning Tuning;
        public Material PrototypeMaterial;
        public Camera View;
        public RunState State;
        public bool AttacksEnabled;
        public int Seed = 2026;
        public string Stage = "Range";
        public bool LevelMode;
        public ShoppingDistrictLevel Level;
        public bool Paused => paused;
        public bool CanShoot => IsCombat && !paused && !(LevelMode && Level.Travelling);
        public bool Reloading => reloadRemaining > 0;
        public bool IsCombat => State != null && State.Health > 0 && Stage != "Transition" && Stage != "Results";
        System.Random encounterRandom, lootRandom;
        Transform content;
        string seedText = "2026", message = "Select a scenario or spawn a target. White flash = attack warning.";
        float reloadRemaining, nextFire, nextSpawn, transitionRemaining;
        int waveIndex, exteriorKills;
        bool paused;
        readonly Rect playArea = new Rect(310, 90, 960, 550);

        void Start()
        {
            Application.targetFrameRate = 60;
            if (Tuning == null) Tuning = ScriptableObject.CreateInstance<HarnessTuning>();
            if (View == null) View = Camera.main;
            View.rect = LevelMode ? new Rect(0,0,1,1) : new Rect(310f / 1280, 80f / 720, 960f / 1280, 550f / 720);
            NewRun();
        }
        public void Message(string text) { message = text; Debug.Log("[BAD GUY] " + text); }
        public void NewRun()
        {
            Time.timeScale = 1; paused = false;
            int.TryParse(seedText, out Seed);
            encounterRandom = new System.Random(Seed); lootRandom = new System.Random(Seed ^ 0x5EED);
            State = new RunState { Magazine = Tuning.MagazineSize, Reserve = Tuning.StartingReserve };
            reloadRemaining = 0; nextFire = 0; waveIndex = 0; exteriorKills = 0;
            if (LevelMode) { Level.ResetLevel(); AttacksEnabled=true; LoadStage("Exterior"); }
            else { LoadStage("Range"); Spawn(ActorKind.Cop, new Vector3(-2, 0, 8)); Spawn(ActorKind.Bhabi, new Vector3(2, 0, 8)); }
            if (!LevelMode) Message("Seed " + Seed + ". Practice range; attacks OFF by default.");
        }
        void ClearContent()
        {
            if (content != null) { content.gameObject.SetActive(false); Destroy(content.gameObject); }
            content = new GameObject("Runtime encounter").transform;
        }
        public void ClearEncounter() { ClearContent(); }
        public void LoadStage(string stage)
        {
            ClearContent(); Stage = stage; State.BhabiCasualty = false;
            reloadRemaining = 0; nextSpawn = Time.time + 1;
            if (LevelMode) Level.StageChanged(stage);
            else if (stage == "Interior") Spawn(ActorKind.Boss, new Vector3(0, 0, 9));
        }
        public bool BhabiVisible()
        {
            foreach (var target in FindObjectsByType<HarnessTarget>(FindObjectsSortMode.None))
            {
                if (target.Kind != ActorKind.Bhabi || target.Dead) continue;
                Vector3 p = View.WorldToViewportPoint(target.transform.position + Vector3.up);
                if (p.z > 0 && p.x > 0 && p.x < 1 && p.y > 0 && p.y < 1) return true;
            }
            return false;
        }
        void Update()
        {
            if (paused) return;
            if (Stage == "Transition")
            {
                transitionRemaining -= Time.deltaTime;
                if (transitionRemaining <= 0) LoadStage("Interior");
                return;
            }
            if (!IsCombat) return;
            if (LevelMode && Level.Travelling) return;
            State.OverdriveRemaining = Mathf.Max(0, State.OverdriveRemaining - Time.deltaTime);
            if (reloadRemaining > 0)
            {
                reloadRemaining -= Time.deltaTime;
                if (reloadRemaining <= 0) State.Reload(Tuning.MagazineSize);
            }
            if (!LevelMode && (Stage == "Exterior" || Stage == "Interior"))
            {
                if (Time.time >= nextSpawn && FindObjectsByType<HarnessTarget>(FindObjectsSortMode.None).Length < 5)
                {
                    nextSpawn = Time.time + 3;
                    ActorKind kind = waveIndex % 5 == 0 ? ActorKind.Bhabi : waveIndex % 4 == 0 ? ActorKind.Runner : waveIndex % 3 == 0 ? ActorKind.Cop : encounterRandom.NextDouble() < .8 ? ActorKind.Rock : ActorKind.Knife;
                    Vector3 pos = Stage == "Interior" ? new Vector3(waveIndex % 2 == 0 ? -3 : 3, 2.5f, 11) : new Vector3((waveIndex % 4 - 1.5f) * 2, 0, 8 + encounterRandom.Next(3));
                    if (kind == ActorKind.Bhabi) pos = new Vector3(3.5f, 0, 7);
                    Spawn(kind, pos); waveIndex++;
                }
                if (Stage == "Exterior" && exteriorKills >= 8) BeginTransition();
            }
            EnsureAmmoRecovery();
        }
        public void EnsureAmmoRecovery()
        {
            if (!CanShoot || State.Magazine != 0 || State.Reserve != 0) return;
            foreach (var item in FindObjectsByType<HarnessPickup>(FindObjectsSortMode.None))
                if (item.Emergency && !item.Collected) return;
            Vector3 point = View.transform.position + View.transform.forward * 5 - View.transform.up * .6f;
            Drop(PickupKind.Ammo, point, 24);
            var items = FindObjectsByType<HarnessPickup>(FindObjectsSortMode.None);
            foreach (var item in items) if ((item.transform.position-point).sqrMagnitude<.01f) { item.Emergency=true; item.Remaining=float.PositiveInfinity; }
            Message("OUT OF AMMO: tap the cyan emergency supply, then RELOAD. No round needed.");
        }
        public bool CanTelegraph(HarnessTarget source)
        {
            if (LevelMode && source.TryGetComponent<LevelActorMotion>(out var motion) && motion.Hidden) return false;
            foreach (var other in FindObjectsByType<HarnessTarget>(FindObjectsSortMode.None))
                if (other != source && other.ReleaseAt > 0) return false;
            return FindObjectsByType<HarnessProjectile>(FindObjectsSortMode.None).Length == 0;
        }
        public void Attack(HarnessTarget source)
        {
            if (!IsCombat || !AttacksEnabled || paused || (LevelMode && Level.Travelling)) return;
            if (source.Kind == ActorKind.Cop || source.Kind == ActorKind.Boss)
            {
                float damage = source.Kind == ActorKind.Boss ? Tuning.BossDamage : Tuning.CopDamage;
                State.Health = Mathf.Max(0, State.Health - damage); Message(source.Kind + " fired: -" + damage + " HP");
            }
            else
            {
                var obj = Shape(source.Kind == ActorKind.Knife ? PrimitiveType.Cube : PrimitiveType.Sphere, "Intercept me", source.transform.position + new Vector3(0, 1.7f, -.5f), source.Kind == ActorKind.Knife ? new Vector3(.6f, .13f, .15f) : Vector3.one * .35f, source.Kind == ActorKind.Knife ? new Color(1,.4f,.05f) : Color.gray);
                var proj = obj.AddComponent<HarnessProjectile>(); proj.Owner = this; proj.Damage = source.Kind == ActorKind.Knife ? Tuning.KnifeDamage : Tuning.RockDamage;
            }
        }
        public void OnDefeat(HarnessTarget target, HitZone zone)
        {
            State.Kills++; State.Score += 100;
            if (LevelMode) Level.EnemyDefeated(target);
            if (Stage == "Exterior") exteriorKills++;
            if (target.Kind == ActorKind.Boss) { LoadStage("Results"); Message("Boss defeated. Restart with the same seed to compare."); return; }
            Vector3 p = target.transform.position + Vector3.up;
            if (lootRandom.NextDouble() < Tuning.AmmoChance) Drop(PickupKind.Ammo, p + Vector3.left * .5f, 12);
            if (target.Kind == ActorKind.Runner && lootRandom.NextDouble() < Tuning.BiryaniChance) Drop(PickupKind.Biryani, p, 25);
            if (!State.AIReady && !State.Overdrive && CombatRules.RollAI(zone, Tuning.AIDropChance, lootRandom.NextDouble())) Drop(PickupKind.AI, p + Vector3.right * .5f, 1);
        }
        GameObject Shape(PrimitiveType primitive, string label, Vector3 p, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(primitive); go.name = label; go.transform.SetParent(content); go.transform.position = p; go.transform.localScale = size;
            var renderer = go.GetComponent<Renderer>();
            renderer.material = PrototypeMaterial != null ? new Material(PrototypeMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit")); renderer.material.color = color;
            return go;
        }
        public void Spawn(ActorKind kind, Vector3 position)
        {
            if (!IsCombat) return;
            var root = new GameObject(kind.ToString()); root.transform.SetParent(content); root.transform.position = position;
            var target = root.AddComponent<HarnessTarget>(); target.Owner = this; target.Kind = kind; target.Health = Tuning.HealthFor(kind); target.NextAttack = Time.time + 3;
            Color color = kind == ActorKind.Cop ? new Color(.05f,.6f,.6f) : kind == ActorKind.Bhabi || kind == ActorKind.Civilian ? Color.green : kind == ActorKind.Runner ? Color.magenta : kind == ActorKind.Boss ? new Color(.8f,.15f,.12f) : new Color(.8f,.6f,.12f);
            target.BaseColor = color;
            Region(target, HitZone.Body, new Vector3(0, 1.35f, 0), new Vector3(.75f,.8f,.35f), color);
            Region(target, HitZone.Head, new Vector3(0, 2, 0), new Vector3(.42f,.42f,.35f), color);
            Region(target, HitZone.Limb, new Vector3(-.23f,.45f,0), new Vector3(.22f,.85f,.25f), color);
            Region(target, HitZone.Limb, new Vector3(.23f,.45f,0), new Vector3(.22f,.85f,.25f), color);
            target.PrecisionRegion = Region(target, HitZone.Crotch, new Vector3(0,.94f,-.24f), new Vector3(.20f,.16f,.12f), Color.yellow).transform;
            var label = new GameObject("Label"); label.transform.SetParent(root.transform); label.transform.localPosition = new Vector3(0,2.5f,0);
            target.Label = label.AddComponent<TextMesh>(); target.Label.fontSize = 40; target.Label.characterSize = .065f; target.Label.anchor = TextAnchor.MiddleCenter; target.Label.color = Color.white;
            if (LevelMode)
            {
                var plate=Shape(PrimitiveType.Cube,"Readable target nameplate",position+new Vector3(0,2.5f,.06f),new Vector3(3,.34f,.02f),new Color(.025f,.035f,.04f));
                plate.transform.SetParent(root.transform,true);plate.GetComponent<Collider>().enabled=false;
            }
            if (kind == ActorKind.Bhabi) Shape(PrimitiveType.Cube, "Phone", position + new Vector3(.55f,1.5f,-.1f), new Vector3(.18f,.3f,.08f), Color.black).transform.SetParent(root.transform, true);
            if (LevelMode && (kind == ActorKind.Cop || kind == ActorKind.Boss))
            {
                var gun=Shape(PrimitiveType.Cube,"Gun / armed hostile",position+new Vector3(.48f,1.3f,-.25f),new Vector3(.15f,.2f,.65f),Color.black);
                gun.transform.SetParent(root.transform,true);gun.GetComponent<Collider>().enabled=false;
            }
            if (LevelMode) Level.ConfigureActor(target);
        }
        GameObject Region(HarnessTarget target, HitZone zone, Vector3 local, Vector3 size, Color color)
        {
            var obj = Shape(PrimitiveType.Cube, zone.ToString(), target.transform.position + local, size, color);
            obj.transform.SetParent(target.transform, true);
            var hit = obj.AddComponent<HitRegion>(); hit.Owner = target; hit.Zone = zone;
            if (zone == HitZone.Body) target.Body = obj.GetComponent<Renderer>();
            return obj;
        }
        public void Drop(PickupKind kind, Vector3 p, int amount)
        {
            var obj = Shape(PrimitiveType.Sphere, kind.ToString(), p, Vector3.one * .5f, kind == PickupKind.Ammo ? Color.cyan : kind == PickupKind.AI ? Color.magenta : Color.green);
            var pickup = obj.AddComponent<HarnessPickup>(); pickup.Owner = this; pickup.Kind = kind; pickup.Amount = amount; pickup.Remaining = Tuning.PickupSeconds;
            var label = new GameObject("Pickup label"); label.transform.SetParent(obj.transform); label.transform.localPosition = Vector3.up * 1.3f;
            var text = label.AddComponent<TextMesh>(); text.text = kind.ToString(); text.fontSize = 32; text.characterSize = .12f; text.anchor = TextAnchor.MiddleCenter;
        }
        public void BeginTransition()
        {
            if (!IsCombat) return;
            ClearContent(); Stage = "Transition"; transitionRemaining = Tuning.TransitionSeconds; Message("Mock placement only. No live ads or SDK installed.");
            if (LevelMode) Level.StageChanged("Transition");
        }
        public void SkipTransition() { if (Stage == "Transition") LoadStage("Interior"); }
        public void Shoot(Vector2 screen)
        {
            if (!CanShoot || reloadRemaining > 0 || Time.time < nextFire) return;
            Ray ray = View.ScreenPointToRay(screen);
            bool hit = Physics.Raycast(ray, out RaycastHit result, 100);
            HarnessPickup pickup = hit ? result.collider.GetComponent<HarnessPickup>() : null;
            bool emergency = pickup != null && pickup.Kind == PickupKind.Ammo && State.Magazine == 0 && State.Reserve == 0;
            if (!emergency && !State.TryFire()) { Message("Empty magazine. Reload, or tap emergency ammo if reserve is empty."); return; }
            nextFire = Time.time + Tuning.FireInterval;
            if (!hit) return;
            if (pickup != null) { pickup.Collect(); return; }
            var projectile = result.collider.GetComponent<HarnessProjectile>();
            if (projectile != null) { State.Score += 50; Destroy(projectile.gameObject); Message("Intercepted! +50"); return; }
            var rickshaw = result.collider.GetComponentInParent<LevelLootRickshaw>();
            if (rickshaw != null) { rickshaw.Hit(); return; }
            var region = result.collider.GetComponent<HitRegion>();
            if (region != null) region.Owner.TakeHit(region.Zone);
        }
        public void Reload() { if (CanShoot && reloadRemaining <= 0 && State.Reserve > 0 && State.Magazine < Tuning.MagazineSize) reloadRemaining = Tuning.ReloadSeconds; }
        void OnApplicationPause(bool value) { if (value && State != null) { paused = true; Time.timeScale = 0; } }
        void OnDestroy() { Time.timeScale = 1; }
        void OnGUI()
        {
            if (State == null) return;
            if (LevelMode) { DrawLevelHUD(); return; }
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            GUI.skin.label.fontSize = 17; GUI.skin.button.fontSize = 15; GUI.skin.box.fontSize = 18;
            GUI.Box(new Rect(0,0,1280,85), "");
            GUI.Box(new Rect(0,85,300,635), "");
            GUI.Box(new Rect(300,645,980,75), "");
            GUI.Label(new Rect(20,12,1200,30), "BAD GUY / DEVELOPMENT HARNESS     " + Stage + "     HP " + State.Health.ToString("0") + "    Ammo " + State.Magazine + "/" + State.Reserve + "    Score " + State.Score);
            GUI.Label(new Rect(20,45,1240,30), message);
            GUILayout.BeginArea(new Rect(15,90,275,550));
            GUILayout.Label("SCENARIOS / DEBUG CONTROLS");
            seedText = GUILayout.TextField(seedText);
            if (GUILayout.Button("Reset same seed / practice")) { AttacksEnabled = false; NewRun(); }
            if (GUILayout.Button("Start exterior (8 kills -> boss)")) { NewRun(); LoadStage("Exterior"); AttacksEnabled = true; }
            if (GUILayout.Button("Jump to boss")) { LoadStage("Interior"); AttacksEnabled = true; }
            if (GUILayout.Button("Preview transition / mock ad")) BeginTransition();
            AttacksEnabled = GUILayout.Toggle(AttacksEnabled,"Enable enemy attacks");
            foreach (ActorKind kind in Enum.GetValues(typeof(ActorKind)))
                if (GUILayout.Button("Spawn " + kind)) Spawn(kind, new Vector3((encounterRandom.Next(7)-3)*1.05f, 0, 7 + encounterRandom.Next(4)));
            if (GUILayout.Button("Force AI pickup")) Drop(PickupKind.AI,new Vector3(0,1,6),1);
            if (GUILayout.Button("Force ammo / biryani")) { Drop(PickupKind.Ammo,new Vector3(-1,1,6),12); Drop(PickupKind.Biryani,new Vector3(1,1,6),25); }
            if (GUILayout.Button("Empty ammo recovery test")) { State.Magazine=0; State.Reserve=0; Drop(PickupKind.Ammo,new Vector3(0,1,5),12); }
            GUILayout.EndArea();
            if (GUI.Button(new Rect(310,655,180,45), reloadRemaining > 0 ? "Reloading..." : "Reload")) Reload();
            if (GUI.Button(new Rect(505,655,250,45), State.Overdrive ? "AI active " + State.OverdriveRemaining.ToString("0.0") : State.AIReady ? "Activate AI Overdrive" : "AI slot empty")) { if (IsCombat && !paused) State.Activate(Tuning.OverdriveSeconds); }
            if (GUI.Button(new Rect(770,655,160,45), paused ? "Resume" : "Pause")) { paused = !paused; Time.timeScale = paused ? 0 : 1; }
            GUI.Label(new Rect(945,655,330,50), "Yellow = crotch zone\nBhabi area: " + CombatRules.AreaFactor(BhabiVisible(),State.BhabiCasualty).ToString("0.00") + "x");
            if (Stage == "Transition")
            {
                GUI.Box(playArea,"BREACH PREPARATION");
                string beat = transitionRemaining > 9 ? "Approaching entrance..." : transitionRemaining > 6 ? "Checking equipment..." : transitionRemaining > 3 ? "Boss: You picked the wrong complex!" : "TIP: Watch the balconies. Intercept blades.";
                GUI.Label(new Rect(480,220,700,60),beat);
                GUI.Box(new Rect(400,490,780,80),"MOCK BANNER / NO LIVE AD\nReserved strip, separate from shooting and controls");
                if (GUI.Button(new Rect(1040,110,200,40),"Skip preview")) LoadStage("Interior");
            }
            if (!IsCombat && Stage != "Transition") GUI.Label(new Rect(500,260,650,100), State.Health <= 0 ? "RUN ENDED / reset or restart" : "LEVEL COMPLETE / Score " + State.Score + " / Civilian casualties " + State.CiviliansHit);
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && playArea.Contains(e.mousePosition))
            {
                Shoot(new Vector2(e.mousePosition.x * scale, Screen.height - e.mousePosition.y * scale)); e.Use();
            }
        }
        void DrawLevelHUD()
        {
            float scale = Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float width=Screen.width/scale, height=Screen.height/scale;
            GUI.skin.label.fontSize=18; GUI.skin.button.fontSize=18; GUI.skin.box.fontSize=22;
            GUI.Box(new Rect(0,0,width,86),"");
            GUI.Label(new Rect(22,12,width-40,30),"BAD GUY: THE PEOPLE'S OUTLAW   /   SHOPPING DISTRICT");
            GUI.Label(new Rect(22,44,1000,30),Stage+"   |   HP "+State.Health.ToString("0")+"   |   Score "+State.Score+"   |   "+Level.Objective);
            GUI.Box(new Rect(0,height-88,width,88),"");
            if(GUI.Button(new Rect(18,height-70,200,52),reloadRemaining>0?"RELOADING...":"RELOAD "+State.Magazine+" / "+State.Reserve)) Reload();
            if(GUI.Button(new Rect(235,height-70,240,52),State.Overdrive?"AI "+State.OverdriveRemaining.ToString("0.0")+"s":State.AIReady?"ACTIVATE AI":"AI / NO CHARGE")) { if(IsCombat&&!paused)State.Activate(Tuning.OverdriveSeconds); }
            if(GUI.Button(new Rect(width-190,height-70,170,52),paused?"RESUME":"PAUSE")) {paused=!paused;Time.timeScale=paused?0:1;}
            GUI.skin.label.wordWrap=true;
            GUI.Label(new Rect(495,height-78,width-710,76),message);
            if(Stage=="Transition")
            {
                GUI.Box(new Rect(width*.2f,150,width*.6f,280),"BREACH PREPARATION");
                string beat=transitionRemaining>9?"Approaching the shopping complex...":transitionRemaining>6?"Checking equipment...":transitionRemaining>3?"The Patron: This market belongs to me!":"Watch the balconies. Shoot incoming blades.";
                GUI.Label(new Rect(width*.24f,220,width*.52f,70),beat);
                GUI.Box(new Rect(width*.22f,340,width*.56f,65),"MOCK AD BANNER / NO LIVE AD");
                if(GUI.Button(new Rect(width*.4f,445,width*.2f,48),"SKIP PREVIEW")) SkipTransition();
            }
            if(Stage=="Results"||State.Health<=0)
            {
                GUI.Box(new Rect(width*.25f,170,width*.5f,300),State.Health<=0?"RUN ENDED":"DISTRICT CLEARED");
                GUI.Label(new Rect(width*.3f,240,width*.4f,70),"Score "+State.Score+"   /   Defeats "+State.Kills+"\nCivilian casualties "+State.CiviliansHit);
                if(GUI.Button(new Rect(width*.35f,350,width*.3f,60),"PLAY AGAIN / SEED "+Seed))NewRun();
            }
            if(paused)GUI.Label(new Rect(width*.45f,120,300,40),"PAUSED");
            Event e=Event.current;
            if(e.type==EventType.KeyDown && e.keyCode==KeyCode.R){Reload();e.Use();}
            else if(e.type==EventType.KeyDown && e.keyCode==KeyCode.Space){if(CanShoot)State.Activate(Tuning.OverdriveSeconds);e.Use();}
            if(e.type==EventType.MouseDown&&e.button==0&&e.mousePosition.y>90&&e.mousePosition.y<height-95)
            { Shoot(new Vector2(e.mousePosition.x*scale,Screen.height-e.mousePosition.y*scale));e.Use(); }
        }
    }
}

