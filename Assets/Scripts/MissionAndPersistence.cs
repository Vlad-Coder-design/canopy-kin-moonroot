using System;
using System.IO;
using UnityEngine;

namespace CanopyKin
{
    public static class GameSettings
    {
        public static float Sensitivity = .075f;
        public static float Shake = .35f;
        public static bool Subtitles = true;
    }

    public sealed class MissionDirector : MonoBehaviour
    {
        public int Step { get; private set; }
        public event Action<int> StepChanged;

        public string Title => Step switch
        {
            0 => GameText.Pick("FIRST FORAGE", "ПЕРВЫЙ ВЫХОД"),
            1 => GameText.Pick("AMBER TRAIL", "ЯНТАРНЫЙ СЛЕД"),
            2 => GameText.Pick("BARK GUARDIAN", "СТРАЖ КОРЫ"),
            3 => GameText.Pick("RIVAL SCOUT", "РАЗВЕДЧИК СОПЕРНИКОВ"),
            4 => GameText.Pick("A LIVING NEST", "ЖИВОЕ ГНЕЗДО"),
            5 => GameText.Pick("ASHBACK ATTACK", "НАПАДЕНИЕ ПЕПЕЛЬНОСПИНА"),
            _ => GameText.Pick("MOONROOT AWAKENS", "ЛУННЫЙ КОРЕНЬ ПРОБУЖДЁН")
        };

        public string Objective => Step switch
        {
            0 => GameText.Pick("Gather 3 moonseeds in the flower glade", "Соберите 3 лунных семени на цветочной поляне"),
            1 => GameText.Pick("Collect 2 amber resin drops beside the fallen branch", "Соберите 2 капли янтарной смолы у поваленной ветки"),
            2 => GameText.Pick("Defeat the bark beetle guarding the trail", "Победите жука-короеда, охраняющего тропу"),
            3 => GameText.Pick("Press 2 to order an attack, then defeat the rival scout", "Нажмите 2 для приказа атаковать и победите чужого разведчика"),
            4 => GameText.Pick("Return to Moonroot and press E to grow the nursery", "Вернитесь в Лунный Корень и нажмите E, чтобы расширить ясли"),
            5 => GameText.Pick("Defend the entrance from the Ashback spider", "Защитите вход от паука Пепельноспина"),
            _ => GameText.Pick("Vertical slice complete — Moonroot survived the first rain", "Вертикальный срез пройден — Лунный Корень пережил первый дождь")
        };

        public void NotifyGather()
        {
            ColonyState colony = WorldBootstrap.Instance.Colony;
            if (Step == 0 && colony.Seeds >= 3) Advance(1);
            else if (Step == 1 && colony.Resin >= 2) Advance(2);
        }

        public void NotifyKill(Creature.Species species)
        {
            if (Step == 2 && species == Creature.Species.Beetle) Advance(3);
            else if (Step == 3 && species == Creature.Species.RivalAnt) Advance(4);
            else if (Step == 5 && species == Creature.Species.Spider) Advance(6);
        }

        public void NotifyUpgrade()
        {
            if (Step == 4) Advance(5);
        }

        public void Restore(int step)
        {
            Step = Mathf.Clamp(step, 0, 6);
            StepChanged?.Invoke(Step);
        }

        void Advance(int next)
        {
            if (next <= Step) return;
            Step = next;
            StepChanged?.Invoke(Step);
            WorldBootstrap.Instance?.OnMissionAdvanced();
        }
    }

    public static class SaveSystem
    {
        static string PathFor(int slot) => Path.Combine(Application.persistentDataPath, $"moonroot_{slot}.json");

        public static bool Save(int slot, WorldBootstrap world)
        {
            try
            {
                Vector3 position = world.Player.transform.position;
                var data = new SaveData
                {
                    seeds = world.Colony.Seeds,
                    protein = world.Colony.Protein,
                    resin = world.Colony.Resin,
                    colonyLevel = world.Colony.Level,
                    missionStep = world.Mission.Step,
                    player = new[] { position.x, position.y, position.z }
                };
                File.WriteAllText(PathFor(slot), JsonUtility.ToJson(data, true));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not save Moonroot: {exception.Message}");
                return false;
            }
        }

        public static bool Load(int slot, WorldBootstrap world)
        {
            try
            {
                string path = PathFor(slot);
                if (!File.Exists(path)) return false;
                SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null || data.version is < 1 or > 2 || data.player?.Length != 3) return false;
                world.Colony.Restore(data);
                world.Mission.Restore(data.missionStep);
                world.Player.Teleport(new Vector3(data.player[0], data.player[1], data.player[2]));
                world.RefreshWorldForMission();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not load Moonroot: {exception.Message}");
                return false;
            }
        }
    }
}
