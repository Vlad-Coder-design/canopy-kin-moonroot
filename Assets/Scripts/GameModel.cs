using System;
using UnityEngine;

namespace CanopyKin
{
    public enum ResourceKind { Seed, Protein, Resin }
    public enum SquadOrder { Follow, Gather, Attack, Defend, Retreat }

    [Serializable] public sealed class SaveData
    {
        public int version = 1, seeds, protein, resin, missionStep, colonyLevel = 1;
        public float[] player = new float[3];
    }

    public sealed class ColonyState : MonoBehaviour
    {
        public int Seeds { get; private set; }
        public int Protein { get; private set; }
        public int Resin { get; private set; }
        public int Level { get; private set; } = 1;
        public event Action Changed;
        public void Add(ResourceKind kind, int amount) { if (kind == ResourceKind.Seed) Seeds += amount; else if (kind == ResourceKind.Protein) Protein += amount; else Resin += amount; Changed?.Invoke(); }
        public bool Upgrade() { if (Seeds < 5 || Resin < 2 || Level >= 2) return false; Seeds -= 5; Resin -= 2; Level++; Changed?.Invoke(); return true; }
        public void Restore(SaveData d) { Seeds=d.seeds; Protein=d.protein; Resin=d.resin; Level=Mathf.Max(1,d.colonyLevel); Changed?.Invoke(); }
    }
}
