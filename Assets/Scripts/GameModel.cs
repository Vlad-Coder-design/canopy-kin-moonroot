using System;
using UnityEngine;

namespace CanopyKin
{
    public enum ResourceKind { Seed, Protein, Resin }
    public enum SquadOrder { Follow, Gather, Attack, Defend, Retreat }

    public static class GameText
    {
        public static bool Russian =>
            Application.systemLanguage == SystemLanguage.Russian ||
            Application.systemLanguage == SystemLanguage.Ukrainian;

        public static string Pick(string english, string russian) => Russian ? russian : english;
    }

    [Serializable]
    public sealed class SaveData
    {
        public int version = 2;
        public int seeds;
        public int protein;
        public int resin;
        public int missionStep;
        public int colonyLevel = 1;
        public float[] player = new float[3];
    }

    public sealed class ColonyState : MonoBehaviour
    {
        public const int UpgradeSeedCost = 3;
        public const int UpgradeResinCost = 2;

        public int Seeds { get; private set; }
        public int Protein { get; private set; }
        public int Resin { get; private set; }
        public int Level { get; private set; } = 1;
        public event Action Changed;

        public void Add(ResourceKind kind, int amount)
        {
            if (kind == ResourceKind.Seed) Seeds += amount;
            else if (kind == ResourceKind.Protein) Protein += amount;
            else Resin += amount;
            Changed?.Invoke();
        }

        public bool Upgrade()
        {
            if (Seeds < UpgradeSeedCost || Resin < UpgradeResinCost || Level >= 2) return false;
            Seeds -= UpgradeSeedCost;
            Resin -= UpgradeResinCost;
            Level++;
            Changed?.Invoke();
            return true;
        }

        public void Restore(SaveData data)
        {
            Seeds = Mathf.Max(0, data.seeds);
            Protein = Mathf.Max(0, data.protein);
            Resin = Mathf.Max(0, data.resin);
            Level = Mathf.Clamp(data.colonyLevel, 1, 2);
            Changed?.Invoke();
        }
    }
}
