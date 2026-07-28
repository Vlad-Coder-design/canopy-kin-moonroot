using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        public Vector3 NestPosition => new(NestPoint.x, GroundHeight(NestPoint.x, NestPoint.z), NestPoint.z);
        public Vector3 SurfacePlayerSpawn => new(0, GroundHeight(0, -4.75f) + .05f, -4.75f);
        public Vector3 UndergroundPlayerSpawn => UndergroundCenter + new Vector3(0, .28f, .9f);
        public Vector3 PlayerRespawn => IsUnderground ? UndergroundPlayerSpawn : SurfacePlayerSpawn;

        readonly List<ResourceNode> resources = new();
        readonly List<Creature> creatures = new();
        SquadController squads;
        Transform environment;
        Transform underground;
        Transform rivalColony;
        GameObject nestUpgrade;
        GameObject undergroundUpgrade;
        GameObject largeThreat;
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
            VisualFactory.Terrain("Layered loam terrain", environment, 82f, GameSettings.Quality == 0 ? 64 : 82, GroundHeight, new Color(.82f, .76f, .65f));
            BuildDistantEnclosure();
            BuildNest();
            BuildLandmarks();
            BuildVegetation();
            BuildResources();
            BuildMissionLocations();
            BuildCreatures();
            BuildPlayerAndSquad();
            RefreshWorldForMission();
            timer.Stop();
            int renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;
            Debug.Log($"MOONROOT_SLICE_READY buildMs={timer.ElapsedMilliseconds} renderers={renderers} quality={GameSettings.Quality}");
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

            Light sun = new GameObject("Canopy-break sunlight").AddComponent<Light>();
            sun.transform.SetParent(transform);
            sun.type = LightType.Directional;
            sun.color = new Color(1f, .9f, .71f);
            sun.intensity = 1.24f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .78f;
            sun.shadowBias = .035f;
            sun.shadowNormalBias = .32f;
            sun.transform.rotation = Quaternion.Euler(42f, -28f, 0);

            Light skyFill = new GameObject("Cool canopy fill").AddComponent<Light>();
            skyFill.transform.SetParent(transform);
            skyFill.type = LightType.Directional;
            skyFill.color = new Color(.36f, .49f, .55f);
            skyFill.intensity = .34f;
            skyFill.shadows = LightShadows.None;
            skyFill.transform.rotation = Quaternion.Euler(62f, 142f, 18f);
            GameSettings.Apply();
        }

        void BuildDistantEnclosure()
        {
            var enclosure = new GameObject("Forest horizon enclosure").transform;
            enclosure.SetParent(environment, false);
            for (int i = 0; i < 20; i++)
            {
                float angle = i / 20f * Mathf.PI * 2f;
                float radius = 36f + Mathf.Sin(i * 3.2f) * 2.4f;
                Vector3 basePoint = new(Mathf.Cos(angle) * radius, GroundHeight(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius) - .8f, Mathf.Sin(angle) * radius);
                Vector3 top = basePoint + new Vector3(Mathf.Sin(angle) * 1.8f, Random.Range(11f, 18f), Mathf.Cos(angle) * 1.8f);
                VisualFactory.TexturedRoot(
                    "Distant bark pillar",
                    enclosure,
                    new[] { basePoint, Vector3.Lerp(basePoint, top, .45f) + Vector3.up * .7f, top },
                    new[] { Random.Range(2.2f, 3.5f), Random.Range(1.7f, 2.5f), Random.Range(1.2f, 1.8f) },
                    false);
            }
            for (int i = 0; i < 14; i++)
            {
                float angle = i / 14f * Mathf.PI * 2f + .17f;
                Vector3 position = new(Mathf.Cos(angle) * 31f, GroundHeight(Mathf.Cos(angle) * 31f, Mathf.Sin(angle) * 31f), Mathf.Sin(angle) * 31f);
                VisualFactory.GrassTuft(enclosure, position, Random.Range(4.6f, 7.4f), Color.Lerp(new Color(.08f, .22f, .055f), new Color(.22f, .42f, .09f), Random.value), i);
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

            Light amber = new GameObject("Amber chamber bounce").AddComponent<Light>();
            amber.transform.SetParent(underground, false);
            amber.transform.localPosition = new Vector3(-1.2f, 2.45f, -.85f);
            amber.type = LightType.Point;
            amber.range = 14f;
            amber.intensity = 3.4f;
            amber.color = new Color(.9f, .57f, .36f);
            amber.shadows = LightShadows.None;

            Light blue = new GameObject("Cool tunnel fill").AddComponent<Light>();
            blue.transform.SetParent(underground, false);
            blue.transform.localPosition = new Vector3(0, 1.1f, 3.3f);
            blue.type = LightType.Point;
            blue.range = 9f;
            blue.intensity = 2.3f;
            blue.color = new Color(.36f, .66f, .58f);

            Light nurseryFill = new GameObject("Nursery soft fill").AddComponent<Light>();
            nurseryFill.transform.SetParent(underground, false);
            nurseryFill.transform.localPosition = new Vector3(2f, 1.45f, -1.2f);
            nurseryFill.type = LightType.Point;
            nurseryFill.range = 8f;
            nurseryFill.intensity = 1.55f;
            nurseryFill.color = new Color(.72f, .38f, .2f);
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
            AntVisual.Create(queen, new Color(.23f, .045f, .012f), 1.55f, AntCaste.HeavySoldier).transform.localPosition = new Vector3(0, .28f, -.35f);
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
            VisualFactory.TexturedRoot(
                "Fallen storm branch",
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
            VisualFactory.TexturedRoot(
                "Broken branch fork",
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

            VisualFactory.TexturedRoot(
                "Root ridge",
                environment,
                new[] { At(-20f, -1f, .32f), At(-16f, 5f, .72f), At(-11f, 13f, 1.1f), At(-7f, 19f, .56f) },
                new[] { 1.25f, 1.05f, .82f, .42f },
                true);
            VisualFactory.TexturedRoot(
                "Climbable root bridge",
                environment,
                new[] { At(1.5f, 17f, .4f), At(6.2f, 18.4f, 1.1f), At(12.5f, 20.4f, 1.55f), At(17f, 21.6f, .65f) },
                new[] { .82f, .72f, .58f, .3f },
                true);
        }

        Vector3 At(float x, float z, float above = 0) => new(x, GroundHeight(x, z) + above, z);

        void BuildVegetation()
        {
            int grassCount = GameSettings.Quality switch { 0 => 105, 1 => 145, _ => 190 };
            for (int i = 0; i < grassCount; i++)
            {
                Vector2 circle = Random.insideUnitCircle * 33f;
                float x = circle.x;
                float z = circle.y + 5f;
                if (KeepClear(x, z)) continue;
                float height = Random.Range(1.05f, 3.45f);
                Color grass = Color.Lerp(new Color(.11f, .28f, .055f), new Color(.38f, .56f, .15f), Random.value);
                GameObject tuft = VisualFactory.GrassTuft(environment, At(x, z), height, grass, i);
                tuft.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360f), Random.Range(-4f, 4f));
            }

            for (int i = 0; i < 34; i++)
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

            for (int i = 0; i < 70; i++)
            {
                Vector2 p = Random.insideUnitCircle * 30f;
                float z = p.y + 5f;
                if (KeepClear(p.x, z)) continue;
                VisualFactory.Stone("Soil clod and pebble", environment, At(p.x, z, .06f),
                    new Vector3(Random.Range(.16f, .46f), Random.Range(.12f, .31f), Random.Range(.18f, .55f)),
                    40 + i, false, i % 5 == 0);
            }
        }

        bool KeepClear(float x, float z)
        {
            bool trail = Mathf.Abs(x - Mathf.Sin(z * .12f) * 1.4f) < 1.2f && z > -6f && z < 22f;
            bool nest = Vector2.Distance(new Vector2(x, z), new Vector2(0, -7)) < 4.6f;
            bool pond = Vector2.Distance(new Vector2(x, z), new Vector2(-13.5f, 13.5f)) < 5.2f;
            return trail || nest || pond;
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
            SpawnCreature(Creature.Species.Beetle, At(7.3f, 14.2f, .035f), 3);
            SpawnCreature(Creature.Species.Spider, At(1.2f, -16.5f, .035f), 8);
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
                SpawnCreature(Creature.Species.RivalAnt, position, 7);
            }
        }

        void BuildPlayerAndSquad()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = GameSettings.FieldOfView;
            camera.nearClipPlane = .025f;
            camera.farClipPlane = 125f;
            camera.allowHDR = false;
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
            if (Application.platform == RuntimePlatform.WebGLPlayer && Mouse.current != null && Mouse.current.leftButton.isPressed)
                Cursor.lockState = CursorLockMode.Locked;
            ShowToast(GameText.Pick("Wake in the nursery and follow the blue tunnel light", "Проснитесь в яслях и следуйте к голубому свету тоннеля"));
        }

        public void TogglePause()
        {
            if (!IsPlaying || IsCinematic) return;
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
                player.Teleport(SurfacePlayerSpawn);
                squads.Teleport(SurfacePlayerSpawn + Vector3.back * .7f);
                ShowToast(GameText.Pick("Forest floor — the rain has stopped", "Лесная подстилка — дождь закончился"));
            }
            else
            {
                IsUnderground = true;
                player.Teleport(UndergroundPlayerSpawn);
                squads.Teleport(UndergroundPlayerSpawn + Vector3.forward * .8f);
                ShowToast(GameText.Pick("Moonroot underground colony", "Подземная колония Лунного Корня"));
            }
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
            SaveSystem.Save(1, this);
            RefreshWorldForMission();
        }

        public void RefreshWorldForMission()
        {
            if (Colony != null && Colony.Level >= 2) ApplyNestUpgrade();
            if (Mission.Step == 7) SpawnRivalWave();
            foreach (Creature creature in creatures)
            {
                if (!creature) continue;
                bool completedEarlier =
                    (creature.Kind == Creature.Species.Beetle && Mission.Step > 3) ||
                    (creature.Kind == Creature.Species.RivalAnt && Mission.Step > 7) ||
                    (creature.Kind == Creature.Species.Spider && Mission.Step > 8);
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
                if (Mission.Step == 0) return environment.Find("Tunnel to forest floor");
                if (Mission.Step == 6) return underground.Find("Nursery growth site");
            }
            return Mission.Step switch
            {
                1 => environment.Find("Moonroot veteran scout"),
                2 => FindNearestResource(Player.transform.position)?.transform,
                3 => FindNearestActiveCreature(Player.transform.position, Creature.Species.Beetle)?.transform,
                4 => environment.Find("Rainwatch Ridge capture point"),
                5 => environment.Find("Moonroot surface entrance"),
                7 => FindNearestActiveCreature(Player.transform.position, Creature.Species.RivalAnt)?.transform,
                8 => FindNearestActiveCreature(Player.transform.position, Creature.Species.Spider)?.transform,
                9 => environment.Find("Root overlook objective"),
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
            if (Mission.Step == 4 || Colony.IsConstructing)
            {
                float progress = Mission.Step == 4 ? Mission.Progress : Colony.ConstructionProgress;
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
