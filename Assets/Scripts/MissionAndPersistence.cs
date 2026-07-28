using System;
using System.IO;
using UnityEngine;

namespace CanopyKin
{
    public static class GameSettings
    {
        public static float Sensitivity { get; set; } = .075f;
        public static float FieldOfView { get; set; } = 64f;
        public static float MasterVolume { get; set; } = .78f;
        public static float Shake { get; set; } = .28f;
        public static bool Subtitles { get; set; } = true;
        public static int Quality { get; set; } = 1;

        public static void Load()
        {
            Sensitivity = PlayerPrefs.GetFloat("settings_sensitivity", .075f);
            FieldOfView = PlayerPrefs.GetFloat("settings_fov", 64f);
            MasterVolume = PlayerPrefs.GetFloat("settings_volume", .78f);
            Shake = PlayerPrefs.GetFloat("settings_shake", .28f);
            Subtitles = PlayerPrefs.GetInt("settings_subtitles", 1) != 0;
            Quality = Mathf.Clamp(PlayerPrefs.GetInt("settings_quality", Application.platform == RuntimePlatform.WebGLPlayer ? 0 : 1), 0, 2);
            Apply();
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("settings_sensitivity", Sensitivity);
            PlayerPrefs.SetFloat("settings_fov", FieldOfView);
            PlayerPrefs.SetFloat("settings_volume", MasterVolume);
            PlayerPrefs.SetFloat("settings_shake", Shake);
            PlayerPrefs.SetInt("settings_subtitles", Subtitles ? 1 : 0);
            PlayerPrefs.SetInt("settings_quality", Quality);
            PlayerPrefs.Save();
            Apply();
        }

        public static void Apply()
        {
            AudioListener.volume = MasterVolume;
            QualitySettings.shadowDistance = Quality switch { 0 => 22f, 1 => 38f, _ => 55f };
            QualitySettings.shadowResolution = Quality == 0 ? ShadowResolution.Low : ShadowResolution.Medium;
            QualitySettings.lodBias = Quality switch { 0 => .7f, 1 => 1f, _ => 1.35f };
            Camera camera = Camera.main;
            if (camera) camera.fieldOfView = FieldOfView;
        }
    }

    public sealed class MissionDirector : MonoBehaviour
    {
        public const int FinalStep = 10;
        public int Step { get; private set; }
        public float Progress { get; private set; }
        public int RivalsDefeated { get; private set; }
        public event Action<int> StepChanged;

        public string Title => Step switch
        {
            0 => GameText.Pick("BENEATH MOONROOT", "ПОД ЛУННЫМ КОРНЕМ"),
            1 => GameText.Pick("THE SCOUT'S SIGNAL", "СИГНАЛ РАЗВЕДЧИКА"),
            2 => GameText.Pick("WORKER TRAIL", "ТРОПА РАБОЧИХ"),
            3 => GameText.Pick("BARK GUARDIAN", "СТРАЖ КОРЫ"),
            4 => GameText.Pick("RAINWATCH RIDGE", "ГРЕБЕНЬ ДОЖДЕВОГО ДОЗОРА"),
            5 => GameText.Pick("CARRY IT HOME", "ДОСТАВИТЬ ДОМОЙ"),
            6 => GameText.Pick("A LIVING NURSERY", "ЖИВЫЕ ЯСЛИ"),
            7 => GameText.Pick("EMBERJAW RAID", "НАБЕГ ОГНЕННЫХ ЖВАЛ"),
            8 => GameText.Pick("ASHBACK", "ПЕПЕЛЬНОСПИН"),
            9 => GameText.Pick("THE FOREST STIRS", "ЛЕС ПРОБУЖДАЕТСЯ"),
            _ => GameText.Pick("MOONROOT ENDURES", "ЛУННЫЙ КОРЕНЬ ВЫСТОЯЛ")
        };

