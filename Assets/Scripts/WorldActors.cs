using System.Collections.Generic;
using UnityEngine;

namespace CanopyKin
{
    public sealed class ResourceNode : MonoBehaviour, IInteractable
    {
        public ResourceKind kind; public int remaining=4; public string Prompt=>remaining>0?"Gather "+kind:"Depleted";
        public void Interact(PlayerAnt p) { if(remaining<=0)return; remaining--; WorldBootstrap.Instance.Colony.Add(kind,1); transform.localScale*=.88f; WorldBootstrap.Instance.Mission.NotifyGather(); }
    }
    public sealed class ColonyEntrance : MonoBehaviour, IInteractable
    {
        public string Prompt=>"Upgrade nursery (5 seed, 2 resin)";
        public void Interact(PlayerAnt p) { if(WorldBootstrap.Instance.Colony.Upgrade()){ transform.localScale*=1.18f; WorldBootstrap.Instance.Mission.NotifyUpgrade(); } }
    }
    public sealed class Creature : MonoBehaviour
    {
        public enum Species { Beetle, Spider, RivalAnt }
        public Species species; public float health=50, speed=1.5f, aggro=7; Vector3 home; float cooldown;
        void Start()=>home=transform.position;
        void Update() { var p=WorldBootstrap.Instance?.Player; if(!p)return; float d=Vector3.Distance(transform.position,p.transform.position); var target=d<aggro?p.transform.position:home; target.y=transform.position.y; if(Vector3.Distance(transform.position,target)>.5f){ transform.position=Vector3.MoveTowards(transform.position,target,speed*Time.deltaTime); transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(target-transform.position),5*Time.deltaTime); } cooldown-=Time.deltaTime; if(d<1.3f&&cooldown<=0){p.Damage(species==Species.Spider?18:10);cooldown=1.2f;} }
        public void Damage(float amount){health-=amount;if(health<=0){WorldBootstrap.Instance.Mission.NotifyKill(species); Destroy(gameObject);}}
    }
    public sealed class SquadController : MonoBehaviour
    {
        public SquadOrder Order {get;private set;}=SquadOrder.Follow; readonly List<Transform> units=new();
        public void Add(Transform t)=>units.Add(t); public void Set(SquadOrder o){Order=o;}
        void Update(){ var p=WorldBootstrap.Instance?.Player;if(!p)return; for(int i=0;i<units.Count;i++){if(!units[i])continue; Vector3 offset=new((i%3-1)*.7f,0,-1.2f-(i/3)*.65f); Vector3 goal=p.transform.TransformPoint(offset); if(Order==SquadOrder.Defend)goal=WorldBootstrap.NestPoint+offset; units[i].position=Vector3.MoveTowards(units[i].position,goal,3*Time.deltaTime); } }
    }
}
