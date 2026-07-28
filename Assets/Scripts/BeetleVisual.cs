using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CanopyKin
{
    /// <summary>
    /// Responsive runtime driver for the rigged CC0 Japanese rhinoceros beetle.
    /// </summary>
    public sealed class BeetleVisual : MonoBehaviour
    {
        sealed class Leg
        {
            public Transform Coxa;
            public Transform Femur;
            public Transform Tibia;
            public Quaternion CoxaRest;
            public Quaternion FemurRest;
            public Quaternion TibiaRest;
            public float Phase;
            public float Side;
            public bool Front;
        }

        readonly List<Leg> legs = new();
        Transform rootBone;
        Transform thorax;
        Transform abdomen;
        Transform head;
        Transform horn;
        Quaternion rootRest;
        Quaternion thoraxRest;
        Quaternion abdomenRest;
        Quaternion headRest;
        Quaternion hornRest;
        Vector3 previousWorldPosition;
        float locomotion;
        float stride;
        float telegraph;
        float telegraphTarget;
        float attack;
        float stagger;
        float death;
        bool dead;

        public static BeetleVisual Create(Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>(
                "Models/Creatures/CanopyKinRhinocerosBeetle");
            if (!prefab) return null;

            var visualRoot = new GameObject("Production CC0 rhinoceros beetle").transform;
            visualRoot.SetParent(parent, false);
            GameObject model = Instantiate(prefab, visualRoot, false);
            model.name = "Rigged rhinoceros beetle model";
            model.transform.localPosition = Vector3.zero;
            // Blender's +Y anatomical heading is exported as Unity -Z. Turn the
            // visual once so its horn, charge direction and gameplay forward agree.
            model.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            model.transform.localScale = Vector3.one;
            foreach (Animator animator in model.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;

            Material material = BuildMaterial();
            SkinnedMeshRenderer[] renderers =
                model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            SkinnedMeshRenderer high = null;
            SkinnedMeshRenderer low = null;
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.updateWhenOffscreen = false;
                if (renderer.name.Contains("LOD0")) high = renderer;
                else if (renderer.name.Contains("LOD1")) low = renderer;
            }
            if (high && low)
            {
                LODGroup group = model.GetComponent<LODGroup>();
                if (!group) group = model.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.CrossFade;
                group.animateCrossFading = true;
                group.SetLODs(new[]
                {
                    new LOD(.4f, new Renderer[] { high }),
                    new LOD(.075f, new Renderer[] { low })
                });
                group.RecalculateBounds();
            }

            BeetleVisual visual = visualRoot.gameObject.AddComponent<BeetleVisual>();
            visual.CacheRig(model.transform);
            return visual;
        }

        static Material BuildMaterial()
        {
            Shader shader = Resources.Load<Shader>("CanopyKinLit") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = "Rhinoceros beetle 8K scan material",
                color = new Color(1.06f, 1.04f, 1.02f),
                enableInstancing = true
            };
            Texture2D albedo = Resources.Load<Texture2D>(
                "HighQuality/Sketchfab/RhinocerosBeetle/rhinoceros_beetle_albedo_8k");
            if (albedo)
            {
                albedo.wrapMode = TextureWrapMode.Clamp;
                albedo.filterMode = FilterMode.Trilinear;
                albedo.anisoLevel = RuntimeQualityProfile.IsFullQuality ? 16 : 4;
                material.SetTexture("_MainTex", albedo);
            }
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", new Color(1.06f, 1.04f, 1.02f));
            // Rhinoceros beetles have a polished chitin shell. CanopyKinLit defaults
            // to a white roughness texture when none is assigned, which made this
            // very dark scan read as matte black and erased the photographed form.
            // A constant low-roughness input preserves the source albedo while
            // restoring the broad, physically plausible shell highlights.
            if (material.HasProperty("_RoughnessMap"))
                material.SetTexture("_RoughnessMap", Texture2D.blackTexture);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .38f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .06f);
            if (material.HasProperty("_NormalStrength")) material.SetFloat("_NormalStrength", .4f);
            return material;
        }

        void CacheRig(Transform model)
        {
            rootBone = Find(model, "Root");
            thorax = Find(model, "Thorax");
            abdomen = Find(model, "Abdomen");
            head = Find(model, "Head");
            horn = Find(model, "Horn");
            rootRest = rootBone ? rootBone.localRotation : Quaternion.identity;
            thoraxRest = thorax ? thorax.localRotation : Quaternion.identity;
            abdomenRest = abdomen ? abdomen.localRotation : Quaternion.identity;
            headRest = head ? head.localRotation : Quaternion.identity;
            hornRest = horn ? horn.localRotation : Quaternion.identity;

            string[] pairs = { "Front", "Middle", "Rear" };
            for (int pair = 0; pair < pairs.Length; pair++)
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                string sideName = sideIndex == 0 ? "L" : "R";
                Transform coxa = Find(model, $"Leg_{sideName}_{pairs[pair]}_Coxa");
                Transform femur = Find(model, $"Leg_{sideName}_{pairs[pair]}_Femur");
                Transform tibia = Find(model, $"Leg_{sideName}_{pairs[pair]}_Tibia");
                if (!coxa || !femur || !tibia) continue;
                legs.Add(new Leg
                {
                    Coxa = coxa,
                    Femur = femur,
                    Tibia = tibia,
                    CoxaRest = coxa.localRotation,
                    FemurRest = femur.localRotation,
                    TibiaRest = tibia.localRotation,
                    Phase = (pair + sideIndex) % 2 == 0 ? 0 : Mathf.PI,
                    Side = sideIndex == 0 ? -1f : 1f,
                    Front = pair == 0
                });
            }
            previousWorldPosition = transform.position;
        }

        public void SetTelegraphing(bool value) => telegraphTarget = value ? 1f : 0f;

        public void PlayAttack()
        {
            attack = 1f;
            telegraphTarget = 0f;
        }

        public void PlayStagger() => stagger = 1f;

        public void PlayDeath()
        {
            dead = true;
            death = 0f;
            telegraphTarget = 0f;
        }

        void Update()
        {
            float delta = Mathf.Max(Time.deltaTime, .0001f);
            float speed = Vector3.Distance(transform.position, previousWorldPosition) / delta;
            previousWorldPosition = transform.position;
            locomotion = Mathf.MoveTowards(locomotion, Mathf.Clamp01(speed / 1.2f), delta * 4.4f);
            telegraph = Mathf.MoveTowards(telegraph, telegraphTarget, delta * 5f);
            attack = Mathf.MoveTowards(attack, 0f, delta * 3.4f);
            stagger = Mathf.MoveTowards(stagger, 0f, delta * 4.4f);
            if (dead) death = Mathf.MoveTowards(death, 1f, delta * 1.3f);
            stride += delta * Mathf.Lerp(1.25f, 9f, locomotion);

            foreach (Leg leg in legs)
            {
                float cycle = Mathf.Sin(stride + leg.Phase);
                float lift = Mathf.Max(0, Mathf.Cos(stride + leg.Phase));
                float brace = telegraph * (leg.Front ? 18f : 5f);
                float fold = death * (leg.Front ? -58f : 52f);
                leg.Coxa.localRotation = leg.CoxaRest * Quaternion.Euler(
                    cycle * locomotion * 7f,
                    cycle * locomotion * 21f,
                    leg.Side * (lift * locomotion * 13f + brace + fold));
                leg.Femur.localRotation = leg.FemurRest * Quaternion.Euler(
                    -cycle * locomotion * 7f,
                    -cycle * locomotion * 9f,
                    -leg.Side * lift * locomotion * 10f);
                leg.Tibia.localRotation = leg.TibiaRest * Quaternion.Euler(
                    cycle * locomotion * 5f,
                    cycle * locomotion * 7f,
                    leg.Side * lift * locomotion * 7f);
            }

            float breathing = Mathf.Sin(Time.time * 1.45f) * 1.2f;
            float charge = Mathf.Sin(attack * Mathf.PI) * -26f;
            if (head)
                head.localRotation = headRest * Quaternion.Euler(
                    telegraph * 22f + charge,
                    stagger * Mathf.Sin(Time.time * 42f) * 10f,
                    0);
            if (horn)
                horn.localRotation = hornRest * Quaternion.Euler(
                    telegraph * 12f + charge * .45f, 0, 0);
            if (thorax)
                thorax.localRotation = thoraxRest * Quaternion.Euler(charge * .16f, 0, 0);
            if (abdomen)
                abdomen.localRotation = abdomenRest * Quaternion.Euler(
                    breathing,
                    0,
                    Mathf.Sin(stride * .5f) * locomotion * 1.6f);
            if (rootBone)
                rootBone.localRotation = rootRest * Quaternion.Euler(
                    stagger * Mathf.Sin(Time.time * 37f) * 6f,
                    death * 7f,
                    -death * 86f);
            transform.localPosition = Vector3.up * (
                Mathf.Abs(Mathf.Sin(stride)) * locomotion * .012f);
        }

        static Transform Find(Transform root, string name)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (item.name == name)
                    return item;
            return null;
        }
    }
}