        public string Objective => Step switch
        {
            0 => GameText.Pick("Leave the nursery through the root tunnel", "Выйдите из яслей через корневой тоннель"),
            1 => GameText.Pick("Follow the firefly markers and meet the scout", "Следуйте за светляками и встретьте разведчика"),
            2 => GameText.Pick("Order workers to gather 5 seeds and 3 resin, then escort them home", "Прикажите рабочим собрать 5 семян и 3 смолы и сопроводите их домой"),
            3 => GameText.Pick("Break the bark beetle's charge and attack its exposed side", "Сорвите рывок жука-короеда и атакуйте открытый бок"),
            4 => GameText.Pick("Hold Rainwatch Ridge with your squad", "Удерживайте Гребень Дождевого Дозора вместе с отрядом"),
            5 => GameText.Pick("Return to the Moonroot entrance with the gathered cargo", "Вернитесь ко входу в Лунный Корень с собранным грузом"),
            6 => GameText.Pick("Enter the colony and expand the nursery chamber", "Войдите в колонию и расширьте камеру яслей"),
            7 => GameText.Pick($"Defend the entrance from Emberjaw raiders ({RivalsDefeated}/5)", $"Защитите вход от налётчиков Огненных Жвал ({RivalsDefeated}/5)"),
            8 => GameText.Pick("Command the soldiers and defeat the Ashback spider", "Командуйте солдатами и победите паука Пепельноспина"),
            9 => GameText.Pick("Climb the root overlook and witness the approaching threat", "Поднимитесь на корневой уступ и увидьте приближающуюся угрозу"),
            _ => GameText.Pick("Mission complete — Moonroot survived the first rain", "Миссия завершена — Лунный Корень пережил первый дождь")
        };

        public void NotifyNestExit()
        {
            if (Step == 0) Advance(1);
            else if (Step == 5) Advance(6);
        }

        public void NotifyScoutReached()
        {
            if (Step == 1) Advance(2);
        }

        public void NotifyGather()
        {
            if (Step != 2) return;
            ColonyState colony = WorldBootstrap.Instance.Colony;
            if (colony.Seeds >= ColonyState.UpgradeSeedCost && colony.Resin >= ColonyState.UpgradeResinCost)
                Advance(3);
        }

        public void NotifyKill(Creature.Species species)
        {
            if (Step == 3 && species == Creature.Species.Beetle) Advance(4);
            else if (Step == 7 && species == Creature.Species.RivalAnt)
            {
                RivalsDefeated++;
                StepChanged?.Invoke(Step);
                if (RivalsDefeated >= 5) Advance(8);
            }
            else if (Step == 8 && species == Creature.Species.Spider) Advance(9);
        }

        public void SetCaptureProgress(float progress)
        {
            if (Step != 4) return;
            Progress = Mathf.Clamp01(progress);
            StepChanged?.Invoke(Step);
            if (Progress >= 1) Advance(5);
        }

        public void NotifyReturnedToNest()
        {
            if (Step == 5) Advance(6);
        }

        public void NotifyUpgrade()
        {
            if (Step == 6) Advance(7);
        }

        public void NotifyThreatReveal()
        {
            if (Step == 9) Advance(FinalStep);
        }

        public void Restore(int step, float progress = 0)
        {
            Step = Mathf.Clamp(step, 0, FinalStep);
            Progress = Mathf.Clamp01(progress);
            RivalsDefeated = Step > 7 ? 5 : 0;
            StepChanged?.Invoke(Step);
        }

        void Advance(int next)
        {
            if (next <= Step) return;
            Step = next;
            Progress = 0;
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
                    population = world.Colony.Population,
                    missionStep = world.Mission.Step,
                    missionProgress = world.Mission.Progress,
                    player = new[] { position.x, position.y, position.z }
                };
                File.WriteAllText(PathFor(slot), JsonUtility.ToJson(data, true));
                PlayerPrefs.Save();
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
                if (data == null || data.version is < 2 or > 3 || data.player?.Length != 3) return false;
                world.Colony.Restore(data);
                world.Mission.Restore(data.missionStep, data.missionProgress);
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
