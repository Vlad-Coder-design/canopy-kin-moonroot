using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace CanopyKin
{
    public sealed class WorldBootstrap : MonoBehaviour
    {
        public static WorldBootstrap Instance { get; private set; }
        public static readonly Vector3 NestPoint = new(0, 0, -5);

        public PlayerAnt Player { get; private set; }
        public ColonyState Colony { get; private set; }
        public MissionDirector Mission { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsUnderground { get; private set; }
        public Vector3 NestPosition => new(NestPoint.x, GroundHeight(NestPoint.x, NestPoint.z), NestPoint.z);
        public Vector3 PlayerSpawn => new(0, GroundHeight(0, .25f) + .06f, .25f);

        SquadController squads;
        readonly List<ResourceNode> resources = new();
        readonly List<Creature> creatures = new();
        Transform environment;
        GameObject nestUpgrade;
        GameObject undergroundUpgrade;
        float toastUntil;
        string toast;
        float crosshairFlash;
        float autoStartAt;
        GUIStyle missionTitle;
        GUIStyle heading;
        GUIStyle body;
        GUIStyle small;
        GUIStyle centered;
        GUIStyle button;
        GUIStyle prompt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Spawn()
        {
            if (!FindFirstObjectByType<WorldBootstrap>())
                new GameObject("Moonroot World").AddComponent<WorldBootstrap>();
        }

        public static float GroundHeight(float x, float z)
        {
            float broad = (Mathf.PerlinNoise((x + 83f) * .055f, (z + 47f) * .055f) - .5f) * 1.55f;
            float detail = Mathf.Sin(x * .19f + z * .11f) * .18f + Mathf.Sin(z * .31f) * .12f;
            float distanceFromNest = Vector2.Distance(new Vector2(x, z), new Vector2(NestPoint.x, NestPoint.z));
            float nestBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.5f, 9f, distanceFromNest));
            return (broad + detail) * nestBlend;
        }

        void Awake()
        {
            Instance = this;
            autoStartAt = Time.realtimeSinceStartup + 6f;
            Random.InitState(241103);
            BuildWorld();
        }

        void BuildWorld()
        {
            environment = new GameObject("Moonroot forest floor").transform;
            Colony = gameObject.AddComponent<ColonyState>();
            Mission = gameObject.AddComponent<MissionDirector>();
            squads = gameObject.AddComponent<SquadController>();
            Mission.StepChanged += _ => RefreshWorldForMission();

            ConfigureLighting();
            VisualFactory.Terrain("Rolling soil", environment, 72f, 44, GroundHeight, new Color(.22f, .145f, .065f));
            BuildNest();
            BuildLandmarks();
            BuildVegetation();
            BuildResources();
            BuildCreatures();
            BuildPlayerAndSquad();
            RefreshWorldForMission();
            Debug.Log($"Moonroot vertical slice ready: {FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length} renderers.");
        }

        void ConfigureLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.44f, .47f, .39f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.46f, .56f, .5f);
            RenderSettings.fogDensity = .012f;
            var sunObject = new GameObject("Rainbreak sun");
            sunObject.transform.SetParent(transform);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, .86f, .62f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(48f, -32f, 0);
        }

        void BuildNest()
        {
            Vector3 nest = NestPosition;
            var surface = new GameObject("Moonroot surface nest").transform;
            surface.SetParent(environment, false);
            surface.position = nest;

            VisualFactory.Primitive(PrimitiveType.Sphere, "Living nest mound", surface, new Vector3(0, .08f, -.55f), new Vector3(3.8f, .82f, 3.2f), new Color(.17f, .095f, .045f), false, .08f);
            for (int i = 0; i < 11; i++)
            {
                float angle = i / 11f * Mathf.PI * 2f;
                float radius = 2.25f + Mathf.Sin(i * 2.1f) * .18f;
                VisualFactory.Primitive(
                    PrimitiveType.Sphere,
                    "Mound pebble",
                    surface,
                    new Vector3(Mathf.Cos(angle) * radius, .2f, Mathf.Sin(angle) * radius - .45f),
                    new Vector3(.58f, .34f, .5f),
                    new Color(.22f, .14f, .075f),
                    false,
                    .12f);
            }

            Vector3 entrancePosition = nest + new Vector3(0, .12f, 1.65f);
            CreateNestDoor("Moonroot entrance", entrancePosition, false);
            VisualFactory.WorldSegment("Left root arch", surface, entrancePosition + new Vector3(-1.05f, .05f, .15f), entrancePosition + new Vector3(-.55f, 1.2f, -.25f), .26f, new Color(.21f, .105f, .04f));
            VisualFactory.WorldSegment("Right root arch", surface, entrancePosition + new Vector3(1.05f, .05f, .15f), entrancePosition + new Vector3(.55f, 1.2f, -.25f), .26f, new Color(.21f, .105f, .04f));
            VisualFactory.WorldSegment("Root arch crown", surface, entrancePosition + new Vector3(-.6f, 1.18f, -.25f), entrancePosition + new Vector3(.6f, 1.18f, -.25f), .25f, new Color(.19f, .085f, .032f));

            nestUpgrade = new GameObject("Expanded surface nursery");
            nestUpgrade.transform.SetParent(surface, false);
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f;
                VisualFactory.Primitive(
                    PrimitiveType.Sphere,
                    "Nursery resin seal",
                    nestUpgrade.transform,
                    new Vector3(Mathf.Cos(a) * 1.45f, .72f, Mathf.Sin(a) * 1.15f - .6f),
                    Vector3.one * .2f,
                    new Color(.92f, .28f, .035f),
                    false,
                    .78f);
            }
            nestUpgrade.SetActive(false);

            BuildUndergroundNest();
        }

        void BuildUndergroundNest()
        {
            var chamber = new GameObject("Usable underground nursery").transform;
            chamber.SetParent(environment, false);
            chamber.position = new Vector3(0, -5.15f, -5f);
            VisualFactory.Primitive(PrimitiveType.Cube, "Chamber floor", chamber, new Vector3(0, -.12f, 0), new Vector3(7.5f, .24f, 7.5f), new Color(.105f, .052f, .028f), true, .04f);
            VisualFactory.Primitive(PrimitiveType.Cube, "Chamber ceiling", chamber, new Vector3(0, 2.8f, 0), new Vector3(7.5f, .28f, 7.5f), new Color(.075f, .038f, .02f), true, .03f);
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f;
                Vector3 lower = new(Mathf.Cos(a) * 3.35f, .1f, Mathf.Sin(a) * 3.35f);
                Vector3 upper = new(Mathf.Cos(a) * 2.45f, 2.8f, Mathf.Sin(a) * 2.45f);
                VisualFactory.Segment("Chamber root wall", chamber, lower, upper, .42f, new Color(.17f, .075f, .032f), true);
            }
            VisualFactory.Primitive(PrimitiveType.Sphere, "Queen chamber glow", chamber, new Vector3(0, .45f, -.9f), new Vector3(1.65f, .42f, 1.2f), new Color(.28f, .13f, .045f), false, .24f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * 2.399f;
                VisualFactory.Primitive(
                    PrimitiveType.Sphere,
                    "Brood",
                    chamber,
                    new Vector3(Mathf.Cos(a) * 1.2f, .34f, Mathf.Sin(a) * .75f - .9f),
                    new Vector3(.22f, .16f, .38f),
                    new Color(.78f, .67f, .45f),
                    false,
                    .32f);
            }

            var lightObject = new GameObject("Bioluminescent chamber light");
            lightObject.transform.SetParent(chamber, false);
            lightObject.transform.localPosition = new Vector3(0, 2.2f, 0);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 9f;
            light.intensity = 2.3f;
            light.color = new Color(.72f, .45f, .2f);

            CreateNestDoor("Tunnel to forest", new Vector3(0, -4.85f, -2.5f), true);
            undergroundUpgrade = new GameObject("Expanded underground nursery");
            undergroundUpgrade.transform.SetParent(chamber, false);
            for (int i = 0; i < 5; i++)
                VisualFactory.Mushroom(undergroundUpgrade.transform, new Vector3(-1.3f + i * .65f, .12f, 1.45f), .55f, new Color(.42f, .16f, .48f));
            undergroundUpgrade.SetActive(false);
        }

        void CreateNestDoor(string name, Vector3 position, bool underground)
        {
            var door = new GameObject(name);
            door.transform.SetParent(environment, false);
            door.transform.position = position;
            var collider = door.AddComponent<SphereCollider>();
            collider.radius = .95f;
            collider.isTrigger = true;
            var entrance = door.AddComponent<ColonyEntrance>();
            entrance.Initialize(underground);
            var host = door.AddComponent<IInteractableHost>();
            host.Target = entrance;
            VisualFactory.Primitive(
                PrimitiveType.Sphere,
                "Dark tunnel",
                door.transform,
                Vector3.zero,
                new Vector3(1.15f, .2f, .9f),
                new Color(.012f, .008f, .004f),
                false,
                .02f);
        }

        void BuildLandmarks()
        {
            Color bark = new(.22f, .085f, .026f);
            Vector3 branchStart = new(5.2f, GroundHeight(5.2f, 8f) + .52f, 8f);
            Vector3 branchEnd = new(14.5f, GroundHeight(14.5f, 13.2f) + .72f, 13.2f);
            VisualFactory.WorldSegment("Fallen rain branch", environment, branchStart, branchEnd, .62f, bark, true, .32f);
            VisualFactory.WorldSegment("Broken branch fork", environment, new Vector3(10.7f, GroundHeight(10.7f, 11f) + .6f, 11f), new Vector3(12.2f, GroundHeight(12.2f, 8f) + 1.2f, 8f), .28f, bark, true, .28f);

            Vector3[] stones =
            {
                new(-7, 0, 7), new(-5.6f, 0, 8.1f), new(-4.2f, 0, 7.35f), new(-2.9f, 0, 8.45f),
                new(-7.8f, 0, 9.1f), new(-6.1f, 0, 10.1f), new(-4.5f, 0, 9.6f)
            };
            for (int i = 0; i < stones.Length; i++)
            {
                Vector3 p = stones[i];
                p.y = GroundHeight(p.x, p.z) + .18f;
                VisualFactory.WorldPrimitive(
                    PrimitiveType.Sphere,
                    "Moss stone",
                    environment,
                    p,
                    new Vector3(1.1f + i % 3 * .22f, .42f + i % 2 * .12f, .82f),
                    new Color(.18f, .29f + i % 2 * .04f, .13f),
                    true,
                    .18f);
            }

            Vector3 pond = new(-13f, GroundHeight(-13f, 15f) - .15f, 15f);
            VisualFactory.WorldPrimitive(PrimitiveType.Sphere, "Rain pool", environment, pond, new Vector3(5.5f, .16f, 4.1f), new Color(.08f, .28f, .31f), false, .88f);
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2f;
                Vector3 p = pond + new Vector3(Mathf.Cos(a) * 3f, .14f, Mathf.Sin(a) * 2.25f);
                VisualFactory.WorldPrimitive(PrimitiveType.Sphere, "Pool pebble", environment, p, new Vector3(.7f, .28f, .55f), new Color(.19f, .17f, .12f), true, .2f);
            }

            VisualFactory.WorldSegment(
                "Giant root ridge",
                environment,
                new Vector3(-18f, GroundHeight(-18, 1) + .35f, 1f),
                new Vector3(-8f, GroundHeight(-8, 17) + .65f, 17f),
                .8f,
                new Color(.18f, .07f, .025f),
                true,
                .23f);
            VisualFactory.WorldSegment(
                "Root bridge",
                environment,
                new Vector3(2f, GroundHeight(2, 17) + .45f, 17f),
                new Vector3(13f, GroundHeight(13, 20) + 1.1f, 20f),
                .55f,
                new Color(.2f, .08f, .027f),
                true,
                .25f);
        }

        void BuildVegetation()
        {
            for (int i = 0; i < 165; i++)
            {
                Vector2 circle = Random.insideUnitCircle * 30f;
                float x = circle.x;
                float z = circle.y + 5f;
                bool clearPath = Mathf.Abs(x) < 2.4f && z > -4f && z < 17f;
                bool clearNest = Vector2.Distance(new Vector2(x, z), new Vector2(0, -5)) < 4.4f;
                bool pond = Vector2.Distance(new Vector2(x, z), new Vector2(-13, 15)) < 5f;
                if (clearPath || clearNest || pond) continue;
                float height = Random.Range(1.15f, 3.7f);
                Color grass = Color.Lerp(new Color(.12f, .29f, .065f), new Color(.36f, .57f, .13f), Random.value);
                VisualFactory.GrassTuft(environment, new Vector3(x, GroundHeight(x, z), z), height, grass);
            }

            Color[] petals =
            {
                new(.55f, .35f, .8f), new(.9f, .36f, .55f), new(.78f, .72f, .23f)
            };
            for (int i = 0; i < 9; i++)
            {
                float x = -3f + i % 3 * 2.8f;
                float z = 4.7f + i / 3 * 2.4f;
                VisualFactory.Flower(environment, new Vector3(x, GroundHeight(x, z), z), petals[i % petals.Length]);
            }

            for (int i = 0; i < 12; i++)
            {
                float x = 6.2f + (i % 4) * 1.7f;
                float z = 6.3f + (i / 4) * 2.2f;
                VisualFactory.Mushroom(environment, new Vector3(x, GroundHeight(x, z), z), Random.Range(.45f, .9f), new Color(.38f, .12f + i % 3 * .06f, .32f));
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 p = Random.insideUnitCircle * 22f;
                float z = p.y + 5f;
                GameObject leaf = VisualFactory.WorldPrimitive(
                    PrimitiveType.Sphere,
                    "Rain-dark leaf",
                    environment,
                    new Vector3(p.x, GroundHeight(p.x, z) + .035f, z),
                    new Vector3(Random.Range(.7f, 1.5f), .035f, Random.Range(.35f, .7f)),
                    Color.Lerp(new Color(.22f, .12f, .025f), new Color(.42f, .25f, .045f), Random.value),
                    false,
                    .12f);
                leaf.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), Random.Range(-6f, 6f));
            }
        }

        void BuildResources()
        {
            SpawnResource(ResourceKind.Seed, new Vector3(-1.8f, 0, 5.8f), 1);
            SpawnResource(ResourceKind.Seed, new Vector3(.4f, 0, 7.3f), 1);
            SpawnResource(ResourceKind.Seed, new Vector3(2.25f, 0, 5.9f), 1);
            SpawnResource(ResourceKind.Resin, new Vector3(7.2f, 0, 8.15f), 1);
            SpawnResource(ResourceKind.Resin, new Vector3(9.4f, 0, 9.6f), 1);
        }

        void SpawnResource(ResourceKind kind, Vector3 position, int amount)
        {
            position.y = GroundHeight(position.x, position.z) + .04f;
            var root = new GameObject($"{kind} cache");
            root.transform.SetParent(environment, false);
            root.transform.position = position;
            ResourceNode resource = root.AddComponent<ResourceNode>();
            resource.Initialize(kind, amount);
            resources.Add(resource);
        }

        void BuildCreatures()
        {
            SpawnCreature(Creature.Species.Beetle, new Vector3(7.1f, 0, 13.2f), 2);
            SpawnCreature(Creature.Species.RivalAnt, new Vector3(-7.5f, 0, 15.8f), 3);
            SpawnCreature(Creature.Species.Spider, new Vector3(1.2f, 0, -13.2f), 5);
        }

        void SpawnCreature(Creature.Species species, Vector3 position, int missionStep)
        {
            position.y = GroundHeight(position.x, position.z) + .03f;
            var root = new GameObject(species.ToString());
            root.transform.SetParent(environment, false);
            root.transform.position = position;
            Creature creature = root.AddComponent<Creature>();
            creature.Initialize(species, missionStep);
            creatures.Add(creature);
        }

        void BuildPlayerAndSquad()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 68f;
            camera.nearClipPlane = .035f;
            camera.farClipPlane = 110f;
            cameraObject.AddComponent<AudioListener>();

            var playerObject = new GameObject("Player ant");
            playerObject.transform.SetParent(environment, false);
            playerObject.transform.position = PlayerSpawn;
            playerObject.AddComponent<CharacterController>();
            Player = playerObject.AddComponent<PlayerAnt>();

            for (int i = 0; i < 6; i++)
            {
                var unit = new GameObject(i < 3 ? $"Worker {i + 1}" : $"Soldier {i - 2}");
                unit.transform.SetParent(environment, false);
                Vector3 position = NestPosition + new Vector3((i % 3 - 1) * .65f, 0, -.25f - (i / 3) * .62f);
                position.y = GroundHeight(position.x, position.z) + .02f;
                unit.transform.position = position;
                Color color = i < 3 ? new Color(.27f, .115f, .035f) : new Color(.46f, .07f, .018f);
                AntVisual.Create(unit.transform, color, i < 3 ? .72f : .86f);
                squads.Add(unit.transform);
            }
        }

        void Update()
        {
            if (!IsPlaying && Time.realtimeSinceStartup >= autoStartAt) BeginPlay();
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !IsPlaying || IsPaused) return;
            if (keyboard.digit1Key.wasPressedThisFrame) squads.Set(SquadOrder.Gather);
            if (keyboard.digit2Key.wasPressedThisFrame) squads.Set(SquadOrder.Attack);
            if (keyboard.digit3Key.wasPressedThisFrame) squads.Set(SquadOrder.Follow);
            if (keyboard.digit4Key.wasPressedThisFrame) squads.Set(SquadOrder.Defend);
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
            if (Application.platform == RuntimePlatform.WebGLPlayer &&
                Mouse.current != null &&
                Mouse.current.leftButton.isPressed)
                Cursor.lockState = CursorLockMode.Locked;
            ShowToast(GameText.Pick("Follow the golden objective marker", "Следуйте за золотой меткой цели"));
        }

        public void TogglePause()
        {
            if (!IsPlaying) return;
            IsPaused = !IsPaused;
            Time.timeScale = IsPaused ? 0 : 1;
            if (IsPaused) Player.UnlockPointer();
            else if (Application.platform == RuntimePlatform.WebGLPlayer) Cursor.lockState = CursorLockMode.Locked;
        }

        public void ToggleNest(PlayerAnt player, bool fromUnderground)
        {
            if (fromUnderground)
            {
                IsUnderground = false;
                player.Teleport(PlayerSpawn);
                ShowToast(GameText.Pick("Forest floor", "Лесная подстилка"));
            }
            else
            {
                IsUnderground = true;
                player.Teleport(new Vector3(0, -4.82f, -5.1f));
                ShowToast(GameText.Pick("Moonroot nursery", "Ясли Лунного Корня"));
            }
        }

        public void ApplyNestUpgrade()
        {
            if (nestUpgrade) nestUpgrade.SetActive(true);
            if (undergroundUpgrade) undergroundUpgrade.SetActive(true);
        }

        public void OnMissionAdvanced()
        {
            ShowToast(GameText.Pick($"New objective: {Mission.Title}", $"Новая цель: {Mission.Title}"));
            SaveSystem.Save(1, this);
        }

        public void RefreshWorldForMission()
        {
            if (Colony != null && Colony.Level >= 2) ApplyNestUpgrade();
            foreach (Creature creature in creatures)
            {
                if (!creature) continue;
                bool completedEarlier =
                    (creature.Kind == Creature.Species.Beetle && Mission.Step > 2) ||
                    (creature.Kind == Creature.Species.RivalAnt && Mission.Step > 3) ||
                    (creature.Kind == Creature.Species.Spider && Mission.Step > 5);
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
            return Mission.Step switch
            {
                0 => FindNearestResource(Player.transform.position, ResourceKind.Seed)?.transform,
                1 => FindNearestResource(Player.transform.position, ResourceKind.Resin)?.transform,
                2 => FindNearestActiveCreature(Player.transform.position, Creature.Species.Beetle)?.transform,
                3 => FindNearestActiveCreature(Player.transform.position, Creature.Species.RivalAnt)?.transform,
                4 => environment.Find("Moonroot entrance"),
                5 => FindNearestActiveCreature(Player.transform.position, Creature.Species.Spider)?.transform,
                _ => null
            };
        }

        public void ShowToast(string message)
        {
            toast = message;
            toastUntil = Time.unscaledTime + 2.8f;
        }

        public void FlashCrosshair(bool hit) => crosshairFlash = hit ? .18f : .06f;

        void EnsureStyles()
        {
            if (body != null) return;
            Font interfaceFont = Resources.Load<Font>("Fonts/NotoSans-Regular");
            if (interfaceFont) GUI.skin.font = interfaceFont;
            body = new GUIStyle(GUI.skin.label) { font = interfaceFont, fontSize = 17, wordWrap = true };
            body.normal.textColor = new Color(.95f, .94f, .86f);
            small = new GUIStyle(body) { fontSize = 14 };
            small.normal.textColor = new Color(.82f, .85f, .78f);
            heading = new GUIStyle(body) { fontSize = 25, fontStyle = FontStyle.Bold };
            heading.normal.textColor = new Color(.92f, .78f, .35f);
            missionTitle = new GUIStyle(body) { fontSize = 14, fontStyle = FontStyle.Bold };
            missionTitle.normal.textColor = new Color(.58f, .83f, .46f);
            centered = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter, fontSize = 19 };
            prompt = new GUIStyle(centered) { fontSize = 17, fontStyle = FontStyle.Bold };
            prompt.normal.textColor = new Color(1f, .84f, .35f);
            button = new GUIStyle(GUI.skin.button) { font = interfaceFont, fontSize = 18, fontStyle = FontStyle.Bold };
            button.normal.textColor = Color.white;
        }

        void OnGUI()
        {
            if (Player == null || Mission == null || Colony == null) return;
            EnsureStyles();
            float scale = Mathf.Clamp(Screen.height / 900f, .78f, 1.4f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            GUI.Box(new Rect(16, 16, 475, 150), "");
            GUI.Label(new Rect(32, 27, 440, 22), Mission.Title, missionTitle);
            GUI.Label(new Rect(32, 50, 440, 34), Mission.Objective, body);
            GUI.Label(
                new Rect(32, 111, 440, 25),
                GameText.Pick(
                    $"Seed {Colony.Seeds}   Resin {Colony.Resin}   Protein {Colony.Protein}   Nest Lv.{Colony.Level}",
                    $"Семена {Colony.Seeds}   Смола {Colony.Resin}   Белок {Colony.Protein}   Гнездо ур.{Colony.Level}"),
                small);
            GUI.Label(
                new Rect(width - 300, 22, 280, 30),
                GameText.Pick($"Health {Player.Health:0}   Stamina {Player.Stamina:0}", $"Здоровье {Player.Health:0}   Выносливость {Player.Stamina:0}"),
                body);

            if (IsPlaying && !IsPaused)
            {
                Color old = GUI.color;
                GUI.color = crosshairFlash > 0 ? new Color(1f, .35f, .12f) : new Color(1f, .86f, .42f);
                GUI.Label(new Rect(width * .5f - 12, height * .5f - 14, 24, 28), "•", centered);
                GUI.color = old;
                if (!string.IsNullOrEmpty(Player.CurrentPrompt))
                    GUI.Label(new Rect(width * .5f - 260, height - 122, 520, 42), Player.CurrentPrompt, prompt);
                DrawObjectiveMarker(scale);
            }

            GUI.Label(
                new Rect(18, height - 58, width - 36, 42),
                GameText.Pick(
                    "WASD move · mouse camera · Shift sprint · Space vault · E interact · LMB bite · 1 gather · 2 attack · 3 follow · 4 defend · Esc pause",
                    "WASD движение · мышь камера · Shift бег · Space прыжок · E действие · ЛКМ укус · 1 сбор · 2 атака · 3 за мной · 4 защита · Esc пауза"),
                small);

            if (Time.unscaledTime < toastUntil && !string.IsNullOrEmpty(toast))
            {
                GUI.Box(new Rect(width * .5f - 230, 24, 460, 48), "");
                GUI.Label(new Rect(width * .5f - 220, 27, 440, 40), toast, centered);
            }

            if (!IsPlaying) DrawStartOverlay(width, height);
            else if (IsPaused) DrawPauseOverlay(width, height);
            else if (Mission.Step >= 6) DrawCompletionOverlay(width, height);
        }

        void DrawObjectiveMarker(float scale)
        {
            Transform target = ObjectiveTarget();
            Camera camera = Camera.main;
            if (!target || !camera) return;
            Vector3 screen = camera.WorldToScreenPoint(target.position + Vector3.up * .9f);
            if (screen.z <= 0) return;
            float x = screen.x / scale;
            float y = (Screen.height - screen.y) / scale;
            float distance = Vector3.Distance(Player.transform.position, target.position);
            GUI.Label(new Rect(x - 70, y - 24, 140, 46), $"◆  {distance:0} m", prompt);
        }

        void DrawStartOverlay(float width, float height)
        {
            float panelWidth = Mathf.Min(650, width - 40);
            Rect panel = new(width * .5f - panelWidth * .5f, height * .5f - 190, panelWidth, 380);
            GUI.Box(panel, "");
            GUI.Label(new Rect(panel.x + 30, panel.y + 28, panel.width - 60, 45), "CANOPY KIN: MOONROOT", heading);
            GUI.Label(
                new Rect(panel.x + 30, panel.y + 82, panel.width - 60, 112),
                GameText.Pick(
                    "The first rain has broken the forest canopy. Guide a young Moonroot scout, feed the nursery, command the colony, and survive what followed the storm.",
                    "Первый дождь прорвал лесной полог. Проведите молодого разведчика Лунного Корня, накормите ясли, возглавьте колонию и переживите угрозу после бури."),
                body);
            GUI.Label(
                new Rect(panel.x + 30, panel.y + 199, panel.width - 60, 55),
                GameText.Pick("Third-person exploration · resource gathering · squad combat · nest growth", "Исследование от третьего лица · сбор ресурсов · командный бой · развитие гнезда"),
                small);
            if (GUI.Button(
                    new Rect(panel.x + panel.width * .5f - 145, panel.y + 274, 290, 58),
                    GameText.Pick("ENTER THE FOREST", "ВЫЙТИ В ЛЕС"),
                    button))
                BeginPlay();
            GUI.Label(
                new Rect(panel.x + 30, panel.y + 340, panel.width - 60, 25),
                GameText.Pick("Click the button or press Enter. Click the game once to capture the mouse.", "Нажмите кнопку или Enter. Затем кликните по игре, чтобы захватить мышь."),
                new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
        }

        void DrawPauseOverlay(float width, float height)
        {
            Rect panel = new(width * .5f - 220, height * .5f - 180, 440, 360);
            GUI.Box(panel, "");
            GUI.Label(new Rect(panel.x + 30, panel.y + 25, panel.width - 60, 42), GameText.Pick("PAUSED", "ПАУЗА"), heading);
            if (GUI.Button(new Rect(panel.x + 70, panel.y + 90, 300, 50), GameText.Pick("Resume", "Продолжить"), button)) TogglePause();
            if (GUI.Button(new Rect(panel.x + 70, panel.y + 152, 300, 50), GameText.Pick("Save slot 1", "Сохранить в слот 1"), button))
                ShowToast(SaveSystem.Save(1, this) ? GameText.Pick("Game saved", "Игра сохранена") : GameText.Pick("Save failed", "Ошибка сохранения"));
            if (GUI.Button(new Rect(panel.x + 70, panel.y + 214, 300, 50), GameText.Pick("Load slot 1", "Загрузить слот 1"), button))
                ShowToast(SaveSystem.Load(1, this) ? GameText.Pick("Save loaded", "Сохранение загружено") : GameText.Pick("No valid save", "Нет исправного сохранения"));
            GUI.Label(new Rect(panel.x + 32, panel.y + 287, panel.width - 64, 48), GameText.Pick("Esc closes this menu. Browser fullscreen may require a second Esc press.", "Esc закрывает меню. Для выхода из полноэкранного режима браузера может понадобиться второе нажатие."), small);
        }

        void DrawCompletionOverlay(float width, float height)
        {
            Rect panel = new(width * .5f - 300, height * .5f - 100, 600, 200);
            GUI.Box(panel, "");
            GUI.Label(new Rect(panel.x + 30, panel.y + 25, panel.width - 60, 42), GameText.Pick("MOONROOT SURVIVED", "ЛУННЫЙ КОРЕНЬ ВЫСТОЯЛ"), heading);
            GUI.Label(new Rect(panel.x + 30, panel.y + 78, panel.width - 60, 85), GameText.Pick("The nursery is safe, but a rival colony has marked this territory. Vertical slice complete.", "Ясли спасены, но соседняя колония уже пометила эту территорию. Вертикальный срез завершён."), centered);
        }

        void OnDestroy()
        {
            Time.timeScale = 1;
            if (Instance == this) Instance = null;
        }
    }
}
