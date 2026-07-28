using System.Collections.Generic;
using UnityEngine;

namespace CanopyKin
{
    public enum AntCaste { Scout, Worker, LightSoldier, HeavySoldier, Rival }

    /// <summary>
    /// Drives the original Blender-authored skinned ant and its anatomical armature.
    /// A cached organic-mesh fallback remains only for import failure recovery.
    /// </summary>
    public sealed class AntVisual : MonoBehaviour
    {
        sealed class LegRig
        {
            public Transform Hip;
            public Transform Knee;
            public Transform Ankle;
            public Quaternion HipRest;
            public Quaternion KneeRest;
            public Quaternion AnkleRest;
            public float Phase;
            public float Side;
            public float Pair;
        }

        readonly List<LegRig> legs = new();
        readonly Transform[] antennae = new Transform[2];
        readonly Quaternion[] antennaRest = new Quaternion[2];
        Transform leftMandible;
        Transform rightMandible;
        Transform abdomen;
        Transform thorax;
        Transform headBone;
        Quaternion leftMandibleRest;
        Quaternion rightMandibleRest;
        Quaternion abdomenRest;
        Quaternion headRest;
        Vector3 thoraxRestPosition;
        Vector3 previousPosition;
        Quaternion previousParentRotation;
        Quaternion slopeRotation = Quaternion.identity;
        float locomotion;
        float stride;
        float attack;
        float stagger;
        float death;
        float carrying;
        float climb;
        float turnLean;
        bool dead;
        bool built;

        public AntCaste Caste { get; private set; }

        public static AntVisual Create(
            Transform parent,
            Color shell,
            float scale = 1f,
            AntCaste caste = AntCaste.Worker)
        {
            var visualRoot = new GameObject($"Detailed {caste} ant").transform;
            visualRoot.SetParent(parent, false);
            visualRoot.localScale = Vector3.one * scale;
            var visual = visualRoot.gameObject.AddComponent<AntVisual>();
            visual.Caste = caste;
            visual.Build(shell);
            return visual;
        }

        void Build(Color shell)
        {
            if (built) return;
            built = true;
            if (TryBuildProductionModel(shell))
            {
                previousPosition = transform.position;
                previousParentRotation = transform.parent ? transform.parent.rotation : transform.rotation;
                return;
            }

            Color joint = Color.Lerp(shell, new Color(.025f, .014f, .008f), .58f);
            Color armor = Color.Lerp(shell, new Color(.65f, .24f, .045f), Caste == AntCaste.Rival ? .25f : .08f);
            Color textureTint = Color.Lerp(new Color(.92f, .84f, .76f), shell, .28f);
            Material shellMaterial = VisualFactory.PbrMaterial(
                "Exoskeleton",
                textureTint,
                .62f,
                .72f,
                new Vector2(3.2f, 3.2f));
            float headSize = Caste switch
            {
                AntCaste.LightSoldier => 1.16f,
                AntCaste.HeavySoldier => 1.34f,
                AntCaste.Rival => 1.15f,
                _ => 1f
            };
            float abdomenSize = Caste == AntCaste.Worker ? 1.08f : 1f;

            GameObject abdomenObject = VisualFactory.OrganicPart(
                "Tapered segmented abdomen",
                transform,
                OrganicMeshFactory.BodyShape.Abdomen,
                new Vector3(0, .36f, -.42f),
                new Vector3(.62f, .54f, .82f * abdomenSize),
                shell,
                .58f);
            abdomenObject.GetComponent<Renderer>().sharedMaterial = shellMaterial;
            abdomen = abdomenObject.transform;
            abdomen.localRotation = Quaternion.Euler(-4f, 0, 0);

            GameObject thoraxObject = VisualFactory.OrganicPart(
                "Armoured thorax",
                transform,
                OrganicMeshFactory.BodyShape.Thorax,
                new Vector3(0, .36f, .08f),
                new Vector3(.51f, .54f, .63f),
                armor,
                .5f);
            thoraxObject.GetComponent<Renderer>().sharedMaterial = shellMaterial;
            thorax = thoraxObject.transform;

            GameObject headObject = VisualFactory.OrganicPart(
                "Anatomical head",
                transform,
                OrganicMeshFactory.BodyShape.Head,
                new Vector3(0, .37f, .48f),
                new Vector3(.58f * headSize, .48f * headSize, .55f * headSize),
                Color.Lerp(shell, joint, .18f),
                .48f);
            headObject.GetComponent<Renderer>().sharedMaterial = shellMaterial;
            Transform head = headObject.transform;
            headBone = head;

            BuildNeckAndWaist(joint);
            BuildEyes(head, headSize);
            BuildMandibles(head, joint, headSize);
            BuildAntennae(head, joint, headSize);
            BuildLegs(joint);
            AddArmorDetail(armor);
            CacheRestPose();
            previousPosition = transform.position;
            previousParentRotation = transform.parent ? transform.parent.rotation : transform.rotation;
        }

