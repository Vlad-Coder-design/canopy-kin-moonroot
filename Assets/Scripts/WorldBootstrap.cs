using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace CanopyKin
{
    public sealed class WorldBootstrap : MonoBehaviour
    {
        public static WorldBootstrap Instance { get; private set; }
        public static readonly Vector3 NestPoint = new(0, 0, -7);
        static readonly Vector3 UndergroundCenter = new(0, -5.45f, -7);

        public PlayerAnt Player { get; private set; }
        public ColonyState Colony { get; private set; }
        public MissionDirector Mission { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsUnderground { get; private set; } = true;
        public bool IsCinematic { get; private set; }
        public bool IsAutomationSmoke { get; private set; }
        public Vector3 NestPosition => new(NestPoint.x, GroundHeight(NestPoint.x, NestPoint.z), NestPoint.z);
        public Vector3 SurfacePlayerSpawn => new(0, GroundHeight(0, -4.15f) + .05f, -4.15f);
        public Vector3 UndergroundPlayerSpawn => UndergroundCenter + new Vector3(0, .28f, .9f);
        public Vector3 PlayerRespawn => IsUnderground ? UndergroundPlayerSpawn : SurfacePlayerSpawn;

        readonly List<ResourceNode> resources = new();
        readonly List<Creature> creatures = new();
        readonly List<Renderer> surfaceRenderers = new();
        readonly List<Renderer> undergroundRenderers = new();
        SquadController squads;
        Transform environment;
        Transform underground;
        Transform rivalColony;
        GameObject nestUpgrade;
        GameObject undergroundUpgrade;
        GameObject largeThreat;
        Light sunLight;
        Light skyFillLight;
        Light amberNestLight;
        Light tunnelFillLight;
        Light nurseryFillLight;
        bool rivalWaveSpawned;
        bool threatRevealStarted;
        float toastUntil;
        string toast;
        float crosshairFlash;
        float autoStartAt;
        float creatureStatusUntil;
        string creatureStatusName;
        float creatureHealth;
        float creatureMaxHealth;
        bool creatureWeakHit;
        GUIStyle missionTitle;
        GUIStyle heading;
        GUIStyle body;
        GUIStyle small;
        GUIStyle centered;
        GUIStyle button;
        GUIStyle prompt;
        GUIStyle command;
        Texture2D panelTexture;
        Texture2D accentTexture;
        Texture2D dangerTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Spawn()
        {
            // The maximum-quality ant approval scene deliberately runs without
            // the generated mission world. It must prove the imported FBX,
            // renderer and clips in isolation before gameplay integration.
            if (FindFirstObjectByType<AntPrototypeShowcase>()) return;
            if (!FindFirstObjectByType<WorldBootstrap>())
                new GameObject("Moonroot vertical slice").AddComponent<WorldBootstrap>();
        }

        public static float GroundHeight(float x, float z)
        {
            float continental = (Mathf.PerlinNoise((x + 73f) * .045f, (z + 51f) * .045f) - .5f) * 3.4f;
            float erosion = (Mathf.PerlinNoise((x + 11f) * .12f, (z + 113f) * .12f) - .5f) * .78f;
            float ridges = Mathf.Abs(Mathf.Sin(x * .12f + z * .075f)) * .42f;
            float height = continental + erosion + ridges;

            // A naturally worn route keeps the mission traversable while the banks
            // and side paths retain substantial ant-scale relief.
            float trailDistance = Mathf.Abs(x - Mathf.Sin(z * .12f) * 1.4f);
            float trailBlend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.2f, 4.6f, trailDistance));
            float trailHeight = Mathf.Sin(z * .11f) * .18f;
            height = Mathf.Lerp(height, trailHeight, trailBlend * .72f);

            float nestDistance = Vector2.Distance(new Vector2(x, z), new Vector2(NestPoint.x, NestPoint.z));
            float nestBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.8f, 7.5f, nestDistance));
            height = Mathf.Lerp(0, height, nestBlend);

            float pondDistance = Vector2.Distance(new Vector2(x, z), new Vector2(-13.5f, 13.5f));
            float pondBlend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.5f, 6.2f, pondDistance));
            height -= pondBlend * .9f;
            return height;
        }

        void Awake()
        {
            Instance = this;
            GameSettings.Load();
            autoStartAt = Time.realtimeSinceStartup + 8f;
            Random.InitState(241103);
            BuildWorld();
            string[] arguments = System.Environment.GetCommandLineArgs();
            if (System.Array.Exists(
                    arguments,
                    argument => string.Equals(
                        argument,
                        "-ant-visual-qa",
                        System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginAntVisualQa());
            else if (System.Array.Exists(
                    arguments,
                    argument => string.Equals(
                        argument,
                        "-surface-smoke",
                        System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginSurfaceSmokeTest());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-spider-qa",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginSpiderQa());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-beetle-qa",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginBeetleQa());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-root-qa",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginRootQa());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-spider-combat-smoke",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginSpiderCombatSmoke());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-beetle-combat-smoke",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginBeetleCombatSmoke());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-mission-flow-smoke",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginMissionFlowSmokeTest());
        }

        IEnumerator BeginAntVisualQa()
        {
            IsAutomationSmoke = true;
            yield return null;
            BeginPlay();

            // Use the real mission surface, player, camera and production
            // AntVisual component. This QA mode does not create a mock scene.
            Mission.Restore(MissionDirector.SpiderStep);
            IsUnderground = false;
            RefreshWorldForMission();
            ApplyLocationLighting();
            Vector3 playerPosition = At(9.1f, 16.1f, .06f);
            Player.Teleport(playerPosition);
            Player.Face(playerPosition + Vector3.forward);
            squads.enabled = false;
            foreach (SquadUnit unit in FindObjectsByType<SquadUnit>(FindObjectsSortMode.None))
                unit.gameObject.SetActive(false);
            foreach (Creature creature in creatures)
                if (creature) creature.gameObject.SetActive(false);

            AntVisual playerVisual = Player.GetComponentInChildren<AntVisual>(true);
            Vector3 normal = QaGroundNormal(playerPosition.x, playerPosition.z);
            playerVisual.SetPlayerMotion(2.2f, .58f, true, normal);
            IsCinematic = true;
            yield return new WaitForSecondsRealtime(.35f);

            Vector3 playerFocus = Player.transform.position + Vector3.up * .42f;
            SetQaCamera(playerFocus, new Vector3(0, .34f, 2.65f), 43f);
            yield return CaptureQaScreenshot("ant-051-windows-player-front.tga");
            SetQaCamera(playerFocus, new Vector3(2.8f, .45f, .12f), 43f);
            yield return CaptureQaScreenshot("ant-051-windows-player-side-close.tga");
            SetQaCamera(playerFocus, new Vector3(0, .45f, -2.7f), 43f);
            yield return CaptureQaScreenshot("ant-051-windows-player-rear.tga");
            SetQaCamera(playerFocus, new Vector3(2.9f, 1.35f, -3.25f), 47f);
            yield return CaptureQaScreenshot("ant-051-windows-player-uneven-ground.tga");
            SetQaCamera(playerFocus, new Vector3(0, 3.2f, -.12f), 39f);
            yield return CaptureQaScreenshot("ant-051-windows-player-top.tga");
            // A deliberately low camera within the real sunlit mission region
            // puts the ant against the bright sky. Any holes, blended shell
            // fragments or inverted faces are immediately visible here.
            SetQaCamera(playerFocus, new Vector3(.22f, -.12f, 2.2f), 36f);
            yield return CaptureQaScreenshot(
                "ant-051-windows-player-bright-background.tga");

            // Arrange the real worker and unlocked soldier SquadUnit actors.
            IsCinematic = false;
            squads.enabled = true;
            squads.SetSoldiersUnlocked(true);
            IsCinematic = true;
            squads.enabled = false;
            SquadUnit[] units = FindObjectsByType<SquadUnit>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (SquadUnit unit in units)
                if (unit) unit.gameObject.SetActive(false);
            UnitRole[] roles =
            {
                UnitRole.Worker,
                UnitRole.LightSoldier,
                UnitRole.HeavySoldier
            };
            // The beetle arena is deliberately kept clear of dense vegetation,
            // which makes caste silhouettes readable without hiding the real
            // forest-floor material or replacing the gameplay environment.
            Vector3 lineup = At(7.3f, 14.2f, .04f);
            for (int i = 0; i < roles.Length; i++)
            {
                SquadUnit unit = units.FirstOrDefault(candidate =>
                    candidate && candidate.Role == roles[i]);
                if (!unit) continue;
                unit.gameObject.SetActive(true);
                unit.SetSelected(false);
                Vector3 position = lineup + Vector3.right * ((i - 1) * 1.35f);
                position.y = GroundHeight(position.x, position.z) + .03f;
                unit.transform.position = position;
                unit.transform.rotation = Quaternion.Euler(0, 180f, 0);
                unit.GetComponentInChildren<AntVisual>(true)?.SetPlayerMotion(
                    i == 0 ? 1.4f : .7f,
                    i == 0 ? .35f : .18f,
                    true,
                    QaGroundNormal(position.x, position.z));
            }
            SetRenderers(Player.transform, false);
            Vector3 lineupFocus = lineup + Vector3.up * .44f;
            SetQaCamera(lineupFocus, new Vector3(0, 1.02f, -4.45f), 38f);
            yield return CaptureQaScreenshot("ant-051-windows-worker-soldiers.tga");

            // Exercise the real cargo attachment and carrying pose on workers.
            foreach (SquadUnit unit in units)
                if (unit) unit.gameObject.SetActive(false);
            SquadUnit[] workers = units
                .Where(candidate => candidate && candidate.Role == UnitRole.Worker)
                .Take(3)
                .ToArray();
            Vector3 carryCenter = At(7.3f, 14.2f, .04f);
            for (int i = 0; i < workers.Length; i++)
            {
                SquadUnit worker = workers[i];
                worker.gameObject.SetActive(true);
                worker.SetSelected(false);
                if (!worker.HasCargo)
                    worker.TakeCargo((ResourceKind)(i % 3));
                Vector3 position = carryCenter +
                                   new Vector3((i - 1) * 1.15f, 0, i * .38f);
                position.y = GroundHeight(position.x, position.z) + .03f;
                worker.transform.position = position;
                worker.transform.rotation = Quaternion.Euler(0, 180f, 0);
                worker.GetComponentInChildren<AntVisual>(true)?.SetPlayerMotion(
                    1.35f,
                    .34f,
                    true,
                    QaGroundNormal(position.x, position.z));
            }
            SetQaCamera(
                carryCenter + Vector3.up * .56f,
                new Vector3(2.75f, 1.02f, -4.25f),
                41f);
            yield return CaptureQaScreenshot("ant-051-windows-workers-carrying.tga");

            // Inspect the actual queen actor in the authored underground chamber.
            IsUnderground = true;
            RefreshWorldForMission();
            ApplyLocationLighting();
            AntVisual queen = FindObjectsByType<AntVisual>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(ant => ant && ant.Caste == AntCaste.Queen);
            if (!queen)
                throw new System.InvalidOperationException(
                    "Ant visual QA could not locate the production queen.");
            SetRenderers(queen.transform, true);
            Vector3 queenFocus = queen.transform.position + Vector3.up * .5f;
            SetQaCamera(queenFocus, new Vector3(2.6f, 1.08f, 2.6f), 42f);
            yield return CaptureQaScreenshot("ant-051-windows-queen-chamber.tga");

            // Return to the real beetle mission actor and capture the player bite
            // while the production mandibles are inside their damage window.
            Mission.Restore(MissionDirector.BeetleStep);
            IsUnderground = false;
            RefreshWorldForMission();
            ApplyLocationLighting();
            Creature beetle = creatures.FirstOrDefault(creature =>
                creature && creature.Kind == Creature.Species.Beetle);
            if (!beetle)
                throw new System.InvalidOperationException(
                    "Ant visual QA requires the production beetle encounter.");
            beetle.gameObject.SetActive(true);
            beetle.FreezeForQa();
            Vector3 bitePosition = beetle.transform.position +
                                   beetle.transform.forward * 1.05f;
            bitePosition.y = GroundHeight(bitePosition.x, bitePosition.z) + .05f;
            Player.Teleport(bitePosition);
            Player.Face(beetle.transform.position + Vector3.up * .3f);
            SetRenderers(Player.transform, true);
            playerVisual.SetPlayerMotion(0, 0, true, QaGroundNormal(
                bitePosition.x,
                bitePosition.z));
            Player.BiteForQa();
            Vector3 biteFocus = Vector3.Lerp(
                                    Player.transform.position,
                                    beetle.transform.position,
                                    .43f) +
                                Vector3.up * .48f;
            SetQaCamera(biteFocus, new Vector3(3.4f, 1.05f, -3.35f), 43f);
            yield return new WaitForSecondsRealtime(.18f);
            yield return CaptureQaScreenshot("ant-051-windows-player-bite.tga");

            Debug.Log(
                "MOONROOT_ANT_VISUAL_QA_OK screenshots=10 " +
                $"playerState={playerVisual.AnimationState} queen={queen.Caste} " +
                $"workers={workers.Length}");
        }

        static Vector3 QaGroundNormal(float x, float z)
        {
            const float sample = .18f;
            float left = GroundHeight(x - sample, z);
            float right = GroundHeight(x + sample, z);
            float back = GroundHeight(x, z - sample);
            float front = GroundHeight(x, z + sample);
            return new Vector3(left - right, sample * 2f, back - front).normalized;
        }

        static void SetRenderers(Transform root, bool visible)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = visible;
        }

        static void SetQaCamera(Vector3 focus, Vector3 offset, float fieldOfView)
        {
            Camera camera = Camera.main;
            if (!camera) return;
            camera.transform.position = focus + offset;
            camera.transform.rotation = Quaternion.LookRotation(
                focus - camera.transform.position,
                Vector3.up);
            camera.fieldOfView = fieldOfView;
        }

        static IEnumerator CaptureQaScreenshot(string fileName)
        {
            yield return new WaitForEndOfFrame();
            string projectRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "..", ".."));
            string directory = Path.Combine(projectRoot, "QA", "Screenshots");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, fileName);
            var texture = new Texture2D(
                Screen.width,
                Screen.height,
                TextureFormat.RGB24,
                false);
            texture.ReadPixels(
                new Rect(0, 0, Screen.width, Screen.height),
                0,
                0,
                false);
            texture.Apply(false, false);
            Color32[] pixels = texture.GetPixels32();
            byte[] tga = new byte[18 + pixels.Length * 3];
            tga[2] = 2;
            tga[12] = (byte)(Screen.width & 0xff);
            tga[13] = (byte)((Screen.width >> 8) & 0xff);
            tga[14] = (byte)(Screen.height & 0xff);
            tga[15] = (byte)((Screen.height >> 8) & 0xff);
            tga[16] = 24;
            int write = 18;
            foreach (Color32 pixel in pixels)
            {
                tga[write++] = pixel.b;
                tga[write++] = pixel.g;
                tga[write++] = pixel.r;
            }
            File.WriteAllBytes(path, tga);
            Object.Destroy(texture);
            Debug.Log($"MOONROOT_ANT_QA_SCREENSHOT path={path}");
            yield return new WaitForSecondsRealtime(.5f);
        }

        IEnumerator BeginBeetleQa()
        {
            IsAutomationSmoke = true;
            yield return null;
            Mission.Restore(MissionDirector.BeetleStep);
            IsUnderground = false;
            RefreshWorldForMission();
            Creature beetle = creatures.Find(creature =>
                creature && creature.Kind == Creature.Species.Beetle);
            BeetleVisual visual = beetle
                ? beetle.GetComponentInChildren<BeetleVisual>(true)
                : null;
            if (!beetle || !visual)
                throw new System.InvalidOperationException(
                    "Beetle QA requires the production mission predator.");
            beetle.FreezeForQa();
            // Place the actual player in front of the frozen mission actor so the
            // QA view proves the horn, eyes and mandibles, not only the elytra.
            Vector3 playerPosition =
                beetle.transform.position + beetle.transform.forward * 7.5f;
            playerPosition.y = GroundHeight(playerPosition.x, playerPosition.z) + .05f;
            Player.Teleport(playerPosition);
            Player.Face(beetle.transform.position + Vector3.up * .4f);
            foreach (SquadUnit unit in FindObjectsByType<SquadUnit>(FindObjectsSortMode.None))
                unit.gameObject.SetActive(false);
            ApplyLocationLighting();
            BeginPlay();
            SkinnedMeshRenderer[] skins =
                visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int triangles = skins
                .Where(renderer => renderer.sharedMesh)
                .Sum(renderer => (int)renderer.sharedMesh.GetIndexCount(0) / 3);
            string bounds = string.Join(
                "; ",
                skins.Select(renderer =>
                    $"{renderer.name}:size={renderer.bounds.size}," +
                    $"offset={renderer.bounds.center - beetle.transform.position}"));
            Debug.Log(
                $"MOONROOT_BEETLE_QA_READY triangles={triangles} " +
                $"lods={skins.Length} bounds={bounds}");
        }

        IEnumerator BeginRootQa()
        {
            IsAutomationSmoke = true;
            yield return null;
            // CaptureStep broadcasts StepChanged every frame while its progress
            // fills, which deliberately restores surface renderers. Use the
            // stable preceding predator stage for an unobstructed environment QA.
            Mission.Restore(MissionDirector.SpiderStep);
            IsUnderground = false;
            RefreshWorldForMission();
            Vector3 playerPosition = At(9.1f, 16.1f, .05f);
            Player.Teleport(playerPosition);
            Player.Face(At(9.1f, 19.35f, .6f));
            ApplyLocationLighting();
            BeginPlay();
            foreach (SquadUnit unit in FindObjectsByType<SquadUnit>(FindObjectsSortMode.None))
                unit.gameObject.SetActive(false);
            foreach (Creature creature in creatures)
                if (creature) creature.gameObject.SetActive(false);
            foreach (AntVisual ant in FindObjectsByType<AntVisual>(FindObjectsSortMode.None))
                foreach (Renderer renderer in ant.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = false;

            GameObject[] networks = FindObjectsByType<LODGroup>(FindObjectsSortMode.None)
                .Where(group => group.name.Contains("Authored branching root network"))
                .Select(group => group.gameObject)
                .ToArray();
            int triangles = networks
                .SelectMany(network => network.GetComponentsInChildren<MeshFilter>(true))
                .Where(filter => filter.sharedMesh)
                .Sum(filter => (int)filter.sharedMesh.GetIndexCount(0) / 3);
            int colliders = networks.Sum(network =>
                network.GetComponentsInChildren<MeshCollider>(true).Length);
            Debug.Log(
                $"MOONROOT_ROOT_QA_READY instances={networks.Length} " +
                $"triangles={triangles} colliders={colliders}");
        }

        IEnumerator BeginBeetleCombatSmoke()
        {
            IsAutomationSmoke = true;
            yield return null;
            Mission.Restore(MissionDirector.BeetleStep);
            IsUnderground = false;
            RefreshWorldForMission();
            Creature beetle = creatures.Find(creature =>
                creature && creature.Kind == Creature.Species.Beetle);
            if (!beetle || !beetle.GetComponentInChildren<BeetleVisual>(true))
                throw new System.InvalidOperationException(
                    "Beetle combat smoke requires the production mission predator.");

            Vector3 playerPosition = beetle.transform.position + Vector3.forward * 1.25f;
            playerPosition.y = GroundHeight(playerPosition.x, playerPosition.z) + .05f;
            Player.Teleport(playerPosition);
            Player.Face(beetle.transform.position + Vector3.up * .35f);
            ApplyLocationLighting();
            BeginPlay();

            float elapsed = 0;
            float biteTimer = .85f;
            bool weakPointLocked = false;
            while (elapsed < 24f && Mission.Step == MissionDirector.BeetleStep)
            {
                elapsed += Time.deltaTime;
                biteTimer -= Time.deltaTime;
                // First let the real AI complete a telegraphed attack. Then hold
                // the predator still and move the player to its authored rear
                // weak point so this smoke test cannot degrade into repeatedly
                // biting the armored horn until the player dies.
                if (!weakPointLocked && beetle.AttackEvents >= 1)
                {
                    weakPointLocked = true;
                    beetle.FreezeForQa();
                }
                if (weakPointLocked)
                {
                    Vector3 weakPoint =
                        beetle.transform.position - beetle.transform.forward * .9f;
                    weakPoint.y = GroundHeight(weakPoint.x, weakPoint.z) + .05f;
                    Player.Teleport(weakPoint);
                }
                Player.Face(beetle.transform.position + Vector3.up * .35f);
                if (biteTimer <= 0)
                {
                    biteTimer = weakPointLocked ? .42f : .55f;
                    Player.BiteForQa();
                }
                yield return null;
            }
            yield return new WaitForSeconds(3f);

            bool missionAdvanced = Mission.Step == MissionDirector.UnlockSoldiersStep;
            bool deathCompleted = !beetle.gameObject.activeSelf;
            if (!missionAdvanced || !deathCompleted || beetle.DamageEvents < 4 ||
                beetle.AttackEvents < 1)
                throw new System.InvalidOperationException(
                    $"Beetle combat smoke failed: mission={Mission.Step} " +
                    $"active={beetle.gameObject.activeSelf} damageEvents={beetle.DamageEvents} " +
                    $"attackEvents={beetle.AttackEvents} hits={beetle.SuccessfulAttacks} " +
                    $"elapsed={elapsed:F1}.");

            Debug.Log(
                $"MOONROOT_BEETLE_COMBAT_SMOKE_OK elapsed={elapsed:F1} " +
                $"damageEvents={beetle.DamageEvents} attackEvents={beetle.AttackEvents} " +
                $"hits={beetle.SuccessfulAttacks} mission={Mission.Step}");
        }

        IEnumerator BeginSpiderCombatSmoke()
        {
            IsAutomationSmoke = true;
            yield return null;
            Mission.Restore(MissionDirector.SpiderStep);
            IsUnderground = false;
            RefreshWorldForMission();
            Creature spider = creatures.Find(creature =>
                creature && creature.Kind == Creature.Species.Spider);
            if (!spider || !spider.GetComponentInChildren<SpiderVisual>(true))
                throw new System.InvalidOperationException(
                    "Spider combat smoke requires the production mission predator.");

            Vector3 playerPosition = spider.transform.position + new Vector3(0, 0, 6f);
            playerPosition.y = GroundHeight(playerPosition.x, playerPosition.z) + .05f;
            Player.Teleport(playerPosition);
            Player.Face(spider.transform.position + Vector3.up * .45f);
            squads.Teleport(spider.transform.position + new Vector3(0, 0, 3.2f));
            ApplyLocationLighting();
            BeginPlay();

            float telegraphElapsed = 0;
            while (telegraphElapsed < 8f &&
                   spider.AttackEvents < 1 &&
                   Mission.Step == MissionDirector.SpiderStep)
            {
                telegraphElapsed += Time.deltaTime;
                Player.Face(spider.transform.position + Vector3.up * .45f);
                yield return null;
            }
            if (spider.AttackEvents < 1)
                throw new System.InvalidOperationException(
                    "Spider combat smoke did not observe a completed telegraphed attack.");

            squads.SelectSoldiers();
            squads.Set(SquadOrder.Attack);

            float elapsed = telegraphElapsed;
            while (elapsed < 32f && Mission.Step == MissionDirector.SpiderStep)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(3f);

            bool missionAdvanced = Mission.Step == MissionDirector.CaptureStep;
            bool deathCompleted = !spider.gameObject.activeSelf;
            if (!missionAdvanced || !deathCompleted || spider.DamageEvents < 2 ||
                spider.AttackEvents < 1)
                throw new System.InvalidOperationException(
                    $"Spider combat smoke failed: mission={Mission.Step} " +
                    $"active={spider.gameObject.activeSelf} damageEvents={spider.DamageEvents} " +
                    $"attackEvents={spider.AttackEvents} hits={spider.SuccessfulAttacks} " +
                    $"elapsed={elapsed:F1}.");

            Debug.Log(
                $"MOONROOT_SPIDER_COMBAT_SMOKE_OK elapsed={elapsed:F1} " +
                $"damageEvents={spider.DamageEvents} attackEvents={spider.AttackEvents} " +
                $"hits={spider.SuccessfulAttacks} mission={Mission.Step}");
        }

        IEnumerator BeginSpiderQa()
        {
            IsAutomationSmoke = true;
            yield return null;
            Mission.Restore(MissionDirector.SpiderStep);
            IsUnderground = false;
            RefreshWorldForMission();
            Creature spider = creatures.Find(creature =>
                creature && creature.Kind == Creature.Species.Spider);
            if (!spider)
                throw new System.InvalidOperationException(
                    "Spider QA could not locate the real mission predator.");
            SpiderVisual visual = spider.GetComponentInChildren<SpiderVisual>(true);
            if (!visual)
                throw new System.InvalidOperationException(
                    "Spider QA found the predator but not its production visual.");
            spider.FreezeForQa();
            Vector3 playerPosition = spider.transform.position + new Vector3(0, 0, 4.5f);
            playerPosition.y = GroundHeight(playerPosition.x, playerPosition.z) + .05f;
            Player.Teleport(playerPosition);
            Player.Face(spider.transform.position + Vector3.up * .45f);
            foreach (SquadUnit unit in FindObjectsByType<SquadUnit>(FindObjectsSortMode.None))
                unit.gameObject.SetActive(false);
            ApplyLocationLighting();
            BeginPlay();
            int triangles = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(renderer => renderer.sharedMesh)
                .Sum(renderer => (int)renderer.sharedMesh.GetIndexCount(0) / 3);
            string bounds = string.Join(
                "; ",
                visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Select(renderer =>
                        $"{renderer.name}:{renderer.bounds.size}"));
            Debug.Log(
                $"MOONROOT_SPIDER_QA_READY triangles={triangles} " +
                $"lods={visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length} " +
                $"bounds={bounds}");
        }

        IEnumerator BeginSurfaceSmokeTest()
        {
            yield return null;
            IsUnderground = false;
            // Movement QA begins at the real surface mouth and uses the same
            // nest-to-resource route as the mission. The diagnostic overlay is
            // forced on for this command-line-only mode so screenshots can
            // prove physical displacement against fixed landmarks.
            Vector3 entranceView = SurfacePlayerSpawn;
            Player.Teleport(entranceView);
            squads.Teleport(entranceView + Vector3.back * 1.8f);
            ApplyLocationLighting();
            BeginPlay();
            Player.SetMovementDiagnostics("1");
            Debug.Log("MOONROOT_SURFACE_SMOKE_READY");
        }

        IEnumerator BeginMissionFlowSmokeTest()
        {
            IsAutomationSmoke = true;
            yield return null;
            BeginPlay();

            RequireMissionStep(MissionDirector.QueenBriefingStep, "queen briefing start");
            int lockedSoldiers = CountActiveSoldiers();
            if (lockedSoldiers != 0)
                throw new System.InvalidOperationException(
                    $"Soldiers must be locked at mission start; active={lockedSoldiers}.");

            Mission.NotifyQueenBriefed();
            RequireMissionStep(MissionDirector.LeaveNestStep, "queen briefing");
            Mission.NotifyNestExit();
            RequireMissionStep(MissionDirector.MeetScoutStep, "nest exit");
            Mission.NotifyScoutReached();
            RequireMissionStep(MissionDirector.RallyWorkersStep, "scout");

            squads.SelectWorkers();
            squads.Set(SquadOrder.Gather);
            RequireMissionStep(MissionDirector.GatherStep, "worker command");
            Colony.Add(ResourceKind.Seed, ColonyState.UpgradeSeedCost);
            Colony.Add(ResourceKind.Resin, ColonyState.UpgradeResinCost);
            Mission.NotifyGather();
            RequireMissionStep(MissionDirector.BeetleStep, "physical delivery threshold");

            Colony.Add(ResourceKind.Protein, 1);
            Mission.NotifyKill(Creature.Species.Beetle);
            RequireMissionStep(MissionDirector.UnlockSoldiersStep, "beetle defeat");
            int unlockedSoldiers = CountActiveSoldiers();
            if (unlockedSoldiers != 4)
                throw new System.InvalidOperationException(
                    $"Four soldiers must unlock after Barkshield; active={unlockedSoldiers}.");

            squads.SelectSoldiers();
            squads.Set(SquadOrder.Attack);
            RequireMissionStep(MissionDirector.SpiderStep, "soldier command");
            Colony.Add(ResourceKind.Protein, 3);
            Mission.NotifyKill(Creature.Species.Spider);
            RequireMissionStep(MissionDirector.CaptureStep, "spider defeat");
            Mission.SetCaptureProgress(1);
            RequireMissionStep(MissionDirector.ReturnHomeStep, "ridge capture");

            IsUnderground = true;
            Mission.NotifyReturnedToNest();
            RequireMissionStep(MissionDirector.UpgradeStep, "colony return");
            Mission.NotifyUpgrade();
            RequireMissionStep(MissionDirector.SoundAlarmStep, "nursery upgrade");

            IsUnderground = false;
            ApplyLocationLighting();
            squads.SelectSoldiers();
            squads.Set(SquadOrder.Defend);
            RequireMissionStep(MissionDirector.RivalDefenseStep, "defend command");
            for (int i = 0; i < 5; i++)
                Mission.NotifyKill(Creature.Species.RivalAnt);
            RequireMissionStep(MissionDirector.OverlookStep, "rival defense");
            Mission.NotifyOverlookReached();
            RequireMissionStep(MissionDirector.RevealStep, "overlook arrival");
            Mission.NotifyThreatReveal();
            RequireMissionStep(MissionDirector.FinalStep, "threat reveal");

            const int smokeSlot = 99;
            bool saved = SaveSystem.Save(smokeSlot, this);
            Mission.Restore(MissionDirector.QueenBriefingStep);
            bool loaded = SaveSystem.Load(smokeSlot, this);
            SaveSystem.Delete(smokeSlot);
            RequireMissionStep(MissionDirector.FinalStep, "save/load restore");
            if (!saved || !loaded)
                throw new System.InvalidOperationException(
                    $"Mission smoke save/load failed: saved={saved} loaded={loaded}.");

            Debug.Log(
                $"MOONROOT_MISSION_FLOW_SMOKE_OK finalStep={Mission.Step} " +
                $"activeSoldiers={CountActiveSoldiers()} saveLoad={saved && loaded}");
        }

        void RequireMissionStep(int expected, string stage)
        {
            if (Mission.Step != expected)
                throw new System.InvalidOperationException(
                    $"Mission flow failed after {stage}: expected={expected} actual={Mission.Step}.");
        }

        static int CountActiveSoldiers()
        {
            int count = 0;
            foreach (SquadUnit unit in
                     FindObjectsByType<SquadUnit>(FindObjectsSortMode.None))
                if (unit.gameObject.activeSelf && unit.Role != UnitRole.Worker)
                    count++;
            return count;
        }

        void BuildWorld()
        {
            var timer = Stopwatch.StartNew();
            environment = new GameObject("Moonroot forest-floor region").transform;
            Colony = gameObject.AddComponent<ColonyState>();
            Mission = gameObject.AddComponent<MissionDirector>();
            squads = gameObject.AddComponent<SquadController>();
            gameObject.AddComponent<AudioDirector>().Initialize();
            gameObject.AddComponent<FxPool>().Initialize();
            Mission.StepChanged += _ => RefreshWorldForMission();

            ConfigureLighting();
            VisualFactory.Terrain(
                "Layered loam terrain",
                environment,
                82f,
                RuntimeQualityProfile.TerrainResolution(GameSettings.Quality),
                GroundHeight,
                new Color(.82f, .76f, .65f));
            BuildDistantEnclosure();
            BuildNest();
            BuildForageRoute();
            BuildLandmarks();
            BuildVegetation();
            BuildResources();
            BuildMissionLocations();
            BuildCreatures();
            BuildPlayerAndSquad();
            CacheLocationRenderers();
            RefreshWorldForMission();
            ApplyLocationLighting();
            gameObject.AddComponent<PerformanceTelemetry>();
            timer.Stop();
            int renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;
            Debug.Log(
                $"MOONROOT_SLICE_READY buildMs={timer.ElapsedMilliseconds} renderers={renderers} " +
                $"quality={GameSettings.Quality} edition={RuntimeQualityProfile.Edition}");
        }

        void ConfigureLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.52f, .59f, .55f);
            RenderSettings.ambientEquatorColor = new Color(.28f, .32f, .26f);
            RenderSettings.ambientGroundColor = new Color(.11f, .075f, .045f);
            RenderSettings.reflectionIntensity = .68f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.48f, .59f, .55f);
            RenderSettings.fogDensity = .009f;

            sunLight = new GameObject("Canopy-break sunlight").AddComponent<Light>();
            sunLight.transform.SetParent(transform);
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1f, .9f, .71f);
            sunLight.intensity = 1.08f;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = .78f;
            sunLight.shadowBias = .035f;
            sunLight.shadowNormalBias = .32f;
            sunLight.transform.rotation = Quaternion.Euler(42f, -28f, 0);

            skyFillLight = new GameObject("Cool canopy fill").AddComponent<Light>();
            skyFillLight.transform.SetParent(transform);
            skyFillLight.type = LightType.Directional;
            skyFillLight.color = new Color(.36f, .49f, .55f);
            skyFillLight.intensity = .3f;
            skyFillLight.shadows = LightShadows.None;
            skyFillLight.transform.rotation = Quaternion.Euler(62f, 142f, 18f);
            GameSettings.Apply();
        }

        void ApplyLocationLighting()
        {
            SetLocationRenderers();
            if (IsUnderground)
            {
                // The nest remains subterranean and warm, but its playable
                // silhouettes must stay readable on ordinary browser displays.
                // This uses ambient/fill energy rather than extra per-pixel
                // lights, so WebGL draw cost is unchanged.
                RenderSettings.ambientSkyColor = new Color(.35f, .37f, .32f);
                RenderSettings.ambientEquatorColor = new Color(.255f, .24f, .195f);
                RenderSettings.ambientGroundColor = new Color(.135f, .108f, .076f);
                RenderSettings.fogColor = new Color(.16f, .185f, .17f);
                RenderSettings.fogDensity = .0065f;
                if (sunLight) sunLight.intensity = .17f;
                if (skyFillLight) skyFillLight.intensity = .17f;
                if (amberNestLight) amberNestLight.intensity = 2.05f;
                if (tunnelFillLight) tunnelFillLight.intensity = 1.55f;
                if (nurseryFillLight) nurseryFillLight.intensity = 1f;
                return;
            }

            RenderSettings.ambientSkyColor = new Color(.52f, .59f, .55f);
            RenderSettings.ambientEquatorColor = new Color(.28f, .32f, .26f);
            RenderSettings.ambientGroundColor = new Color(.11f, .075f, .045f);
            RenderSettings.fogColor = new Color(.48f, .59f, .55f);
            RenderSettings.fogDensity = .009f;
            if (sunLight) sunLight.intensity = 1.08f;
            if (skyFillLight) skyFillLight.intensity = .3f;
            if (amberNestLight) amberNestLight.intensity = .16f;
            if (tunnelFillLight) tunnelFillLight.intensity = .12f;
            if (nurseryFillLight) nurseryFillLight.intensity = .08f;
        }

        void CacheLocationRenderers()
        {
            surfaceRenderers.Clear();
            undergroundRenderers.Clear();
            foreach (Renderer renderer in environment.GetComponentsInChildren<Renderer>(true))
            {
                // The player and squad cross the boundary and must never be baked
                // into either visibility partition.
                if (renderer.GetComponentInParent<PlayerAnt>() ||
                    renderer.GetComponentInParent<SquadUnit>())
                    continue;

                bool belowGround = renderer.transform.IsChildOf(underground) ||
                    renderer.bounds.center.y < -2.25f;
                (belowGround ? undergroundRenderers : surfaceRenderers).Add(renderer);
            }
        }

        void SetLocationRenderers()
        {
            foreach (Renderer renderer in surfaceRenderers)
                if (renderer) renderer.enabled = !IsUnderground;
            foreach (Renderer renderer in undergroundRenderers)
                if (renderer) renderer.enabled = IsUnderground;
        }

        void BuildDistantEnclosure()
        {
            var enclosure = new GameObject("Forest horizon enclosure").transform;
            enclosure.SetParent(environment, false);
            int treeCount = RuntimeQualityProfile.DistantTreeCount(GameSettings.Quality);
            for (int i = 0; i < treeCount; i++)
            {
                float angle = i / (float)treeCount * Mathf.PI * 2f;
                float radius = 36f + Mathf.Sin(i * 3.2f) * 2.4f;
                Vector3 basePoint = new(Mathf.Cos(angle) * radius, GroundHeight(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius) - .8f, Mathf.Sin(angle) * radius);
                Vector3 top = basePoint + new Vector3(Mathf.Sin(angle) * 1.8f, Random.Range(11f, 18f), Mathf.Cos(angle) * 1.8f);
                GameObject distantTree = VisualFactory.TexturedRoot(
                    "Distant bark pillar",
                    enclosure,
                    new[] { basePoint, Vector3.Lerp(basePoint, top, .45f) + Vector3.up * .7f, top },
                    new[] { Random.Range(2.2f, 3.5f), Random.Range(1.7f, 2.5f), Random.Range(1.2f, 1.8f) },
                    false);
                distantTree.GetComponent<Renderer>().shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            for (int i = 0; i < 14; i++)
            {
                float angle = i / 14f * Mathf.PI * 2f + .17f;
                Vector3 position = new(Mathf.Cos(angle) * 31f, GroundHeight(Mathf.Cos(angle) * 31f, Mathf.Sin(angle) * 31f), Mathf.Sin(angle) * 31f);
                VisualFactory.GrassTuft(enclosure, position, Random.Range(4.6f, 7.4f), Color.Lerp(new Color(.15f, .31f, .07f), new Color(.31f, .51f, .13f), Random.value), i);
            }
        }

        void BuildNest()
        {
            Vector3 nest = NestPosition;
            var surface = new GameObject("Moonroot surface colony").transform;
            surface.SetParent(environment, false);
            surface.position = nest;

            VisualFactory.OrganicPart(
                "Layered earth mound",
                surface,
                OrganicMeshFactory.BodyShape.SpiderBody,
                new Vector3(0, .15f, -.65f),
                new Vector3(7.4f, 1.65f, 6.3f),
                new Color(.28f, .16f, .075f),
                .05f,
                true).GetComponent<Renderer>().sharedMaterial =
                VisualFactory.PbrMaterial("Soil", new Color(.84f, .72f, .58f), .04f, 1.25f, new Vector2(2.6f, 2.6f));

            for (int i = 0; i < 18; i++)
            {
                float angle = i / 18f * Mathf.PI * 2f;
                float radius = 2.2f + Mathf.Sin(i * 2.1f) * .35f;
                VisualFactory.Stone(
                    "Mound-bound pebble",
                    surface,
                    new Vector3(Mathf.Cos(angle) * radius, .24f + (i % 3) * .025f, Mathf.Sin(angle) * radius - .48f),
                    new Vector3(.72f + i % 4 * .12f, .4f + i % 2 * .1f, .62f),
                    i,
                    false,
                    i % 3 == 0);
            }

            Vector3 entrance = nest + new Vector3(0, .12f, 1.8f);
            CreateNestDoor("Moonroot surface entrance", entrance, false);
            VisualFactory.TexturedRoot(
                "Living root gateway",
                surface,
                new[]
                {
                    surface.InverseTransformPoint(entrance + new Vector3(-1.15f, .02f, .18f)),
                    surface.InverseTransformPoint(entrance + new Vector3(-.7f, 1.12f, -.2f)),
                    surface.InverseTransformPoint(entrance + new Vector3(0, 1.46f, -.28f)),
                    surface.InverseTransformPoint(entrance + new Vector3(.7f, 1.12f, -.2f)),
                    surface.InverseTransformPoint(entrance + new Vector3(1.15f, .02f, .18f))
                },
                new[] { .34f, .29f, .27f, .29f, .34f },
                true);

            nestUpgrade = new GameObject("Expanded surface galleries");
            nestUpgrade.transform.SetParent(surface, false);
            for (int i = 0; i < 9; i++)
            {
                float angle = i / 9f * Mathf.PI * 2f;
                VisualFactory.OrganicPart(
                    "Hardened resin seal",
                    nestUpgrade.transform,
                    OrganicMeshFactory.BodyShape.Brood,
                    new Vector3(Mathf.Cos(angle) * 1.55f, .65f + Mathf.Sin(i) * .08f, Mathf.Sin(angle) * 1.15f - .62f),
                    new Vector3(.3f, .2f, .38f),
                    new Color(.95f, .22f, .025f),
                    .82f);
            }
            nestUpgrade.SetActive(false);
            BuildUndergroundNest();
        }

        void BuildUndergroundNest()
        {
            underground = new GameObject("Playable underground colony").transform;
            underground.SetParent(environment, false);
            underground.position = UndergroundCenter;

            GameObject floor = VisualFactory.Terrain("Compacted chamber floor", underground, 11.5f, 28,
                (x, z) => Mathf.PerlinNoise((x + 9f) * .3f, (z + 13f) * .3f) * .11f,
                new Color(.58f, .43f, .31f));
            floor.GetComponent<Renderer>().sharedMaterial =
                VisualFactory.PbrMaterial("Soil", new Color(.6f, .45f, .32f), .03f, 1.2f, new Vector2(.8f, .8f));

            VisualFactory.MeshObject(
                "Continuous earthen chamber shell",
                underground,
                OrganicMeshFactory.CaveShell(),
                Vector3.zero,
                Vector3.one,
                VisualFactory.PbrMaterial("Soil", new Color(.5f, .38f, .27f), .025f, 1.05f, new Vector2(.72f, .72f)),
                true);

            for (int i = 0; i < 16; i++)
            {
                float angle = i / 16f * Mathf.PI * 2f;
                Vector3 lower = new(Mathf.Cos(angle) * 4.75f, .05f, Mathf.Sin(angle) * 4.75f);
                Vector3 middle = new(Mathf.Cos(angle) * 4.2f, 1.7f + Mathf.Sin(i * 1.8f) * .25f, Mathf.Sin(angle) * 4.2f);
                Vector3 upper = new(Mathf.Cos(angle) * 3f, 3.3f, Mathf.Sin(angle) * 3f);
                VisualFactory.TexturedRoot(
                    "Woven root chamber wall",
                    underground,
                    new[] { lower, middle, upper },
                    new[] { .6f, .48f, .32f },
                    true);
            }
            for (int i = 0; i < 7; i++)
            {
                float angle = i / 7f * Mathf.PI * 2f;
                VisualFactory.TexturedRoot(
                    "Ceiling support root",
                    underground,
                    new[]
                    {
                        new Vector3(Mathf.Cos(angle) * 3f, 3.25f, Mathf.Sin(angle) * 3f),
                        new Vector3(Mathf.Cos(angle) * 1.5f, 3.55f, Mathf.Sin(angle) * 1.5f),
                        new Vector3(0, 3.72f, 0)
                    },
                    new[] { .34f, .27f, .18f },
                    false);
            }

            BuildQueenChamber();
            BuildStorageChambers();
            CreateNestDoor("Tunnel to forest floor", UndergroundCenter + new Vector3(0, .3f, 3.45f), true);

            amberNestLight = new GameObject("Amber chamber bounce").AddComponent<Light>();
            amberNestLight.transform.SetParent(underground, false);
            amberNestLight.transform.localPosition = new Vector3(-1.2f, 2.45f, -.85f);
            amberNestLight.type = LightType.Point;
            amberNestLight.range = 12f;
            amberNestLight.intensity = 1.18f;
            amberNestLight.color = new Color(.72f, .49f, .34f);
            amberNestLight.shadows = LightShadows.Soft;
            amberNestLight.shadowStrength = .32f;

            tunnelFillLight = new GameObject("Cool tunnel fill").AddComponent<Light>();
            tunnelFillLight.transform.SetParent(underground, false);
            tunnelFillLight.transform.localPosition = new Vector3(0, 1.1f, 3.3f);
            tunnelFillLight.type = LightType.Point;
            tunnelFillLight.range = 8f;
            tunnelFillLight.intensity = .92f;
            tunnelFillLight.color = new Color(.31f, .52f, .46f);

            nurseryFillLight = new GameObject("Nursery soft fill").AddComponent<Light>();
            nurseryFillLight.transform.SetParent(underground, false);
            nurseryFillLight.transform.localPosition = new Vector3(2f, 1.45f, -1.2f);
            nurseryFillLight.type = LightType.Point;
            nurseryFillLight.range = 7f;
            nurseryFillLight.intensity = .48f;
            nurseryFillLight.color = new Color(.67f, .43f, .29f);
        }

        void BuildQueenChamber()
        {
            var queen = new GameObject("Queen chamber").transform;
            queen.SetParent(underground, false);
            queen.localPosition = new Vector3(-2.9f, .08f, -1.75f);
            VisualFactory.OrganicPart(
                "Queen chamber packed earth",
                queen,
                OrganicMeshFactory.BodyShape.SpiderBody,
                Vector3.zero,
                new Vector3(3.6f, .54f, 2.25f),
                new Color(.32f, .15f, .055f),
                .06f).GetComponent<Renderer>().sharedMaterial =
                VisualFactory.PbrMaterial("Soil", new Color(.73f, .52f, .34f), .04f, 1.15f, new Vector2(1.2f, 1.2f));
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 2.399f;
                VisualFactory.OrganicPart(
                    i % 3 == 0 ? "Pupa" : "Larva",
                    queen,
                    OrganicMeshFactory.BodyShape.Brood,
                    new Vector3(Mathf.Cos(angle) * 1.65f, .31f + (i % 2) * .04f, Mathf.Sin(angle) * .78f),
                    new Vector3(.34f, .25f, .52f) * (i % 3 == 0 ? 1.15f : .9f),
                    new Color(.78f, .68f, .48f),
                    .34f);
            }
            AntVisual.Create(queen, new Color(.23f, .045f, .012f), 1.28f, AntCaste.Queen)
                .transform.localPosition = new Vector3(0, .28f, -.35f);
            for (int nurseIndex = 0; nurseIndex < 2; nurseIndex++)
            {
                var nurse = new GameObject($"Brood nurse {nurseIndex + 1}").transform;
                nurse.SetParent(queen, false);
                nurse.localPosition = new Vector3(
                    nurseIndex == 0 ? -1.05f : 1.08f,
                    .25f,
                    .28f + nurseIndex * .22f);
                nurse.localRotation = Quaternion.Euler(
                    0,
                    nurseIndex == 0 ? 36f : -148f,
                    0);
                AntVisual.Create(
                    nurse,
                    new Color(.28f, .075f, .022f),
                    .64f,
                    AntCaste.Nurse);
            }
            queen.gameObject.AddComponent<QueenBriefing>().Initialize();
        }

        void BuildStorageChambers()
        {
            var storage = new GameObject("Food storage chamber").transform;
            storage.SetParent(underground, false);
            storage.localPosition = new Vector3(-3.35f, .16f, .95f);
            for (int i = 0; i < 9; i++)
                ResourceNode.CreateCargoVisual(storage, i % 4 == 0 ? ResourceKind.Resin : ResourceKind.Seed,
                    new Vector3((i % 3 - 1) * .4f, .12f + i / 3 * .11f, (i / 3 - 1) * .35f), .75f);

            var stationObject = new GameObject("Nursery growth site");
            stationObject.transform.SetParent(underground, false);
            stationObject.transform.localPosition = new Vector3(2.9f, .25f, -.25f);
            VisualFactory.TexturedRoot(
                "Unfinished chamber ribs",
                stationObject.transform,
                new[]
                {
                    new Vector3(-.85f, 0, 0),
                    new Vector3(-.45f, .8f, -.1f),
                    new Vector3(0, 1.05f, -.18f),
                    new Vector3(.45f, .8f, -.1f),
                    new Vector3(.85f, 0, 0)
                },
                new[] { .18f, .15f, .13f, .15f, .18f },
                false);
            stationObject.AddComponent<UpgradeStation>().Initialize();

            undergroundUpgrade = new GameObject("Expanded worker and soldier chambers");
            undergroundUpgrade.transform.SetParent(underground, false);
            undergroundUpgrade.transform.localPosition = new Vector3(2.9f, .12f, -.25f);
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f;
                VisualFactory.Mushroom(undergroundUpgrade.transform, new Vector3(Mathf.Cos(a) * 1.2f, 0, Mathf.Sin(a) * .8f), .52f, new Color(.24f, .12f, .34f));
            }
            undergroundUpgrade.SetActive(false);
        }

        void CreateNestDoor(string name, Vector3 position, bool undergroundDoor)
        {
            var door = new GameObject(name);
            door.transform.SetParent(environment, false);
            door.transform.position = position;
            var collider = door.AddComponent<SphereCollider>();
            collider.radius = 1f;
            collider.isTrigger = true;
            door.AddComponent<ColonyEntrance>().Initialize(undergroundDoor);
            door.AddComponent<IInteractableHost>().Target = door.GetComponent<ColonyEntrance>();
            GameObject opening = VisualFactory.OrganicPart(
                "Deep earthen tunnel",
                door.transform,
                OrganicMeshFactory.BodyShape.Eye,
                Vector3.zero,
                new Vector3(2.15f, .42f, 1.7f),
                new Color(.008f, .005f, .003f),
                .02f);
            opening.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        void BuildLandmarks()
        {
            GameObject productionTrunk = VisualFactory.ProductionDeadTree(
                environment,
                At(11.7f, 10.9f, .34f),
                Quaternion.Euler(5f, -28f, 7f),
                Vector3.one * 4.1f);
            if (!productionTrunk)
            {
                VisualFactory.TexturedRoot(
                    "Fallen storm branch import fallback",
                    environment,
                    new[]
                    {
                        At(5.2f, 7.5f, .58f),
                        At(9.3f, 10.1f, .9f),
                        At(14.8f, 12.9f, .72f),
                        At(18.2f, 14.2f, .44f)
                    },
                    new[] { .9f, .78f, .62f, .26f },
                    true);
            }
            GameObject branchFork = VisualFactory.ProductionRootNetwork(
                environment,
                At(12.35f, 9.25f, .08f),
                Quaternion.Euler(2f, 26f, -5f),
                new Vector3(.92f, .88f, .76f));
            if (!branchFork)
                VisualFactory.TexturedRoot(
                    "Broken branch fork import fallback",
                    environment,
                    new[] { At(11.2f, 11.1f, .72f), At(13.2f, 8.3f, 1.45f), At(14.1f, 6.9f, 1.75f) },
                    new[] { .42f, .28f, .12f },
                    true);

            Vector3[] stones =
            {
                new(-8,0,5.8f), new(-6.7f,0,7.2f), new(-5.1f,0,6.45f), new(-3.8f,0,7.65f),
                new(-8.7f,0,8.1f), new(-6.8f,0,9.4f), new(-4.9f,0,9f), new(-3.1f,0,10.2f)
            };
            for (int i = 0; i < stones.Length; i++)
            {
                Vector3 p = stones[i];
                p.y = GroundHeight(p.x, p.z) + .16f;
                VisualFactory.Stone("Moss-covered stepping stone", environment, p,
                    new Vector3(1.45f + i % 3 * .26f, .78f + i % 2 * .18f, 1.2f), i, true, true);
            }

            Vector3 pond = At(-13.5f, 13.5f, -.48f);
            VisualFactory.Water(environment, pond, new Vector3(7.2f, .045f, 5.4f));
            for (int i = 0; i < 13; i++)
            {
                float a = i / 13f * Mathf.PI * 2f;
                Vector3 p = pond + new Vector3(Mathf.Cos(a) * 3.7f, .05f, Mathf.Sin(a) * 2.85f);
                VisualFactory.Stone("Wet pool stone", environment, p, new Vector3(.9f, .46f, .75f), 20 + i, true, i % 2 == 0);
            }

            GameObject rootRidge = VisualFactory.ProductionRootNetwork(
                environment,
                At(-13.65f, 8.8f, .03f),
                Quaternion.Euler(-2f, -57f, 3f),
                new Vector3(3.75f, 1.55f, 1.08f));
            if (!rootRidge)
                VisualFactory.TexturedRoot(
                    "Root ridge import fallback",
                    environment,
                    new[] { At(-20f, -1f, .32f), At(-16f, 5f, .72f), At(-11f, 13f, 1.1f), At(-7f, 19f, .56f) },
                    new[] { 1.25f, 1.05f, .82f, .42f },
                    true);

            GameObject rootBridge = VisualFactory.ProductionRootNetwork(
                environment,
                At(9.1f, 19.35f, .12f),
                Quaternion.Euler(5f, -16.5f, -2f),
                new Vector3(2.75f, 1.45f, .9f));
            if (!rootBridge)
                VisualFactory.TexturedRoot(
                    "Climbable root bridge import fallback",
                    environment,
                    new[] { At(1.5f, 17f, .4f), At(6.2f, 18.4f, 1.1f), At(12.5f, 20.4f, 1.55f), At(17f, 21.6f, .65f) },
                    new[] { .82f, .72f, .58f, .3f },
                    true);
        }

        Vector3 At(float x, float z, float above = 0) => new(x, GroundHeight(x, z) + above, z);

        void BuildVegetation()
        {
            InstancedVegetation instancedGrass =
                environment.gameObject.AddComponent<InstancedVegetation>();
            int grassCount = RuntimeQualityProfile.GrassCount(GameSettings.Quality);
            for (int i = 0; i < grassCount; i++)
            {
                Vector2 circle = Random.insideUnitCircle * 33f;
                float x = circle.x;
                float z = circle.y + 5f;
                if (KeepClear(x, z)) continue;
                float height = Random.Range(1.05f, 3.45f);
                Color grass = Color.Lerp(new Color(.18f, .37f, .075f), new Color(.46f, .65f, .19f), Random.value);
                Quaternion rotation = Quaternion.Euler(
                    0, Random.Range(0, 360f), Random.Range(-4f, 4f));
                Vector3 scale = new(.82f, height, .82f);
                if (SystemInfo.supportsInstancing)
                    instancedGrass.Add(i, At(x, z), rotation, scale, grass);
                else
                {
                    GameObject tuft = VisualFactory.GrassTuft(environment, At(x, z), height, grass, i);
                    tuft.transform.localRotation = rotation;
                }
            }
            instancedGrass.Complete();

            int leafCount = RuntimeQualityProfile.LeafCount(GameSettings.Quality);
            for (int i = 0; i < leafCount; i++)
            {
                Vector2 p = Random.insideUnitCircle * 29f;
                float z = p.y + 5f;
                if (KeepClear(p.x, z)) continue;
                VisualFactory.FallenLeaf(environment, At(p.x, z, .035f),
                    new Vector3(Random.Range(.8f, 1.65f), 1, Random.Range(.75f, 1.35f)), i);
            }

            Color[] petals = { new(.52f, .31f, .72f), new(.82f, .3f, .44f), new(.78f, .7f, .18f) };
            for (int i = 0; i < 14; i++)
            {
                float x = -3.3f + i % 4 * 2.2f;
                float z = 4.2f + i / 4 * 2.25f;
                VisualFactory.Flower(environment, At(x, z), petals[i % petals.Length]);
            }
            for (int i = 0; i < 18; i++)
            {
                float x = 5.7f + (i % 5) * 1.45f;
                float z = 5.3f + (i / 5) * 1.75f;
                VisualFactory.Mushroom(environment, At(x, z), Random.Range(.42f, .92f), Color.Lerp(new Color(.27f, .08f, .24f), new Color(.55f, .19f, .28f), Random.value));
            }

            int debrisCount = RuntimeQualityProfile.DebrisCount(GameSettings.Quality);
            for (int i = 0; i < debrisCount; i++)
            {
                Vector2 p = Random.insideUnitCircle * 30f;
                float z = p.y + 5f;
                if (KeepClear(p.x, z)) continue;
                GameObject debris = VisualFactory.Stone("Soil clod and pebble", environment, At(p.x, z, .06f),
                    new Vector3(Random.Range(.16f, .46f), Random.Range(.12f, .31f), Random.Range(.18f, .55f)),
                    40 + i, false, i % 5 == 0);
                debris.GetComponent<Renderer>().shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        void BuildForageRoute()
        {
            var route = new GameObject("Physical nest-to-forage route").transform;
            route.SetParent(environment, false);

            // A low root arch makes the player pass through the world rather
            // than merely viewing scenery beyond the playable corridor.
            GameObject arch = VisualFactory.TexturedRoot(
                "Walk-under bark arch",
                route,
                new[]
                {
                    At(-1.42f, -2.05f, .02f),
                    At(-.94f, -1.95f, .92f),
                    At(0, -1.9f, 1.34f),
                    At(.94f, -1.86f, .92f),
                    At(1.42f, -1.78f, .02f)
                },
                new[] { .24f, .2f, .17f, .2f, .24f },
                true);
            arch.AddComponent<MovementSurface>().Initialize("Wood", .96f);

            // A step-height feeder root verifies stable CharacterController
            // stepping without requiring a scripted teleport.
            GameObject stepRoot = VisualFactory.TexturedRoot(
                "Traversable feeder root",
                route,
                new[]
                {
                    At(-1.35f, -.15f, .06f),
                    At(-.2f, .02f, .13f),
                    At(1.3f, .18f, .07f)
                },
                new[] { .13f, .11f, .08f },
                true);
            stepRoot.AddComponent<MovementSurface>().Initialize("Wood", .94f);

            Material routeSoil = VisualFactory.PbrMaterial(
                "Soil",
                new Color(.72f, .58f, .42f),
                .035f,
                1.35f,
                new Vector2(1.4f, 1.4f));
            Vector3[] clumpPositions =
            {
                At(-1.5f, -3.25f, .08f), At(1.42f, -3.02f, .06f),
                At(-1.28f, -.8f, .07f), At(1.34f, .92f, .06f),
                At(-1.4f, 3.75f, .08f), At(1.36f, 4.22f, .07f)
            };
            for (int i = 0; i < clumpPositions.Length; i++)
            {
                GameObject clump = VisualFactory.Stone(
                    "Route soil bank",
                    route,
                    clumpPositions[i],
                    new Vector3(.86f + i % 2 * .18f, .48f, .72f),
                    90 + i,
                    true,
                    false);
                clump.GetComponent<Renderer>().sharedMaterial = routeSoil;
                clump.AddComponent<MovementSurface>().Initialize("Soil");
            }

            // The glade narrows between two real colliders. The opening is
            // wider than the scout but requires a visible navigation choice.
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject stone = VisualFactory.Stone(
                    side < 0 ? "Left moss gate stone" : "Right moss gate stone",
                    route,
                    At(side * .76f, 1.28f, .12f),
                    new Vector3(.74f, .66f, .84f),
                    112 + side,
                    true,
                    true);
                stone.AddComponent<MovementSurface>().Initialize("Moss", .88f);
            }

            // A shallow wet patch slows the ant. A thick curled leaf provides
            // the dry, shorter bridge while the banks remain passable.
            GameObject wetPatch = VisualFactory.Water(
                route,
                At(0, 2.72f, .025f),
                new Vector3(2.45f, .055f, 1.2f));
            wetPatch.name = "Slow wet-soil crossing";
            wetPatch.AddComponent<BoxCollider>();
            wetPatch.AddComponent<MovementSurface>().Initialize("Wet soil", .68f);

            GameObject leafBridge = VisualFactory.FallenLeaf(
                route,
                At(.05f, 2.73f, .13f),
                new Vector3(1.7f, 1.15f, 1.55f),
                27);
            leafBridge.name = "Traversable curled leaf bridge";
            leafBridge.transform.localRotation = Quaternion.Euler(-4f, 4f, 1.5f);
            MeshFilter bridgeMesh = leafBridge.GetComponent<MeshFilter>();
            if (bridgeMesh && bridgeMesh.sharedMesh)
                leafBridge.AddComponent<MeshCollider>().sharedMesh = bridgeMesh.sharedMesh;
            leafBridge.AddComponent<MovementSurface>().Initialize("Wood", .98f);

            // Nearby broad leaves bend and shed pollen when the player or a
            // squad passes. They sit inside the camera frustum for strong
            // foreground parallax.
            for (int i = 0; i < 14; i++)
            {
                float side = (i & 1) == 0 ? -1f : 1f;
                float z = -3.45f + i * .62f;
                GameObject grass = VisualFactory.GrassTuft(
                    route,
                    At(side * (.72f + (i % 3) * .18f), z),
                    1.25f + (i % 4) * .24f,
                    Color.Lerp(new Color(.17f, .38f, .08f), new Color(.47f, .65f, .2f), i / 13f),
                    120 + i);
                grass.name = "Reactive route grass";
                grass.AddComponent<ReactiveVegetation>().Initialize();
                var trigger = grass.AddComponent<CapsuleCollider>();
                trigger.center = new Vector3(0, .48f, 0);
                trigger.height = 1.05f;
                trigger.radius = .09f;
                trigger.isTrigger = true;
            }

            // Small loose clods are actual rigidbodies and can be nudged aside
            // by the CharacterController.
            for (int i = 0; i < 6; i++)
            {
                float x = -.48f + (i % 3) * .46f;
                float z = 3.45f + (i / 3) * .52f;
                GameObject debris = VisualFactory.Stone(
                    "Pushable route pebble",
                    route,
                    At(x, z, .12f),
                    Vector3.one * (.2f + i % 2 * .045f),
                    150 + i,
                    false,
                    i % 2 == 0);
                var collider = debris.AddComponent<SphereCollider>();
                collider.radius = .48f;
                var rigidbody = debris.AddComponent<Rigidbody>();
                rigidbody.mass = .085f;
                rigidbody.linearDamping = .9f;
                rigidbody.angularDamping = .7f;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }

            // Physical pheromone beads sit directly on the travel line and
            // visibly converge on the first seed glade.
            for (int i = 0; i < 18; i++)
            {
                float t = i / 17f;
                float z = Mathf.Lerp(-4.05f, 4.95f, t);
                float x = Mathf.Sin(t * Mathf.PI * 3.2f) * .3f;
                GameObject marker = VisualFactory.OrganicPart(
                    "Pheromone trail bead",
                    route,
                    OrganicMeshFactory.BodyShape.Brood,
                    At(x, z, .065f),
                    new Vector3(.09f, .045f, .13f),
                    new Color(.18f, .76f, .82f),
                    .62f);
                marker.AddComponent<PheromonePulse>().Initialize(i);
            }

            // Neutral foragers give the route independent ecosystem motion.
            for (int i = 0; i < 3; i++)
            {
                Vector3 a = At(-1.55f + i * .35f, -3.2f + i * 2.1f, .035f);
                Vector3 b = At(-1.2f + i * .42f, 1.2f + i * 1.65f, .035f);
                var forager = new GameObject($"Independent route forager {i + 1}");
                forager.transform.SetParent(route, false);
                forager.transform.position = a;
                AntVisual.Create(
                    forager.transform,
                    new Color(.17f, .04f, .012f),
                    .72f,
                    AntCaste.Worker);
                var collider = forager.AddComponent<SphereCollider>();
                collider.center = new Vector3(0, .22f, 0);
                collider.radius = .18f;
                forager.AddComponent<AmbientAntPatrol>().Initialize(a, b, 1.05f + i * .12f);
            }
        }

        bool KeepClear(float x, float z)
        {
            bool trail = Mathf.Abs(x - Mathf.Sin(z * .12f) * 1.4f) < 1.2f && z > -6f && z < 22f;
            bool nest = Vector2.Distance(new Vector2(x, z), new Vector2(0, -7)) < 4.6f;
            bool pond = Vector2.Distance(new Vector2(x, z), new Vector2(-13.5f, 13.5f)) < 5.2f;
            bool spiderArena =
                Vector2.Distance(new Vector2(x, z), new Vector2(1.2f, -16.5f)) < 7.2f;
            bool beetleArena =
                Vector2.Distance(new Vector2(x, z), new Vector2(7.3f, 14.2f)) < 6.4f;
            return trail || nest || pond || spiderArena || beetleArena;
        }

        void BuildResources()
        {
            Vector3[] seeds =
            {
                At(-2.4f,5.1f,.03f), At(-.8f,6.4f,.03f), At(1.2f,5.6f,.03f),
                At(2.8f,7.2f,.03f), At(-2.1f,8.3f,.03f), At(.4f,9.1f,.03f),
                At(2.6f,9.7f,.03f)
            };
            foreach (Vector3 seed in seeds) SpawnResource(ResourceKind.Seed, seed, 1);
            Vector3[] resin =
            {
                At(7.1f,7.7f,.05f), At(8.8f,9.2f,.05f), At(10.1f,10.5f,.05f),
                At(11.9f,11.4f,.05f), At(8.2f,11.8f,.05f)
            };
            foreach (Vector3 drop in resin) SpawnResource(ResourceKind.Resin, drop, 1);
        }

        void SpawnResource(ResourceKind kind, Vector3 position, int amount)
        {
            var root = new GameObject($"{kind} forage source");
            root.transform.SetParent(environment, false);
            root.transform.position = position;
            ResourceNode resource = root.AddComponent<ResourceNode>();
            resource.Initialize(kind, amount);
            resources.Add(resource);
        }

        void BuildMissionLocations()
        {
            var scout = new GameObject("Moonroot veteran scout");
            scout.transform.SetParent(environment, false);
            scout.transform.position = At(0.7f, .8f, .03f);
            AntVisual.Create(scout.transform, new Color(.22f, .05f, .012f), .95f, AntCaste.Scout);
            scout.AddComponent<ScoutGuide>().Initialize();

            var capture = new GameObject("Rainwatch Ridge capture point").transform;
            capture.SetParent(environment, false);
            capture.position = At(-7.4f, 16.2f, .08f);
            GameObject marker = VisualFactory.Stone("Capture marker", capture, Vector3.zero, new Vector3(1.2f, .34f, 1.2f), 72, false, true);
            marker.name = "Capture marker";
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f;
                VisualFactory.TexturedRoot(
                    "Rainwatch root spur",
                    capture,
                    new[] { Vector3.zero, new Vector3(Mathf.Cos(a) * 1.7f, .18f, Mathf.Sin(a) * 1.7f) },
                    new[] { .16f, .06f },
                    false);
            }
            capture.gameObject.AddComponent<CapturePoint>().Initialize();

            rivalColony = new GameObject("Emberjaw rival colony").transform;
            rivalColony.SetParent(environment, false);
            rivalColony.position = At(-16.5f, 22f, .12f);
            VisualFactory.OrganicPart(
                "Rival red-earth mound",
                rivalColony,
                OrganicMeshFactory.BodyShape.SpiderBody,
                Vector3.zero,
                new Vector3(5.8f, 1.35f, 5.1f),
                new Color(.45f, .12f, .035f),
                .08f,
                true).GetComponent<Renderer>().sharedMaterial =
                VisualFactory.PbrMaterial("Soil", new Color(.94f, .52f, .31f), .05f, 1.1f, new Vector2(2f, 2f));
            VisualFactory.OrganicPart("Rival tunnel", rivalColony, OrganicMeshFactory.BodyShape.Eye,
                new Vector3(0, .18f, 1.45f), new Vector3(1.6f, .34f, 1.2f), new Color(.01f, .004f, .002f), .01f);

            var overlook = new GameObject("Root overlook objective");
            overlook.transform.SetParent(environment, false);
            overlook.transform.position = At(8.5f, 21.1f, 1.1f);
            var trigger = overlook.AddComponent<SphereCollider>();
            trigger.radius = 1.7f;
            trigger.isTrigger = true;
            overlook.AddComponent<ThreatRevealTrigger>();

            largeThreat = new GameObject("Distant horned forest threat");
            largeThreat.transform.SetParent(environment, false);
            largeThreat.transform.position = At(8.5f, 31f, 2.2f);
            largeThreat.transform.localScale = Vector3.one * 3.6f;
            CreatureVisuals.BuildBeetle(largeThreat.transform);
            VisualFactory.TexturedRoot(
                "Threat left horn",
                largeThreat.transform,
                new[] { new Vector3(-.26f, .7f, .84f), new Vector3(-.55f, .9f, 1.35f), new Vector3(-.18f, .8f, 1.74f) },
                new[] { .09f, .075f, .025f },
                false);
            VisualFactory.TexturedRoot(
                "Threat right horn",
                largeThreat.transform,
                new[] { new Vector3(.26f, .7f, .84f), new Vector3(.55f, .9f, 1.35f), new Vector3(.18f, .8f, 1.74f) },
                new[] { .09f, .075f, .025f },
                false);
            largeThreat.SetActive(false);
        }

        void BuildCreatures()
        {
            SpawnCreature(
                Creature.Species.Beetle,
                At(7.3f, 14.2f, .035f),
                MissionDirector.BeetleStep);
            SpawnCreature(
                Creature.Species.Spider,
                At(1.2f, -16.5f, .035f),
                MissionDirector.SpiderStep);
        }

        Creature SpawnCreature(Creature.Species species, Vector3 position, int missionStep)
        {
            var root = new GameObject(species.ToString());
            root.transform.SetParent(environment, false);
            root.transform.position = position;
            Creature creature = root.AddComponent<Creature>();
            creature.Initialize(species, missionStep);
            creatures.Add(creature);
            return creature;
        }

        void SpawnRivalWave()
        {
            if (rivalWaveSpawned) return;
            rivalWaveSpawned = true;
            for (int i = 0; i < 5; i++)
            {
                Vector3 position = Vector3.Lerp(rivalColony.position, NestPosition, .35f + i * .07f);
                position.x += (i - 2) * .72f;
                position.y = GroundHeight(position.x, position.z) + .03f;
                SpawnCreature(
                    Creature.Species.RivalAnt,
                    position,
                    MissionDirector.RivalDefenseStep);
            }
        }

        void BuildPlayerAndSquad()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = GameSettings.FieldOfView;
            camera.nearClipPlane = .025f;
            camera.farClipPlane = RuntimeQualityProfile.IsFullQuality ? 180f : 125f;
            camera.allowHDR = RuntimeQualityProfile.IsFullQuality;
            camera.allowMSAA = GameSettings.Quality > 0;
            cameraObject.AddComponent<AudioListener>();

            var playerObject = new GameObject("Player scout ant");
            playerObject.transform.SetParent(environment, false);
            playerObject.transform.position = UndergroundPlayerSpawn;
            playerObject.AddComponent<CharacterController>();
            Player = playerObject.AddComponent<PlayerAnt>();

            UnitRole[] roles =
            {
                UnitRole.Worker, UnitRole.Worker, UnitRole.Worker, UnitRole.Worker,
                UnitRole.LightSoldier, UnitRole.LightSoldier, UnitRole.LightSoldier, UnitRole.HeavySoldier
            };
            for (int i = 0; i < roles.Length; i++)
            {
                var unit = new GameObject($"{roles[i]} {i + 1}");
                unit.transform.SetParent(environment, false);
                Vector3 offset = new((i % 4 - 1.5f) * .68f, 0, (i / 4) * .65f);
                unit.transform.position = UndergroundPlayerSpawn + offset + Vector3.forward * .8f;
                squads.Add(unit.transform, roles[i]);
            }
        }

        void Update()
        {
            if (!IsPlaying && Time.realtimeSinceStartup >= autoStartAt) BeginPlay();
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !IsPlaying || IsPaused || IsCinematic) return;
            if (keyboard.digit1Key.wasPressedThisFrame) squads.Set(SquadOrder.Gather);
            if (keyboard.digit2Key.wasPressedThisFrame) squads.Set(SquadOrder.Attack);
            if (keyboard.digit3Key.wasPressedThisFrame) squads.Set(SquadOrder.Follow);
            if (keyboard.digit4Key.wasPressedThisFrame) squads.Set(SquadOrder.Defend);
            if (keyboard.digit5Key.wasPressedThisFrame) squads.Set(SquadOrder.Patrol);
            if (keyboard.digit6Key.wasPressedThisFrame) squads.Set(SquadOrder.Retreat);
            if (keyboard.digit7Key.wasPressedThisFrame) squads.Set(SquadOrder.ReturnToNest);
            if (keyboard.zKey.wasPressedThisFrame) squads.SelectAll();
            if (keyboard.xKey.wasPressedThisFrame) squads.SelectWorkers();
            if (keyboard.cKey.wasPressedThisFrame) squads.SelectSoldiers();
            if (keyboard.f5Key.wasPressedThisFrame)
                ShowToast(SaveSystem.Save(1, this) ? GameText.Pick("Game saved", "Игра сохранена") : GameText.Pick("Save failed", "Ошибка сохранения"));
            if (keyboard.f9Key.wasPressedThisFrame)
                ShowToast(SaveSystem.Load(1, this) ? GameText.Pick("Save loaded", "Сохранение загружено") : GameText.Pick("No valid save", "Нет исправного сохранения"));
            crosshairFlash -= Time.unscaledDeltaTime;
        }

        public void BeginPlay()
        {
            IsPlaying = true;
            IsPaused = false;
            Time.timeScale = 1;
            if (Application.platform != RuntimePlatform.WebGLPlayer ||
                (Mouse.current != null && Mouse.current.leftButton.isPressed))
                Player?.RequestPointerCapture();
            ShowToast(GameText.Pick("Wake in the nursery and follow the blue tunnel light", "Проснитесь в яслях и следуйте к голубому свету тоннеля"));
        }

        public void TogglePause()
        {
            if (!IsPlaying || IsCinematic) return;
            IsPaused = !IsPaused;
            Time.timeScale = IsPaused ? 0 : 1;
            if (IsPaused) Player.UnlockPointer();
            else Player.RequestPointerCapture();
        }

        public void ToggleNest(PlayerAnt player, bool fromUnderground)
        {
            if (fromUnderground)
            {
                IsUnderground = false;
                player.Teleport(SurfacePlayerSpawn);
                squads.Teleport(SurfacePlayerSpawn + Vector3.forward * .8f);
                ShowToast(GameText.Pick("Forest floor — the rain has stopped", "Лесная подстилка — дождь закончился"));
            }
            else
            {
                IsUnderground = true;
                player.Teleport(UndergroundPlayerSpawn);
                squads.Teleport(UndergroundPlayerSpawn + Vector3.forward * 1.25f);
                ShowToast(GameText.Pick("Moonroot underground colony", "Подземная колония Лунного Корня"));
            }
            ApplyLocationLighting();
        }

        public void CommandSquad(SquadOrder order, Vector3 position, ResourceNode resource, Creature creature)
            => squads.Command(order, position, resource, creature);

        public void ApplyNestUpgrade()
        {
            if (nestUpgrade) nestUpgrade.SetActive(true);
            if (undergroundUpgrade) undergroundUpgrade.SetActive(true);
        }

        public void OnMissionAdvanced()
        {
            ShowToast(GameText.Pick($"New objective: {Mission.Title}", $"Новая цель: {Mission.Title}"));
            if (!IsAutomationSmoke) SaveSystem.Save(1, this);
            RefreshWorldForMission();
        }

        public void RefreshWorldForMission()
        {
            if (Colony != null && Colony.Level >= 2) ApplyNestUpgrade();
            squads?.SetSoldiersUnlocked(Mission.Step >= MissionDirector.UnlockSoldiersStep);
            if (Mission.Step == MissionDirector.RivalDefenseStep) SpawnRivalWave();
            if (largeThreat && Mission.Step >= MissionDirector.FinalStep)
                largeThreat.SetActive(true);
            foreach (Creature creature in creatures)
            {
                if (!creature) continue;
                bool completedEarlier =
                    (creature.Kind == Creature.Species.Beetle &&
                     Mission.Step > MissionDirector.BeetleStep) ||
                    (creature.Kind == Creature.Species.Spider &&
                     Mission.Step > MissionDirector.SpiderStep) ||
                    (creature.Kind == Creature.Species.RivalAnt &&
                     Mission.Step > MissionDirector.RivalDefenseStep);
                if (completedEarlier) creature.gameObject.SetActive(false);
            }
        }

        public ResourceNode FindNearestResource(Vector3 point, ResourceKind? kind = null)
        {
            ResourceNode best = null;
            float distance = float.MaxValue;
            foreach (ResourceNode resource in resources)
            {
                if (!resource || !resource.Available || (kind.HasValue && resource.Kind != kind.Value)) continue;
                float candidate = (resource.transform.position - point).sqrMagnitude;
                if (candidate >= distance) continue;
                distance = candidate;
                best = resource;
            }
            return best;
        }

        public Creature FindNearestActiveCreature(Vector3 point, Creature.Species? species = null)
        {
            Creature best = null;
            float distance = float.MaxValue;
            foreach (Creature creature in creatures)
            {
                if (!creature || !creature.IsActive || (species.HasValue && creature.Kind != species.Value)) continue;
                float candidate = (creature.transform.position - point).sqrMagnitude;
                if (candidate >= distance) continue;
                distance = candidate;
                best = creature;
            }
            return best;
        }

        Transform ObjectiveTarget()
        {
            if (IsUnderground)
            {
                if (Mission.Step == MissionDirector.QueenBriefingStep)
                    return underground.Find("Queen chamber");
                if (Mission.Step == MissionDirector.LeaveNestStep ||
                    Mission.Step == MissionDirector.SoundAlarmStep)
                    return environment.Find("Tunnel to forest floor");
                if (Mission.Step == MissionDirector.UpgradeStep)
                    return underground.Find("Nursery growth site");
            }
            return Mission.Step switch
            {
                MissionDirector.MeetScoutStep => environment.Find("Moonroot veteran scout"),
                MissionDirector.RallyWorkersStep => environment.Find("Moonroot veteran scout"),
                MissionDirector.GatherStep => FindNearestResource(Player.transform.position)?.transform,
                MissionDirector.BeetleStep =>
                    FindNearestActiveCreature(Player.transform.position, Creature.Species.Beetle)?.transform,
                MissionDirector.UnlockSoldiersStep => Player.transform,
                MissionDirector.SpiderStep =>
                    FindNearestActiveCreature(Player.transform.position, Creature.Species.Spider)?.transform,
                MissionDirector.CaptureStep => environment.Find("Rainwatch Ridge capture point"),
                MissionDirector.ReturnHomeStep => environment.Find("Moonroot surface entrance"),
                MissionDirector.SoundAlarmStep => environment.Find("Moonroot surface entrance"),
                MissionDirector.RivalDefenseStep =>
                    FindNearestActiveCreature(Player.transform.position, Creature.Species.RivalAnt)?.transform,
                MissionDirector.OverlookStep => environment.Find("Root overlook objective"),
                _ => null
            };
        }

        public void BeginThreatReveal()
        {
            if (threatRevealStarted) return;
            threatRevealStarted = true;
            StartCoroutine(ThreatReveal());
        }

        IEnumerator ThreatReveal()
        {
            IsCinematic = true;
            Player.UnlockPointer();
            if (largeThreat) largeThreat.SetActive(true);
            Camera camera = Camera.main;
            Vector3 startPosition = camera.transform.position;
            Quaternion startRotation = camera.transform.rotation;
            Vector3 destination = Player.transform.position + new Vector3(-2.8f, 2.2f, -2.6f);
            float elapsed = 0;
            while (elapsed < 2.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / 2.2f);
                camera.transform.position = Vector3.Lerp(startPosition, destination, t);
                camera.transform.rotation = Quaternion.Slerp(startRotation, Quaternion.LookRotation(largeThreat.transform.position + Vector3.up * 1.8f - destination), t);
                yield return null;
            }
            ShowToast(GameText.Pick("Something vast answers the rain...", "На зов дождя отвечает нечто огромное..."));
            yield return new WaitForSecondsRealtime(3.2f);
            Mission.NotifyThreatReveal();
            IsCinematic = false;
            Player.SnapCamera();
        }

        public void ShowToast(string message)
        {
            toast = message;
            toastUntil = Time.unscaledTime + 3.4f;
        }

        public void ShowCreatureStatus(string name, float health, float maximum, bool weakHit)
        {
            creatureStatusName = name;
            creatureHealth = health;
            creatureMaxHealth = maximum;
            creatureWeakHit = weakHit;
            creatureStatusUntil = Time.unscaledTime + 2.5f;
        }

        public void FlashCrosshair(bool hit) => crosshairFlash = hit ? .18f : .06f;

        void EnsureStyles()
        {
            if (body != null) return;
            Font interfaceFont = Resources.Load<Font>("Fonts/NotoSans-Regular");
            if (interfaceFont) GUI.skin.font = interfaceFont;
            panelTexture = MakeTexture(new Color(.035f, .055f, .043f, .9f));
            accentTexture = MakeTexture(new Color(.34f, .58f, .22f, .94f));
            dangerTexture = MakeTexture(new Color(.68f, .14f, .045f, .95f));
            GUI.skin.box.normal.background = panelTexture;
            body = new GUIStyle(GUI.skin.label) { font = interfaceFont, fontSize = 17, wordWrap = true };
            body.normal.textColor = new Color(.94f, .94f, .84f);
            small = new GUIStyle(body) { fontSize = 13 };
            small.normal.textColor = new Color(.78f, .84f, .72f);
            heading = new GUIStyle(body) { fontSize = 27, fontStyle = FontStyle.Bold };
            heading.normal.textColor = new Color(.91f, .74f, .28f);
            missionTitle = new GUIStyle(body) { fontSize = 14, fontStyle = FontStyle.Bold };
            missionTitle.normal.textColor = new Color(.55f, .84f, .42f);
            centered = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
            prompt = new GUIStyle(centered) { fontSize = 16, fontStyle = FontStyle.Bold };
            prompt.normal.textColor = new Color(1f, .83f, .3f);
            button = new GUIStyle(GUI.skin.button) { font = interfaceFont, fontSize = 17, fontStyle = FontStyle.Bold };
            button.normal.textColor = Color.white;
            button.normal.background = accentTexture;
            command = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            command.normal.textColor = new Color(.82f, .94f, .72f);
        }

        static Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(new[] { color, color * 1.06f, color * .9f, color });
            texture.Apply();
            return texture;
        }

        void OnGUI()
        {
            if (Player == null || Mission == null || Colony == null) return;
            EnsureStyles();
            float scale = Mathf.Clamp(Screen.height / 900f, .72f, 1.35f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            DrawMissionPanel();
            DrawVitals(width);
            DrawSquadPanel(width, height);
            DrawCreatureStatus(width);

            if (IsPlaying && !IsPaused && !IsCinematic)
            {
                Color old = GUI.color;
                GUI.color = crosshairFlash > 0 ? new Color(1f, .32f, .1f) : new Color(1f, .82f, .32f);
                GUI.Label(new Rect(width * .5f - 12, height * .5f - 14, 24, 28), "•", centered);
                GUI.color = old;
                if (!string.IsNullOrEmpty(Player.CurrentPrompt))
                {
                    GUI.Box(new Rect(width * .5f - 285, height - 121, 570, 45), "");
                    GUI.Label(new Rect(width * .5f - 275, height - 119, 550, 39), Player.CurrentPrompt, prompt);
                }
                DrawObjectiveMarker(scale);
                if (Player.TacticalView) DrawTacticalMenu(width, height);
            }

            if (Time.unscaledTime < toastUntil && !string.IsNullOrEmpty(toast))
            {
                GUI.Box(new Rect(width * .5f - 255, 22, 510, 52), "");
                GUI.Label(new Rect(width * .5f - 243, 25, 486, 44), toast, centered);
            }

            if (!IsPlaying) DrawStartOverlay(width, height);
            else if (IsPaused) DrawPauseOverlay(width, height);
            else if (Mission.Step >= MissionDirector.FinalStep) DrawCompletionOverlay(width, height);
            else if (IsCinematic)
                GUI.Label(new Rect(width * .5f - 320, height - 95, 640, 50), GameText.Pick("The canopy trembles beyond Rainwatch Ridge", "За Гребнем Дождевого Дозора дрожит лесной полог"), centered);
        }

        void DrawMissionPanel()
        {
            GUI.Box(new Rect(16, 16, 500, 154), "");
            GUI.Label(new Rect(31, 27, 464, 22), Mission.Title, missionTitle);
            GUI.Label(new Rect(31, 51, 464, 52), Mission.Objective, body);
            GUI.Label(new Rect(31, 112, 464, 23),
                GameText.Pick(
                    $"Seeds {Colony.Seeds}   Resin {Colony.Resin}   Protein {Colony.Protein}   Colony {Colony.Population}/{Colony.Capacity}",
                    $"Семена {Colony.Seeds}   Смола {Colony.Resin}   Белок {Colony.Protein}   Колония {Colony.Population}/{Colony.Capacity}"),
                small);
            if (Mission.Step == MissionDirector.CaptureStep || Colony.IsConstructing)
            {
                float progress = Mission.Step == MissionDirector.CaptureStep
                    ? Mission.Progress
                    : Colony.ConstructionProgress;
                DrawBar(new Rect(31, 140, 464, 8), progress, new Color(.42f, .78f, .24f));
            }
        }

        void DrawVitals(float width)
        {
            GUI.Box(new Rect(width - 294, 16, 278, 89), "");
            GUI.Label(new Rect(width - 278, 24, 110, 20), GameText.Pick("HEALTH", "ЗДОРОВЬЕ"), small);
            DrawBar(new Rect(width - 160, 29, 128, 11), Player.Health / 100f, new Color(.7f, .12f, .045f));
            GUI.Label(new Rect(width - 278, 52, 110, 20), GameText.Pick("STAMINA", "ВЫНОСЛИВОСТЬ"), small);
            DrawBar(new Rect(width - 160, 57, 128, 11), Player.Stamina / 100f, new Color(.43f, .68f, .2f));
            GUI.Label(new Rect(width - 278, 75, 246, 18), IsUnderground ? GameText.Pick("UNDERGROUND COLONY", "ПОДЗЕМНАЯ КОЛОНИЯ") : GameText.Pick("FOREST FLOOR", "ЛЕСНАЯ ПОДСТИЛКА"), small);
        }

        void DrawSquadPanel(float width, float height)
        {
            GUI.Box(new Rect(width - 344, height - 120, 328, 101), "");
            GUI.Label(new Rect(width - 332, height - 111, 304, 23), squads.StatusText, command);
            GUI.Label(new Rect(width - 329, height - 83, 298, 54),
                GameText.Pick("Z/X/C select · 1 gather · 2 attack · 3 follow · 4 defend\n5 patrol · 6 retreat · 7 return · Q tactical", "Z/X/C выбор · 1 сбор · 2 атака · 3 следовать · 4 защита\n5 патруль · 6 отход · 7 домой · Q тактика"),
                small);
        }

        void DrawCreatureStatus(float width)
        {
            if (Time.unscaledTime >= creatureStatusUntil || creatureMaxHealth <= 0) return;
            GUI.Box(new Rect(width * .5f - 190, 84, 380, 64), "");
            GUI.Label(new Rect(width * .5f - 175, 91, 350, 22),
                creatureWeakHit ? creatureStatusName + GameText.Pick("  — WEAK POINT", "  — УЯЗВИМОСТЬ") : creatureStatusName,
                new GUIStyle(missionTitle) { alignment = TextAnchor.MiddleCenter });
            DrawBar(new Rect(width * .5f - 164, 121, 328, 10), creatureHealth / creatureMaxHealth, new Color(.72f, .16f, .05f));
        }

        static void DrawBar(Rect rect, float value, Color color)
        {
            Color previous = GUI.color;
            GUI.color = new Color(.04f, .055f, .04f, .95f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x + 2, rect.y + 2, Mathf.Max(0, rect.width - 4) * Mathf.Clamp01(value), rect.height - 4), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        void DrawTacticalMenu(float width, float height)
        {
            GUI.Box(new Rect(18, height * .5f - 95, 238, 190), "");
            GUI.Label(new Rect(31, height * .5f - 83, 212, 26), GameText.Pick("TACTICAL PHEROMONES", "ФЕРОМОННАЯ ТАКТИКА"), missionTitle);
            GUI.Label(new Rect(31, height * .5f - 48, 212, 130),
                GameText.Pick("Left-click a resource or enemy.\nRight-click terrain to move.\n\nWorkers carry resources home.\nSoldiers surround threats.\nPress Q or Tab to return.", "ЛКМ — ресурс или противник.\nПКМ — приказ двигаться.\n\nРабочие несут груз домой.\nСолдаты окружают угрозу.\nQ или Tab — вернуться."),
                small);
        }

        void DrawObjectiveMarker(float scale)
        {
            Transform target = ObjectiveTarget();
            Camera camera = Camera.main;
            if (!target || !camera) return;
            Vector3 screen = camera.WorldToScreenPoint(target.position + Vector3.up * .85f);
            if (screen.z <= 0) return;
            float x = screen.x / scale;
            float y = (Screen.height - screen.y) / scale;
            float distance = Vector3.Distance(Player.transform.position, target.position);
            GUI.Label(new Rect(x - 75, y - 26, 150, 48), $"◆  {distance:0} m", prompt);
        }

        void DrawStartOverlay(float width, float height)
        {
            float panelWidth = Mathf.Min(700, width - 40);
            Rect panel = new(width * .5f - panelWidth * .5f, height * .5f - 220, panelWidth, 440);
            GUI.Box(panel, "");
            GUI.Label(new Rect(panel.x + 35, panel.y + 29, panel.width - 70, 45), "CANOPY KIN: MOONROOT", heading);
            GUI.Label(new Rect(panel.x + 35, panel.y + 87, panel.width - 70, 112),
                GameText.Pick(
                    "The first rain has opened new paths beneath the canopy. Lead a Moonroot scout from the nursery, guide workers along the forage trail, command soldiers, and defend the colony from the Emberjaw incursion.",
                    "Первый дождь открыл новые тропы под лесным пологом. Выведите разведчика Лунного Корня из яслей, проведите рабочих по кормовой тропе, командуйте солдатами и защитите колонию от Огненных Жвал."),
                body);
            GUI.Label(new Rect(panel.x + 35, panel.y + 211, panel.width - 70, 58),
                GameText.Pick("Third-person exploration · tactical squads · carrying workers · colony growth · predator combat", "Исследование от третьего лица · тактика отрядов · перенос ресурсов · развитие колонии · битвы с хищниками"),
                new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
            if (GUI.Button(new Rect(panel.x + panel.width * .5f - 160, panel.y + 290, 320, 60),
                    GameText.Pick("AWAKEN IN THE NURSERY", "ПРОСНУТЬСЯ В ЯСЛЯХ"), button))
                BeginPlay();
            GUI.Label(new Rect(panel.x + 35, panel.y + 365, panel.width - 70, 52),
                GameText.Pick("WASD move · mouse camera · Shift sprint · Space climb/vault · E interact · LMB bite · Q tactical · Esc menu", "WASD движение · мышь камера · Shift бег · Space подъём · E действие · ЛКМ укус · Q тактика · Esc меню"),
                new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
        }

        void DrawPauseOverlay(float width, float height)
        {
            Rect panel = new(width * .5f - 270, height * .5f - 245, 540, 490);
            GUI.Box(panel, "");
            GUI.Label(new Rect(panel.x + 35, panel.y + 23, panel.width - 70, 42), GameText.Pick("PAUSED & SETTINGS", "ПАУЗА И НАСТРОЙКИ"), heading);
            GUI.Label(new Rect(panel.x + 45, panel.y + 82, 180, 24), GameText.Pick("Mouse sensitivity", "Чувствительность мыши"), small);
            GameSettings.Sensitivity = GUI.HorizontalSlider(new Rect(panel.x + 245, panel.y + 91, 235, 18), GameSettings.Sensitivity, .025f, .15f);
            GUI.Label(new Rect(panel.x + 45, panel.y + 122, 180, 24), GameText.Pick("Field of view", "Поле зрения"), small);
            GameSettings.FieldOfView = GUI.HorizontalSlider(new Rect(panel.x + 245, panel.y + 131, 235, 18), GameSettings.FieldOfView, 54f, 82f);
            GUI.Label(new Rect(panel.x + 45, panel.y + 162, 180, 24), GameText.Pick("Master volume", "Общая громкость"), small);
            GameSettings.MasterVolume = GUI.HorizontalSlider(new Rect(panel.x + 245, panel.y + 171, 235, 18), GameSettings.MasterVolume, 0f, 1f);
            if (GUI.Button(new Rect(panel.x + 45, panel.y + 205, 140, 40), GameText.Pick("LOW", "НИЗКО"), button)) GameSettings.Quality = 0;
            if (GUI.Button(new Rect(panel.x + 200, panel.y + 205, 140, 40), GameText.Pick("MEDIUM", "СРЕДНЕ"), button)) GameSettings.Quality = 1;
            if (GUI.Button(new Rect(panel.x + 355, panel.y + 205, 140, 40), GameText.Pick("HIGH", "ВЫСОКО"), button)) GameSettings.Quality = 2;
            if (GUI.Button(new Rect(panel.x + 45, panel.y + 263, 215, 46), GameText.Pick("SAVE SLOT 1", "СОХРАНИТЬ"), button))
                ShowToast(SaveSystem.Save(1, this) ? GameText.Pick("Game saved", "Игра сохранена") : GameText.Pick("Save failed", "Ошибка сохранения"));
            if (GUI.Button(new Rect(panel.x + 280, panel.y + 263, 215, 46), GameText.Pick("LOAD SLOT 1", "ЗАГРУЗИТЬ"), button))
                ShowToast(SaveSystem.Load(1, this) ? GameText.Pick("Save loaded", "Сохранение загружено") : GameText.Pick("No valid save", "Нет сохранения"));
            if (GUI.Button(new Rect(panel.x + 120, panel.y + 330, 300, 54), GameText.Pick("APPLY & RESUME", "ПРИМЕНИТЬ И ПРОДОЛЖИТЬ"), button))
            {
                GameSettings.Save();
                TogglePause();
            }
            GUI.Label(new Rect(panel.x + 45, panel.y + 408, panel.width - 90, 54),
                GameText.Pick($"Preset {GameSettings.Quality + 1}/3 · F5 quick-save · F9 quick-load", $"Профиль {GameSettings.Quality + 1}/3 · F5 быстрое сохранение · F9 загрузка"),
                new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
        }

        void DrawCompletionOverlay(float width, float height)
        {
            Rect panel = new(width * .5f - 335, height * .5f - 132, 670, 264);
            GUI.Box(panel, "");
            GUI.Label(new Rect(panel.x + 34, panel.y + 28, panel.width - 68, 45), GameText.Pick("MOONROOT ENDURES", "ЛУННЫЙ КОРЕНЬ ВЫСТОЯЛ"), heading);
            GUI.Label(new Rect(panel.x + 34, panel.y + 85, panel.width - 68, 94),
                GameText.Pick("The nursery has grown and the Emberjaw raid is broken. Beyond Rainwatch Ridge, a horned giant has answered the storm. The Moonroot colony will need every worker and soldier for what comes next.", "Ясли выросли, а набег Огненных Жвал отражён. За Гребнем Дождевого Дозора на бурю ответил рогатый гигант. Лунному Корню понадобятся все рабочие и солдаты."),
                centered);
            GUI.Label(new Rect(panel.x + 34, panel.y + 205, panel.width - 68, 30), GameText.Pick("Vertical slice complete · progress saved", "Вертикальный срез завершён · прогресс сохранён"), missionTitle);
        }

        void OnDestroy()
        {
            Time.timeScale = 1;
            if (Instance == this) Instance = null;
        }
    }
}
