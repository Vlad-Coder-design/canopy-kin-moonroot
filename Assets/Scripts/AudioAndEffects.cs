using System;
using System.Collections.Generic;
using UnityEngine;

namespace CanopyKin
{
    /// <summary>
    /// Original procedural soundscape. The clips are synthesized once at startup,
    /// avoiding unlicensed samples and keeping the WebGL download compact.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        public static AudioDirector Instance { get; private set; }

        AudioSource forest;
        AudioSource underground;
        AudioSource music;
        readonly List<AudioSource> oneShots = new();
        AudioClip bite;
        AudioClip hit;
        AudioClip step;
        AudioClip order;
        int voice;

        public void Initialize()
        {
            Instance = this;
            forest = LoopingSource("Forest rain ambience", MakeForest(), .36f);
            underground = LoopingSource("Underground colony ambience", MakeUnderground(), 0);
            music = LoopingSource("Moonroot adaptive score", MakeMusic(), .12f);
            bite = MakeTransient("Mandible snap", .14f, 1450, 160, .34f);
            hit = MakeTransient("Shell impact", .22f, 280, 75, .6f);
            step = MakeTransient("Soil footstep", .08f, 420, 125, .16f);
            order = MakeTransient("Pheromone order", .28f, 930, 420, .24f);
            for (int i = 0; i < 8; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = .62f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.maxDistance = 18;
                source.minDistance = .5f;
                oneShots.Add(source);
            }
            GameSettings.Apply();
        }

        AudioSource LoopingSource(string name, AudioClip clip, float volume)
        {
            var sourceObject = new GameObject(name);
            sourceObject.transform.SetParent(transform, false);
            var source = sourceObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = volume;
            source.spatialBlend = 0;
            source.Play();
            return source;
        }

        void Update()
        {
            WorldBootstrap world = WorldBootstrap.Instance;
            if (!world) return;
            float undergroundTarget = world.IsUnderground ? .42f : 0;
            float forestTarget = world.IsUnderground ? .04f : .36f;
            underground.volume = Mathf.MoveTowards(underground.volume, undergroundTarget, Time.unscaledDeltaTime * .28f);
            forest.volume = Mathf.MoveTowards(forest.volume, forestTarget, Time.unscaledDeltaTime * .28f);
            bool underAttack = world.Mission != null &&
                               world.Mission.Step >= MissionDirector.RivalDefenseStep &&
                               world.Mission.Step < MissionDirector.OverlookStep;
            music.pitch = Mathf.MoveTowards(
                music.pitch, underAttack ? 1.08f : 1f, Time.unscaledDeltaTime * .04f);
            music.volume = Mathf.MoveTowards(
                music.volume, underAttack ? .2f : .12f, Time.unscaledDeltaTime * .06f);
        }

        public void PlayBite(Vector3 position) => Play(bite, position, .72f, UnityEngine.Random.Range(.93f, 1.08f));
        public void PlayHit(Vector3 position) => Play(hit, position, .78f, UnityEngine.Random.Range(.9f, 1.04f));
        public void PlayStep(Vector3 position) => Play(step, position, .22f, UnityEngine.Random.Range(.82f, 1.15f));
        public void PlayOrder(Vector3 position) => Play(order, position, .48f, UnityEngine.Random.Range(.96f, 1.05f));

        void Play(AudioClip clip, Vector3 position, float volume, float pitch)
        {
            AudioSource source = oneShots[voice++ % oneShots.Count];
            source.transform.position = position;
            source.pitch = pitch;
            source.volume = volume;
            source.PlayOneShot(clip);
        }

        static AudioClip MakeForest()
        {
            const int rate = 22050;
            const int seconds = 5;
            float[] data = new float[rate * seconds];
            var random = new System.Random(9031);
            float brown = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                brown = Mathf.Lerp(brown, (float)random.NextDouble() * 2 - 1, .018f);
                float rain = ((float)random.NextDouble() * 2 - 1) * .035f;
                float insect = Mathf.Sin(t * Mathf.PI * 2 * 317f) * Mathf.Max(0, Mathf.Sin(t * .73f)) * .008f;
                float canopy = Mathf.Sin(t * .21f) * brown * .11f;
                data[i] = canopy + rain + insect;
            }
            return Clip("Original forest after rain", rate, data);
        }

        static AudioClip MakeUnderground()
        {
            const int rate = 22050;
            const int seconds = 4;
            float[] data = new float[rate * seconds];
            var random = new System.Random(1147);
            float low = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                low = Mathf.Lerp(low, (float)random.NextDouble() * 2 - 1, .004f);
                float chamber = Mathf.Sin(t * Mathf.PI * 2 * 54f) * .022f + Mathf.Sin(t * Mathf.PI * 2 * 81f) * .009f;
                data[i] = chamber + low * .055f;
            }
            return Clip("Original underground resonance", rate, data);
        }

        static AudioClip MakeMusic()
        {
            const int rate = 22050;
            const int seconds = 8;
            float[] data = new float[rate * seconds];
            float[] notes = { 110f, 146.83f, 164.81f, 220f };
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float envelope = .55f + Mathf.Sin(t * Mathf.PI / seconds) * .45f;
                float value = 0;
                for (int n = 0; n < notes.Length; n++)
                    value += Mathf.Sin(t * Mathf.PI * 2 * notes[n] + n * .7f) * (.014f / (n + 1));
                data[i] = value * envelope;
            }
            return Clip("Original Moonroot tonal score", rate, data);
        }

        static AudioClip MakeTransient(string name, float seconds, float startFrequency, float endFrequency, float noise)
        {
            const int rate = 22050;
            int count = Mathf.CeilToInt(rate * seconds);
            float[] data = new float[count];
            var random = new System.Random(name.GetHashCode());
            float phase = 0;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
                phase += frequency / rate * Mathf.PI * 2;
                float envelope = Mathf.Pow(1f - t, 2.2f);
                data[i] = (Mathf.Sin(phase) * (1 - noise) + ((float)random.NextDouble() * 2 - 1) * noise) * envelope * .45f;
            }
            return Clip(name, rate, data);
        }

        static AudioClip Clip(string name, int rate, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }

    /// <summary>Small pooled contact effect used for bites, impacts, and commands.</summary>
    public sealed class FxPool : MonoBehaviour
    {
        public static FxPool Instance { get; private set; }
        readonly List<ParticleSystem> pool = new();
        int next;

        public void Initialize()
        {
            Instance = this;
            for (int i = 0; i < 10; i++)
            {
                var root = new GameObject($"Pooled forest motes {i}");
                root.transform.SetParent(transform, false);
                var particles = root.AddComponent<ParticleSystem>();
                var main = particles.main;
                main.playOnAwake = false;
                main.duration = .45f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(.18f, .42f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(.25f, 1.1f);
                main.startSize = new ParticleSystem.MinMaxCurve(.025f, .075f);
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 20;
                var emission = particles.emission;
                emission.enabled = false;
                var shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Hemisphere;
                shape.radius = .18f;
                var renderer = particles.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = VisualFactory.Material(new Color(.75f, .48f, .17f), .1f);
                pool.Add(particles);
            }
        }

        public void Burst(Vector3 position, Color color, int count = 10)
        {
            if (pool.Count == 0) return;
            ParticleSystem particles = pool[next++ % pool.Count];
            particles.transform.position = position;
            var main = particles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color * .72f, color);
            particles.Emit(count);
        }
    }
}
