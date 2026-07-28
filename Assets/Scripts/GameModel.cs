using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CanopyKin
{
    public enum ResourceKind { Seed, Protein, Resin }
    public enum SquadOrder { Follow, Move, Attack, Gather, Defend, Patrol, Retreat, ReturnToNest }
    public enum UnitRole { Worker, LightSoldier, HeavySoldier }

    public static class GameText
    {
        public static bool Russian =>
            Application.systemLanguage == SystemLanguage.Russian ||
            Application.systemLanguage == SystemLanguage.Ukrainian;

        public static string Pick(string english, string russian) => Russian ? russian : english;
    }

    [CreateAssetMenu(menuName = "Canopy Kin/Ant Definition")]
    public sealed class AntDefinition : ScriptableObject
    {
        public UnitRole role;
        public AntCaste caste;
        public Color shell;
        public float speed;
        public float maxHealth;
        public float damage;
        public float attackRange;
        public float carryCapacity;
        public float visualScale;
    }

    [CreateAssetMenu(menuName = "Canopy Kin/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public Creature.Species species;
        public float maxHealth;
        public float speed;
        public float aggroRadius;
        public float damage;
        public float attackRange;
        public float attackInterval;
    }

    [CreateAssetMenu(menuName = "Canopy Kin/Resource Definition")]
    public sealed class ResourceDefinition : ScriptableObject
    {
        public ResourceKind kind;
        public int carryingValue = 1;
        public Color color;
        public float gatherSeconds = 1.2f;
    }

    [CreateAssetMenu(menuName = "Canopy Kin/Upgrade Definition")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        public string id;
        public int seeds;
        public int resin;
        public int protein;
        public float buildSeconds;
        public int capacityBonus;
    }

    public static class GameDefinitions
    {
        static readonly Dictionary<UnitRole, AntDefinition> Ants = new();
        static readonly Dictionary<Creature.Species, EnemyDefinition> Enemies = new();

        public static AntDefinition Ant(UnitRole role)
        {
            if (Ants.TryGetValue(role, out AntDefinition definition)) return definition;
            definition = ScriptableObject.CreateInstance<AntDefinition>();
            definition.name = $"{role} runtime definition";
            definition.role = role;
            switch (role)
            {
                case UnitRole.Worker:
                    definition.caste = AntCaste.Worker;
                    definition.shell = new Color(.24f, .075f, .018f);
                    definition.speed = 2.45f;
                    definition.maxHealth = 42;
                    definition.damage = 4;
                    definition.attackRange = .72f;
                    definition.carryCapacity = 1;
                    definition.visualScale = .74f;
                    break;
                case UnitRole.LightSoldier:
                    definition.caste = AntCaste.LightSoldier;
                    definition.shell = new Color(.43f, .045f, .012f);
                    definition.speed = 2.75f;
                    definition.maxHealth = 78;
                    definition.damage = 10;
                    definition.attackRange = .84f;
                    definition.visualScale = .86f;
                    break;
                default:
                    definition.caste = AntCaste.HeavySoldier;
                    definition.shell = new Color(.31f, .028f, .011f);
                    definition.speed = 2.05f;
                    definition.maxHealth = 130;
                    definition.damage = 17;
                    definition.attackRange = .94f;
                    definition.visualScale = 1.02f;
                    break;
            }
            Ants[role] = definition;
            return definition;
        }

        public static EnemyDefinition Enemy(Creature.Species species)
        {
            if (Enemies.TryGetValue(species, out EnemyDefinition definition)) return definition;
            definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.name = $"{species} runtime definition";
            definition.species = species;
            switch (species)
            {
                case Creature.Species.Spider:
                    definition.maxHealth = 260;
                    definition.speed = 1.65f;
                    definition.aggroRadius = 13;
                    definition.damage = 20;
                    definition.attackRange = 1.55f;
                    definition.attackInterval = 1.35f;
                    break;
                case Creature.Species.RivalAnt:
                    definition.maxHealth = 68;
                    definition.speed = 2.55f;
                    definition.aggroRadius = 10;
                    definition.damage = 8;
                    definition.attackRange = .92f;
                    definition.attackInterval = 1.05f;
                    break;
                default:
                    definition.maxHealth = 155;
                    definition.speed = 1.35f;
                    definition.aggroRadius = 9;
                    definition.damage = 13;
                    definition.attackRange = 1.18f;
                    definition.attackInterval = 1.4f;
                    break;
            }
            Enemies[species] = definition;
            return definition;
        }

        public static UpgradeDefinition NurseryUpgrade()
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
            upgrade.name = "Nursery expansion runtime definition";
            upgrade.id = "nursery_2";
            upgrade.seeds = ColonyState.UpgradeSeedCost;
            upgrade.resin = ColonyState.UpgradeResinCost;
            upgrade.protein = ColonyState.UpgradeProteinCost;
            upgrade.buildSeconds = 8f;
            upgrade.capacityBonus = 6;
            return upgrade;
        }
    }

    [Serializable]
    public sealed class SaveData
    {
        public int version = 4;
        public int seeds;
        public int protein;
        public int resin;
        public int missionStep;
        public int colonyLevel = 1;
        public int population = 8;
        public float missionProgress;
        public float[] player = new float[3];
    }

    public sealed class ColonyState : MonoBehaviour
    {
        public const int UpgradeSeedCost = 5;
        public const int UpgradeResinCost = 3;
        public const int UpgradeProteinCost = 1;

        public int Seeds { get; private set; }
        public int Protein { get; private set; }
        public int Resin { get; private set; }
        public int Level { get; private set; } = 1;
        public int Population { get; private set; } = 8;
        public int Capacity => 10 + (Level - 1) * 6;
        public bool IsConstructing { get; private set; }
        public float ConstructionProgress { get; private set; }
        public event Action Changed;

        public void Add(ResourceKind kind, int amount)
        {
            if (kind == ResourceKind.Seed) Seeds += amount;
            else if (kind == ResourceKind.Protein) Protein += amount;
            else Resin += amount;
            Changed?.Invoke();
        }

        public bool CanUpgrade =>
            Level < 2 &&
            !IsConstructing &&
            Seeds >= UpgradeSeedCost &&
            Resin >= UpgradeResinCost &&
            Protein >= UpgradeProteinCost;

        public bool Upgrade()
        {
            if (!CanUpgrade) return false;
            ConsumeUpgradeCost();
            CompleteUpgrade();
            return true;
        }

        public bool BeginUpgrade(MonoBehaviour host)
        {
            if (!CanUpgrade || !host) return false;
            ConsumeUpgradeCost();
            host.StartCoroutine(BuildNursery(GameDefinitions.NurseryUpgrade().buildSeconds));
            return true;
        }

        void ConsumeUpgradeCost()
        {
            Seeds -= UpgradeSeedCost;
            Resin -= UpgradeResinCost;
            Protein -= UpgradeProteinCost;
            IsConstructing = true;
            ConstructionProgress = 0;
            Changed?.Invoke();
        }

        IEnumerator BuildNursery(float seconds)
        {
            while (ConstructionProgress < 1f)
            {
                ConstructionProgress = Mathf.Clamp01(ConstructionProgress + Time.deltaTime / Mathf.Max(.1f, seconds));
                Changed?.Invoke();
                yield return null;
            }
            CompleteUpgrade();
            WorldBootstrap.Instance?.ApplyNestUpgrade();
            WorldBootstrap.Instance?.Mission.NotifyUpgrade();
            WorldBootstrap.Instance?.ShowToast(GameText.Pick("The nursery expansion is complete", "Расширение яслей завершено"));
        }

        void CompleteUpgrade()
        {
            Level = Mathf.Max(Level, 2);
            Population = Mathf.Min(Capacity, Population + 3);
            IsConstructing = false;
            ConstructionProgress = 1;
            Changed?.Invoke();
        }

        public void Restore(SaveData data)
        {
            Seeds = Mathf.Max(0, data.seeds);
            Protein = Mathf.Max(0, data.protein);
            Resin = Mathf.Max(0, data.resin);
            Level = Mathf.Clamp(data.colonyLevel, 1, 2);
            Population = Mathf.Clamp(data.population, 1, Capacity);
            IsConstructing = false;
            ConstructionProgress = Level > 1 ? 1 : 0;
            Changed?.Invoke();
        }
    }
}
