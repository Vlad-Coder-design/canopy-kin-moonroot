using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace CanopyKin
{
    /// <summary>
    /// Collects player-build measurements instead of presenting guessed performance.
    /// A warm sample is written to Player.log and, on desktop, to a JSON report.
    /// </summary>
    public sealed class PerformanceTelemetry : MonoBehaviour
    {
        [Serializable]
        sealed class ProfileReport
        {
            public string edition;
            public int quality;
            public float sampleSeconds;
            public int frames;
            public float averageFps;
            public float averageFrameMs;
            public float p95FrameMs;
            public double frameTimingCpuMs;
            public double frameTimingGpuMs;
            public double averageBatches;
            public double averageSetPassCalls;
            public double averageTriangles;
            public double averageGcBytesPerFrame;
            public long allocatedMemoryBytes;
            public long reservedMemoryBytes;
        }

        const float WarmupSeconds = 5f;
        const float SampleSeconds = 20f;

        readonly List<float> frameMilliseconds = new(1800);
        readonly List<double> cpuMilliseconds = new(1800);
        readonly List<double> gpuMilliseconds = new(1800);
        ProfilerRecorder batches;
        ProfilerRecorder setPass;
        ProfilerRecorder triangles;
        ProfilerRecorder gcPerFrame;
        double batchSum;
        double setPassSum;
        double triangleSum;
        double gcSum;
        int counterSamples;
        float startedAt;
        bool complete;

        void Start()
        {
            startedAt = Time.realtimeSinceStartup;
            batches = StartRecorder(ProfilerCategory.Render, "Batches Count");
            setPass = StartRecorder(ProfilerCategory.Render, "SetPass Calls Count");
            triangles = StartRecorder(ProfilerCategory.Render, "Triangles Count");
            gcPerFrame = StartRecorder(ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        static ProfilerRecorder StartRecorder(ProfilerCategory category, string marker)
        {
            try
            {
                return ProfilerRecorder.StartNew(category, marker, 1);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"MOONROOT_PROFILE_MARKER_UNAVAILABLE marker={marker} reason={exception.Message}");
                return default;
            }
        }

        void Update()
        {
            if (complete) return;
            float elapsed = Time.realtimeSinceStartup - startedAt;
            if (elapsed < WarmupSeconds) return;

            frameMilliseconds.Add(Time.unscaledDeltaTime * 1000f);
            FrameTimingManager.CaptureFrameTimings();
            var timings = new FrameTiming[1];
            if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
            {
                if (timings[0].cpuFrameTime > 0) cpuMilliseconds.Add(timings[0].cpuFrameTime);
                if (timings[0].gpuFrameTime > 0) gpuMilliseconds.Add(timings[0].gpuFrameTime);
            }

            SampleCounter(batches, ref batchSum);
            SampleCounter(setPass, ref setPassSum);
            SampleCounter(triangles, ref triangleSum);
            SampleCounter(gcPerFrame, ref gcSum);
            counterSamples++;

            if (elapsed >= WarmupSeconds + SampleSeconds)
                CompleteSample();
        }

        static void SampleCounter(ProfilerRecorder recorder, ref double sum)
        {
            if (recorder.Valid) sum += recorder.LastValue;
        }

        void CompleteSample()
        {
            complete = true;
            float averageFrame = frameMilliseconds.Count > 0 ? frameMilliseconds.Average() : 0;
            float p95Frame = Percentile(frameMilliseconds, .95f);
            double averageCpu = cpuMilliseconds.Count > 0 ? cpuMilliseconds.Average() : 0;
            double averageGpu = gpuMilliseconds.Count > 0 ? gpuMilliseconds.Average() : 0;
            double divisor = Math.Max(1, counterSamples);
            long allocatedMemory = Profiler.GetTotalAllocatedMemoryLong();
            long reservedMemory = Profiler.GetTotalReservedMemoryLong();
            var report = new ProfileReport
            {
                edition = RuntimeQualityProfile.Edition.ToString(),
                quality = GameSettings.Quality,
                sampleSeconds = SampleSeconds,
                frames = frameMilliseconds.Count,
                averageFps = averageFrame > 0 ? 1000f / averageFrame : 0,
                averageFrameMs = averageFrame,
                p95FrameMs = p95Frame,
                frameTimingCpuMs = averageCpu,
                frameTimingGpuMs = averageGpu,
                averageBatches = batchSum / divisor,
                averageSetPassCalls = setPassSum / divisor,
                averageTriangles = triangleSum / divisor,
                averageGcBytesPerFrame = gcSum / divisor,
                allocatedMemoryBytes = allocatedMemory,
                reservedMemoryBytes = reservedMemory
            };
            string json = JsonUtility.ToJson(report, true);
            Debug.Log($"MOONROOT_PROFILE\n{json}");

#if !UNITY_WEBGL || UNITY_EDITOR
            try
            {
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "performance-latest.json"), json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"MOONROOT_PROFILE_WRITE_FAILED {exception.Message}");
            }
#endif
        }

        static float Percentile(List<float> source, float percentile)
        {
            if (source.Count == 0) return 0;
            float[] sorted = source.ToArray();
            Array.Sort(sorted);
            int index = Mathf.Clamp(Mathf.CeilToInt(sorted.Length * percentile) - 1, 0, sorted.Length - 1);
            return sorted[index];
        }

        void OnDestroy()
        {
            batches.Dispose();
            setPass.Dispose();
            triangles.Dispose();
            gcPerFrame.Dispose();
        }
    }
}
