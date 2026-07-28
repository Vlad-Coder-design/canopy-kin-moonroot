using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CanopyKin
{
    /// <summary>
    /// Runtime driver for the licensed, skinned fishing-spider scan.  The FBX
    /// also contains authored clips for inspection/reuse, while this driver
    /// keeps gait, telegraph and hit reactions synchronized with live AI.
    /// </summary>
    public sealed class SpiderVisual : MonoBehaviour
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
        Quaternion rootRest;
        Quaternion thoraxRest;
        Quaternion abdomenRest;
        Quaternion headRest;
        Vector3 previousWorldPosition;
        float stride;
        float locomotion;
        float telegraph;
        float telegraphTarget;
        float attack;
        float stagger;
        float death;
        bool dead;

        public static SpiderVisual Create(Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>(
                "Models/Creatures/CanopyKinFishingSpider");
            if (!prefab) return null;

            var visualRoot = new GameObject("Production CC0 fishing spider").transform;
            visualRoot.SetParent(parent, false);
            GameObject model = Instantiate(prefab, visualRoot, false);
            model.name = "Rigged fishing spider model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            foreach (Animator animator in model.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;

            Material material = BuildMaterial();
            SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.updateWhenOffscreen = false;
            }

            SkinnedMeshRenderer high = null;
            SkinnedMeshRenderer low = null;
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
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
                    new LOD(.38f, new Renderer[] { high }),
                    new LOD(.075f, new Renderer[] { low })
                });
                group.RecalculateBounds();
            }

            SpiderVisual visual = visualRoot.gameObject.AddComponent<SpiderVisual>();
            visual.CacheRig(model.transform);
            return visual;
        }

        static Material BuildMaterial()
        {
            Shader shader = Resources.Load<Shader>("CanopyKinLit") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = "Fishing spider 8K scan material",
                color = new Color(1.12f, 1.08f, 1.03f),
                enableInstancing = true
            };
            Texture2D albedo = Resources.Load<Texture2D>(
                "HighQuality/Sketchfab/FishingSpider/fishing_spider_albedo_8k");
            if (albedo)
            {
                albedo.wrapMode = TextureWrapMode.Clamp;
                albedo.filterMode = FilterMode.Trilinear;
                albedo.anisoLevel = RuntimeQualityProfile.IsFullQuality ? 16 : 4;
                material.SetTexture("_MainTex", albedo);
            }
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", new Color(1.12f, 1.08f, 1.03f));
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .18f);
            if (material.HasProperty("_NormalStrength")) material.SetFloat("_NormalStrength", .35f);
            return material;
        }

        void CacheRig(Transform model)
        {
            rootBone = Find(model, "Root");
            thorax = Find(model, "Thorax");
            abdomen = Find(model, "Abdomen");
            head = Find(model, "Head");
            rootRest = rootBone ? rootBone.localRotation : Quaternion.identity;
            thoraxRest = thorax ? thorax.localRotation : Quaternion.identity;
            abdomenRest = abdomen ? abdomen.localRotation : Quaternion.identity;
            headRest = head ? head.localRotation : Quaternion.identity;

            string[] pairs = { "Front", "FrontMid", "RearMid", "Rear" };
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
                    Front = pair < 2
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
            locomotion = Mathf.MoveTowards(locomotion, Mathf.Clamp01(speed / 1.15f), delta * 4.8f);
            telegraph = Mathf.MoveTowards(telegraph, telegraphTarget, delta * 5.6f);
            attack = Mathf.MoveTowards(attack, 0f, delta * 3.8f);
            stagger = Mathf.MoveTowards(stagger, 0f, delta * 4.5f);
            if (dead) death = Mathf.MoveTowards(death, 1f, delta * 1.35f);
            stride += delta * Mathf.Lerp(1.5f, 10.5f, locomotion);

            for (int index = 0; index < legs.Count; index++)
            {
                Leg leg = legs[index];
                float cycle = Mathf.Sin(stride + leg.Phase);
                float lift = Mathf.Max(0, Mathf.Cos(stride + leg.Phase));
                float warningRaise = telegraph * (leg.Front ? 30f : 5f);
                float attackSnap = Mathf.Sin(attack * Mathf.PI) * (leg.Front ? -24f : 4f);
                float fold = death * (leg.Front ? -66f : 58f);
                leg.Coxa.localRotation = leg.CoxaRest * Quaternion.Euler(
                    cycle * locomotion * 9f + attackSnap,
                    cycle * locomotion * 24f,
                    leg.Side * (lift * locomotion * 15f + warningRaise + fold));
                leg.Femur.localRotation = leg.FemurRest * Quaternion.Euler(
                    -cycle * locomotion * 8f,
                    -cycle * locomotion * 10f,
                    -leg.Side * lift * locomotion * 12f);
                leg.Tibia.localRotation = leg.TibiaRest * Quaternion.Euler(
                    cycle * locomotion * 6f,
                    cycle * locomotion * 8f,
                    leg.Side * lift * locomotion * 8f);
            }

            float breathing = Mathf.Sin(Time.time * 1.8f) * 1.5f;
            float attackLunge = Mathf.Sin(attack * Mathf.PI) * 22f;
            if (head)
                head.localRotation = headRest * Quaternion.Euler(
                    -telegraph * 24f + attackLunge,
                    stagger * Mathf.Sin(Time.time * 45f) * 13f,
                    0);
            if (thorax)
                thorax.localRotation = thoraxRest * Quaternion.Euler(attackLunge * .18f, 0, 0);
            if (abdomen)
                abdomen.localRotation = abdomenRest * Quaternion.Euler(
                    breathing - locomotion * 2f,
                    0,
                    Mathf.Sin(stride * .5f) * locomotion * 2.5f);
            if (rootBone)
                rootBone.localRotation = rootRest * Quaternion.Euler(
                    stagger * Mathf.Sin(Time.time * 38f) * 7f,
                    death * 9f,
                    -death * 86f);
            transform.localPosition = Vector3.up * (
                Mathf.Abs(Mathf.Sin(stride)) * locomotion * .018f);
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