        bool TryBuildProductionModel(Color shell)
        {
            GameObject prefab = Resources.Load<GameObject>("Models/Ant/CanopyKinProductionAnt");
            if (!prefab) return false;

            GameObject model = Instantiate(prefab, transform, false);
            model.name = $"Production {Caste} ant rig";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * .72f;
            foreach (Animator animator in model.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;

            abdomen = Find(model.transform, "Abdomen");
            thorax = Find(model.transform, "Thorax");
            headBone = Find(model.transform, "Head");
            leftMandible = Find(model.transform, "Mandible_L");
            rightMandible = Find(model.transform, "Mandible_R");
            antennae[0] = Find(model.transform, "Antenna_L_1");
            antennae[1] = Find(model.transform, "Antenna_R_1");

            string[] pairs = { "Front", "Middle", "Rear" };
            for (int pair = 0; pair < pairs.Length; pair++)
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                string sideName = sideIndex == 0 ? "L" : "R";
                float side = sideIndex == 0 ? -1f : 1f;
                Transform hip = Find(model.transform, $"Leg_{sideName}_{pairs[pair]}_Coxa");
                Transform knee = Find(model.transform, $"Leg_{sideName}_{pairs[pair]}_Femur");
                Transform ankle = Find(model.transform, $"Leg_{sideName}_{pairs[pair]}_Tibia");
                if (!hip || !knee || !ankle) continue;
                legs.Add(new LegRig
                {
                    Hip = hip,
                    Knee = knee,
                    Ankle = ankle,
                    HipRest = hip.localRotation,
                    KneeRest = knee.localRotation,
                    AnkleRest = ankle.localRotation,
                    Side = side,
                    Pair = pair,
                    Phase = (pair + sideIndex) % 2 == 0 ? 0 : Mathf.PI
                });
            }

            Color textureTint = Color.Lerp(new Color(.78f, .68f, .58f), shell, .48f);
            Material shellMaterial = VisualFactory.PbrMaterial(
                "Exoskeleton",
                textureTint,
                .68f,
                1.05f,
                new Vector2(4.5f, 4.5f));
            Material jointMaterial = VisualFactory.Material(Color.Lerp(shell, Color.black, .72f), .42f);
            Material eyeMaterial = VisualFactory.Material(new Color(.006f, .018f, .012f), .88f);
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string materialName = materials[i] ? materials[i].name : string.Empty;
                    materials[i] = materialName.Contains("CompoundEye")
                        ? eyeMaterial
                        : materialName.Contains("AntJoint") ? jointMaterial : shellMaterial;
                }
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            float headSize = Caste switch
            {
                AntCaste.LightSoldier => 1.10f,
                AntCaste.HeavySoldier => 1.22f,
                AntCaste.Rival => 1.08f,
                _ => 1f
            };
            if (headBone) headBone.localScale *= headSize;
            if (abdomen && Caste == AntCaste.Worker)
                abdomen.localScale = Vector3.Scale(abdomen.localScale, new Vector3(1.05f, 1.08f, 1.05f));

            CacheRestPose();
            Debug.Log($"MOONROOT_PRODUCTION_ANT_READY caste={Caste} bones={legs.Count * 3 + 9}");
            return abdomen && thorax && headBone && legs.Count == 6;
        }

