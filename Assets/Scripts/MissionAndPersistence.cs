using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CanopyKin
{
    public static class GameSettings { public static float Sensitivity=.08f; public static float Shake=.5f; public static bool Subtitles=true; }
    public sealed class MissionDirector : MonoBehaviour
    {
        public int Step {get;private set;} public string Objective=>Step switch{0=>"Leave the Moonroot nest and gather 3 seeds",1=>"Gather 2 resin near the fallen branch",2=>"Defeat a bark beetle",3=>"Press 1/2 to command workers/soldiers; defeat the rival scout",4=>"Return to the nest and upgrade the nursery",_=>"Vertical slice complete — the Ashback spider has awakened"};
        int seeds,resin; public void NotifyGather(){seeds=WorldBootstrap.Instance.Colony.Seeds;resin=WorldBootstrap.Instance.Colony.Resin;if(Step==0&&seeds>=3)Step=1;if(Step==1&&resin>=2)Step=2;}
        public void NotifyKill(Creature.Species s){if(Step==2&&s==Creature.Species.Beetle)Step=3;else if(Step==3&&s==Creature.Species.RivalAnt)Step=4;}
        public void NotifyUpgrade(){if(Step==4)Step=5;} public void Restore(int step)=>Step=Mathf.Clamp(step,0,5);
    }
    public static class SaveSystem
    {
        static string PathFor(int slot)=>System.IO.Path.Combine(Application.persistentDataPath,$"moonroot_{slot}.json");
        public static void Save(int slot, WorldBootstrap w){var p=w.Player.transform.position;var d=new SaveData{seeds=w.Colony.Seeds,protein=w.Colony.Protein,resin=w.Colony.Resin,colonyLevel=w.Colony.Level,missionStep=w.Mission.Step,player=new[]{p.x,p.y,p.z}};File.WriteAllText(PathFor(slot),JsonUtility.ToJson(d,true));}
        public static bool Load(int slot,WorldBootstrap w){try{var path=PathFor(slot);if(!File.Exists(path))return false;var d=JsonUtility.FromJson<SaveData>(File.ReadAllText(path));if(d==null||d.version!=1||d.player?.Length!=3)return false;w.Colony.Restore(d);w.Mission.Restore(d.missionStep);w.Player.transform.position=new Vector3(d.player[0],d.player[1],d.player[2]);return true;}catch{return false;}}
    }
}
