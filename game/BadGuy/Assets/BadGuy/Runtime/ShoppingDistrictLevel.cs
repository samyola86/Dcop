using System;
using System.Collections.Generic;
using UnityEngine;

namespace BadGuy
{
    // Fixed recipes and seeded encounters; no external assets or services.
    public sealed class ShoppingDistrictLevel : MonoBehaviour
    {
        public HarnessController Combat;
        public Transform World;
        public int Defeats, Stop;
        public const int ThreatsPerStop = 6;
        public const float SectionLength = 24;
        public bool Travelling => travelRemaining > 0;
        public int Scheduled => scheduled;
        public string Objective => Travelling ? "Moving / weapons safe" : Combat.Stage == "Exterior" ? "Street " + (Stop+1) + " / 3   |   Cleared " + Defeats + " / 18" : Combat.Stage == "Interior" ? "Defeat The Patron / watch upper balconies" : Combat.Stage == "Results" ? "District cleared" : "Prepare to breach";
        public static readonly Vector3[] StreetSlots = {new Vector3(-3,0,7),new Vector3(0,0,9),new Vector3(3,0,7),new Vector3(-4,3,12),new Vector3(4,3,12)};
        readonly Dictionary<Color,Material> materials = new Dictionary<Color,Material>();
        System.Random random;
        int scheduled, civilianIndex, supportIndex;
        float nextSpawn, nextCivilian, travelRemaining;
        Vector3 cameraStart, cameraEnd;
        public Vector3 Offset => Combat.Stage == "Exterior" ? Vector3.forward * Stop * SectionLength : Vector3.zero;
        public Vector3 CameraPosition(int section) => new Vector3(0,3,-10 + section*SectionLength);
        public void ResetLevel()
        {
            Defeats=Stop=scheduled=civilianIndex=supportIndex=0;
            random = new System.Random(Combat.Seed); travelRemaining=0;
        }
        public void StageChanged(string stage)
        {
            if(stage == "Transition" || stage == "Results") { Combat.AttacksEnabled=false; travelRemaining=0; return; }
            BuildEnvironment(stage == "Interior");
            Combat.View.rect=new Rect(0,0,1,1); Combat.View.fieldOfView=48;
            Combat.View.transform.position=CameraPosition(0);
            Combat.View.transform.LookAt(new Vector3(0,2.7f,9));
            Combat.View.clearFlags=CameraClearFlags.SolidColor;
            Combat.View.backgroundColor=new Color(.32f,.43f,.50f);
            Combat.AttacksEnabled=true; nextSpawn=Time.time+1.5f; nextCivilian=Time.time+6;
            if(stage=="Interior")
            {
                Combat.Spawn(ActorKind.Boss,new Vector3(0,0,8));
                Combat.Message("THE PATRON / ground floor. Watch the balconies and intercept blades.");
            }
            else
            {
                Combat.Message("Clear three street sections. Green = civilians. Cyan EMPTY rickshaw guarantees ammo. R = reload / Space = AI.");
                Combat.Drop(PickupKind.Ammo,new Vector3(-1,.7f,2),24);
            }
        }
        void Update()
        {
            if(!Combat.IsCombat || Combat.Paused) return;
            if(Travelling)
            {
                travelRemaining=Mathf.Max(0,travelRemaining-Time.deltaTime);
                float t=1-travelRemaining/2.2f;
                Combat.View.transform.position=Vector3.Lerp(cameraStart,cameraEnd,Mathf.SmoothStep(0,1,t));
                if(!Travelling) { Combat.AttacksEnabled=true; nextSpawn=Time.time+1; nextCivilian=Time.time+4; Combat.Message("Street "+(Stop+1)+" / 3. Clear the threats; let civilians pass."); }
                return;
            }
            var targets=FindObjectsByType<HarnessTarget>(FindObjectsSortMode.None);
            int hostile=0,civilians=0;
            foreach(var target in targets) if(!target.Dead) { if(target.Civilian) civilians++; else if(target.Kind!=ActorKind.Boss) hostile++; }
            if(Combat.Stage=="Exterior" && scheduled<(Stop+1)*ThreatsPerStop && hostile<3 && Time.time>=nextSpawn)
            {
                if(TryFreeSocket(targets,StreetSlots,Offset,out var position))
                {
                    // Each stop: two armed cops, two rock cadres, one blade cadre, one runner.
                    int n=scheduled%ThreatsPerStop;
                    ActorKind kind=n==0||n==3?ActorKind.Cop:n==2?ActorKind.Knife:n==5?ActorKind.Runner:ActorKind.Rock;
                    Combat.Spawn(kind,position); scheduled++; nextSpawn=Time.time+2.4f;
                }
                else nextSpawn=Time.time+.4f; // Retry without consuming the wave when every socket is occupied.
            }
            if(Combat.Stage=="Exterior" && Defeats>=(Stop+1)*ThreatsPerStop && hostile==0)
            {
                if(Stop==2) Combat.BeginTransition();
                else
                {
                    Stop++; travelRemaining=2.2f; cameraStart=Combat.View.transform.position; cameraEnd=CameraPosition(Stop);
                    Combat.AttacksEnabled=false;
                    Combat.ClearEncounter(); // Pedestrians and old loot never accumulate between sections.
                    Combat.Message("Street cleared. Moving to the next section / weapons safe...");
                }
                return;
            }
            if(Combat.Stage=="Interior" && hostile<2 && Time.time>=nextSpawn)
            {
                Vector3[] balconies={new Vector3(-4,3,12),new Vector3(4,3,12)};
                if(TryFreeSocket(targets,balconies,Vector3.zero,out var position))
                {
                    Combat.Spawn(supportIndex++%4==3?ActorKind.Knife:ActorKind.Rock,position);
                    nextSpawn=Time.time+5;
                }
            }
            if(Combat.Stage=="Exterior" && civilians<2 && Time.time>=nextCivilian)
            {
                nextCivilian=Time.time+11;
                Combat.Spawn(civilianIndex++%2==0?ActorKind.Bhabi:ActorKind.Civilian,Offset+new Vector3(-7,0,3));
            }
        }
        bool TryFreeSocket(HarnessTarget[] targets,Vector3[] sockets,Vector3 offset,out Vector3 position)
        {
            int start=random.Next(sockets.Length);
            for(int i=0;i<sockets.Length;i++)
            {
                position=sockets[(start+i)%sockets.Length]+offset;
                bool occupied=false;
                foreach(var target in targets)
                {
                    Vector3 home=target.TryGetComponent<LevelActorMotion>(out var motion)?motion.Home:target.transform.position;
                    if(!target.Dead && Vector3.Distance(home,position)<1.5f) {occupied=true;break;}
                }
                if(!occupied) return true;
            }
            position=Vector3.zero;return false;
        }
        public void EnemyDefeated(HarnessTarget target) { if(Combat.Stage=="Exterior") Defeats++; }
        public void ConfigureActor(HarnessTarget target)
        {
            var motion=target.gameObject.AddComponent<LevelActorMotion>();motion.Target=target;motion.Level=this;motion.Home=target.transform.position;
            if(target.Kind==ActorKind.Runner) {motion.Home=Offset+new Vector3(-7,0,4);target.transform.position=motion.Home;}
            if(target.Kind==ActorKind.Cop)
            {
                // A physical barrier stops player rays while the cop crouches. It must stand before firing.
                Box("Cop cover",motion.Home+new Vector3(0,.55f,-1),new Vector3(1.35f,1.1f,.5f),new Color(.16f,.24f,.25f));
            }
            if(target.Kind==ActorKind.Boss) target.NextAttack=Time.time+4;
        }
        Material Mat(Color color)
        {
            if(!materials.TryGetValue(color,out var mat)) {mat=Combat.PrototypeMaterial!=null?new Material(Combat.PrototypeMaterial):new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=color;materials.Add(color,mat);}return mat;
        }
        public GameObject Box(string name,Vector3 position,Vector3 size,Color color,bool solid=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(World);go.transform.position=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=Mat(color);
            if(!solid) {go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());}return go;
        }
        public void Sign(string text,Vector3 position,float size,Color color)
        {
            var go=new GameObject(text);go.transform.SetParent(World);go.transform.position=position;
            var label=go.AddComponent<TextMesh>();label.text=text;label.fontSize=60;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.color=color;
        }
        public void BuildEnvironment(bool inside)
        {
            if(World!=null) {World.gameObject.SetActive(false);Destroy(World.gameObject);}
            World=new GameObject(inside?"Shopping atrium greybox":"Dhaka shopping street greybox").transform;
            Color concrete=new Color(.62f,.58f,.49f),dark=new Color(.11f,.17f,.19f),glass=new Color(.16f,.38f,.43f),orange=new Color(.91f,.38f,.13f);
            if(inside)
            {
                Box("Atrium floor",new Vector3(0,-.15f,7),new Vector3(22,.3f,30),new Color(.48f,.49f,.44f));
                Box("Atrium back",new Vector3(0,5,17),new Vector3(22,10,.5f),concrete);
                for(int side=-1;side<=1;side+=2)
                {
                    Box("Side wall",new Vector3(side*10,4,7),new Vector3(.5f,8,25),concrete);
                    Box("Upper gallery",new Vector3(side*4.5f,2.85f,12),new Vector3(5,.3f,3),dark);
                    Box("Gallery rail",new Vector3(side*4.5f,3.35f,10.4f),new Vector3(5,.45f,.12f),orange);
                    for(int z=4;z<=15;z+=5)Box("Pillar",new Vector3(side*8,3,z),new Vector3(.6f,6,.6f),concrete);
                    for(int x=0;x<3;x++)Box("Upper shop window",new Vector3(side*(3+x*2),5.5f,16.6f),new Vector3(1.4f,2,.1f),glass);
                }
                Box("Boss dais",new Vector3(0,-.02f,8),new Vector3(3,.1f,3),orange);
                Box("Atrium sign board",new Vector3(0,7.4f,16.65f),new Vector3(15,.7f,.05f),dark);
                Sign("SHOPPING COMPLEX / ATRIUM",new Vector3(0,7.4f,16.6f),.13f,Color.white);
                Sign("THE PATRON",new Vector3(0,6.3f,16.6f),.17f,orange);
                return;
            }
            Box("Dhaka road",new Vector3(0,-.25f,31),new Vector3(32,.4f,100),new Color(.18f,.21f,.23f));
            for(int stop=0;stop<3;stop++)
            {
                Vector3 offset=Vector3.forward*stop*SectionLength;
                for(int side=-1;side<=1;side+=2)
                {
                    Box("Pavement",offset+new Vector3(side*8,0,8),new Vector3(4,.2f,24),concrete);
                    for(int z=2;z<=20;z+=6)
                    {
                        Box("Multistory shop",offset+new Vector3(side*10,4,z),new Vector3(3,8,5.5f),concrete);
                        Box("Shop glass",offset+new Vector3(side*8.45f,1.7f,z),new Vector3(.12f,2.8f,4.5f),glass);
                        Box("Awning",offset+new Vector3(side*8,3.3f,z),new Vector3(1.5f,.2f,5),orange);
                        for(int y=5;y<8;y+=2)Box("Upper window",offset+new Vector3(side*8.4f,y,z),new Vector3(.14f,1.2f,2.5f),glass);
                    }
                    Box("Balcony socket platform",offset+new Vector3(side*4,2.85f,12.5f),new Vector3(2.7f,.3f,1.6f),dark);
                    Box("Crate cover",offset+new Vector3(side*5.5f,.5f,10),new Vector3(1,1,1),new Color(.46f,.32f,.18f));
                }
                for(int z=-2;z<22;z+=4)Box("Lane paint",offset+new Vector3(0,-.035f,z),new Vector3(.12f,.02f,1.6f),new Color(.85f,.8f,.6f),false);
                Vehicle("Parked car",offset+new Vector3(-5.7f,0,7),new Color(.38f,.18f,.16f));
                Vehicle("CNG",offset+new Vector3(5.7f,0,9),new Color(.13f,.4f,.25f));
                var rick=Vehicle("Empty loot rickshaw",offset+new Vector3(5.4f,0,1),new Color(.08f,.65f,.72f),true);
                rick.AddComponent<LevelLootRickshaw>().Level=this;
                Sign("EMPTY / AMMO",offset+new Vector3(5.4f,2.7f,1),.065f,Color.cyan);
                Box("Street bin",offset+new Vector3(-6,.5f,2),new Vector3(.7f,1,.7f),dark);
                Sign(stop==0?"MOBILE REPAIR":stop==1?"BIRYANI HOUSE":"SHOPPING ENTRANCE",offset+new Vector3(-5.2f,6,14),.10f,Color.white);
                Sign("SECTION 0"+(stop+1),offset+new Vector3(5.2f,6,14),.11f,orange);
            }
            Box("Shopping complex facade",new Vector3(0,5,70),new Vector3(22,10,1),concrete);
            Box("Entrance glass",new Vector3(0,2,69.4f),new Vector3(4,4,.15f),glass);
            for(int x=-8;x<=8;x+=4)Box("Facade window",new Vector3(x,6.3f,69.4f),new Vector3(2.7f,2.2f,.15f),glass);
            Box("Complex name board",new Vector3(0,8.5f,69.3f),new Vector3(16,1.2f,.15f),dark);
            Sign("DHAKA / SHOPPING COMPLEX",new Vector3(0,8.5f,69.1f),.15f,Color.white);
        }
        GameObject Vehicle(string name,Vector3 p,Color color,bool rickshaw=false)
        {
            var root=new GameObject(name);root.transform.SetParent(World);root.transform.position=p;
            Box(name+" body",p+new Vector3(0,.6f,0),new Vector3(1.8f,.7f,2.4f),color).transform.SetParent(root.transform,true);
            if(!rickshaw)Box(name+" cabin",p+new Vector3(0,1.1f,.2f),new Vector3(1.5f,.55f,1.3f),new Color(.2f,.35f,.38f)).transform.SetParent(root.transform,true);
            else
            {
                Box("Empty passenger seat",p+new Vector3(0,1,.5f),new Vector3(1.5f,.15f,.9f),Color.black).transform.SetParent(root.transform,true);
                Box("Rickshaw canopy",p+new Vector3(0,1.8f,.5f),new Vector3(1.8f,.15f,1.4f),color).transform.SetParent(root.transform,true);
                for(int side=-1;side<=1;side+=2)Box("Canopy post",p+new Vector3(side*.75f,1.35f,.5f),new Vector3(.08f,.9f,.08f),color).transform.SetParent(root.transform,true);
            }
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box("Wheel",p+new Vector3(x*.88f,.26f,z*.75f),new Vector3(.22f,.5f,.5f),Color.black).transform.SetParent(root.transform,true);
            return root;
        }
        void OnDestroy() {foreach(var mat in materials.Values)if(mat!=null)Destroy(mat);}
    }
    public sealed class LevelActorMotion : MonoBehaviour
    {
        public HarnessTarget Target;public ShoppingDistrictLevel Level;public Vector3 Home;
        public bool Hidden {get;private set;}
        public bool ReadyToFire => Target.Kind!=ActorKind.Cop || (!Hidden && Vector3.Distance(transform.position,Home)<.15f);
        float age;
        void Update()
        {
            if(Target==null||Target.Dead||!Level.Combat.IsCombat||Level.Combat.Paused||Level.Travelling)return;
            age+=Time.deltaTime;
            if(Target.Civilian||Target.Kind==ActorKind.Runner)
            {
                transform.position+=Vector3.right*(Target.Civilian?1.4f:2.5f)*Time.deltaTime;
                if(transform.position.x>8 || age>14)
                {
                    if(!Target.Civilian) {Level.Combat.State.Health=Mathf.Max(0,Level.Combat.State.Health-5);Level.EnemyDefeated(Target);Level.Combat.Message("Runner escaped / -5 HP. Encounter continues.");}
                    Target.Dead=true;Destroy(gameObject);
                }
            }
            else if(Target.Kind==ActorKind.Cop)
            {
                Hidden=Target.ReleaseAt<0 && Time.time<Target.NextAttack-.7f;
                transform.position=Vector3.MoveTowards(transform.position,Home+Vector3.down*(Hidden?1.8f:0),Time.deltaTime*5);
            }
            else if(Target.Kind==ActorKind.Boss)transform.position=Home+Vector3.right*Mathf.Sin(age*.5f)*1.5f;
        }
    }
    public sealed class LevelLootRickshaw : MonoBehaviour
    {
        public ShoppingDistrictLevel Level;int hits;public bool Used {get;private set;}
        public void Hit()
        {
            if(Used)return;
            if(++hits<3) {Level.Combat.Message("Empty rickshaw: "+(3-hits)+" hits to guaranteed ammo");return;}
            Used=true;
            // Supply appears in the clear shooting lane, outside the destroyed vehicle collider.
            Level.Combat.Drop(PickupKind.Ammo,Level.Offset+new Vector3(2,.8f,2),24);
            gameObject.SetActive(false);Level.Combat.Message("Empty rickshaw destroyed / guaranteed ammunition.");
        }
    }
}