        void CacheRestPose()
        {
            if (leftMandible) leftMandibleRest = leftMandible.localRotation;
            if (rightMandible) rightMandibleRest = rightMandible.localRotation;
            if (abdomen) abdomenRest = abdomen.localRotation;
            if (headBone) headRest = headBone.localRotation;
            if (thorax) thoraxRestPosition = thorax.localPosition;
            for (int i = 0; i < antennae.Length; i++)
                if (antennae[i]) antennaRest[i] = antennae[i].localRotation;
        }

        static Transform Find(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        void BuildNeckAndWaist(Color joint)
        {
            VisualFactory.Segment(
                "Flexible neck",
                transform,
                new Vector3(0, .34f, .28f),
                new Vector3(0, .35f, .38f),
                .065f,
                joint,
                false,
                .36f);
            VisualFactory.Segment(
                "Petiole waist",
                transform,
                new Vector3(0, .34f, -.13f),
                new Vector3(0, .33f, -.25f),
                .07f,
                joint,
                false,
                .38f);
            VisualFactory.OrganicPart(
                "Waist node",
                transform,
                OrganicMeshFactory.BodyShape.Thorax,
                new Vector3(0, .35f, -.2f),
                new Vector3(.18f, .19f, .2f),
                joint,
                .4f);
        }

        void BuildEyes(Transform head, float headSize)
        {
            Color eye = Caste == AntCaste.Rival
                ? new Color(.055f, .012f, .008f)
                : new Color(.018f, .035f, .022f);
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                Transform eyeRoot = new GameObject(side < 0 ? "Left compound eye" : "Right compound eye").transform;
                eyeRoot.SetParent(head, false);
                eyeRoot.localPosition = new Vector3(side * .39f, .16f, .3f);
                eyeRoot.localRotation = Quaternion.Euler(0, side * 24f, side * -7f);
                VisualFactory.OrganicPart(
                    "Faceted eye plate",
                    eyeRoot,
                    OrganicMeshFactory.BodyShape.Eye,
                    Vector3.zero,
                    new Vector3(.22f, .28f, .18f) * headSize,
                    eye,
                    .74f);
                for (int facet = 0; facet < 4; facet++)
                {
                    float fy = (facet / 2 - .5f) * .075f;
                    float fz = (facet % 2 - .5f) * .065f;
                    VisualFactory.OrganicPart(
                        "Eye facet",
                        eyeRoot,
                        OrganicMeshFactory.BodyShape.Eye,
                        new Vector3(side * .11f, fy, fz),
                        Vector3.one * .045f,
                        Color.Lerp(eye, new Color(.16f, .2f, .12f), .3f),
                        .86f);
                }
            }
        }

        void BuildMandibles(Transform head, Color color, float size)
        {
            leftMandible = BuildMandible(head, true, color, size);
            rightMandible = BuildMandible(head, false, color, size);
        }

        static Transform BuildMandible(Transform head, bool left, Color color, float size)
        {
            var root = new GameObject(left ? "Left hooked mandible" : "Right hooked mandible").transform;
            root.SetParent(head, false);
            root.localPosition = new Vector3(left ? -.12f : .12f, -.08f, .42f);
            root.localScale = Vector3.one * size;
            VisualFactory.MeshObject(
                "Serrated jaw",
                root,
                OrganicMeshFactory.Mandible(left),
                Vector3.zero,
                Vector3.one,
                VisualFactory.Material(color, .46f));
            return root;
        }

