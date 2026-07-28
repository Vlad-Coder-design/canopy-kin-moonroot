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
        public static int Quality { get; set; } = RuntimeQualityProfile.IsFullQuality ? 2 : 0;

        public static void Load()
        {
            Sensitivity = PlayerPrefs.GetFloat("settings_sensitivity", .075f);
            FieldOfView = PlayerPrefs.GetFloat("settings_fov", 64f);
            MasterVolume = PlayerPrefs.GetFloat("settings_volume", .78f);
            Shake = PlayerPrefs.GetFloat("settings_shake", .28f);
            Subtitles = PlayerPrefs.GetInt("settings_subtitles", 1) != 0;
            int defaultQuality = RuntimeQualityProfile.IsFullQuality ? 2 : 0;
            Quality = Mathf.Clamp(PlayerPrefs.GetInt("settings_quality", defaultQuality), 0, 2);
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
            bool full = RuntimeQualityProfile.IsFullQuality;
            int unityLevel = full
                ? Quality switch { 0 => 3, 1 => 4, _ => 5 }
                : Quality switch { 0 => 1, 1 => 2, _ => 3 };
            QualitySettings.SetQualityLevel(unityLevel, true);
            QualitySettings.shadows = Quality == 0 && !full ? ShadowQuality.HardOnly : ShadowQuality.All;
            QualitySettings.shadowDistance = full
                ? Quality switch { 0 => 55f, 1 => 90f, _ => 140f }
                : Quality switch { 0 => 18f, 1 => 32f, _ => 46f };
            QualitySettings.shadowResolution = full
                ? Quality switch { 0 => ShadowResolution.Medium, 1 => ShadowResolution.High, _ => ShadowResolution.VeryHigh }
                : Quality switch { 0 => ShadowResolution.Low, 1 => ShadowResolution.Medium, _ => ShadowResolution.High };
            QualitySettings.shadowCascades = full && Quality == 2 ? 4 : Quality == 0 ? 1 : 2;
            QualitySettings.lodBias = full
                ? Quality switch { 0 => 1.2f, 1 => 1.75f, _ => 2.35f }
                : Quality switch { 0 => .65f, 1 => .9f, _ => 1.15f };
            QualitySettings.antiAliasing = full
                ? Quality switch { 0 => 2, 1 => 4, _ => 8 }
                : Quality == 2 ? 2 : 0;
            QualitySettings.anisotropicFiltering = full
                ? AnisotropicFiltering.ForceEnable
                : AnisotropicFiltering.Enable;
            QualitySettings.realtimeReflectionProbes = full && Quality > 0;
            QualitySettings.softParticles = Quality > 0;
            QualitySettings.streamingMipmapsActive = true;
            QualitySettings.streamingMipmapsMemoryBudget = full
                ? Quality switch { 0 => 768f, 1 => 1280f, _ => 2048f }
                : Quality switch { 0 => 192f, 1 => 256f, _ => 384f };
            QualitySettings.vSyncCount = full ? 1 : 0;
            Application.targetFrameRate = 60;
            Camera camera = Camera.main;
            if (camera) camera.fieldOfView = FieldOfView;
        }
    }

    public sealed class MissionDirector : MonoBehaviour
    {
        public const int QueenBriefingStep = 0;
        public const int LeaveNestStep = 1;
        public const int MeetScoutStep = 2;
        public const int RallyWorkersStep = 3;
        public const int GatherStep = 4;
        public const int BeetleStep = 5;
        public const int UnlockSoldiersStep = 6;
        public const int SpiderStep = 7;
        public const int CaptureStep = 8;
        public const int ReturnHomeStep = 9;
        public const int UpgradeStep = 10;
        public const int SoundAlarmStep = 11;
        public const int RivalDefenseStep = 12;
        public const int OverlookStep = 13;
        public const int RevealStep = 14;
        public const int FinalStep = 15;

        public int Step { get; private set; }
        public float Progress { get; private set; }
        public int RivalsDefeated { get; private set; }
        public event Action<int> StepChanged;

        public string Title => Step switch
        {
            QueenBriefingStep => GameText.Pick("THE QUEEN'S WARNING", "ПРЕДУПРЕЖДЕНИЕ КОРОЛЕВЫ"),
            LeaveNestStep => GameText.Pick("INTO THE FIRST RAIN", "НАВСТРЕЧУ ПЕРВОМУ ДОЖДЮ"),
            MeetScoutStep => GameText.Pick("PHEROMONE TRAIL", "ФЕРОМОННАЯ ТРОПА"),
            RallyWorkersStep => GameText.Pick("WORKERS OF MOONROOT", "РАБОЧИЕ ЛУННОГО КОРНЯ"),
            GatherStep => GameText.Pick("A COLONY MUST EAT", "КОЛОНИЯ ДОЛЖНА ЕСТЬ"),
            BeetleStep => GameText.Pick("BARKSHIELD", "КОРА-ЩИТ"),
            UnlockSoldiersStep => GameText.Pick("THE SOLDIER CHAMBER", "КАМЕРА СОЛДАТ"),
            SpiderStep => GameText.Pick("ASHBACK HUNTS", "ОХОТА ПЕПЕЛЬНОСПИНА"),
            CaptureStep => GameText.Pick("RAINWATCH RIDGE", "ГРЕБЕНЬ ДОЖДЕВОГО ДОЗОРА"),
            ReturnHomeStep => GameText.Pick("CARRY IT HOME", "ДОСТАВИТЬ ДОМОЙ"),
            UpgradeStep => GameText.Pick("A LIVING NURSERY", "ЖИВЫЕ ЯСЛИ"),
            SoundAlarmStep => GameText.Pick("EMBERJAW ALARM", "ТРЕВОГА ОГНЕННЫХ ЖВАЛ"),
            RivalDefenseStep => GameText.Pick("HOLD THE ENTRANCE", "УДЕРЖАТЬ ВХОД"),
            OverlookStep => GameText.Pick("THE FOREST STIRS", "ЛЕС ПРОБУЖДАЕТСЯ"),
            RevealStep => GameText.Pick("BEYOND THE RAIN", "ЗА ПЕЛЕНОЙ ДОЖДЯ"),
            _ => GameText.Pick("MOONROOT ENDURES", "ЛУННЫЙ КОРЕНЬ ВЫСТОЯЛ")
        };

        public string Objective => Step switch
        {
            QueenBriefingStep => GameText.Pick(
                "Approach the queen and hear why Moonroot needs you",
                "Подойдите к королеве и узнайте, почему вы нужны Лунному Корню"),
            LeaveNestStep => GameText.Pick(
                "Leave the nursery through the blue-lit root tunnel",
                "Покиньте ясли через освещённый синим корневой тоннель"),
            MeetScoutStep => GameText.Pick(
                "Learn movement and follow the firefly trail to the veteran scout",
                "Освойте движение и следуйте за светлячками к разведчику"),
            RallyWorkersStep => GameText.Pick(
                "Select workers (X), then order them to gather (1) or follow (3)",
                "Выберите рабочих (X), затем прикажите собирать (1) или следовать (3)"),
            GatherStep => GameText.Pick(
                "Escort workers while they physically deliver 5 seeds and 3 resin",
                "Сопровождайте рабочих, пока они доставят 5 семян и 3 смолы"),
            BeetleStep => GameText.Pick(
                "Bait the Barkshield charge, then bite its exposed side",
                "Спровоцируйте рывок Коры-Щита и кусайте открытый бок"),
            UnlockSoldiersStep => GameText.Pick(
                "Select the unlocked soldiers (C) and order attack (2) or follow (3)",
                "Выберите разблокированных солдат (C) и прикажите атаковать (2) или следовать (3)"),
            SpiderStep => GameText.Pick(
                "Command the soldiers and defeat the Ashback spider",
                "Командуйте солдатами и победите паука Пепельноспина"),
            CaptureStep => GameText.Pick(
                "Hold Rainwatch Ridge with your squad",
                "Удерживайте Гребень Дождевого Дозора вместе с отрядом"),
            ReturnHomeStep => GameText.Pick(
                "Return to the Moonroot entrance and enter the colony",
                "Вернитесь ко входу в Лунный Корень и войдите в колонию"),
            UpgradeStep => GameText.Pick(
                "Spend the gathered resources at the nursery growth site",
                "Потратьте собранные ресурсы на месте расширения яслей"),
            SoundAlarmStep => GameText.Pick(
                "Climb to the surface, select soldiers (C), and issue Defend (4)",
                "Поднимитесь на поверхность, выберите солдат (C) и прикажите защищать (4)"),
            RivalDefenseStep => GameText.Pick(
                $"Defend the entrance from Emberjaw raiders ({RivalsDefeated}/5)",
                $"Защитите вход от налётчиков Огненных Жвал ({RivalsDefeated}/5)"),
            OverlookStep => GameText.Pick(
                "Follow the retreating pheromone trace to the root overlook",
                "Следуйте по следу отступающих феромонов к корневому уступу"),
            RevealStep => GameText.Pick(
                "Witness the danger beyond the canopy",
                "Станьте свидетелем угрозы за пологом леса"),
            _ => GameText.Pick(
                "Mission complete — Moonroot survived the first rain",
                "Миссия завершена — Лунный Корень пережил первый дождь")
        };

        public void NotifyQueenBriefed()
        {
            if (Step == QueenBriefingStep) Advance(LeaveNestStep);
        }

        public void NotifyNestExit()
        {
            if (Step == LeaveNestStep) Advance(MeetScoutStep);
        }

        public void NotifyScoutReached()
        {
            if (Step == MeetScoutStep) Advance(RallyWorkersStep);
        }

        public void NotifySquadCommand(
            SquadOrder order,
            bool workersSelected,
            bool soldiersSelected)
        {
            if (Step == RallyWorkersStep && workersSelected &&
                (order == SquadOrder.Gather || order == SquadOrder.Follow))
                Advance(GatherStep);
            else if (Step == UnlockSoldiersStep && soldiersSelected &&
                     (order == SquadOrder.Attack || order == SquadOrder.Follow))
                Advance(SpiderStep);
            else if (Step == SoundAlarmStep && soldiersSelected &&
                     order == SquadOrder.Defend &&
                     WorldBootstrap.Instance && !WorldBootstrap.Instance.IsUnderground)
                Advance(RivalDefenseStep);
        }

        public void NotifyGather()
        {
            if (Step != GatherStep) return;
            ColonyState colony = WorldBootstrap.Instance.Colony;
            if (colony.Seeds >= ColonyState.UpgradeSeedCost &&
                colony.Resin >= ColonyState.UpgradeResinCost)
                Advance(BeetleStep);
        }

        public void NotifyKill(Creature.Species species)
        {
            if (Step == BeetleStep && species == Creature.Species.Beetle)
                Advance(UnlockSoldiersStep);
            else if (Step == SpiderStep && species == Creature.Species.Spider)
                Advance(CaptureStep);
            else if (Step == RivalDefenseStep && species == Creature.Species.RivalAnt)
            {
                RivalsDefeated++;
                StepChanged?.Invoke(Step);
                if (RivalsDefeated >= 5) Advance(OverlookStep);
            }
        }

        public void SetCaptureProgress(float progress)
        {
            if (Step != CaptureStep) return;
            Progress = Mathf.Clamp01(progress);
            StepChanged?.Invoke(Step);
            if (Progress >= 1) Advance(ReturnHomeStep);
        }

        public void NotifyReturnedToNest()
        {
            if (Step == ReturnHomeStep) Advance(UpgradeStep);
        }

        public void NotifyUpgrade()
        {
            if (Step == UpgradeStep) Advance(SoundAlarmStep);
        }

        public void NotifyOverlookReached()
        {
            if (Step == OverlookStep) Advance(RevealStep);
        }

        public void NotifyThreatReveal()
        {
            if (Step == RevealStep) Advance(FinalStep);
        }

        public void Restore(int step, float progress = 0)
        {
            Step = Mathf.Clamp(step, 0, FinalStep);
            Progress = Mathf.Clamp01(progress);
            RivalsDefeated = Step > RivalDefenseStep ? 5 : 0;
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
        static string PathFor(int slot) =>
            Path.Combine(Application.persistentDataPath, $"moonroot_{slot}.json");

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
                if (data == null || data.version is < 2 or > 4 || data.player?.Length != 3)
                    return false;

                world.Colony.Restore(data);
                world.Mission.Restore(MigrateMissionStep(data.version, data.missionStep), data.missionProgress);
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

        public static void Delete(int slot)
        {
            try
            {
                string path = PathFor(slot);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not remove Moonroot QA save: {exception.Message}");
            }
        }

        static int MigrateMissionStep(int version, int oldStep)
        {
            if (version >= 4) return oldStep;
            return oldStep switch
            {
                <= 0 => MissionDirector.QueenBriefingStep,
                1 => MissionDirector.MeetScoutStep,
                2 => MissionDirector.GatherStep,
                3 => MissionDirector.BeetleStep,
                4 => MissionDirector.CaptureStep,
                5 => MissionDirector.ReturnHomeStep,
                6 => MissionDirector.UpgradeStep,
                7 => MissionDirector.RivalDefenseStep,
                8 => MissionDirector.SpiderStep,
                9 => MissionDirector.OverlookStep,
                _ => MissionDirector.FinalStep
            };
        }
    }
}
