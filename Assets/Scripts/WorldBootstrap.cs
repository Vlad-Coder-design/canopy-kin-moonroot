using UnityEngine;
using UnityEngine.InputSystem;

namespace CanopyKin
{
    public sealed class WorldBootstrap : MonoBehaviour
    {
        public static WorldBootstrap Instance {get;private set;} public static readonly Vector3 NestPoint=new(0,1,0); public PlayerAnt Player{get;private set;} public ColonyState Colony{get;private set;} public MissionDirector Mission{get;private set;} SquadController squads; GUIStyle title,body;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Spawn(){if(!FindFirstObjectByType<WorldBootstrap>())new GameObject("MoonrootWorld").AddComponent<WorldBootstrap>();}
        void Awake(){Instance=this;Build();}
        Material Mat(Color c,float smooth=.2f){var s=Resources.Load<Shader>("CanopyKinLit");var m=new Material(s){color=c};m.SetFloat("_Smoothness",smooth);return m;}
        GameObject Prim(PrimitiveType t,string n,Vector3 p,Vector3 scale,Color c){var g=GameObject.CreatePrimitive(t);g.name=n;g.transform.SetPositionAndRotation(p,Quaternion.identity);g.transform.localScale=scale;g.GetComponent<Renderer>().material=Mat(c);return g;}
        void Build()
        {
            Colony=gameObject.AddComponent<ColonyState>();Mission=gameObject.AddComponent<MissionDirector>();squads=gameObject.AddComponent<SquadController>();
            RenderSettings.ambientLight=new Color(.32f,.38f,.3f); var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.25f;sun.transform.rotation=Quaternion.Euler(45,-30,0);
            Prim(PrimitiveType.Plane,"Forest Floor",Vector3.zero,new Vector3(8,1,8),new Color(.16f,.12f,.07f));
            for(int i=0;i<70;i++){float a=i*2.399f,r=5+(i%12)*2.3f;var g=Prim(PrimitiveType.Cylinder,"Grass",new Vector3(Mathf.Cos(a)*r,.8f,Mathf.Sin(a)*r),new Vector3(.08f,Random.Range(.7f,1.8f),.08f),new Color(.18f+Random.value*.08f,.34f+Random.value*.16f,.12f));g.transform.rotation=Quaternion.Euler(Random.Range(-12,12),0,Random.Range(-12,12));}
            Prim(PrimitiveType.Cylinder,"Fallen Branch",new Vector3(8,.7f,8),new Vector3(1,8,1),new Color(.24f,.11f,.045f)).transform.rotation=Quaternion.Euler(0,0,78);
            for(int i=0;i<8;i++)Prim(PrimitiveType.Sphere,"Moss Stone",new Vector3(-8+i*1.4f,.35f,8+Mathf.Sin(i)*1.3f),new Vector3(1.8f,.7f,1.3f),new Color(.18f,.28f,.12f));
            Prim(PrimitiveType.Cylinder,"Nest mound",NestPoint,new Vector3(4,.6f,4),new Color(.25f,.14f,.07f)); var entrance=Prim(PrimitiveType.Cylinder,"Moonroot Nest",new Vector3(0,.75f,0),new Vector3(1,.15f,1),new Color(.05f,.035f,.02f));var eh=entrance.AddComponent<IInteractableHost>();eh.Target=entrance.AddComponent<ColonyEntrance>();
            Prim(PrimitiveType.Cylinder,"Underground chamber",new Vector3(0,-2,0),new Vector3(5,.15f,5),new Color(.18f,.09f,.04f)); for(int i=0;i<5;i++)Prim(PrimitiveType.Cube,"Root tunnel",new Vector3(Mathf.Cos(i*1.256f)*3,-.8f,Mathf.Sin(i*1.256f)*3),new Vector3(.45f,2.5f,.45f),new Color(.2f,.1f,.04f));
            SpawnResource(ResourceKind.Seed,new Vector3(5,.35f,3),new Color(.55f,.35f,.12f));SpawnResource(ResourceKind.Seed,new Vector3(7,.35f,4),new Color(.55f,.35f,.12f));SpawnResource(ResourceKind.Resin,new Vector3(10,.4f,7),new Color(.85f,.48f,.08f));
            SpawnCreature(Creature.Species.Beetle,new Vector3(12,.55f,-4),new Color(.12f,.18f,.15f),new Vector3(1, .55f,1.4f));SpawnCreature(Creature.Species.RivalAnt,new Vector3(-13,.35f,5),new Color(.55f,.08f,.04f),new Vector3(.7f,.35f,1));SpawnCreature(Creature.Species.Spider,new Vector3(-20,.7f,-15),new Color(.12f,.08f,.06f),new Vector3(2, .7f,2));
            var pg=Prim(PrimitiveType.Capsule,"Player Ant",NestPoint+new Vector3(0,1,2),new Vector3(.55f,.35f,.8f),new Color(.05f,.035f,.025f),true); var cc=pg.AddComponent<CharacterController>();cc.height=1;cc.radius=.35f;Player=pg.AddComponent<PlayerAnt>();
            var camGo=new GameObject("Main Camera");camGo.tag="MainCamera";camGo.AddComponent<Camera>();camGo.AddComponent<AudioListener>();camGo.transform.position=pg.transform.position+new Vector3(0,3,-5);
            for(int i=0;i<6;i++){var u=Prim(PrimitiveType.Capsule,i<3?"Worker":"Soldier",NestPoint+new Vector3(i-3,.4f,-2),new Vector3(.35f,.22f,.5f),i<3?new Color(.18f,.1f,.04f):new Color(.32f,.08f,.03f));u.GetComponent<Collider>().enabled=false;squads.Add(u.transform);}
            Debug.Log($"Moonroot world ready: {FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length} renderers.");
        }
        GameObject Prim(PrimitiveType t,string n,Vector3 p,Vector3 s,Color c,bool keepCollider){var g=Prim(t,n,p,s,c);if(!keepCollider){}return g;}
        void SpawnResource(ResourceKind k,Vector3 p,Color c){var g=Prim(PrimitiveType.Sphere,k+" cache",p,new Vector3(.65f,.35f,.65f),c);var r=g.AddComponent<ResourceNode>();r.kind=k;var h=g.AddComponent<IInteractableHost>();h.Target=r;}
        void SpawnCreature(Creature.Species s,Vector3 p,Color c,Vector3 scale){var g=Prim(PrimitiveType.Sphere,s.ToString(),p,scale,c);var cr=g.AddComponent<Creature>();cr.species=s;cr.health=s==Creature.Species.Spider?140:s==Creature.Species.RivalAnt?65:50;cr.speed=s==Creature.Species.Spider?1.2f:1.7f;}
        void Update(){var k=Keyboard.current;if(k==null)return;if(k.digit1Key.wasPressedThisFrame)squads.Set(SquadOrder.Gather);if(k.digit2Key.wasPressedThisFrame)squads.Set(SquadOrder.Attack);if(k.digit3Key.wasPressedThisFrame)squads.Set(SquadOrder.Follow);if(k.digit4Key.wasPressedThisFrame)squads.Set(SquadOrder.Defend);if(k.f5Key.wasPressedThisFrame)SaveSystem.Save(1,this);if(k.f9Key.wasPressedThisFrame)SaveSystem.Load(1,this);}
        void OnGUI(){title??=new GUIStyle(GUI.skin.label){fontSize=22,fontStyle=FontStyle.Bold,normal={textColor=new Color(.9f,.8f,.5f)}};body??=new GUIStyle(GUI.skin.label){fontSize=16,normal={textColor=Color.white},wordWrap=true};GUI.Box(new Rect(18,18,480,142),"");GUI.Label(new Rect(34,28,440,30),"MOONROOT: FIRST RAIN",title);GUI.Label(new Rect(34,60,440,48),Mission.Objective,body);GUI.Label(new Rect(34,112,440,28),$"Seed {Colony.Seeds}   Resin {Colony.Resin}   Protein {Colony.Protein}   Nest Lv.{Colony.Level}",body);GUI.Label(new Rect(18,Screen.height-66,900,50),"WASD move · mouse look · Shift sprint · Space vault · E interact/upgrade · LMB bite · 1 gather · 2 attack · 3 follow · 4 defend · F5 save · F9 load",body);if(Player!=null)GUI.Label(new Rect(Screen.width-220,22,200,50),$"Health {Player.Health:0}  Stamina {Player.Stamina:0}",body);}
    }
}