        void BuildAntennae(Transform head, Color color, float size)
        {
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                Transform root = new GameObject(side < 0 ? "Left responsive antenna" : "Right responsive antenna").transform;
                root.SetParent(head, false);
                root.localPosition = new Vector3(side * .18f, .24f, .4f);
                root.localScale = Vector3.one * size;
                VisualFactory.Segment(
                    "Scape",
                    root,
                    Vector3.zero,
                    new Vector3(side * .12f, .18f, .25f),
                    .025f,
                    color,
                    false,
                    .38f);
                Transform tip = new GameObject("Antenna elbow").transform;
                tip.SetParent(root, false);
                tip.localPosition = new Vector3(side * .12f, .18f, .25f);
                VisualFactory.Segment(
                    "Flexible flagellum",
                    tip,
                    Vector3.zero,
                    new Vector3(side * .18f, .06f, .34f),
                    .019f,
                    color,
                    false,
                    .36f);
                antennae[i] = root;
                antennaRest[i] = root.localRotation;
            }
        }

        void BuildLegs(Color color)
        {
            float[] z = { .26f, .03f, -.18f };
            for (int pair = 0; pair < 3; pair++)
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                float forward = (1 - pair) * .18f;
                var rig = new LegRig
                {
                    Side = side,
                    Pair = pair,
                    Phase = (pair + sideIndex) % 2 == 0 ? 0 : Mathf.PI
                };

                rig.Hip = new GameObject($"Leg {pair + 1} {(side < 0 ? "L" : "R")} coxa").transform;
                rig.Hip.SetParent(transform, false);
                rig.Hip.localPosition = new Vector3(side * .19f, .34f, z[pair]);
                Vector3 coxaEnd = new(side * .17f, -.035f, forward * .3f);
                VisualFactory.Segment("Coxa", rig.Hip, Vector3.zero, coxaEnd, .058f, color, false, .36f);

                rig.Knee = new GameObject("Femur joint").transform;
                rig.Knee.SetParent(rig.Hip, false);
                rig.Knee.localPosition = coxaEnd;
                Vector3 femurEnd = new(side * .28f, -.17f, forward);
                VisualFactory.Segment("Sculpted femur", rig.Knee, Vector3.zero, femurEnd, .049f, color, false, .34f);

                rig.Ankle = new GameObject("Tibia joint").transform;
                rig.Ankle.SetParent(rig.Knee, false);
                rig.Ankle.localPosition = femurEnd;
                Vector3 tibiaEnd = new(side * .27f, -.17f, forward * .55f);
                VisualFactory.Segment("Tapered tibia", rig.Ankle, Vector3.zero, tibiaEnd, .036f, color, false, .31f);
                VisualFactory.Segment(
                    "Hooked tarsus",
                    rig.Ankle,
                    tibiaEnd,
                    tibiaEnd + new Vector3(side * .1f, -.018f, .055f),
                    .022f,
                    color,
                    false,
                    .28f);

                rig.HipRest = Quaternion.Euler(0, (pair - 1) * side * 8f, 0);
                rig.KneeRest = Quaternion.identity;
                rig.AnkleRest = Quaternion.identity;
                legs.Add(rig);
            }
        }

        void AddArmorDetail(Color armor)
        {
            for (int i = 0; i < 4; i++)
            {
                float z = -.22f - i * .12f;
                VisualFactory.Segment(
                    "Abdominal armor seam",
                    abdomen,
                    new Vector3(-.28f + i * .018f, .16f, z),
                    new Vector3(.28f - i * .018f, .16f, z),
                    .011f,
                    Color.Lerp(armor, Color.black, .42f),
                    false,
                    .24f);
            }
        }

        public void SetCarrying(bool value) => carrying = value ? 1f : 0f;
        public void PlayAttack() => attack = 1f;
        public void PlayStagger() => stagger = 1f;
        public void PlayDeath()
        {
            dead = true;
            death = Mathf.Max(death, .05f);
        }

        void Update()
        {
            if (!built) return;
            float dt = Mathf.Max(Time.deltaTime, .0001f);
            Vector3 displacement = transform.position - previousPosition;
            float speed = new Vector2(displacement.x, displacement.z).magnitude / dt;
            float verticalSpeed = Mathf.Abs(displacement.y) / dt;
            previousPosition = transform.position;
            Quaternion parentRotation = transform.parent ? transform.parent.rotation : transform.rotation;
            float yawRate = Vector3.SignedAngle(
                previousParentRotation * Vector3.forward,
                parentRotation * Vector3.forward,
                Vector3.up) / dt;
            previousParentRotation = parentRotation;
            locomotion = Mathf.MoveTowards(locomotion, Mathf.InverseLerp(.05f, 3.5f, speed), dt * 6f);
            climb = Mathf.MoveTowards(climb, Mathf.InverseLerp(.15f, 1.25f, verticalSpeed), dt * 4f);
            turnLean = Mathf.MoveTowards(turnLean, Mathf.Clamp(yawRate / 220f, -1f, 1f), dt * 5f);
            stride += dt * Mathf.Lerp(3.2f, 12.5f, locomotion);
            attack = Mathf.MoveTowards(attack, 0, dt * 3.8f);
            stagger = Mathf.MoveTowards(stagger, 0, dt * 3.2f);
            death = Mathf.MoveTowards(death, dead ? 1f : 0f, dt * 1.2f);

            for (int i = 0; i < legs.Count; i++)
            {
                LegRig leg = legs[i];
                float cycle = Mathf.Sin(stride + leg.Phase);
                float lift = Mathf.Max(0, cycle) * locomotion;
                float sweep = cycle * Mathf.Lerp(3f, 26f, locomotion);
                float climbReach = climb * (leg.Pair == 0 ? 24f : leg.Pair == 2 ? -9f : 6f);
                leg.Hip.localRotation = leg.HipRest * Quaternion.Euler(-lift * 18f + climbReach, sweep * leg.Side, turnLean * leg.Side * 8f);
                leg.Knee.localRotation = leg.KneeRest * Quaternion.Euler(lift * 32f - Mathf.Abs(cycle) * 5f - climbReach * .42f, 0, -sweep * .14f);
                leg.Ankle.localRotation = leg.AnkleRest * Quaternion.Euler(-lift * 24f + Mathf.Abs(cycle) * 7f + climbReach * .3f, 0, sweep * .08f);
            }

            float mandibleClose = Mathf.Sin(attack * Mathf.PI) * 34f;
            if (leftMandible) leftMandible.localRotation = leftMandibleRest * Quaternion.Euler(0, 0, mandibleClose);
            if (rightMandible) rightMandible.localRotation = rightMandibleRest * Quaternion.Euler(0, 0, -mandibleClose);

            for (int i = 0; i < antennae.Length; i++)
            {
                if (!antennae[i]) continue;
                float side = i == 0 ? -1f : 1f;
                float search = Mathf.Sin(Time.time * 2.7f + i * 1.8f) * 9f;
                float response = locomotion * Mathf.Sin(stride * .5f + i) * 7f;
                antennae[i].localRotation = antennaRest[i] * Quaternion.Euler(search * .5f, side * (search + response), -locomotion * 7f);
            }

            float bob = Mathf.Abs(Mathf.Sin(stride)) * .018f * locomotion;
            transform.localPosition = new Vector3(0, bob + carrying * .025f, 0);
            transform.localRotation = slopeRotation *
                Quaternion.Euler(stagger * Mathf.Sin(Time.time * 34f) * 8f, 0, -death * 72f);
            if (abdomen)
                abdomen.localRotation = abdomenRest *
                    Quaternion.Euler(-carrying * 10f + locomotion * Mathf.Sin(stride * .5f) * 2f, 0, -turnLean * 5f);
            if (headBone)
                headBone.localRotation = headRest * Quaternion.Euler(climb * -8f, turnLean * 8f, 0);
            if (thorax)
                thorax.localPosition = thoraxRestPosition + Vector3.up * bob * .35f;
        }

        void LateUpdate()
        {
            Vector3 origin = transform.parent ? transform.parent.position + Vector3.up * .75f : transform.position + Vector3.up * .75f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1.8f, ~0, QueryTriggerInteraction.Ignore))
            {
                Quaternion target = Quaternion.FromToRotation(Vector3.up, hit.normal);
                slopeRotation = Quaternion.Slerp(slopeRotation, target, Time.deltaTime * 5.5f);
            }
        }
    }

    public static class CreatureVisuals
    {
        public static void BuildBeetle(Transform parent)
        {
            Color shell = new(.045f, .11f, .075f);
            Color wing = new(.12f, .27f, .15f);
            VisualFactory.OrganicPart("Ridged beetle abdomen", parent, OrganicMeshFactory.BodyShape.BeetleShell, new Vector3(0, .48f, -.18f), new Vector3(1.15f, 1.05f, 1.3f), shell, .68f);
            Transform wings = new GameObject("Split wing cases").transform;
            wings.SetParent(parent, false);
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                GameObject wingCase = VisualFactory.OrganicPart(
                    side < 0 ? "Left textured elytron" : "Right textured elytron",
                    wings,
                    OrganicMeshFactory.BodyShape.BeetleShell,
                    new Vector3(side * .22f, .63f, -.22f),
                    new Vector3(.54f, .3f, 1.08f),
                    Color.Lerp(wing, Color.black, sideIndex * .09f),
                    .77f);
                wingCase.transform.localRotation = Quaternion.Euler(-4f, side * 3f, side * -4f);
            }
            VisualFactory.OrganicPart("Shielded beetle head", parent, OrganicMeshFactory.BodyShape.Head, new Vector3(0, .4f, .72f), new Vector3(.82f, .66f, .72f), shell, .56f);
            BuildCreatureLegs(parent, 3, .34f, .98f, new Color(.025f, .05f, .032f), .055f);
            VisualFactory.Segment("Left feeler", parent, new Vector3(-.18f, .5f, .92f), new Vector3(-.52f, .54f, 1.35f), .026f, shell);
            VisualFactory.Segment("Right feeler", parent, new Vector3(.18f, .5f, .92f), new Vector3(.52f, .54f, 1.35f), .026f, shell);
        }

        public static SpiderVisual BuildSpider(Transform parent)
        {
            SpiderVisual production = SpiderVisual.Create(parent);
            if (production) return production;

            Color body = new(.085f, .027f, .014f);
            VisualFactory.OrganicPart("Hair-textured spider abdomen", parent, OrganicMeshFactory.BodyShape.SpiderBody, new Vector3(0, .72f, -.32f), new Vector3(1.42f, 1.25f, 1.52f), body, .28f);
            VisualFactory.OrganicPart("Spider cephalothorax", parent, OrganicMeshFactory.BodyShape.Thorax, new Vector3(0, .5f, .58f), new Vector3(.9f, .78f, .82f), Color.Lerp(body, Color.black, .2f), .3f);
            for (int eye = 0; eye < 6; eye++)
            {
                float side = (eye % 3 - 1) * .16f;
                float row = eye / 3;
                VisualFactory.OrganicPart(
                    "Reflective spider eye",
                    parent,
                    OrganicMeshFactory.BodyShape.Eye,
                    new Vector3(side, .67f + row * .09f, .98f - row * .04f),
                    Vector3.one * (row == 0 ? .105f : .08f),
                    new Color(.035f, .065f, .045f),
                    .9f);
            }
            BuildCreatureLegs(parent, 4, .4f, 1.55f, new Color(.052f, .014f, .009f), .065f);
            VisualFactory.MeshObject("Left fang", parent, OrganicMeshFactory.Mandible(true), new Vector3(-.12f, .34f, .92f), Vector3.one * 1.25f, VisualFactory.Material(body, .32f));
            VisualFactory.MeshObject("Right fang", parent, OrganicMeshFactory.Mandible(false), new Vector3(.12f, .34f, .92f), Vector3.one * 1.25f, VisualFactory.Material(body, .32f));
            return null;
        }

        static void BuildCreatureLegs(Transform parent, int pairs, float y, float reach, Color color, float radius)
        {
            for (int pair = 0; pair < pairs; pair++)
            for (int s = -1; s <= 1; s += 2)
            {
                float normalized = pairs == 1 ? 0 : pair / (float)(pairs - 1);
                float z = Mathf.Lerp(.46f, -.48f, normalized);
                Vector3 hip = new(s * .27f, y, z);
                Vector3 knee = new(s * reach * .58f, y + .18f, z + (pair - (pairs - 1) * .5f) * .14f);
                Vector3 foot = new(s * reach, .045f, z + (pair - (pairs - 1) * .5f) * .3f);
                VisualFactory.Segment("Muscular upper leg", parent, hip, knee, radius, color, false, .26f);
                VisualFactory.Segment("Tapered lower leg", parent, knee, foot, radius * .72f, color, false, .24f);
            }
        }
    }
}
