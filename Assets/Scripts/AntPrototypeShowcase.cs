using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace CanopyKin
{
    /// <summary>
    /// Interactive Game View proof for the real imported Formica-rufa FBX.
    /// This scene exists to approve anatomy, materials, skinning and source
    /// animation clips before the gameplay Player is replaced.
    /// </summary>
    public sealed class AntPrototypeShowcase : MonoBehaviour
    {
        static readonly string[] PreferredClips =
        {
            "ANT_CalmIdle",
            "ANT_AlertIdle",
            "ANT_ExploreAntennae",
            "ANT_SlowWalk",
            "ANT_NormalWalk",
            "ANT_FastRun",
            "ANT_TurnLeft",
            "ANT_TurnRight",
            "ANT_Attack_Primary",
            "ANT_Bite",
            "ANT_GrabHeavyBite",
            "ANT_Dig",
            "ANT_ColonyWork",
            "ANT_Trophallaxis"
        };

        [SerializeField] Animator animator;
        [SerializeField] Camera viewCamera;
        [SerializeField] Transform antRoot;
        [SerializeField] Light keyLight;
        SkinnedMeshRenderer skin;
        readonly List<string> clips = new();
        int clipIndex;
        float orbitYaw = 28f;
        float orbitPitch = 16f;
        float orbitDistance = 2.15f;
        GUIStyle title;
        GUIStyle text;
        GUIStyle button;
        string status = "Imported FBX ready";

        public void Initialize(
            Transform importedAnt,
            Animator importedAnimator,
            Camera camera,
            Light mainLight)
        {
            antRoot = importedAnt;
            animator = importedAnimator;
            viewCamera = camera;
            keyLight = mainLight;
        }

        void Awake()
        {
            if (!antRoot)
            {
                Animator found = GetComponentInChildren<Animator>(true);
                if (found)
                {
                    animator = found;
                    antRoot = found.transform;
                }
            }
            if (!viewCamera) viewCamera = Camera.main;
            skin = antRoot ? antRoot.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
            if (skin)
            {
                skin.updateWhenOffscreen = true;
                skin.shadowCastingMode = ShadowCastingMode.On;
                skin.receiveShadows = true;
                skin.quality = SkinQuality.Bone4;
            }
            ApplyMaximumQualityMaterials();
        }

        void Start()
        {
            if (animator && animator.runtimeAnimatorController)
            {
                var available = animator.runtimeAnimatorController.animationClips
                    .Select(item => item.name)
                    .Distinct()
                    .ToHashSet(StringComparer.Ordinal);
                clips.AddRange(PreferredClips.Where(available.Contains));
                clips.AddRange(available.Where(name => !clips.Contains(name)));
            }
            if (clips.Count > 0) Play(0);
            string[] arguments = Environment.GetCommandLineArgs();
            if (Array.Exists(arguments, value =>
                    string.Equals(value, "-prototype-qa", StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(CaptureQa());
        }

        void ApplyMaximumQualityMaterials()
        {
            if (!antRoot) return;
            Material red = VisualFactory.PbrMaterial(
                "AntExoskeleton",
                new Color(.92f, .24f, .065f),
                .34f,
                1.28f,
                new Vector2(2.8f, 2.8f));
            Material dark = VisualFactory.PbrMaterial(
                "AntExoskeleton",
                new Color(.16f, .045f, .022f),
                .28f,
                1.34f,
                new Vector2(3.1f, 3.1f));
            Material joint = VisualFactory.PbrMaterial(
                "AntExoskeleton",
                new Color(.29f, .07f, .025f),
                .2f,
                1.05f,
                new Vector2(3.6f, 3.6f));
            Material eye = VisualFactory.Material(new Color(.003f, .009f, .006f), .88f);
            Material[] formicaMaterials = { red, dark, joint, eye };
            foreach (Renderer renderer in antRoot.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;
                if (slots.Length == 0) slots = new Material[formicaMaterials.Length];
                for (int index = 0; index < slots.Length; index++)
                {
                    string sourceName = slots[index] ? slots[index].name : string.Empty;
                    if (sourceName.Contains("CompoundEye", StringComparison.OrdinalIgnoreCase))
                        slots[index] = eye;
                    else if (sourceName.Contains("DarkGaster", StringComparison.OrdinalIgnoreCase))
                        slots[index] = dark;
                    else if (sourceName.Contains("DarkJoint", StringComparison.OrdinalIgnoreCase))
                        slots[index] = joint;
                    else
                        slots[index] = formicaMaterials[Mathf.Min(index, formicaMaterials.Length - 1)];
                    VisualFactory.ConfigureOpaque(slots[index]);
                }
                renderer.sharedMaterials = slots;
            }
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard != null)
            {
                if (keyboard.rightArrowKey.wasPressedThisFrame) Play(clipIndex + 1);
                if (keyboard.leftArrowKey.wasPressedThisFrame) Play(clipIndex - 1);
                if (keyboard.digit1Key.wasPressedThisFrame) PlayNamed("ANT_CalmIdle");
                if (keyboard.digit2Key.wasPressedThisFrame) PlayNamed("ANT_NormalWalk");
                if (keyboard.digit3Key.wasPressedThisFrame) PlayNamed("ANT_FastRun");
                if (keyboard.digit4Key.wasPressedThisFrame) PlayNamed("ANT_TurnLeft");
                if (keyboard.digit5Key.wasPressedThisFrame) PlayNamed("ANT_TurnRight");
                if (keyboard.digit6Key.wasPressedThisFrame) PlayNamed("ANT_Attack_Primary");
                if (keyboard.digit7Key.wasPressedThisFrame) PlayNamed("ANT_GrabHeavyBite");
                if (keyboard.rKey.wasPressedThisFrame) ResetView();
            }
            if (mouse != null && mouse.leftButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                orbitYaw += delta.x * .18f;
                orbitPitch = Mathf.Clamp(orbitPitch - delta.y * .14f, -25f, 68f);
            }
            if (mouse != null)
                orbitDistance = Mathf.Clamp(
                    orbitDistance - mouse.scroll.ReadValue().y * .0012f,
                    1.1f,
                    4.2f);
        }

        void LateUpdate()
        {
            if (!viewCamera || !antRoot) return;
            Vector3 target = antRoot.position + Vector3.up * .3f;
            Quaternion orbit = Quaternion.Euler(orbitPitch, orbitYaw, 0);
            viewCamera.transform.position = target + orbit * new Vector3(0, .08f, -orbitDistance);
            viewCamera.transform.rotation = Quaternion.LookRotation(target - viewCamera.transform.position);
        }

        void Play(int index)
        {
            if (clips.Count == 0 || !animator) return;
            clipIndex = (index % clips.Count + clips.Count) % clips.Count;
            animator.Play(clips[clipIndex], 0, 0);
            status = $"Playing genuine clip: {clips[clipIndex]}";
        }

        void PlayNamed(string name)
        {
            int index = clips.IndexOf(name);
            if (index >= 0) Play(index);
        }

        void ResetView()
        {
            orbitYaw = 28f;
            orbitPitch = 16f;
            orbitDistance = 2.15f;
            if (keyLight) keyLight.intensity = 1.55f;
        }

        IEnumerator CaptureQa()
        {
            string directory = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "QA", "AntPrototype", "Unity"));
            Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(1.5f);
            foreach ((string clip, float yaw, float pitch, string file) in new[]
            {
                ("ANT_CalmIdle", 5f, 12f, "unity-prototype-front.png"),
                ("ANT_NormalWalk", 88f, 10f, "unity-prototype-walk-side.png"),
                ("ANT_FastRun", 42f, 20f, "unity-prototype-run.png"),
                ("ANT_TurnLeft", 22f, 34f, "unity-prototype-turn.png"),
                ("ANT_Attack_Primary", 18f, 8f, "unity-prototype-mandibles.png"),
                ("ANT_GrabHeavyBite", -32f, 19f, "unity-prototype-carry-pose.png")
            })
            {
                PlayNamed(clip);
                orbitYaw = yaw;
                orbitPitch = pitch;
                yield return new WaitForSeconds(.8f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, file), 1);
                yield return new WaitForSeconds(.5f);
            }
            Debug.Log(
                $"CANOPY_KIN_FORMICA_PROTOTYPE_QA_OK clips={clips.Count} " +
                $"triangles={(skin && skin.sharedMesh ? skin.sharedMesh.triangles.Length / 3 : 0)} " +
                $"bones={(skin ? skin.bones.Length : 0)} directory={directory}");
            status = "Automated Game View evidence captured";
            if (!Application.isEditor) Application.Quit(0);
        }

        void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, .71f, .28f) }
            };
            text = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            button = new GUIStyle(GUI.skin.button) { fontSize = 13 };
        }

        void OnGUI()
        {
            EnsureStyles();
            GUI.Box(new Rect(18, 18, 430, 194), GUIContent.none);
            GUI.Label(new Rect(34, 30, 390, 30), "FORMICA RUFA — VISUAL PROTOTYPE", title);
            string geometry = skin && skin.sharedMesh
                ? $"SkinnedMeshRenderer · {skin.sharedMesh.triangles.Length / 3:N0} tris · {skin.bones.Length} bones"
                : "SkinnedMeshRenderer unavailable";
            GUI.Label(new Rect(34, 67, 390, 62),
                $"{status}\n{geometry}\n4K PBR: albedo · normal · roughness · AO", text);
            GUI.Label(new Rect(34, 130, 390, 36),
                "1 Idle · 2 Walk · 3 Run · 4/5 Turn · 6 Attack · 7 Carry", text);
            GUI.Label(new Rect(34, 166, 390, 30),
                "Mouse drag: orbit · wheel: zoom · arrows: all clips · R: reset", text);
            if (GUI.Button(new Rect(Screen.width - 190, 20, 170, 34), "Previous clip", button))
                Play(clipIndex - 1);
            if (GUI.Button(new Rect(Screen.width - 190, 60, 170, 34), "Next clip", button))
                Play(clipIndex + 1);
        }
    }
}
