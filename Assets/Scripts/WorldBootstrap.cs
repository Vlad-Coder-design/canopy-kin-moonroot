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
        public static readonly Vector2 HeroMicrohabitatCenter = new(9.1f, 16.1f);
        static readonly Vector3 UndergroundCenter = new(0, -5.45f, -7);
        public const float PlayerColliderRadius = .23f;
        public const float PlayerColliderHeight = .68f;
        public const float CameraCollisionRadius = .19f;
        public const float MinimumNormalTunnelWidth = 2.3f;
        public const float MinimumBusyTunnelWidth = 2.8f;
        const int CentralChamberIndex = 0;
        const int QueenChamberIndex = 1;
        const int FoodChamberIndex = 2;
        const int NurseryChamberIndex = 3;
        const int EntranceChamberIndex = 4;
        const int EggChamberIndex = 5;
        const int PupaChamberIndex = 6;
        const int SanitationChamberIndex = 7;
        const int GuardChamberIndex = 8;
        const int EntranceTunnelIndex = 3;
        static readonly Vector3[] UndergroundChamberCenters =
        {
            new(0, 0, .35f), new(-7f, 0, -2.8f),
            new(-7f, 0, 3.5f), new(7.2f, 0, -.6f),
            new(0, 0, 7.3f), new(6.6f, 0, -6.8f),
            new(7.4f, 0, 4.6f), new(-1.1f, 0, -6.7f),
            new(3.8f, 0, 8.7f)
        };
        static readonly Vector3[] UndergroundChamberRadii =
        {
            new(3.8f, 3.2f, 3.4f), new(3.3f, 2.8f, 2.8f),
            new(3.1f, 2.6f, 2.7f), new(4.25f, 3.1f, 3.6f),
            new(3.2f, 2.6f, 2.65f), new(3.2f, 2.6f, 2.7f),
            new(3.2f, 2.6f, 2.8f), new(2.7f, 2.4f, 2.5f),
            new(2.8f, 2.4f, 2.45f)
        };
        static readonly Vector3[][] UndergroundTunnelPaths =
        {
            new[] { new Vector3(-3f,.02f,-1.1f), new Vector3(-3.22f,.018f,-1.3f), new Vector3(-3.45f,.016f,-1.52f), new Vector3(-3.68f,.018f,-1.74f), new Vector3(-3.9f,.02f,-1.94f), new Vector3(-4.08f,.019f,-2.1f), new Vector3(-4.2f,.02f,-2.2f) },
            new[] { new Vector3(-2.8f,.018f,1.7f), new Vector3(-3.04f,.015f,1.9f), new Vector3(-3.29f,.014f,2.13f), new Vector3(-3.55f,.016f,2.38f), new Vector3(-3.8f,.018f,2.62f), new Vector3(-4.02f,.017f,2.83f), new Vector3(-4.2f,.018f,3f) },
            new[] { new Vector3(3f,.018f,-.1f), new Vector3(3.18f,.017f,-.18f), new Vector3(3.36f,.016f,-.27f), new Vector3(3.54f,.018f,-.35f), new Vector3(3.74f,.02f,-.42f), new Vector3(3.94f,.019f,-.47f), new Vector3(4.1f,.018f,-.5f) },
            new[] { new Vector3(0,.018f,3f), new Vector3(.06f,.025f,3.34f), new Vector3(.1f,.038f,3.7f), new Vector3(.06f,.055f,4.05f), new Vector3(-.04f,.08f,4.4f), new Vector3(-.08f,.12f,4.72f), new Vector3(0,.18f,5f) },
            new[] { new Vector3(6.6f,.018f,-3.15f), new Vector3(6.52f,.02f,-3.35f), new Vector3(6.48f,.022f,-3.55f), new Vector3(6.5f,.024f,-3.75f), new Vector3(6.56f,.022f,-3.96f), new Vector3(6.61f,.02f,-4.17f), new Vector3(6.6f,.018f,-4.35f) },
            new[] { new Vector3(7.3f,.018f,2.25f), new Vector3(7.316f,.02f,2.275f), new Vector3(7.333f,.022f,2.3f), new Vector3(7.35f,.024f,2.325f), new Vector3(7.366f,.022f,2.35f), new Vector3(7.383f,.02f,2.375f), new Vector3(7.4f,.018f,2.4f) },
            new[] { new Vector3(3.55f,.018f,-6.8f), new Vector3(3.22f,.02f,-6.88f), new Vector3(2.88f,.022f,-6.93f), new Vector3(2.52f,.024f,-6.94f), new Vector3(2.16f,.022f,-6.89f), new Vector3(1.8f,.02f,-6.79f), new Vector3(1.45f,.018f,-6.7f) },
            new[] { new Vector3(-2.8f,.018f,-5.4f), new Vector3(-3.1f,.02f,-5.22f), new Vector3(-3.42f,.024f,-5.03f), new Vector3(-3.73f,.026f,-4.83f), new Vector3(-4.03f,.024f,-4.62f), new Vector3(-4.33f,.02f,-4.42f), new Vector3(-4.6f,.018f,-4.25f) },
            new[] { new Vector3(-7f,.018f,-.4f), new Vector3(-7.08f,.02f,-.2f), new Vector3(-7.12f,.022f,.02f), new Vector3(-7.1f,.024f,.24f), new Vector3(-7.04f,.022f,.46f), new Vector3(-6.99f,.02f,.67f), new Vector3(-7f,.018f,.85f) },
            new[] { new Vector3(-4.05f,.018f,4.15f), new Vector3(-3.82f,.02f,4.42f), new Vector3(-3.58f,.023f,4.72f), new Vector3(-3.34f,.028f,5.03f), new Vector3(-3.1f,.034f,5.36f), new Vector3(-2.85f,.042f,5.72f), new Vector3(-2.6f,.055f,6.05f) },
            new[] { new Vector3(6f,.018f,6.7f), new Vector3(5.98f,.02f,6.86f), new Vector3(5.96f,.022f,7.02f), new Vector3(5.93f,.024f,7.18f), new Vector3(5.9f,.022f,7.32f), new Vector3(5.86f,.02f,7.44f), new Vector3(5.8f,.018f,7.55f) },
            new[] { new Vector3(1.3f,.018f,8.2f), new Vector3(1.54f,.02f,8.23f), new Vector3(1.8f,.022f,8.23f), new Vector3(2.06f,.024f,8.19f), new Vector3(2.3f,.022f,8.12f), new Vector3(2.54f,.02f,8.02f), new Vector3(2.75f,.018f,7.9f) }
        };
        static readonly float[] UndergroundTunnelRadii = { 1.45f, 1.4f, 1.75f, 1.8f, 1.45f, 1.45f, 1.2f, 1.25f, 1.2f, 1.4f, 1.25f, 1.45f };
        static readonly float[] UndergroundTunnelHeights = { 2.35f, 2.25f, 2.6f, 2.65f, 2.3f, 2.3f, 2.05f, 2.1f, 2.05f, 2.25f, 2.1f, 2.3f };
        static readonly bool[] UndergroundTunnelBusy = { true, true, true, true, true, true, false, false, false, true, false, true };
        static readonly string[] UndergroundTunnelNames =
        {
            "Queen gallery main tunnel", "Food storage main tunnel",
            "Nursery main two-way tunnel", "Main sloped entrance tunnel",
            "Nursery to egg gallery tunnel", "Nursery to pupa gallery tunnel",
            "Egg gallery sanitation side tunnel", "Sanitation to queen side tunnel",
            "Queen to food service tunnel", "Food to entrance alternate tunnel",
            "Pupa gallery to guard post side tunnel", "Guard post to entrance tunnel"
        };
        static readonly int[] UndergroundTunnelVariants = { 11, 12, 13, 14, 21, 22, 23, 24, 25, 26, 27, 28 };
        static readonly string[] UndergroundChamberNames =
        {
            "Central colony crossroads", "Queen chamber and royal brood",
            "Food and seed storage chamber", "Great nursery chamber",
            "Defensive entrance vestibule", "Egg incubation gallery",
            "Larva and pupa gallery", "Sanitation and refuse chamber",
            "Entrance guard and worker chamber"
        };
        static readonly float[][] UndergroundChamberPortals =
        {
            new[] { -154f, 154f, -9f, 90f }, new[] { 12f, -31f, 90f },
            new[] { -10f, -90f, 12f }, new[] { 178f, -103f, 88f },
            new[] { -90f, -154f, 12f }, new[] { 90f, 180f },
            new[] { -90f, 124f }, new[] { 0f, 143f }, new[] { -30f, -169f }
        };
        static readonly float[][] UndergroundChamberPortalHalfAngles =
        {
            new[] { 32f, 32f, 40f, 40f }, new[] { 36f, 34f, 34f },
            new[] { 38f, 34f, 38f }, new[] { 40f, 36f, 36f },
            new[] { 40f, 36f, 36f }, new[] { 36f, 34f },
            new[] { 36f, 34f }, new[] { 34f, 34f }, new[] { 34f, 36f }
        };

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
        public Vector3 UndergroundPlayerSpawn => UndergroundCenter +
            UndergroundChamberCenters[NurseryChamberIndex] + new Vector3(-.75f, .035f, .25f);
        public Vector3 UndergroundEntrySpawn => UndergroundCenter +
            UndergroundChamberCenters[EntranceChamberIndex] + new Vector3(0, .035f, -1.15f);
        public Vector3 UndergroundSquadBay => UndergroundCenter +
            UndergroundChamberCenters[NurseryChamberIndex] + new Vector3(-1.55f, .035f, 1.35f);
        public Vector3 PlayerRespawn => IsUnderground ? UndergroundPlayerSpawn : SurfacePlayerSpawn;

        public Vector3 ConstrainCameraPosition(Vector3 position)
        {
            if (IsUnderground)
            {
                Vector3 local = position - UndergroundCenter;
                local = ConstrainUndergroundHorizontal(local, .2f);
                float ceiling = UndergroundCeilingAt(local);
                local.y = Mathf.Clamp(local.y, .2f, Mathf.Max(.24f, ceiling - .21f));
                position = UndergroundCenter + local;
                return position;
            }

            // The gameplay camera uses a 0.19 m collision sphere.  Keep its
            // centre farther than that from the sampled ground/floor so the
            // near plane cannot start inside a bank between height samples.
            position.y = Mathf.Max(position.y, CameraSurfaceHeight(position.x, position.z) + .28f);
            return position;
        }

        public Vector3 ConstrainCameraPosition(Vector3 position, Vector3 playerPosition)
        {
            if (!IsUnderground) return ConstrainCameraPosition(position);
            Vector3 playerLocal = playerPosition - UndergroundCenter;
            Vector2 playerPoint = new(playerLocal.x, playerLocal.z);
            int pathIndex = -1;
            float playerDistance = float.MaxValue;
            for (int candidatePath = 0; candidatePath < UndergroundTunnelPaths.Length; candidatePath++)
            {
                Vector3[] candidate = UndergroundTunnelPaths[candidatePath];
                for (int segment = 0; segment < candidate.Length - 1; segment++)
                {
                    float distance = DistanceToSegment(playerPoint,
                        new Vector2(candidate[segment].x, candidate[segment].z),
                        new Vector2(candidate[segment + 1].x, candidate[segment + 1].z));
                    if (distance >= playerDistance) continue;
                    playerDistance = distance;
                    pathIndex = candidatePath;
                }
            }
            if (pathIndex < 0 || playerDistance > UndergroundTunnelRadii[pathIndex] + .28f)
                return ConstrainCameraPosition(position);

            Vector3 local = position - UndergroundCenter;
            Vector2 wantedPoint = new(local.x, local.z);
            Vector3[] path = UndergroundTunnelPaths[pathIndex];
            Vector2 closest = wantedPoint;
            float closestFloor = 0;
            float best = float.MaxValue;
            for (int segment = 0; segment < path.Length - 1; segment++)
            {
                Vector2 a = new(path[segment].x, path[segment].z);
                Vector2 b = new(path[segment + 1].x, path[segment + 1].z);
                Vector2 delta = b - a;
                float t = delta.sqrMagnitude > .000001f
                    ? Mathf.Clamp01(Vector2.Dot(wantedPoint - a, delta) / delta.sqrMagnitude)
                    : 0;
                Vector2 candidate = Vector2.Lerp(a, b, t);
                float distance = (candidate - wantedPoint).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                closest = candidate;
                closestFloor = Mathf.Lerp(path[segment].y, path[segment + 1].y, t);
            }

            float safeRadius = Mathf.Max(.42f,
                UndergroundTunnelRadii[pathIndex] - CameraCollisionRadius - .045f);
            Vector2 lateral = wantedPoint - closest;
            if (lateral.sqrMagnitude > safeRadius * safeRadius)
                wantedPoint = closest + lateral.normalized * safeRadius;
            local.x = wantedPoint.x;
            local.z = wantedPoint.y;
            float lateralDistance = Vector2.Distance(wantedPoint, closest);
            float ceiling = closestFloor + UndergroundTunnelHeights[pathIndex] *
                Mathf.Sqrt(Mathf.Clamp01(1f - lateralDistance * lateralDistance /
                    (UndergroundTunnelRadii[pathIndex] * UndergroundTunnelRadii[pathIndex])));
            local.y = Mathf.Clamp(local.y,
                closestFloor + CameraCollisionRadius + .025f,
                Mathf.Max(closestFloor + CameraCollisionRadius + .035f,
                    ceiling - CameraCollisionRadius - .035f));
            return UndergroundCenter + local;
        }

        float CameraSurfaceHeight(float x, float z)
        {
            float sampledHeight = GroundHeight(x, z);
            if (!layeredTerrainCollider) return sampledHeight;
            var ray = new Ray(new Vector3(x, 16f, z), Vector3.down);
            if (layeredTerrainCollider.Raycast(ray, out RaycastHit hit, 40f))
                sampledHeight = hit.point.y;
            return sampledHeight;
        }

        public bool IsPlayerPositionValid(PlayerAnt player, Vector3 position)
        {
            if (!player) return false;
            if (IsUnderground)
            {
                Vector3 local = position - UndergroundCenter;
                float margin = player.Body ? player.Body.radius + .035f : .27f;
                if (local.y < -.08f || local.y > 1.02f ||
                    !InsideUndergroundHorizontal(local, margin))
                    return false;
                float ceiling = UndergroundCeilingAt(local);
                if (local.y + (player.Body ? player.Body.height : .68f) > ceiling - .025f)
                    return false;
            }
            else
            {
                if (Mathf.Abs(position.x) > 53.5f || Mathf.Abs(position.z) > 53.5f)
                    return false;
                float floor = CameraSurfaceHeight(position.x, position.z);
                if (position.y < floor - .035f || position.y > floor + 3.2f)
                    return false;
            }
            return !player.HasBlockingOverlapAt(position, .012f);
        }

        public bool TryResolvePlayerPosition(
            PlayerAnt player,
            Vector3 requested,
            Vector3 fallback,
            out Vector3 resolved)
        {
            if (!player)
            {
                resolved = requested;
                return false;
            }

            Vector3 Project(Vector3 value)
            {
                if (IsUnderground)
                {
                    Vector3 local = value - UndergroundCenter;
                    local = ConstrainUndergroundHorizontal(
                        local,
                        player.Body.radius + .04f);
                    local.y = Mathf.Clamp(local.y, .025f, .82f);
                    value = UndergroundCenter + local;
                }
                else
                {
                    value.x = Mathf.Clamp(value.x, -53f, 53f);
                    value.z = Mathf.Clamp(value.z, -53f, 53f);
                    value.y = CameraSurfaceHeight(value.x, value.z) + .035f;
                }
                return value;
            }

            requested = Project(requested);
            if (IsPlayerPositionValid(player, requested))
            {
                resolved = requested;
                return true;
            }

            for (int ring = 1; ring <= 7; ring++)
            for (int directionIndex = 0; directionIndex < 16; directionIndex++)
            {
                float angle = directionIndex / 16f * Mathf.PI * 2f;
                Vector3 candidate = requested + new Vector3(
                    Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (.16f * ring);
                candidate = Project(candidate);
                if (!IsPlayerPositionValid(player, candidate)) continue;
                resolved = candidate;
                return true;
            }

            fallback = Project(fallback == Vector3.zero ? PlayerRespawn : fallback);
            if (IsPlayerPositionValid(player, fallback))
            {
                resolved = fallback;
                return true;
            }

            resolved = Project(PlayerRespawn);
            return IsPlayerPositionValid(player, resolved);
        }

        public Vector3 ConstrainActorPosition(Vector3 position, float margin = .19f)
        {
            if (IsUnderground)
            {
                Vector3 local = position - UndergroundCenter;
                local = ConstrainUndergroundHorizontal(local, margin);
                local.y = .035f;
                return UndergroundCenter + local;
            }
            position.x = Mathf.Clamp(position.x, -53f, 53f);
            position.z = Mathf.Clamp(position.z, -53f, 53f);
            position.y = CameraSurfaceHeight(position.x, position.z) + .035f;
            return position;
        }

        public bool TryGetUndergroundPassageFrame(
            Vector3 worldPosition,
            out Vector3 centerline,
            out Vector3 forward,
            out Vector3 side,
            out float halfWidth,
            out float height,
            out bool busy)
        {
            centerline = worldPosition;
            forward = Vector3.forward;
            side = Vector3.right;
            halfWidth = 0;
            height = 0;
            busy = false;
            if (!IsUnderground) return false;

            Vector3 local = worldPosition - UndergroundCenter;
            Vector2 point = new(local.x, local.z);
            float best = float.MaxValue;
            int bestPath = -1;
            Vector2 bestPoint = point;
            Vector2 bestDirection = Vector2.up;
            for (int pathIndex = 0; pathIndex < UndergroundTunnelPaths.Length; pathIndex++)
            {
                Vector3[] path = UndergroundTunnelPaths[pathIndex];
                for (int segment = 0; segment < path.Length - 1; segment++)
                {
                    Vector2 a = new(path[segment].x, path[segment].z);
                    Vector2 b = new(path[segment + 1].x, path[segment + 1].z);
                    Vector2 closest = ClosestPointOnSegment(point, a, b);
                    float candidate = (closest - point).sqrMagnitude;
                    if (candidate >= best) continue;
                    best = candidate;
                    bestPath = pathIndex;
                    bestPoint = closest;
                    bestDirection = (b - a).sqrMagnitude > .0001f
                        ? (b - a).normalized
                        : Vector2.up;
                }
            }

            if (bestPath < 0 || best > Mathf.Pow(UndergroundTunnelRadii[bestPath] + .28f, 2f))
                return false;
            Vector3 pathPoint = new(bestPoint.x, 0, bestPoint.y);
            centerline = UndergroundCenter + pathPoint;
            forward = new Vector3(bestDirection.x, 0, bestDirection.y).normalized;
            side = Vector3.Cross(Vector3.up, forward).normalized;
            halfWidth = UndergroundTunnelRadii[bestPath];
            height = UndergroundTunnelHeights[bestPath];
            busy = UndergroundTunnelBusy[bestPath];
            return true;
        }

        public float CameraBoomDistanceAt(Vector3 playerPosition, float requested)
        {
            if (!TryGetUndergroundPassageFrame(
                    playerPosition, out _, out _, out _, out float halfWidth,
                    out float passageHeight, out bool busy))
                return requested;
            float wallLimited = Mathf.Max(1.18f, halfWidth * 1.42f);
            float heightLimited = Mathf.Max(1.18f, passageHeight * .94f);
            return Mathf.Min(requested, Mathf.Min(wallLimited, heightLimited) + (busy ? .12f : 0));
        }

        public static bool ValidateNestPassageSpecifications(out string report)
        {
            var failures = new List<string>();
            if (UndergroundTunnelPaths.Length != UndergroundTunnelRadii.Length ||
                UndergroundTunnelPaths.Length != UndergroundTunnelHeights.Length ||
                UndergroundTunnelPaths.Length != UndergroundTunnelBusy.Length ||
                UndergroundTunnelPaths.Length != UndergroundTunnelNames.Length ||
                UndergroundTunnelPaths.Length != UndergroundTunnelVariants.Length)
                failures.Add("tunnel specification arrays have different lengths");
            if (UndergroundChamberCenters.Length != UndergroundChamberRadii.Length ||
                UndergroundChamberCenters.Length != UndergroundChamberNames.Length ||
                UndergroundChamberCenters.Length != UndergroundChamberPortals.Length ||
                UndergroundChamberCenters.Length != UndergroundChamberPortalHalfAngles.Length)
                failures.Add("chamber specification arrays have different lengths");
            float tightestWidth = float.MaxValue;
            float tightestHeight = float.MaxValue;
            for (int i = 0; i < UndergroundTunnelPaths.Length; i++)
            {
                float width = UndergroundTunnelRadii[i] * 2f;
                tightestWidth = Mathf.Min(tightestWidth, width);
                tightestHeight = Mathf.Min(tightestHeight, UndergroundTunnelHeights[i]);
                float requiredWidth = UndergroundTunnelBusy[i]
                    ? MinimumBusyTunnelWidth
                    : MinimumNormalTunnelWidth;
                if (width + .0001f < requiredWidth)
                    failures.Add($"{UndergroundTunnelNames[i]} width {width:F2} < {requiredWidth:F2}");
                if (UndergroundTunnelHeights[i] < PlayerColliderHeight + CameraCollisionRadius * 2f + .28f)
                    failures.Add($"{UndergroundTunnelNames[i]} height {UndergroundTunnelHeights[i]:F2}");
                if (UndergroundTunnelPaths[i].Length < 7)
                    failures.Add($"{UndergroundTunnelNames[i]} has angular low-resolution path");
                for (int segment = 1; segment < UndergroundTunnelPaths[i].Length - 1; segment++)
                {
                    Vector3 incoming = UndergroundTunnelPaths[i][segment] - UndergroundTunnelPaths[i][segment - 1];
                    Vector3 outgoing = UndergroundTunnelPaths[i][segment + 1] - UndergroundTunnelPaths[i][segment];
                    if (Vector3.Angle(incoming, outgoing) > 36f)
                        failures.Add($"{UndergroundTunnelNames[i]} sharp corner {segment}");
                }
            }
            for (int i = 0; i < UndergroundChamberCenters.Length; i++)
            {
                Vector3 radii = UndergroundChamberRadii[i];
                if (radii.x < 2.65f || radii.z < 2.4f || radii.y < 2.35f)
                    failures.Add($"{UndergroundChamberNames[i]} is below explorable chamber minimum");
                if (UndergroundChamberPortals[i].Length !=
                    UndergroundChamberPortalHalfAngles[i].Length)
                    failures.Add($"{UndergroundChamberNames[i]} portal metadata mismatch");
            }
            if (UndergroundChamberRadii[NurseryChamberIndex].x < 4f ||
                UndergroundChamberRadii[NurseryChamberIndex].z < 3.4f)
                failures.Add("great nursery does not meet free-roaming size");
            report = $"chambers={UndergroundChamberCenters.Length} " +
                     $"tunnels={UndergroundTunnelPaths.Length} minWidth={tightestWidth:F2} " +
                     $"minHeight={tightestHeight:F2} playerDiameter={PlayerColliderRadius * 2f:F2} " +
                     $"playerHeight={PlayerColliderHeight:F2} busyMinimum={MinimumBusyTunnelWidth:F2} " +
                     $"failures={failures.Count}" +
                     (failures.Count > 0 ? " details=" + string.Join("; ", failures) : string.Empty);
            return failures.Count == 0;
        }

        static bool InsideUndergroundHorizontal(Vector3 local, float margin)
        {
            Vector2 point = new(local.x, local.z);
            for (int i = 0; i < UndergroundChamberCenters.Length; i++)
            {
                Vector3 center = UndergroundChamberCenters[i];
                Vector3 radii = UndergroundChamberRadii[i];
                float rx = Mathf.Max(.2f, radii.x - margin);
                float rz = Mathf.Max(.2f, radii.z - margin);
                float x = (point.x - center.x) / rx;
                float z = (point.y - center.z) / rz;
                if (x * x + z * z <= 1f) return true;
            }

            for (int pathIndex = 0; pathIndex < UndergroundTunnelPaths.Length; pathIndex++)
            {
                Vector3[] path = UndergroundTunnelPaths[pathIndex];
                float radius = Mathf.Max(.16f, UndergroundTunnelRadii[pathIndex] - margin);
                for (int segment = 0; segment < path.Length - 1; segment++)
                    if (DistanceToSegment(point,
                            new Vector2(path[segment].x, path[segment].z),
                            new Vector2(path[segment + 1].x, path[segment + 1].z)) <= radius)
                        return true;
            }
            return false;
        }

        static Vector3 ConstrainUndergroundHorizontal(Vector3 local, float margin)
        {
            if (InsideUndergroundHorizontal(local, margin)) return local;
            Vector2 point = new(local.x, local.z);
            Vector2 best = point;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < UndergroundChamberCenters.Length; i++)
            {
                Vector3 center3 = UndergroundChamberCenters[i];
                Vector2 center = new(center3.x, center3.z);
                Vector3 radii3 = UndergroundChamberRadii[i];
                Vector2 radii = new(
                    Mathf.Max(.2f, radii3.x - margin),
                    Mathf.Max(.2f, radii3.z - margin));
                Vector2 delta = point - center;
                float normalized = Mathf.Sqrt(
                    delta.x * delta.x / (radii.x * radii.x) +
                    delta.y * delta.y / (radii.y * radii.y));
                Vector2 candidate = normalized <= 1f || normalized < .0001f
                    ? point
                    : center + delta / normalized;
                float distance = (candidate - point).sqrMagnitude;
                if (distance < bestDistance) { bestDistance = distance; best = candidate; }
            }

            for (int pathIndex = 0; pathIndex < UndergroundTunnelPaths.Length; pathIndex++)
            {
                Vector3[] path = UndergroundTunnelPaths[pathIndex];
                float radius = Mathf.Max(.16f, UndergroundTunnelRadii[pathIndex] - margin);
                for (int segment = 0; segment < path.Length - 1; segment++)
                {
                    Vector2 closest = ClosestPointOnSegment(point,
                        new Vector2(path[segment].x, path[segment].z),
                        new Vector2(path[segment + 1].x, path[segment + 1].z));
                    Vector2 offset = point - closest;
                    Vector2 candidate = offset.sqrMagnitude <= radius * radius
                        ? point
                        : closest + offset.normalized * radius;
                    float distance = (candidate - point).sqrMagnitude;
                    if (distance < bestDistance) { bestDistance = distance; best = candidate; }
                }
            }
            local.x = best.x;
            local.z = best.y;
            return local;
        }

        static float UndergroundCeilingAt(Vector3 local)
        {
            Vector2 point = new(local.x, local.z);
            float ceiling = .72f;
            for (int i = 0; i < UndergroundChamberCenters.Length; i++)
            {
                Vector3 center = UndergroundChamberCenters[i];
                Vector3 radii = UndergroundChamberRadii[i];
                float x = (point.x - center.x) / radii.x;
                float z = (point.y - center.z) / radii.z;
                float radial = x * x + z * z;
                if (radial <= 1f)
                    ceiling = Mathf.Max(ceiling, radii.y * Mathf.Sqrt(1f - radial));
            }
            for (int pathIndex = 0; pathIndex < UndergroundTunnelPaths.Length; pathIndex++)
            {
                Vector3[] path = UndergroundTunnelPaths[pathIndex];
                float radius = UndergroundTunnelRadii[pathIndex];
                for (int segment = 0; segment < path.Length - 1; segment++)
                {
                    float distance = DistanceToSegment(point,
                        new Vector2(path[segment].x, path[segment].z),
                        new Vector2(path[segment + 1].x, path[segment + 1].z));
                    if (distance <= radius)
                        ceiling = Mathf.Max(ceiling,
                            UndergroundTunnelHeights[pathIndex] *
                            Mathf.Sqrt(Mathf.Clamp01(1f - distance * distance / (radius * radius))));
                }
            }
            return ceiling;
        }

        static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
            => Vector2.Distance(point, ClosestPointOnSegment(point, a, b));

        static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 segment = b - a;
            float length = segment.sqrMagnitude;
            if (length <= .000001f) return a;
            return a + segment * Mathf.Clamp01(Vector2.Dot(point - a, segment) / length);
        }

        readonly List<ResourceNode> resources = new();
        readonly List<Creature> creatures = new();
        readonly List<Renderer> surfaceRenderers = new();
        readonly List<Renderer> undergroundRenderers = new();
        SquadController squads;
        Transform environment;
        Transform underground;
        Collider layeredTerrainCollider;
        Transform rivalColony;
        GameObject nestUpgrade;
        GameObject undergroundUpgrade;
        GameObject largeThreat;
        Light sunLight;
        Light skyFillLight;
        Light amberNestLight;
        Light tunnelFillLight;
        Light nurseryFillLight;
        readonly List<Light> undergroundGuideLights = new();
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
        string collisionQaCaption;

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
            return height + HeroMicroDisplacement(x, z);
        }

        static float HeroMicroDisplacement(float x, float z)
        {
            float dx = x - HeroMicrohabitatCenter.x;
            float dz = z - HeroMicrohabitatCenter.y;
            float distance = Mathf.Sqrt(dx * dx / 36f + dz * dz / 25f);
            float mask = 1f - Mathf.SmoothStep(.68f, 1f, distance);
            if (mask <= 0) return 0;

            float clods = (Mathf.PerlinNoise(x * .92f + 27f, z * .92f + 16f) - .5f) * .19f;
            float grains = (Mathf.PerlinNoise(x * 2.65f + 4f, z * 2.65f + 39f) - .5f) * .055f;
            float rootBank = Mathf.Exp(-Mathf.Pow((dz - 2.25f - Mathf.Sin(dx * .52f) * .32f) / .72f, 2f)) * .17f;
            float puddle = Mathf.Clamp01(1f - Vector2.Distance(
                new Vector2(x, z),
                HeroMicrohabitatCenter + new Vector2(-3.25f, -.85f)) / 1.35f);
            return (clods + grains + rootBank - puddle * .17f) * mask;
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
                             "-environment-slice-qa",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginEnvironmentSliceQa());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-environment-video-qa",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginEnvironmentVideoQa());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-environment-profile-qa",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginEnvironmentProfileQa());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-world-assets-qa",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginWorldAssetsQa());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-physical-world-qa",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginPhysicalWorldQa());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-physical-world-video-qa",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginPhysicalWorldVideoQa());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-camera-containment-smoke",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginCameraContainmentSmoke());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-tunnel-clearance-qa",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginTunnelClearanceQa());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-tunnel-clearance-video-qa",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginTunnelClearanceVideoQa());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-nest-home-qa",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginNestHomeQa());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-collision-safety-qa",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginCollisionSafetyQa());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-collision-safety-video-qa",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginCollisionSafetyVideoQa());
            else if (System.Array.Exists(
                         arguments,
                         argument => string.Equals(
                             argument,
                             "-environment-traversal-smoke",
                             System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(BeginEnvironmentTraversalSmoke());
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

        IEnumerator BeginEnvironmentSliceQa()
        {
            IsAutomationSmoke = true;
            yield return null;
            BeginPlay();
            Mission.Restore(MissionDirector.SpiderStep);
            IsUnderground = false;
            RefreshWorldForMission();
            ApplyLocationLighting();

            Vector3 playerPosition = At(
                HeroMicrohabitatCenter.x,
                HeroMicrohabitatCenter.y,
                .06f);
            Player.Teleport(playerPosition);
            Player.Face(playerPosition + Vector3.forward);
            squads.enabled = false;
            foreach (SquadUnit unit in FindObjectsByType<SquadUnit>(FindObjectsSortMode.None))
                unit.gameObject.SetActive(false);
            foreach (Creature creature in creatures)
                if (creature) creature.gameObject.SetActive(false);
            AntVisual playerVisual = Player.GetComponentInChildren<AntVisual>(true);
            playerVisual.SetPlayerMotion(1.45f, .32f, true,
                QaGroundNormal(playerPosition.x, playerPosition.z));
            IsCinematic = true;
            yield return new WaitForSecondsRealtime(.8f);

            Vector3 focus = playerPosition + Vector3.up * .4f;
            SetQaCamera(focus, new Vector3(2.8f, .45f, .12f), 43f);
            yield return CaptureQaScreenshot("environment-080-player-side-close.tga");
            SetQaCamera(focus + new Vector3(-.35f, .18f, 1.15f),
                new Vector3(5.5f, 2.45f, -5.7f), 48f);
            yield return CaptureQaScreenshot("environment-080-hero-wide.tga");
            SetQaCamera(focus + new Vector3(-1.55f, -.12f, .25f),
                new Vector3(1.75f, .72f, -2.2f), 41f);
            yield return CaptureQaScreenshot("environment-080-ground-detail.tga");
            SetQaCamera(At(11.2f, 15.55f, .72f),
                new Vector3(1.5f, .85f, -2.2f), 37f);
            yield return CaptureQaScreenshot("environment-080-veined-grass.tga");
            SetQaCamera(At(9.55f, 18.15f, .42f),
                new Vector3(3.2f, 1.25f, -3.15f), 42f);
            yield return CaptureQaScreenshot("environment-080-roots-moss-stones.tga");
            SetQaCamera(At(5.85f, 15.25f, .18f),
                new Vector3(.35f, 2.65f, -1.15f), 35f);
            yield return CaptureQaScreenshot("environment-080-puddle-leaves.tga");
            SetQaCamera(At(8.08f, 15.35f, .14f),
                new Vector3(1.25f, .86f, -1.5f), 32f);
            yield return CaptureQaScreenshot("environment-080-dead-leaf-detail.tga");

            yield return new WaitForSecondsRealtime(1.1f);
            SetQaCamera(At(11.2f, 15.55f, .72f),
                new Vector3(1.5f, .85f, -2.2f), 37f);
            yield return CaptureQaScreenshot("environment-080-veined-grass-wind.tga");
            Debug.Log(
                "MOONROOT_ENVIRONMENT_SLICE_QA_OK screenshots=8 " +
                "ground=PBR-blended grass=solid-geometry roots=collidable puddle=physical");
            if (!Application.isEditor)
                Application.Quit(0);
        }

        IEnumerator BeginEnvironmentVideoQa()
        {
            IsAutomationSmoke = true;
            yield return null;
            BeginPlay();
            Mission.Restore(MissionDirector.SpiderStep);
            IsUnderground = false;
            RefreshWorldForMission();
            ApplyLocationLighting();
            squads.enabled = false;
            foreach (SquadUnit unit in FindObjectsByType<SquadUnit>(FindObjectsSortMode.None))
                unit.gameObject.SetActive(false);
            foreach (Creature creature in creatures)
                if (creature) creature.gameObject.SetActive(false);

            string projectRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "..", ".."));
            string directory = Path.Combine(
                projectRoot,
                "QA",
                "VideoFrames",
                "environment-070-contact");
            Directory.CreateDirectory(directory);
            foreach (string oldFrame in Directory.GetFiles(directory, "frame-*.tga"))
                File.Delete(oldFrame);

            const int frames = 90;
            const float frameRate = 15f;
            Vector3 start = At(8.55f, 15.45f, .06f);
            Vector3 end = At(11.75f, 15.35f, .06f);
            AntVisual visual = Player.GetComponentInChildren<AntVisual>(true);
            IsCinematic = true;
            for (int frame = 0; frame < frames; frame++)
            {
                float t = frame / (float)(frames - 1);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                Vector3 position = Vector3.Lerp(start, end, eased);
                position.z += Mathf.Sin(t * Mathf.PI * 2f) * .22f;
                position.y = GroundHeight(position.x, position.z) + .06f;
                Vector3 next = position + new Vector3(.25f, 0, Mathf.Cos(t * Mathf.PI * 2f) * .08f);
                Player.Teleport(position);
                Player.Face(next);
                visual.SetPlayerMotion(2.15f, .54f, true,
                    QaGroundNormal(position.x, position.z));
                Vector3 focus = position + Vector3.up * .42f;
                SetQaCamera(focus, new Vector3(2.6f, 1.02f, -3.15f), 44f);
                yield return new WaitForSecondsRealtime(1f / frameRate);
                yield return new WaitForEndOfFrame();
                WriteQaTga(
                    Path.Combine(directory, $"frame-{frame:D4}.tga"),
                    960,
                    540);
            }

            Debug.Log(
                $"MOONROOT_ENVIRONMENT_VIDEO_QA_OK frames={frames} fps={frameRate} " +
                $"directory={directory}");
            if (!Application.isEditor)
                Application.Quit(0);
        }

        IEnumerator BeginEnvironmentProfileQa()
        {
            IsAutomationSmoke = true;
            yield return null;
            BeginPlay();
            Mission.Restore(MissionDirector.SpiderStep);
            IsUnderground = false;
            RefreshWorldForMission();
            ApplyLocationLighting();
            Vector3 position = At(9.1f, 16.1f, .06f);
            Player.Teleport(position);
            Player.Face(position + Vector3.forward);
            yield return new WaitForSecondsRealtime(27f);
            Debug.Log("MOONROOT_ENVIRONMENT_PROFILE_QA_OK seconds=27");
            if (!Application.isEditor)
                Application.Quit(0);
        }

        IEnumerator BeginWorldAssetsQa()
        {
            IsAutomationSmoke = true;
            yield return null;
            BeginPlay();
            Mission.Restore(MissionDirector.QueenBriefingStep);
            IsUnderground = true;
            RefreshWorldForMission();
            ApplyLocationLighting();
            squads.enabled = false;
            foreach (SquadUnit unit in FindObjectsByType<SquadUnit>(FindObjectsSortMode.None))
                unit.gameObject.SetActive(false);
            IsCinematic = true;
            yield return new WaitForSecondsRealtime(.8f);

            Vector3 queenFocus = UndergroundCenter +
                UndergroundChamberCenters[QueenChamberIndex] + Vector3.up * .55f;
            SetQaCamera(queenFocus, new Vector3(3.2f, 1.18f, 3.35f), 43f);
            yield return CaptureQaScreenshot("world-080-queen-chamber.tga");
            SetQaCamera(queenFocus + new Vector3(.35f, -.14f, .55f),
                new Vector3(1.25f, .64f, 1.42f), 34f);
            yield return CaptureQaScreenshot("world-080-brood-detail.tga");

            Vector3 storageFocus = UndergroundCenter +
                UndergroundChamberCenters[FoodChamberIndex] + Vector3.up * .38f;
            SetQaCamera(storageFocus, new Vector3(1.45f, .72f, 1.8f), 35f);
            yield return CaptureQaScreenshot("world-080-storage-cargo.tga");
            SetQaCamera(UndergroundCenter + UndergroundChamberCenters[CentralChamberIndex] +
                Vector3.up * .72f,
                new Vector3(-3.7f, 1.45f, -3.45f), 49f);
            yield return CaptureQaScreenshot("world-080-colony-wide.tga");
            SetQaCamera(UndergroundCenter + UndergroundChamberCenters[EntranceChamberIndex] +
                new Vector3(0, .78f, -1.75f),
                new Vector3(2.15f, 1.1f, -2.35f), 38f);
            yield return CaptureQaScreenshot("world-080-tunnel-entrance.tga");

            Debug.Log(
                "MOONROOT_WORLD_ASSET_QA_OK screenshots=5 " +
                "brood=egg-larva-pupa cargo=seed-resin-protein nest=modeled-chambers-tunnels");
            if (!Application.isEditor)
                Application.Quit(0);
        }

        IEnumerator BeginPhysicalWorldQa()
        {
            IsAutomationSmoke = true;
            yield return null;
            BeginPlay();
            Mission.Restore(MissionDirector.SpiderStep);
            IsUnderground = false;
            RefreshWorldForMission();
            ApplyLocationLighting();
            squads.enabled = false;
            foreach (SquadUnit unit in FindObjectsByType<SquadUnit>(FindObjectsSortMode.None))
                unit.gameObject.SetActive(false);
            foreach (Creature creature in creatures)
                if (creature) creature.gameObject.SetActive(false);
            IsCinematic = true;
            yield return new WaitForSecondsRealtime(.8f);

            Renderer[] renderers = environment.GetComponentsInChildren<Renderer>(true);
            MeshFilter[] filters = environment.GetComponentsInChildren<MeshFilter>(true);
            int forbiddenBackdropCount = renderers.Count(renderer =>
                renderer.name.IndexOf("photographic", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                renderer.name.IndexOf("backdrop", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                (renderer.sharedMaterial && renderer.sharedMaterial.shader &&
                 renderer.sharedMaterial.shader.name.IndexOf(
                     "ForestBackdrop", System.StringComparison.OrdinalIgnoreCase) >= 0));
            int modeledTrees = filters.Count(filter =>
                filter.name == "Irregular modeled trunk" && filter.sharedMesh);
            int solidGrass = filters.Count(filter =>
                filter.sharedMesh && filter.sharedMesh.name.IndexOf(
                    "Solid curved", System.StringComparison.OrdinalIgnoreCase) >= 0);
            int solidLeaves = filters.Count(filter =>
                filter.sharedMesh && filter.sharedMesh.name.IndexOf(
                    "Solid", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                filter.sharedMesh.name.IndexOf(
                    "leaf", System.StringComparison.OrdinalIgnoreCase) >= 0);
            int transparentVegetation = renderers.Count(renderer =>
                renderer.name.IndexOf("foliage", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                renderer.name.IndexOf("leaf", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                renderer.name.IndexOf("botanical", System.StringComparison.OrdinalIgnoreCase) >= 0
                    ? renderer.sharedMaterial &&
                      (renderer.sharedMaterial.renderQueue >= 2450 ||
                       renderer.sharedMaterial.shader.name.IndexOf(
                           "Transparent", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                       renderer.sharedMaterial.shader.name.IndexOf(
                           "Cutout", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    : false);

            Vector3 forestFocus = At(8.9f, 16.1f, .42f);
            SetQaCamera(forestFocus, new Vector3(6.4f, 2.75f, -7.2f), 49f);
            yield return CaptureQaScreenshot("physical-090-forest-no-photo-wall.png");
            yield return CaptureQaWireframeScreenshot("physical-090-forest-wireframe.png");
            SetQaCamera(At(11.2f, 15.55f, .58f), new Vector3(1.45f, .74f, -2.05f), 36f);
            yield return CaptureQaWireframeScreenshot("physical-090-solid-grass-wireframe.png");
            SetQaCamera(At(6.4f, 14.5f, .48f), new Vector3(1.35f, .82f, -1.9f), 34f);
            yield return CaptureQaWireframeScreenshot("physical-090-solid-plants-wireframe.png");
            Transform nearestTree = environment.GetComponentsInChildren<Transform>(true)
                .Where(candidate => candidate.name.StartsWith("Modeled forest tree"))
                .OrderBy(candidate => Vector3.Distance(candidate.position, forestFocus))
                .FirstOrDefault();
            if (nearestTree)
            {
                SetQaCamera(nearestTree.position + Vector3.up * 2.2f,
                    new Vector3(6.2f, 2.6f, -6.4f), 45f);
                yield return CaptureQaWireframeScreenshot(
                    "physical-090-tree-roots-wireframe.png");
            }

            IsUnderground = true;
            RefreshWorldForMission();
            ApplyLocationLighting();
            Player.Teleport(UndergroundPlayerSpawn);
            Vector3 queenFocus = UndergroundCenter +
                UndergroundChamberCenters[QueenChamberIndex] + Vector3.up * .72f;
            SetQaCamera(queenFocus, new Vector3(1.05f, .52f, .8f), 46f);
            yield return CaptureQaScreenshot("physical-090-queen-chamber-gameplay.png");
            yield return CaptureQaWireframeScreenshot("physical-090-queen-chamber-wireframe.png");
            SetQaCamera(UndergroundCenter + new Vector3(-1.32f, .48f, -.48f),
                new Vector3(.62f, .08f, .48f), 52f);
            yield return CaptureQaWireframeScreenshot("physical-090-nest-tunnel-wireframe.png");
            SetQaCamera(UndergroundCenter + UndergroundChamberCenters[QueenChamberIndex] +
                new Vector3(0, .45f, .2f),
                new Vector3(1.02f, .44f, .72f), 43f);
            yield return CaptureQaWireframeScreenshot("physical-090-resources-brood-wireframe.png");

            int chamberShells = filters.Count(filter =>
                filter.sharedMesh && filter.sharedMesh.name.StartsWith(
                    "Organic closed chamber shell"));
            int tunnelShells = filters.Count(filter =>
                filter.sharedMesh && filter.sharedMesh.name.StartsWith(
                    "Curved excavated tunnel shell"));
            bool passed = forbiddenBackdropCount == 0 && modeledTrees >= 16 &&
                          solidGrass > 0 && solidLeaves > 0 &&
                          transparentVegetation == 0 && chamberShells >= 5 &&
                          tunnelShells >= 4;
            string result =
                $"backdrops={forbiddenBackdropCount} modeledTrees={modeledTrees} " +
                $"solidGrassMeshes={solidGrass} solidLeafMeshes={solidLeaves} " +
                $"transparentVegetation={transparentVegetation} chamberShells={chamberShells} " +
                $"tunnelShells={tunnelShells}";
            if (passed)
                Debug.Log("MOONROOT_PHYSICAL_WORLD_QA_OK " + result);
            else
                Debug.LogError("MOONROOT_PHYSICAL_WORLD_QA_FAILED " + result);
            if (!Application.isEditor)
                Application.Quit(passed ? 0 : 2);
        }

        IEnumerator BeginPhysicalWorldVideoQa()
        {
            IsAutomationSmoke = true;
            yield return null;
            BeginPlay();
            Mission.Restore(MissionDirector.SpiderStep);
            IsUnderground = false;
            RefreshWorldForMission();
            ApplyLocationLighting();
            squads.enabled = false;
            foreach (SquadUnit unit in FindObjectsByType<SquadUnit>(FindObjectsSortMode.None))
                unit.gameObject.SetActive(false);
            foreach (Creature creature in creatures)
                if (creature) creature.gameObject.SetActive(false);

            string projectRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "..", ".."));
            string directory = Path.Combine(
                projectRoot, "QA", "VideoFrames", "physical-090-walkthrough");
            Directory.CreateDirectory(directory);
            foreach (string oldFrame in Directory.GetFiles(directory, "frame-*.tga"))
                File.Delete(oldFrame);

            const float frameRate = 15f;
            const int surfaceFrames = 105;
            const int nestFrames = 75;
            const int exitFrames = 30;
            const int totalFrames = surfaceFrames + nestFrames + exitFrames;
            int frameNumber = 0;
            AntVisual visual = Player.GetComponentInChildren<AntVisual>(true);
            IsCinematic = true;

            Vector3 surfaceStart = At(5.55f, 14.1f, .06f);
            Vector3 surfaceEnd = At(12.25f, 17.05f, .06f);
            for (int frame = 0; frame < surfaceFrames; frame++)
            {
                float t = frame / (float)(surfaceFrames - 1);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                Vector3 position = Vector3.Lerp(surfaceStart, surfaceEnd, eased);
                position.z += Mathf.Sin(t * Mathf.PI * 2f) * .72f;
                position.y = GroundHeight(position.x, position.z) + .06f;
                Vector3 next = position + new Vector3(.3f, 0, .13f +
                    Mathf.Cos(t * Mathf.PI * 2f) * .16f);
                Player.Teleport(position);
                Player.Face(next);
                visual.SetPlayerMotion(2.35f, .62f, true,
                    QaGroundNormal(position.x, position.z));
                Player.SnapCamera();
                Camera.main.fieldOfView = 44f;
                yield return CapturePhysicalVideoFrame(directory, frameNumber++, frameRate);
            }

            ToggleNest(Player, false);
            yield return new WaitForSecondsRealtime(.35f);
            Vector3 nestStart = UndergroundPlayerSpawn;
            Vector3 nestEnd = UndergroundCenter + new Vector3(-3.1f, .08f, -1.55f);
            for (int frame = 0; frame < nestFrames; frame++)
            {
                float t = frame / (float)(nestFrames - 1);
                Vector3 position = Vector3.Lerp(nestStart, nestEnd,
                    Mathf.SmoothStep(0f, 1f, t));
                position.y = UndergroundCenter.y + .09f + Mathf.Sin(t * Mathf.PI) * .025f;
                Vector3 next = Vector3.Lerp(position, nestEnd, .18f) + Vector3.forward * .08f;
                Player.Teleport(position);
                Player.Face(next, 5f);
                visual.SetPlayerMotion(1.7f, .46f, true, Vector3.up);
                Player.SnapCamera();
                Camera.main.fieldOfView = 46f;
                yield return CapturePhysicalVideoFrame(directory, frameNumber++, frameRate);
            }

            ToggleNest(Player, true);
            for (int frame = 0; frame < exitFrames; frame++)
            {
                float t = frame / (float)(exitFrames - 1);
                Vector3 position = Player.transform.position;
                float angle = Mathf.Lerp(-28f, 18f, t) * Mathf.Deg2Rad;
                Player.Face(position + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)));
                Player.SnapCamera();
                Camera.main.fieldOfView = 44f;
                visual.SetPlayerMotion(0f, 0f, true,
                    QaGroundNormal(position.x, position.z));
                yield return CapturePhysicalVideoFrame(directory, frameNumber++, frameRate);
            }

            Debug.Log(
                $"MOONROOT_PHYSICAL_WORLD_VIDEO_QA_OK frames={totalFrames} " +
                $"fps={frameRate} surface={surfaceFrames} nest={nestFrames} " +
                $"exit={exitFrames} directory={directory}");
            if (!Application.isEditor)
                Application.Quit(0);
        }

        static IEnumerator CapturePhysicalVideoFrame(
            string directory,
            int frame,
            float frameRate)
        {
            yield return new WaitForSecondsRealtime(1f / frameRate);
            yield return new WaitForEndOfFrame();
            WriteQaTga(Path.Combine(directory, $"frame-{frame:D4}.tga"), 960, 540);
        }

        IEnumerator BeginCameraContainmentSmoke()
        {
            IsAutomationSmoke = true;
            yield return null;
            BeginPlay();
            Mission.Restore(MissionDirector.SpiderStep);
            int samples = 0;
            int failures = 0;
            int solidOverlaps = 0;
            int tooClose = 0;
            Camera camera = Camera.main;

            IsUnderground = false;
            RefreshWorldForMission();
            ApplyLocationLighting();
            Vector3[] surfacePoints =
            {
                SurfacePlayerSpawn,
                At(0, -1.8f, .06f),
                At(7.2f, 14.8f, .06f),
                At(9.1f, 16.1f, .06f),
                At(11.6f, 18.1f, .06f),
                At(-8.2f, 7.5f, .06f)
            };
            for (int pointIndex = 0; pointIndex < surfacePoints.Length; pointIndex++)
            for (int directionIndex = 0; directionIndex < 8; directionIndex++)
            {
                Vector3 point = surfacePoints[pointIndex];
                float angle = directionIndex / 8f * Mathf.PI * 2f;
                Player.Teleport(point);
                Player.Face(point + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)));
                camera.transform.position = point - Vector3.up * 3f;
                Player.SnapCamera();
                yield return null;
                samples++;
                float minimum = CameraSurfaceHeight(
                    camera.transform.position.x,
                    camera.transform.position.z) + .16f;
                if (camera.transform.position.y < minimum) failures++;
                if (Vector3.Distance(
                        camera.transform.position,
                        Player.transform.position + Vector3.up * .38f) < .82f)
                {
                    tooClose++;
                    failures++;
                }
                if (CameraOverlapsSolid(camera.transform.position, .15f))
                {
                    solidOverlaps++;
                    failures++;
                }
            }

            ToggleNest(Player, false);
            for (int directionIndex = 0; directionIndex < 12; directionIndex++)
            {
                float angle = directionIndex / 12f * Mathf.PI * 2f;
                Player.Face(Player.transform.position +
                            new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)));
                camera.transform.position = UndergroundCenter - Vector3.up * 3f;
                Player.SnapCamera();
                yield return null;
                samples++;
                float localY = camera.transform.position.y - UndergroundCenter.y;
                if (localY < .16f || localY > 1.84f) failures++;
                if (Vector3.Distance(
                        camera.transform.position,
                        Player.transform.position + Vector3.up * .38f) < .82f)
                {
                    tooClose++;
                    failures++;
                }
                if (CameraOverlapsSolid(camera.transform.position, .15f))
                {
                    solidOverlaps++;
                    failures++;
                }
            }

            ToggleNest(Player, true);
            yield return null;
            samples++;
            float transitionMinimum = CameraSurfaceHeight(
                camera.transform.position.x,
                camera.transform.position.z) + .16f;
            if (camera.transform.position.y < transitionMinimum) failures++;
            if (Vector3.Distance(
                    camera.transform.position,
                    Player.transform.position + Vector3.up * .38f) < .82f)
            {
                tooClose++;
                failures++;
            }
            if (CameraOverlapsSolid(camera.transform.position, .15f))
            {
                solidOverlaps++;
                failures++;
            }

            string result =
                $"samples={samples} failures={failures} solidOverlaps={solidOverlaps} " +
                $"tooClose={tooClose} finalLocation=" +
                (IsUnderground ? "underground" : "surface") +
                $" camera={camera.transform.position:F3}";
            if (failures == 0 && !IsUnderground)
                Debug.Log("MOONROOT_CAMERA_CONTAINMENT_OK " + result);
            else
                Debug.LogError("MOONROOT_CAMERA_CONTAINMENT_FAILED " + result);
            if (!Application.isEditor)
                Application.Quit(failures == 0 && !IsUnderground ? 0 : 2);
        }

        IEnumerator BeginTunnelClearanceQa()
        {
            IsAutomationSmoke = true;
            yield return null;
            BeginPlay();
            Mission.Restore(MissionDirector.SpiderStep);
            RefreshWorldForMission();
            IsUnderground = true;
            ApplyLocationLighting();

            int tests = 0;
            int failures = 0;
            void Check(bool passed, string name, string detail = "")
            {
                tests++;
                if (passed) Debug.Log($"MOONROOT_TUNNEL_CASE_OK {name} {detail}");
                else
                {
                    failures++;
                    Debug.LogError($"MOONROOT_TUNNEL_CASE_FAILED {name} {detail}");
                }
            }

            bool specificationValid = ValidateNestPassageSpecifications(out string specificationReport);
            Check(specificationValid, "single-source-clearance-specification", specificationReport);
            TunnelClearanceMarker[] markers = underground.GetComponentsInChildren<TunnelClearanceMarker>(true);
            Check(markers.Length == UndergroundTunnelPaths.Length,
                "every-playable-tunnel-has-runtime-clearance-marker", $"markers={markers.Length}");
            Check(markers.All(marker => marker.FloorCollider && marker.ShellCollider),
                "every-tunnel-has-continuous-floor-and-shell-collider");
            Check(markers.All(marker => marker.ClearWidth + .001f >=
                    (marker.IsBusyRoute ? MinimumBusyTunnelWidth : MinimumNormalTunnelWidth)),
                "normal-and-busy-width-minimums");
            Check(markers.All(marker => marker.ShellCollider.sharedMaterial == null ||
                    marker.ShellCollider.enabled),
                "no-disabled-or-orphaned-tunnel-collider");
            Transform safetyEnvelope = underground.Find("Closed watertight underground safety envelope");
            Check(safetyEnvelope && safetyEnvelope.GetComponent<MeshRenderer>() &&
                  safetyEnvelope.GetComponent<MeshFilter>()?.sharedMesh,
                "closed-watertight-underground-backstop-present");

            squads.enabled = false;
            Vector3[] entrancePath = UndergroundTunnelPaths[EntranceTunnelIndex];
            Vector3 entranceStart = UndergroundCenter + entrancePath[0] + Vector3.up * .035f;
            Vector3 entranceEnd = UndergroundCenter + entrancePath[^1] + Vector3.up * .035f;
            Player.Teleport(entranceStart);
            bool safeTraversal = true;
            for (int pointIndex = 1; pointIndex < entrancePath.Length; pointIndex++)
            {
                Vector3 target = UndergroundCenter + entrancePath[pointIndex] + Vector3.up * .035f;
                for (int step = 0; step < 30; step++)
                {
                    Vector3 toward = target - Player.transform.position;
                    toward.y = 0;
                    if (Vector3.ProjectOnPlane(toward, Vector3.up).sqrMagnitude < .025f) break;
                    Player.MoveForQa(toward, 2.3f, .035f);
                    Physics.SyncTransforms();
                    safeTraversal &= IsPlayerPositionValid(Player, Player.transform.position) &&
                                     !Player.HasBlockingOverlapAt(Player.transform.position, .016f);
                }
            }
            Debug.Log($"MOONROOT_TUNNEL_ENTRY_DIAGNOSTIC position={Player.transform.position:F3} " +
                      $"target={entranceEnd:F3} " +
                      Player.CollisionProbeForQa(entranceEnd - Player.transform.position, 1.25f) + " " +
                      Player.OverlapProbeForQa(Player.transform.position));
            Check(safeTraversal && Vector3.Distance(Player.transform.position, entranceEnd) < .72f,
                "player-enters-main-passage-without-snags",
                $"remaining={Vector3.Distance(Player.transform.position, entranceEnd):F2}");

            Vector3 turnPosition = Player.transform.position;
            int clearTurnSamples = 0;
            float cameraTravel = 0;
            Vector3 previousCamera = Camera.main.transform.position;
            for (int directionIndex = 0; directionIndex < 16; directionIndex++)
            {
                Player.SetCameraOrbitForQa(directionIndex * 22.5f, 18f + Mathf.Sin(directionIndex) * 8f);
                Physics.SyncTransforms();
                Vector3 cameraPosition = Camera.main.transform.position;
                cameraTravel += Vector3.Distance(previousCamera, cameraPosition);
                previousCamera = cameraPosition;
                if (!CameraOverlapsSolid(cameraPosition, CameraCollisionRadius)) clearTurnSamples++;
            }
            Check(Vector3.Distance(turnPosition, Player.transform.position) < .001f && cameraTravel > 2f,
                "camera-rotates-independently-at-entrance-dead-end", $"travel={cameraTravel:F2}");
            Check(clearTurnSamples == 16, "camera-never-enters-wall-or-ceiling-during-360-turn",
                $"clear={clearTurnSamples}/16");

            for (int pointIndex = entrancePath.Length - 2; pointIndex >= 0; pointIndex--)
            {
                Vector3 target = UndergroundCenter + entrancePath[pointIndex] + Vector3.up * .035f;
                for (int step = 0; step < 30; step++)
                {
                    Vector3 toward = target - Player.transform.position;
                    toward.y = 0;
                    if (Vector3.ProjectOnPlane(toward, Vector3.up).sqrMagnitude < .025f) break;
                    Player.MoveForQa(toward, 2.3f, .035f);
                    Physics.SyncTransforms();
                }
            }
            Debug.Log($"MOONROOT_TUNNEL_EXIT_DIAGNOSTIC position={Player.transform.position:F3} " +
                      $"target={entranceStart:F3} " +
                      Player.CollisionProbeForQa(entranceStart - Player.transform.position, 1.25f) + " " +
                      Player.OverlapProbeForQa(Player.transform.position));
            Check(Vector3.Distance(Player.transform.position, entranceStart) < .72f &&
                  IsPlayerPositionValid(Player, Player.transform.position),
                "player-turns-and-leaves-main-passage",
                $"remaining={Vector3.Distance(Player.transform.position, entranceStart):F2}");

            SquadUnit[] allUnits = FindObjectsByType<SquadUnit>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (SquadUnit unit in allUnits) unit.gameObject.SetActive(true);
            squads.enabled = true;
            Vector3[] busyPath = UndergroundTunnelPaths[2];
            Vector3 pathA = UndergroundCenter + busyPath[1] + Vector3.up * .035f;
            Vector3 pathB = UndergroundCenter + busyPath[^2] + Vector3.up * .035f;
            bool pairReady = squads.BeginOpposingPassForQa(pathA, pathB, pathB, pathA,
                out SquadUnit first, out SquadUnit second);
            float pairMinimum = float.MaxValue;
            for (int frame = 0; pairReady && frame < 180; frame++)
            {
                pairMinimum = Mathf.Min(pairMinimum,
                    Vector3.Distance(first.transform.position, second.transform.position));
                yield return null;
            }
            Vector3 passageDirection = Vector3.ProjectOnPlane(pathB - pathA, Vector3.up).normalized;
            bool crossed = pairReady &&
                           Vector3.Dot(first.transform.position - pathA, passageDirection) > .72f &&
                           Vector3.Dot(second.transform.position - pathB, -passageDirection) > .72f;
            Check(crossed, "two-ants-pass-in-opposite-directions", $"minimumSeparation={pairMinimum:F2}");
            Check(pairReady && pairMinimum > .22f, "npc-local-separation-prevents-visible-overlap",
                $"minimum={pairMinimum:F2}");
            squads.EndOpposingPassForQa();

            bool escapeSafe = allUnits.All(unit => unit.BodyCollider && unit.BodyCollider.isTrigger);
            Check(escapeSafe, "npc-colliders-cannot-form-inescapable-player-plug");
            Player.Teleport(pathA);
            squads.BeginOpposingPassForQa(pathA + passageDirection * .42f, pathB,
                pathB - passageDirection * .42f, pathA, out _, out _);
            bool playerSafeInTraffic = true;
            for (int frame = 0; frame < 150; frame++)
            {
                Player.MoveForQa(pathB - Player.transform.position, 2.15f, 1f / 60f);
                Physics.SyncTransforms();
                playerSafeInTraffic &= IsPlayerPositionValid(Player, Player.transform.position) &&
                                       !Player.HasBlockingOverlapAt(Player.transform.position, .016f);
                yield return null;
            }
            Check(playerSafeInTraffic && Vector3.Dot(Player.transform.position - pathA, passageDirection) > .75f,
                "player-crosses-active-two-way-npc-traffic");
            squads.EndOpposingPassForQa();

            Player.Teleport(UndergroundCenter + entrancePath[3] + Vector3.up * .035f);
            Player.SetCameraOrbitForQa(90f, 14f);
            bool sideClear = !CameraOverlapsSolid(Camera.main.transform.position, CameraCollisionRadius);
            Player.SetCameraOrbitForQa(270f, 34f);
            bool ceilingClear = !CameraOverlapsSolid(Camera.main.transform.position, CameraCollisionRadius);
            Check(sideClear && ceilingClear, "camera-pushes-inward-at-sidewall-and-ceiling");

            yield return CaptureQaScreenshot("tunnel-092-main-entrance-clearance.png");
            yield return CaptureQaWireframeScreenshot("tunnel-092-collider-wireframe.png");
            Check(IsPlayerPositionValid(Player, Player.transform.position),
                "final-player-position-valid-after-all-tunnel-tests");

            string result = $"tests={tests} failures={failures} oldWidths=1.84-2.70 " +
                            $"newWidths={UndergroundTunnelRadii.Min() * 2f:F2}-{UndergroundTunnelRadii.Max() * 2f:F2} " +
                            $"newHeights={UndergroundTunnelHeights.Min():F2}-{UndergroundTunnelHeights.Max():F2} " +
                            $"playerRadius={Player.Body.radius:F2} playerHeight={Player.Body.height:F2} " +
                            $"cameraRadius={CameraCollisionRadius:F2}";
            if (failures == 0) Debug.Log("MOONROOT_TUNNEL_CLEARANCE_QA_OK " + result);
            else Debug.LogError("MOONROOT_TUNNEL_CLEARANCE_QA_FAILED " + result);
            if (!Application.isEditor) Application.Quit(failures == 0 ? 0 : 2);
        }

        IEnumerator BeginTunnelClearanceVideoQa()
        {
            IsAutomationSmoke = true;
            yield return null;
            BeginPlay();
            Mission.Restore(MissionDirector.SpiderStep);
            RefreshWorldForMission();
            IsUnderground = true;
            ApplyLocationLighting();
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
            string directory = Path.Combine(projectRoot, "QA", "VideoFrames", "tunnel-092-proof");
            Directory.CreateDirectory(directory);
            foreach (string oldFrame in Directory.GetFiles(directory, "frame-*.tga")) File.Delete(oldFrame);
            const float frameRate = 15f;
            int frameNumber = 0;
            Vector3[] entrancePath = UndergroundTunnelPaths[EntranceTunnelIndex];
            Vector3 entranceStart = UndergroundCenter + entrancePath[0] + Vector3.up * .035f;
            Vector3 entranceEnd = UndergroundCenter + entrancePath[^1] + Vector3.up * .035f;

            squads.enabled = false;
            Player.Teleport(entranceStart);
            Player.Face(entranceEnd, 15f);
            collisionQaCaption = "1/4  ENTER, TURN, LEAVE — 2.70 m main tunnel";
            for (int frame = 0; frame < 72; frame++)
            {
                Vector3 target = frame < 36 ? entranceEnd : entranceStart;
                Vector3 toward = target - Player.transform.position;
                toward.y = 0;
                Player.MoveForQa(toward, 1.65f, 1f / frameRate);
                Player.SetCameraOrbitForQa(8f + frame * 2.6f, 18f);
                yield return CapturePhysicalVideoFrame(directory, frameNumber++, frameRate);
            }

            squads.enabled = true;
            Vector3[] busyPath = UndergroundTunnelPaths[2];
            Vector3 pathA = UndergroundCenter + busyPath[1] + Vector3.up * .035f;
            Vector3 pathB = UndergroundCenter + busyPath[^2] + Vector3.up * .035f;
            squads.BeginOpposingPassForQa(pathA, pathB, pathB, pathA, out _, out _);
            Player.enabled = false;
            collisionQaCaption = "2/4  TWO-WAY TRAFFIC — workers use separate lanes";
            SetQaCamera((pathA + pathB) * .5f + Vector3.up * .32f,
                new Vector3(1.9f, 1.25f, -1.85f), 47f);
            for (int frame = 0; frame < 60; frame++)
                yield return CapturePhysicalVideoFrame(directory, frameNumber++, frameRate);
            squads.EndOpposingPassForQa();
            Player.enabled = true;

            Player.Teleport(UndergroundCenter + entrancePath[3] + Vector3.up * .035f);
            Player.MoveForQa(Vector3.right, 3.2f, .4f);
            collisionQaCaption = "3/4  PLAYER BLOCKED — camera still rotates independently";
            for (int frame = 0; frame < 60; frame++)
            {
                Player.SetCameraOrbitForQa(90f + frame * 5.2f, 10f + Mathf.PingPong(frame * .7f, 22f));
                yield return CapturePhysicalVideoFrame(directory, frameNumber++, frameRate);
            }

            collisionQaCaption = "4/4  CAMERA SURFACE TEST — sidewalls and ceiling contain boom";
            for (int frame = 0; frame < 60; frame++)
            {
                Player.SetCameraOrbitForQa(frame * 6f, Mathf.Lerp(7f, 38f, Mathf.PingPong(frame / 30f, 1f)));
                yield return CapturePhysicalVideoFrame(directory, frameNumber++, frameRate);
            }

            collisionQaCaption = null;
            bool safe = IsPlayerPositionValid(Player, Player.transform.position) &&
                        !CameraOverlapsSolid(Camera.main.transform.position, CameraCollisionRadius);
            Debug.Log($"MOONROOT_TUNNEL_CLEARANCE_VIDEO_QA_{(safe ? "OK" : "FAILED")} " +
                      $"frames={frameNumber} fps={frameRate} chapters=4 directory={directory}");
            if (!Application.isEditor) Application.Quit(safe ? 0 : 2);
        }

        IEnumerator BeginNestHomeQa()
        {
            IsAutomationSmoke = true;
            yield return null;
            BeginPlay();
            Mission.Restore(MissionDirector.SpiderStep);
            RefreshWorldForMission();
            IsUnderground = true;
            ApplyLocationLighting();

            int tests = 0;
            int failures = 0;
            void Check(bool passed, string name, string detail = "")
            {
                tests++;
                if (passed) Debug.Log($"MOONROOT_NEST_HOME_CASE_OK {name} {detail}");
                else
                {
                    failures++;
                    Debug.LogError($"MOONROOT_NEST_HOME_CASE_FAILED {name} {detail}");
                }
            }

            NestChamberMarker[] chamberMarkers =
                underground.GetComponentsInChildren<NestChamberMarker>(true);
            TunnelClearanceMarker[] tunnelMarkers =
                underground.GetComponentsInChildren<TunnelClearanceMarker>(true);
            Check(chamberMarkers.Length == UndergroundChamberCenters.Length,
                "all-explorable-chambers-built",
                $"live={chamberMarkers.Length} specified={UndergroundChamberCenters.Length}");
            Check(tunnelMarkers.Length == UndergroundTunnelPaths.Length,
                "all-main-and-side-tunnels-built",
                $"live={tunnelMarkers.Length} specified={UndergroundTunnelPaths.Length}");
            Check(chamberMarkers.All(marker => marker.FloorCollider && marker.ShellCollider),
                "every-chamber-has-floor-wall-and-ceiling-collision");
            Check(tunnelMarkers.All(marker => marker.FloorCollider && marker.ShellCollider),
                "every-tunnel-has-continuous-collision");
            Check(chamberMarkers[NurseryChamberIndex].ClearRadii.x >= 4f &&
                  chamberMarkers[NurseryChamberIndex].ClearRadii.z >= 3.4f,
                "nursery-free-roaming-envelope",
                $"radii={chamberMarkers[NurseryChamberIndex].ClearRadii:F2}");
            Check(tunnelMarkers.Min(marker => marker.ClearWidth) >= MinimumNormalTunnelWidth,
                "minimum-tunnel-width-relative-to-player",
                $"minimum={tunnelMarkers.Min(marker => marker.ClearWidth):F2} " +
                $"playerDiameter={Player.Body.radius * 2f:F2}");
            Check(tunnelMarkers.Min(marker => marker.ClearHeight) >= 2.05f,
                "minimum-tunnel-height-relative-to-player-and-camera",
                $"minimum={tunnelMarkers.Min(marker => marker.ClearHeight):F2} " +
                $"playerHeight={Player.Body.height:F2}");

            (string name, Vector3[] localRoute)[] routes =
            {
                ("entrance-to-central-crossroads", ComposeNestRoute(
                    UndergroundChamberCenters[EntranceChamberIndex],
                    UndergroundChamberCenters[CentralChamberIndex],
                    (EntranceTunnelIndex, true))),
                ("central-to-great-nursery", ComposeNestRoute(
                    UndergroundChamberCenters[CentralChamberIndex],
                    UndergroundChamberCenters[NurseryChamberIndex],
                    (2, false))),
                ("complete-loop-around-nursery", new[]
                {
                    UndergroundChamberCenters[NurseryChamberIndex],
                    UndergroundChamberCenters[NurseryChamberIndex] + new Vector3(-2.6f,0,-1.8f),
                    UndergroundChamberCenters[NurseryChamberIndex] + new Vector3(0,0,-2.75f),
                    UndergroundChamberCenters[NurseryChamberIndex] + new Vector3(2.65f,0,-1.75f),
                    UndergroundChamberCenters[NurseryChamberIndex] + new Vector3(3.15f,0,.45f),
                    UndergroundChamberCenters[NurseryChamberIndex] + new Vector3(2.2f,0,2.35f),
                    UndergroundChamberCenters[NurseryChamberIndex] + new Vector3(-.35f,0,2.7f),
                    UndergroundChamberCenters[NurseryChamberIndex] + new Vector3(-2.75f,0,1.65f),
                    UndergroundChamberCenters[NurseryChamberIndex]
                }),
                ("nursery-to-egg-gallery", ComposeNestRoute(
                    UndergroundChamberCenters[NurseryChamberIndex],
                    UndergroundChamberCenters[EggChamberIndex], (4, false))),
                ("egg-gallery-to-sanitation", ComposeNestRoute(
                    UndergroundChamberCenters[EggChamberIndex],
                    UndergroundChamberCenters[SanitationChamberIndex], (6, false))),
                ("sanitation-to-queen", ComposeNestRoute(
                    UndergroundChamberCenters[SanitationChamberIndex],
                    UndergroundChamberCenters[QueenChamberIndex], (7, false))),
                ("queen-to-food-storage", ComposeNestRoute(
                    UndergroundChamberCenters[QueenChamberIndex],
                    UndergroundChamberCenters[FoodChamberIndex], (8, false))),
                ("food-storage-to-entrance-alternate", ComposeNestRoute(
                    UndergroundChamberCenters[FoodChamberIndex],
                    UndergroundChamberCenters[EntranceChamberIndex], (9, false))),
                ("entrance-to-guard-chamber", ComposeNestRoute(
                    UndergroundChamberCenters[EntranceChamberIndex],
                    UndergroundChamberCenters[GuardChamberIndex], (11, true))),
                ("guard-to-pupa-gallery", ComposeNestRoute(
                    UndergroundChamberCenters[GuardChamberIndex],
                    UndergroundChamberCenters[PupaChamberIndex], (10, true))),
                ("pupa-gallery-to-nursery", ComposeNestRoute(
                    UndergroundChamberCenters[PupaChamberIndex],
                    UndergroundChamberCenters[NurseryChamberIndex], (5, true))),
                ("nursery-back-to-central", ComposeNestRoute(
                    UndergroundChamberCenters[NurseryChamberIndex],
                    UndergroundChamberCenters[CentralChamberIndex], (2, true))),
                ("central-back-to-exit", ComposeNestRoute(
                    UndergroundChamberCenters[CentralChamberIndex],
                    UndergroundChamberCenters[EntranceChamberIndex],
                    (EntranceTunnelIndex, false)))
            };

            squads.enabled = false;
            foreach (SquadUnit unit in FindObjectsByType<SquadUnit>(FindObjectsSortMode.None))
                unit.gameObject.SetActive(false);
            Player.Teleport(UndergroundEntrySpawn);
            bool remainedValid = true;
            int totalWaypoints = 0;
            for (int routeIndex = 0; routeIndex < routes.Length; routeIndex++)
            {
                (string routeName, Vector3[] localRoute) = routes[routeIndex];
                bool reachedRouteEnd = true;
                Vector3 lastTarget = Player.transform.position;
                foreach (Vector3 localTarget in localRoute)
                {
                    totalWaypoints++;
                    Vector3 target = UndergroundCenter + localTarget + Vector3.up * .035f;
                    lastTarget = target;
                    int steps = 0;
                    while (Vector3.Distance(
                               Vector3.ProjectOnPlane(Player.transform.position, Vector3.up),
                               Vector3.ProjectOnPlane(target, Vector3.up)) > .2f && steps++ < 150)
                    {
                        Vector3 toward = target - Player.transform.position;
                        toward.y = 0;
                        Player.MoveForQa(toward, 3.05f, 1f / 45f);
                        Physics.SyncTransforms();
                        remainedValid &= IsPlayerPositionValid(Player, Player.transform.position) &&
                                         !Player.HasBlockingOverlapAt(Player.transform.position, .016f);
                        if ((steps & 3) == 0) yield return null;
                    }
                    if (steps >= 150) reachedRouteEnd = false;
                }
                Player.SetCameraOrbitForQa(routeIndex * 47f, 12f + routeIndex % 3 * 7f);
                bool cameraClear = !CameraOverlapsSolid(
                    Camera.main.transform.position, CameraCollisionRadius);
                Check(reachedRouteEnd && remainedValid && cameraClear,
                    routeName,
                    $"player={Player.transform.position:F2} target={lastTarget:F2} " +
                    $"cameraClear={cameraClear} " +
                    Player.CollisionProbeForQa(lastTarget - Player.transform.position, 1.1f) + " " +
                    Player.OverlapProbeForQa(Player.transform.position));
            }
            Check(remainedValid, "no-hidden-blockers-or-player-penetration-on-complete-tour",
                $"waypoints={totalWaypoints} recoveries={Player.AntiStuckRecoveries}");

            Player.Teleport(UndergroundCenter +
                UndergroundChamberCenters[NurseryChamberIndex] + Vector3.up * .035f);
            int clearCameraSamples = 0;
            float cameraTravel = 0;
            Vector3 lastCamera = Camera.main.transform.position;
            for (int sample = 0; sample < 24; sample++)
            {
                Player.SetCameraOrbitForQa(sample * 15f, 8f + Mathf.PingPong(sample * 3f, 26f));
                Vector3 cameraPosition = Camera.main.transform.position;
                cameraTravel += Vector3.Distance(lastCamera, cameraPosition);
                lastCamera = cameraPosition;
                if (!CameraOverlapsSolid(cameraPosition, CameraCollisionRadius)) clearCameraSamples++;
            }
            Check(clearCameraSamples == 24 && cameraTravel > 4f,
                "nursery-camera-free-and-contained",
                $"clear={clearCameraSamples}/24 travel={cameraTravel:F2}");

            NestWorkerRoutine[] workers =
                underground.GetComponentsInChildren<NestWorkerRoutine>(true);
            Check(workers.Length >= 12, "living-colony-worker-population",
                $"workers={workers.Length}");
            Check(workers.All(worker => worker.RoutePointCount >= 2),
                "every-worker-has-a-real-room-to-room-route");
            Check(workers.All(worker =>
                    worker.GetComponent<Collider>() && worker.GetComponent<Collider>().isTrigger),
                "ambient-workers-cannot-form-solid-player-plug");
            Check(workers.Count(worker => worker.Load != NestWorkerLoad.None) >= 8,
                "workers-visibly-carry-brood-food-and-refuse");

            float[] workerTravelStarts = workers
                .Select(worker => worker.TotalTravelDistance)
                .ToArray();
            float trafficTestStarted = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - trafficTestStarted < 3.25f)
                yield return null;
            int movingWorkers = 0;
            for (int i = 0; i < workers.Length; i++)
                if (workers[i].TotalTravelDistance - workerTravelStarts[i] > .35f)
                    movingWorkers++;
            Check(movingWorkers >= Mathf.Min(9, workers.Length),
                "colony-traffic-moves-between-rooms",
                $"moving={movingWorkers}/{workers.Length}");

            yield return CaptureQaScreenshot("nest-093-great-nursery-gameplay.png");
            yield return CaptureQaWireframeScreenshot("nest-093-great-nursery-collision.png");
            Player.Teleport(UndergroundEntrySpawn);
            bool beforeExitValid = IsPlayerPositionValid(Player, Player.transform.position);
            ToggleNest(Player, true);
            yield return null;
            Check(beforeExitValid && !IsUnderground &&
                  IsPlayerPositionValid(Player, Player.transform.position),
                "complete-tour-returns-outside-through-real-transition");

            string result = $"tests={tests} failures={failures} chambers={chamberMarkers.Length} " +
                            $"tunnels={tunnelMarkers.Length} workers={workers.Length} " +
                            $"movingWorkers={movingWorkers} waypoints={totalWaypoints} " +
                            $"width={tunnelMarkers.Min(marker => marker.ClearWidth):F2}-" +
                            $"{tunnelMarkers.Max(marker => marker.ClearWidth):F2} " +
                            $"height={tunnelMarkers.Min(marker => marker.ClearHeight):F2}-" +
                            $"{tunnelMarkers.Max(marker => marker.ClearHeight):F2}";
            if (failures == 0) Debug.Log("MOONROOT_NEST_HOME_QA_OK " + result);
            else Debug.LogError("MOONROOT_NEST_HOME_QA_FAILED " + result);
            if (!Application.isEditor) Application.Quit(failures == 0 ? 0 : 2);
        }

        IEnumerator BeginCollisionSafetyQa()
        {
            IsAutomationSmoke = true;
            yield return null;
            BeginPlay();
            Mission.Restore(MissionDirector.SpiderStep);
            squads.enabled = false;
            foreach (SquadUnit unit in FindObjectsByType<SquadUnit>(FindObjectsSortMode.None))
                unit.gameObject.SetActive(false);
            foreach (Creature creature in creatures)
                if (creature) creature.gameObject.SetActive(false);

            int tests = 0;
            int failures = 0;
            void Check(bool passed, string name)
            {
                tests++;
                if (passed) Debug.Log($"MOONROOT_COLLISION_CASE_OK {name}");
                else
                {
                    failures++;
                    Debug.LogError($"MOONROOT_COLLISION_CASE_FAILED {name} position={Player.transform.position:F3}");
                }
            }

            Transform[] all = environment.GetComponentsInChildren<Transform>(true);
            Transform tree = all.FirstOrDefault(candidate =>
                candidate.name.StartsWith("Modeled forest tree"));
            CapsuleCollider trunk = tree
                ? tree.GetComponentsInChildren<CapsuleCollider>(true)
                    .FirstOrDefault(collider => collider.transform.parent &&
                        collider.transform.parent.name == "Irregular modeled trunk")
                : null;
            Transform heroRoot = all.FirstOrDefault(candidate =>
                candidate.name == "Fine feeder root beside the ant path");
            CapsuleCollider root = heroRoot
                ? heroRoot.GetComponentsInChildren<CapsuleCollider>(true).FirstOrDefault()
                : null;
            int solidTrees = all.Count(candidate =>
                candidate.name.StartsWith("Modeled forest tree") &&
                candidate.GetComponentsInChildren<CapsuleCollider>(true).Length >= 6);
            int nestShells = underground.GetComponentsInChildren<MeshCollider>(true).Count(collider =>
                collider.name.Contains("walls and ceiling") || collider.name.Contains("floor"));
            Check(tree && trunk, "tree-trunk-collider-present");
            Check(heroRoot && root && root.GetComponent<SolidWorldGeometry>(),
                "root-compound-collider-present");
            Check(solidTrees == RuntimeQualityProfile.DistantTreeCount(GameSettings.Quality),
                "every-modeled-tree-solid");
            Check(nestShells >= 18, "nest-floors-walls-ceilings-continuous");
            Check(Player.Body && Mathf.Abs(Player.Body.radius - .23f) < .001f &&
                  Mathf.Abs(Player.Body.height - .68f) < .001f &&
                  Player.Body.center == new Vector3(0, .34f, 0),
                "player-collider-dimensions");

            IsUnderground = false;
            RefreshWorldForMission();
            ApplyLocationLighting();
            if (trunk)
            {
                Vector3 away = new Vector3(trunk.bounds.center.x, 0, trunk.bounds.center.z).normalized;
                if (away.sqrMagnitude < .1f) away = Vector3.right;
                Vector3 start = QaOutsideCollider(trunk, away);
                Player.Teleport(start);
                QaPushPlayer(-away, 2.55f, 1f / 60f, 110);
                Check(IsPlayerPositionValid(Player, Player.transform.position) &&
                      !Player.HasBlockingOverlapAt(Player.transform.position, .018f) &&
                      Vector3.Dot(Player.transform.position - trunk.bounds.center, away) > -.08f,
                    "walk-directly-into-tree");

                Player.Teleport(start);
                bool safeAtEveryLowFpsStep = true;
                for (int step = 0; step < 36; step++)
                {
                    Player.MoveForQa(-away, 4.25f, .12f);
                    Physics.SyncTransforms();
                    safeAtEveryLowFpsStep &=
                        IsPlayerPositionValid(Player, Player.transform.position) &&
                        !Player.HasBlockingOverlapAt(Player.transform.position, .018f);
                }
                Check(safeAtEveryLowFpsStep,
                    "low-fps-sprint-into-tree-no-tunneling");

                Vector3 blocked = Player.transform.position;
                Vector3 firstCamera = Vector3.zero;
                float cameraTravel = 0;
                int clearCameraSamples = 0;
                for (int directionIndex = 0; directionIndex < 12; directionIndex++)
                {
                    Player.SetCameraOrbitForQa(directionIndex * 30f, 18f);
                    Physics.SyncTransforms();
                    Vector3 cameraPosition = Camera.main.transform.position;
                    if (directionIndex == 0) firstCamera = cameraPosition;
                    else cameraTravel += Vector3.Distance(firstCamera, cameraPosition);
                    if (!CameraOverlapsSolid(cameraPosition, .19f)) clearCameraSamples++;
                }
                Check(Vector3.Distance(blocked, Player.transform.position) < .001f &&
                      cameraTravel > 1.2f, "camera-rotates-while-tree-blocks-player");
                Check(clearCameraSamples == 12, "camera-never-enters-tree-or-ground");

            }

            Vector3 rootSafePosition = Player.transform.position;
            if (root)
            {
                Vector3 segment = Vector3.ProjectOnPlane(root.transform.up, Vector3.up);
                Vector3 away = Vector3.Cross(Vector3.up,
                    segment.sqrMagnitude > .05f ? segment.normalized : Vector3.forward).normalized;
                Vector3 start = QaOutsideCollider(root, away);
                Player.Teleport(start);
                QaPushPlayer(-away, 2.55f, 1f / 60f, 100);
                rootSafePosition = Player.transform.position;
                Check(IsPlayerPositionValid(Player, rootSafePosition) &&
                      !Player.HasBlockingOverlapAt(rootSafePosition, .018f),
                    "walk-into-root-stops-outside");

                Player.Teleport(start);
                QaPushPlayer((-away + root.transform.up * .72f).normalized, 4.25f, .1f, 42);
                Check(IsPlayerPositionValid(Player, Player.transform.position) &&
                      !Player.HasBlockingOverlapAt(Player.transform.position, .018f),
                    "diagonal-root-contact-slides-without-wedging");

                Player.SetCameraOrbitForQa(35f, 14f);
                Vector3 cameraA = Camera.main.transform.position;
                Player.SetCameraOrbitForQa(145f, 24f);
                Vector3 cameraB = Camera.main.transform.position;
                Check(Vector3.Distance(cameraA, cameraB) > .35f &&
                      !CameraOverlapsSolid(cameraB, .19f),
                    "camera-rotates-while-root-blocks-player");
            }

            Vector3 seamStart = new(
                HeroMicrohabitatCenter.x - 6.35f,
                0,
                HeroMicrohabitatCenter.y);
            seamStart.y = CameraSurfaceHeight(seamStart.x, seamStart.z) + .035f;
            Player.Teleport(seamStart);
            QaPushPlayer(Vector3.right, 4.25f, .1f, 32);
            Check(IsPlayerPositionValid(Player, Player.transform.position) &&
                  Player.transform.position.x > HeroMicrohabitatCenter.x - 4.2f,
                "surface-terrain-seam-low-fps");

            Player.Teleport(rootSafePosition);
            SaveSystem.Delete(7);
            bool rootSaved = SaveSystem.Save(7, this);
            if (trunk) Player.ForceUnsafePositionForQa(trunk.bounds.center);
            bool rootLoaded = SaveSystem.Load(7, this);
            Check(rootSaved && rootLoaded && IsPlayerPositionValid(Player, Player.transform.position),
                "save-near-root-loads-valid-position");

            ToggleNest(Player, false);
            Vector3 queenCenter = UndergroundCenter +
                UndergroundChamberCenters[QueenChamberIndex] + Vector3.up * .035f;
            Vector3 wallDirection = new Vector3(-1f, 0, -.18f).normalized;
            Player.Teleport(queenCenter);
            QaPushPlayer(wallDirection, 2.55f, 1f / 60f, 120);
            Check(IsPlayerPositionValid(Player, Player.transform.position) &&
                  !Player.HasBlockingOverlapAt(Player.transform.position, .018f),
                "walk-directly-into-nest-wall");

            Player.Teleport(queenCenter);
            QaPushPlayer(wallDirection, 4.25f, .12f, 40);
            Check(IsPlayerPositionValid(Player, Player.transform.position) &&
                  !Player.HasBlockingOverlapAt(Player.transform.position, .018f),
                "run-directly-into-nest-wall-low-fps");

            Player.Teleport(queenCenter);
            QaPushPlayer(new Vector3(-1f, 0, -1f), 4.25f, .1f, 44);
            Check(IsPlayerPositionValid(Player, Player.transform.position) &&
                  !Player.HasBlockingOverlapAt(Player.transform.position, .018f),
                "diagonal-nest-corner-no-escape");

            Vector3 invalidCorner = UndergroundCenter +
                UndergroundChamberCenters[QueenChamberIndex] +
                new Vector3(-UndergroundChamberRadii[QueenChamberIndex].x - .35f, .035f, -.4f);
            Player.ForceUnsafePositionForQa(invalidCorner);
            bool beganOutsideNest = !IsPlayerPositionValid(Player, invalidCorner);
            bool recoveredFromCorner = Player.RecoverNowForQa(Vector3.right);
            Check(beganOutsideNest && recoveredFromCorner &&
                  IsPlayerPositionValid(Player, Player.transform.position) &&
                  !Player.HasBlockingOverlapAt(Player.transform.position, .012f),
                "anti-stuck-recovers-from-intentional-nest-corner");

            Vector3 blockedAtWall = Player.transform.position;
            Player.SetCameraOrbitForQa(15f, 12f);
            Vector3 nestCameraA = Camera.main.transform.position;
            Player.SetCameraOrbitForQa(215f, 28f);
            Vector3 nestCameraB = Camera.main.transform.position;
            Check(Vector3.Distance(blockedAtWall, Player.transform.position) < .001f &&
                  Vector3.Distance(nestCameraA, nestCameraB) > .25f,
                "camera-independent-while-nest-wall-blocks-player");
            Check(!CameraOverlapsSolid(nestCameraA, .19f) &&
                  !CameraOverlapsSolid(nestCameraB, .19f),
                "camera-outside-nest-walls-and-ceiling");

            Vector3 tunnelStart = UndergroundCenter + UndergroundTunnelPaths[0][^1] + Vector3.up * .035f;
            Vector3 tunnelEnd = UndergroundCenter + UndergroundTunnelPaths[0][0] + Vector3.up * .035f;
            Player.Teleport(tunnelStart);
            Vector3 beforeTunnel = Player.transform.position;
            Vector3[] tunnelPath = UndergroundTunnelPaths[0];
            for (int pointIndex = tunnelPath.Length - 2; pointIndex >= 0; pointIndex--)
            {
                Vector3 target = UndergroundCenter + tunnelPath[pointIndex] + Vector3.up * .035f;
                for (int step = 0; step < 28; step++)
                {
                    Vector3 toward = target - Player.transform.position;
                    toward.y = 0;
                    if (toward.sqrMagnitude < .06f) break;
                    Player.MoveForQa(toward, 2.2f, .05f);
                    Physics.SyncTransforms();
                }
            }
            Debug.Log(
                $"MOONROOT_TUNNEL_DIAGNOSTIC start={beforeTunnel:F3} " +
                $"end={Player.transform.position:F3} target={tunnelEnd:F3} " +
                $"valid={IsPlayerPositionValid(Player, Player.transform.position)} " +
                Player.CollisionProbeForQa(tunnelEnd - Player.transform.position));
            Check(IsPlayerPositionValid(Player, Player.transform.position) &&
                  Vector3.Distance(beforeTunnel, Player.transform.position) > 1.25f &&
                  Vector3.Distance(Player.transform.position, tunnelEnd) < .9f,
                "narrow-curved-tunnel-traversal");
            Player.SetCameraOrbitForQa(310f, 32f);
            Check(!CameraOverlapsSolid(Camera.main.transform.position, .19f),
                "narrow-tunnel-camera-clearance");

            bool wallSaved = SaveSystem.Save(7, this);
            Player.ForceUnsafePositionForQa(UndergroundCenter + new Vector3(-7f, .05f, -5f));
            bool wallLoaded = SaveSystem.Load(7, this);
            Check(wallSaved && wallLoaded && IsPlayerPositionValid(Player, Player.transform.position),
                "save-beside-wall-loads-inside-nest");

            Camera.main.transform.position = UndergroundCenter + Vector3.up * 4f;
            Player.SnapCamera();
            Check(!CameraOverlapsSolid(Camera.main.transform.position, .19f) &&
                  Camera.main.transform.position.y < UndergroundCenter.y + 1.9f,
                "forced-camera-ceiling-resolves-inside");

            Vector3 pauseCamera = Camera.main.transform.position;
            TogglePause();
            TogglePause();
            Player.SetCameraOrbitForQa(82f, 18f);
            Check(!IsPaused && Vector3.Distance(pauseCamera, Camera.main.transform.position) > .18f,
                "camera-resumes-after-pause");

            for (int transition = 0; transition < 3; transition++)
            {
                ToggleNest(Player, true);
                Check(!IsUnderground && IsPlayerPositionValid(Player, Player.transform.position),
                    $"nest-exit-{transition + 1}-valid");
                ToggleNest(Player, false);
                Check(IsUnderground && IsPlayerPositionValid(Player, Player.transform.position),
                    $"nest-entry-{transition + 1}-valid");
            }

            ToggleNest(Player, true);
            Camera.main.transform.position = Player.transform.position + Vector3.down * 2f;
            Player.SnapCamera();
            float floorHeight = CameraSurfaceHeight(
                Camera.main.transform.position.x,
                Camera.main.transform.position.z);
            Check(Camera.main.transform.position.y >= floorHeight + .19f &&
                  !CameraOverlapsSolid(Camera.main.transform.position, .19f),
                "forced-camera-ground-resolves-above-terrain");

            int squadBodies = FindObjectsByType<SquadUnit>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).Count(unit => unit.BodyCollider != null);
            int solidMarkers = FindObjectsByType<SolidWorldGeometry>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).Length;
            Check(squadBodies >= 8, "npc-ant-colliders-present");
            Check(solidMarkers >= 120, "solid-world-collider-audit-count");
            SaveSystem.Delete(7);

            string result =
                $"tests={tests} failures={failures} solidMarkers={solidMarkers} " +
                $"solidTrees={solidTrees} nestMeshes={nestShells} squadBodies={squadBodies} " +
                $"recoveries={Player.AntiStuckRecoveries} collider=CharacterController " +
                $"radius={Player.Body.radius:F2} height={Player.Body.height:F2} " +
                $"center={Player.Body.center:F2}";
            if (failures == 0)
                Debug.Log("MOONROOT_COLLISION_SAFETY_QA_OK " + result);
            else
                Debug.LogError("MOONROOT_COLLISION_SAFETY_QA_FAILED " + result);
            if (!Application.isEditor)
                Application.Quit(failures == 0 ? 0 : 2);
        }

        Vector3 QaOutsideCollider(Collider collider, Vector3 away)
        {
            away = Vector3.ProjectOnPlane(away, Vector3.up).normalized;
            if (away.sqrMagnitude < .1f) away = Vector3.right;
            Vector3 reference = collider.bounds.center;
            reference.y = CameraSurfaceHeight(reference.x, reference.z) + .28f;
            Vector3 closest = collider.ClosestPoint(reference + away * 8f);
            Vector3 start = closest + away * (Player.Body.radius + .075f);
            start.y = CameraSurfaceHeight(start.x, start.z) + .035f;
            return start;
        }

        void QaPushPlayer(Vector3 direction, float speed, float deltaTime, int steps)
        {
            direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            for (int step = 0; step < steps; step++)
            {
                Player.MoveForQa(direction, speed, deltaTime);
                Physics.SyncTransforms();
            }
        }

        IEnumerator BeginCollisionSafetyVideoQa()
        {
            IsAutomationSmoke = true;
            yield return null;
            BeginPlay();
            Mission.Restore(MissionDirector.SpiderStep);
            squads.enabled = false;
            foreach (SquadUnit unit in FindObjectsByType<SquadUnit>(FindObjectsSortMode.None))
                unit.gameObject.SetActive(false);
            foreach (Creature creature in creatures)
                if (creature) creature.gameObject.SetActive(false);

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
            string directory = Path.Combine(projectRoot, "QA", "VideoFrames", "collision-091-proof");
            Directory.CreateDirectory(directory);
            foreach (string oldFrame in Directory.GetFiles(directory, "frame-*.tga"))
                File.Delete(oldFrame);
            const float frameRate = 15f;
            int frameNumber = 0;

            IsUnderground = false;
            RefreshWorldForMission();
            ApplyLocationLighting();
            Transform tree = environment.GetComponentsInChildren<Transform>(true)
                .First(candidate => candidate.name.StartsWith("Modeled forest tree"));
            CapsuleCollider trunk = tree.GetComponentsInChildren<CapsuleCollider>(true)
                .First(collider => collider.transform.parent &&
                                   collider.transform.parent.name == "Irregular modeled trunk");
            Vector3 treeAway = new Vector3(trunk.bounds.center.x, 0, trunk.bounds.center.z).normalized;
            if (treeAway.sqrMagnitude < .1f) treeAway = Vector3.right;
            Player.Teleport(QaOutsideCollider(trunk, treeAway));
            Player.Face(new Vector3(trunk.bounds.center.x, Player.transform.position.y,
                trunk.bounds.center.z), 16f);
            collisionQaCaption = "1/6  TREE TRUNK — sprint collision, no tunnelling";
            for (int frame = 0; frame < 48; frame++)
            {
                Player.MoveForQa(-treeAway, 4.25f, 1f / frameRate);
                Player.SetCameraOrbitForQa(35f + frame * .18f, 17f);
                yield return CapturePhysicalVideoFrame(directory, frameNumber++, frameRate);
            }

            Transform heroRoot = environment.GetComponentsInChildren<Transform>(true)
                .First(candidate => candidate.name == "Fine feeder root beside the ant path");
            CapsuleCollider root = heroRoot.GetComponentsInChildren<CapsuleCollider>(true).First();
            Vector3 segment = Vector3.ProjectOnPlane(root.transform.up, Vector3.up);
            Vector3 rootAway = Vector3.Cross(Vector3.up,
                segment.sqrMagnitude > .05f ? segment.normalized : Vector3.forward).normalized;
            Player.Teleport(QaOutsideCollider(root, rootAway));
            Player.Face(Player.transform.position - rootAway, 14f);
            collisionQaCaption = "2/6  BRANCHING ROOT — wall slide stays outside mesh";
            for (int frame = 0; frame < 48; frame++)
            {
                Player.MoveForQa((-rootAway + root.transform.up * .62f).normalized,
                    3.2f, 1f / frameRate);
                Player.SetCameraOrbitForQa(118f + frame * .22f, 16f);
                yield return CapturePhysicalVideoFrame(directory, frameNumber++, frameRate);
            }

            ToggleNest(Player, false);
            Vector3 queen = UndergroundCenter +
                UndergroundChamberCenters[QueenChamberIndex] + Vector3.up * .035f;
            Vector3 wall = new Vector3(-1f, 0, -.18f).normalized;
            Player.Teleport(queen);
            Player.Face(queen + wall, 13f);
            collisionQaCaption = "3/6  NEST WALL — continuous floor, wall and ceiling";
            for (int frame = 0; frame < 48; frame++)
            {
                Player.MoveForQa(wall, 4.25f, 1f / frameRate);
                Player.SetCameraOrbitForQa(198f, 14f);
                yield return CapturePhysicalVideoFrame(directory, frameNumber++, frameRate);
            }

            collisionQaCaption = "4/6  BLOCKED ANT — camera remains independently controllable";
            for (int frame = 0; frame < 54; frame++)
            {
                Player.SetCameraOrbitForQa(Mathf.Lerp(198f, 518f, frame / 53f),
                    Mathf.Lerp(9f, 31f, Mathf.PingPong(frame / 26f, 1f)));
                yield return CapturePhysicalVideoFrame(directory, frameNumber++, frameRate);
            }

            collisionQaCaption = "5/6  ANTI-STUCK — intentional overlap returns to safe ground";
            Player.ForceUnsafePositionForQa(UndergroundCenter +
                UndergroundChamberCenters[QueenChamberIndex] +
                new Vector3(-UndergroundChamberRadii[QueenChamberIndex].x - .2f, .04f, 0));
            Player.RecoverNowForQa(-wall);
            for (int frame = 0; frame < 42; frame++)
            {
                Player.SetCameraOrbitForQa(75f + frame * 1.1f, 18f);
                yield return CapturePhysicalVideoFrame(directory, frameNumber++, frameRate);
            }

            collisionQaCaption = "6/6  NARROW TUNNEL — camera, player and save position contained";
            Vector3 tunnelStart = UndergroundCenter + UndergroundTunnelPaths[0][^1] + Vector3.up * .035f;
            Vector3 tunnelEnd = UndergroundCenter + UndergroundTunnelPaths[0][0] + Vector3.up * .035f;
            Player.Teleport(tunnelStart);
            Player.Face(tunnelEnd, 18f);
            for (int frame = 0; frame < 54; frame++)
            {
                Player.MoveForQa(tunnelEnd - Player.transform.position,
                    1.85f, 1f / frameRate);
                Player.SetCameraOrbitForQa(310f + Mathf.Sin(frame * .09f) * 42f, 21f);
                yield return CapturePhysicalVideoFrame(directory, frameNumber++, frameRate);
            }

            collisionQaCaption = null;
            bool safe = IsPlayerPositionValid(Player, Player.transform.position) &&
                        !Player.HasBlockingOverlapAt(Player.transform.position, .018f) &&
                        !CameraOverlapsSolid(Camera.main.transform.position, .19f);
            Debug.Log($"MOONROOT_COLLISION_VIDEO_QA_{(safe ? "OK" : "FAILED")} " +
                      $"frames={frameNumber} fps={frameRate} directory={directory}");
            if (!Application.isEditor) Application.Quit(safe ? 0 : 2);
        }

        bool CameraOverlapsSolid(Vector3 position, float radius)
        {
            foreach (Collider collider in Physics.OverlapSphere(
                         position,
                         radius,
                         ~0,
                         QueryTriggerInteraction.Ignore))
            {
                if (!collider || collider.isTrigger) continue;
                // An open terrain MeshCollider is reported as an enclosing
                // half-space by overlap queries on some PhysX backends.  Its
                // clearance is tested directly against CameraSurfaceHeight.
                if (collider == layeredTerrainCollider) continue;
                Transform candidate = collider.transform;
                if (candidate == Player.transform || candidate.IsChildOf(Player.transform))
                    continue;
                if (collider.GetComponentInParent<SquadUnit>() ||
                    collider.GetComponentInParent<Creature>())
                    continue;
                return true;
            }
            return false;
        }

        IEnumerator BeginEnvironmentTraversalSmoke()
        {
            IsAutomationSmoke = true;
            yield return null;
            BeginPlay();
            Mission.Restore(MissionDirector.SpiderStep);
            IsUnderground = false;
            RefreshWorldForMission();
            ApplyLocationLighting();

            Transform habitat = environment
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name ==
                    "Maximum-quality playable microhabitat");
            if (!habitat)
            {
                Debug.LogError("MOONROOT_ENVIRONMENT_TRAVERSAL_FAILED reason=missing-habitat");
                if (!Application.isEditor) Application.Quit(2);
                yield break;
            }

            CapsuleCollider[] capsuleColliders = habitat.GetComponentsInChildren<CapsuleCollider>(true);
            int rootColliderSegments = capsuleColliders.Count(collider =>
                collider.GetComponentsInParent<Transform>(true).Any(candidate =>
                    candidate.name.IndexOf("root", System.StringComparison.OrdinalIgnoreCase) >= 0));
            MovementSurface[] surfaces = habitat.GetComponentsInChildren<MovementSurface>(true);
            bool hasSoil = surfaces.Any(surface => surface.DisplayName == "Layered forest soil");
            bool hasPuddle = surfaces.Any(surface => surface.DisplayName == "Shallow water");

            int terrainHits = 0;
            float minimumHeight = float.MaxValue;
            float maximumHeight = float.MinValue;
            for (int z = -4; z <= 4; z += 2)
            for (int x = -5; x <= 5; x += 2)
            {
                float worldX = HeroMicrohabitatCenter.x + x;
                float worldZ = HeroMicrohabitatCenter.y + z;
                float height = GroundHeight(worldX, worldZ);
                minimumHeight = Mathf.Min(minimumHeight, height);
                maximumHeight = Mathf.Max(maximumHeight, height);
                RaycastHit[] hits = Physics.RaycastAll(
                    new Vector3(worldX, height + 6f, worldZ),
                    Vector3.down,
                    12f,
                    ~0,
                    QueryTriggerInteraction.Ignore);
                if (hits.Any(hit => hit.collider.GetComponentInParent<MovementSurface>()?.DisplayName ==
                                    "Layered forest soil"))
                    terrainHits++;
            }

            Vector3 laneStart = At(HeroMicrohabitatCenter.x, HeroMicrohabitatCenter.y - 3.6f, .08f);
            Player.Teleport(laneStart);
            CharacterController controller = Player.GetComponent<CharacterController>();
            for (int step = 0; step < 46; step++)
            {
                controller.Move(Vector3.forward * .052f);
                yield return null;
            }
            float laneProgress = Vector3.ProjectOnPlane(
                Player.transform.position - laneStart,
                Vector3.up).magnitude;
            float displacement = maximumHeight - minimumHeight;
            bool passed = hasSoil && hasPuddle && rootColliderSegments >= 9 &&
                          terrainHits >= 25 && displacement >= .12f && laneProgress >= 1.8f;
            string result =
                $"terrainHits={terrainHits}/30 rootColliderSegments={rootColliderSegments} " +
                $"surfaces={surfaces.Length} displacement={displacement:F3} " +
                $"laneProgress={laneProgress:F3} soil={hasSoil} puddle={hasPuddle}";
            if (passed)
                Debug.Log("MOONROOT_ENVIRONMENT_TRAVERSAL_OK " + result);
            else
                Debug.LogError("MOONROOT_ENVIRONMENT_TRAVERSAL_FAILED " + result);
            if (!Application.isEditor)
                Application.Quit(passed ? 0 : 2);
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
            yield return CaptureQaScreenshot("ant-060-windows-player-front.tga");
            SetQaCamera(playerFocus, new Vector3(2.8f, .45f, .12f), 43f);
            yield return CaptureQaScreenshot("ant-060-windows-player-side-close.tga");
            SetQaCamera(playerFocus, new Vector3(0, .45f, -2.7f), 43f);
            yield return CaptureQaScreenshot("ant-060-windows-player-rear.tga");
            SetQaCamera(playerFocus, new Vector3(2.9f, 1.35f, -3.25f), 47f);
            yield return CaptureQaScreenshot("ant-060-windows-player-uneven-ground.tga");
            SetQaCamera(playerFocus, new Vector3(0, 3.2f, -.12f), 39f);
            yield return CaptureQaScreenshot("ant-060-windows-player-top.tga");
            // A deliberately low camera within the real sunlit mission region
            // puts the ant against the bright sky. Any holes, blended shell
            // fragments or inverted faces are immediately visible here.
            SetQaCamera(playerFocus, new Vector3(.22f, -.12f, 2.2f), 36f);
            yield return CaptureQaScreenshot(
                "ant-060-windows-player-bright-background.tga");

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
            yield return CaptureQaScreenshot("ant-060-windows-worker-soldiers.tga");

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
            yield return CaptureQaScreenshot("ant-060-windows-workers-carrying.tga");

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
            yield return CaptureQaScreenshot("ant-060-windows-queen-chamber.tga");

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
            yield return CaptureQaScreenshot("ant-060-windows-player-bite.tga");

            Debug.Log(
                "MOONROOT_ANT_VISUAL_QA_OK screenshots=10 " +
                $"playerState={playerVisual.AnimationState} queen={queen.Caste} " +
                $"workers={workers.Length}");
            if (!Application.isEditor)
                Application.Quit(0);
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
            WriteQaTga(path, 1600, 900);
            Debug.Log($"MOONROOT_ANT_QA_SCREENSHOT path={path}");
            yield return new WaitForSecondsRealtime(.5f);
        }

        static IEnumerator CaptureQaWireframeScreenshot(string fileName)
        {
            yield return new WaitForEndOfFrame();
            string projectRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "..", ".."));
            string directory = Path.Combine(projectRoot, "QA", "Screenshots");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, fileName);
            GL.wireframe = true;
            try
            {
                WriteQaTga(path, 1600, 900);
            }
            finally
            {
                GL.wireframe = false;
            }
            Debug.Log($"MOONROOT_WIREFRAME_QA_SCREENSHOT path={path}");
            yield return new WaitForSecondsRealtime(.5f);
        }

        static void WriteQaTga(string path, int width, int height)
        {
            Camera camera = Camera.main;
            if (!camera)
                throw new System.InvalidOperationException(
                    "QA screenshot requires a main camera.");
            RenderTexture priorTarget = camera.targetTexture;
            RenderTexture priorActive = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                texture.Apply(false, false);
            }
            finally
            {
                camera.targetTexture = priorTarget;
                RenderTexture.active = priorActive;
                RenderTexture.ReleaseTemporary(target);
            }
            Color32[] pixels = texture.GetPixels32();
            if (string.Equals(Path.GetExtension(path), ".png",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.Destroy(texture);
                return;
            }
            byte[] tga = new byte[18 + pixels.Length * 3];
            tga[2] = 2;
            tga[12] = (byte)(width & 0xff);
            tga[13] = (byte)((width >> 8) & 0xff);
            tga[14] = (byte)(height & 0xff);
            tga[15] = (byte)((height >> 8) & 0xff);
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
            if (!Application.isEditor)
                Application.Quit(0);
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
            Physics.queriesHitBackfaces = true;
            Physics.queriesHitTriggers = true;
            Physics.defaultContactOffset = .012f;
            environment = new GameObject("Moonroot forest-floor region").transform;
            Colony = gameObject.AddComponent<ColonyState>();
            Mission = gameObject.AddComponent<MissionDirector>();
            squads = gameObject.AddComponent<SquadController>();
            gameObject.AddComponent<AudioDirector>().Initialize();
            gameObject.AddComponent<FxPool>().Initialize();
            Mission.StepChanged += _ => RefreshWorldForMission();

            ConfigureLighting();
            GameObject layeredTerrain = VisualFactory.Terrain(
                "Layered loam terrain",
                environment,
                110f,
                RuntimeQualityProfile.TerrainResolution(GameSettings.Quality),
                GroundHeight,
                new Color(.82f, .76f, .65f));
            layeredTerrainCollider = layeredTerrain.GetComponent<Collider>();
            BuildDistantEnclosure();
            BuildNest();
            BuildForageRoute();
            BuildLandmarks();
            BuildHeroMicrohabitat();
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
            RenderSettings.ambientGroundColor = new Color(.15f, .11f, .072f);
            RenderSettings.reflectionIntensity = .68f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.48f, .59f, .55f);
            RenderSettings.fogDensity = .0115f;

            sunLight = new GameObject("Canopy-break sunlight").AddComponent<Light>();
            sunLight.transform.SetParent(transform);
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1f, .9f, .71f);
            sunLight.intensity = .94f;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = .62f;
            sunLight.shadowBias = .035f;
            sunLight.shadowNormalBias = .32f;
            sunLight.transform.rotation = Quaternion.Euler(42f, -28f, 0);
            sunLight.gameObject.AddComponent<CanopyLightMotion>().Initialize(sunLight);

            skyFillLight = new GameObject("Cool canopy fill").AddComponent<Light>();
            skyFillLight.transform.SetParent(transform);
            skyFillLight.type = LightType.Directional;
            skyFillLight.color = new Color(.36f, .49f, .55f);
            skyFillLight.intensity = .48f;
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
                RenderSettings.ambientSkyColor = new Color(.64f, .58f, .48f);
                RenderSettings.ambientEquatorColor = new Color(.48f, .42f, .33f);
                RenderSettings.ambientGroundColor = new Color(.31f, .245f, .17f);
                RenderSettings.fogColor = new Color(.285f, .27f, .225f);
                RenderSettings.fogDensity = .0036f;
                if (sunLight) sunLight.intensity = .32f;
                if (skyFillLight) skyFillLight.intensity = .35f;
                if (amberNestLight) amberNestLight.intensity = 3f;
                if (tunnelFillLight) tunnelFillLight.intensity = 2.25f;
                if (nurseryFillLight) nurseryFillLight.intensity = 2.1f;
                foreach (Light guide in undergroundGuideLights)
                    if (guide) guide.enabled = true;
                return;
            }

            RenderSettings.ambientSkyColor = new Color(.52f, .59f, .55f);
            RenderSettings.ambientEquatorColor = new Color(.28f, .32f, .26f);
            RenderSettings.ambientGroundColor = new Color(.15f, .11f, .072f);
            RenderSettings.fogColor = new Color(.48f, .59f, .55f);
            RenderSettings.fogDensity = .0115f;
            if (sunLight) sunLight.intensity = .94f;
            if (skyFillLight) skyFillLight.intensity = .48f;
            if (amberNestLight) amberNestLight.intensity = .16f;
            if (tunnelFillLight) tunnelFillLight.intensity = .12f;
            if (nurseryFillLight) nurseryFillLight.intensity = .08f;
            foreach (Light guide in undergroundGuideLights)
                if (guide) guide.enabled = false;
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
            var enclosure = new GameObject("Layered modeled forest enclosure").transform;
            enclosure.SetParent(environment, false);
            int treeCount = RuntimeQualityProfile.DistantTreeCount(GameSettings.Quality);
            for (int i = 0; i < treeCount; i++)
            {
                int layer = i % 3;
                float angle = i * 2.399963f + layer * .37f;
                float radius = layer switch
                {
                    0 => 27.5f + Mathf.Sin(i * 2.7f) * 2.8f,
                    1 => 38.5f + Mathf.Sin(i * 3.1f) * 3.4f,
                    _ => 49f + Mathf.Sin(i * 2.3f) * 3.2f
                };
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius + 3f;
                Vector3 basePoint = new(x, GroundHeight(x, z) - .35f, z);
                float height = layer switch
                {
                    0 => Random.Range(15f, 22f),
                    1 => Random.Range(20f, 29f),
                    _ => Random.Range(25f, 36f)
                };
                float trunkRadius = Mathf.Lerp(.72f, 1.55f, height / 36f) *
                                    Random.Range(.88f, 1.18f);
                VisualFactory.ModeledForestTree(
                    enclosure,
                    basePoint,
                    height,
                    trunkRadius,
                    i,
                    true);
            }

            int understoryCount = RuntimeQualityProfile.IsFullQuality ? 72 : 34;
            for (int i = 0; i < understoryCount; i++)
            {
                float angle = i * 2.399963f + .19f;
                float radius = Random.Range(25f, 52f);
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius + 3f;
                Vector3 position = new(x, GroundHeight(x, z), z);
                if ((i & 1) == 0)
                    VisualFactory.GroundcoverPatch(
                        enclosure,
                        position,
                        Random.Range(1.35f, 2.8f),
                        Color.Lerp(new Color(.19f, .37f, .08f),
                            new Color(.45f, .58f, .17f), Random.value),
                        1400 + i);
                else
                    VisualFactory.GrassTuft(
                        enclosure,
                        position,
                        Random.Range(1.6f, 3.2f),
                        Color.Lerp(new Color(.16f, .32f, .065f),
                            new Color(.39f, .52f, .15f), Random.value),
                        1500 + i);
            }

            // Uneven modeled banks overlap the outer edge of the sculpted
            // terrain.  They are geometry, not an inaccessible image wall.
            for (int i = 0; i < 24; i++)
            {
                float angle = i / 24f * Mathf.PI * 2f;
                float radius = 51.5f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                GameObject bank = VisualFactory.Stone(
                    "Modeled outer soil ridge",
                    enclosure,
                    new Vector3(x, GroundHeight(x, z) - .55f, z),
                    new Vector3(4.8f, 2.2f + i % 4 * .38f, 7.2f),
                    1700 + i,
                    true,
                    i % 3 == 0);
                bank.transform.localRotation = Quaternion.Euler(
                    0,
                    -angle * Mathf.Rad2Deg,
                    (i % 5 - 2) * 2f);
            }

            Debug.Log(
                $"MOONROOT_MODELED_FOREST_READY trees={treeCount} understory={understoryCount} " +
                "boundaryBanks=24 photographicBackdrops=0 billboards=0");
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
                    true,
                    i % 3 == 0);
            }

            Vector3 entrance = nest + new Vector3(0, .12f, 1.8f);
            CreateNestDoor("Moonroot surface entrance", entrance, false);
            VisualFactory.TexturedRoot(
                "Living root gateway",
                surface,
                new[]
                {
                    surface.InverseTransformPoint(entrance + new Vector3(-1.6f, .02f, .18f)),
                    surface.InverseTransformPoint(entrance + new Vector3(-.98f, 1.48f, -.2f)),
                    surface.InverseTransformPoint(entrance + new Vector3(0, 1.92f, -.28f)),
                    surface.InverseTransformPoint(entrance + new Vector3(.98f, 1.48f, -.2f)),
                    surface.InverseTransformPoint(entrance + new Vector3(1.6f, .02f, .18f))
                },
                new[] { .31f, .27f, .24f, .27f, .31f },
                false);

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

            VisualFactory.MeshObject(
                "Closed watertight underground safety envelope",
                underground,
                NestGeometryFactory.ClosedNestSafetyEnvelope(new Vector3(16f, 8f, 15f)),
                new Vector3(0, .5f, .45f),
                Vector3.one,
                VisualFactory.NestSoilMaterial(),
                false);

            BuildModeledNestNetwork();

            for (int i = 0; i < 8; i++)
            {
                float angle = i / 8f * Mathf.PI * 2f;
                Vector3 lower = new(Mathf.Cos(angle) * 12.8f, -.65f, Mathf.Sin(angle) * 11.6f);
                Vector3 middle = new(Mathf.Cos(angle) * 11.5f, 3.1f + Mathf.Sin(i * 1.8f) * .28f, Mathf.Sin(angle) * 10.4f);
                Vector3 upper = new(Mathf.Cos(angle) * 8.8f, 6.45f, Mathf.Sin(angle) * 7.9f);
                VisualFactory.TexturedRoot(
                    "Buried structural root",
                    underground,
                    new[] { lower, middle, upper },
                    new[] { .52f, .39f, .24f },
                    false);
            }
            BuildQueenChamber();
            BuildStorageChambers();
            BuildNurseryAndServiceZones();
            BuildLivingColony();
            CreateNestDoor(
                "Tunnel to forest floor",
                UndergroundCenter + UndergroundChamberCenters[EntranceChamberIndex] +
                new Vector3(0, .3f, 1.5f),
                true);

            amberNestLight = new GameObject("Amber chamber bounce").AddComponent<Light>();
            amberNestLight.transform.SetParent(underground, false);
            amberNestLight.transform.localPosition =
                UndergroundChamberCenters[QueenChamberIndex] + new Vector3(.4f, 2.15f, .25f);
            amberNestLight.type = LightType.Point;
            amberNestLight.range = 12f;
            amberNestLight.intensity = 1.62f;
            amberNestLight.color = new Color(.86f, .58f, .39f);
            amberNestLight.shadows = LightShadows.Soft;
            amberNestLight.shadowStrength = .26f;

            tunnelFillLight = new GameObject("Cool tunnel fill").AddComponent<Light>();
            tunnelFillLight.transform.SetParent(underground, false);
            tunnelFillLight.transform.localPosition =
                UndergroundChamberCenters[CentralChamberIndex] + new Vector3(0, 1.65f, .35f);
            tunnelFillLight.type = LightType.Point;
            tunnelFillLight.range = 8f;
            tunnelFillLight.intensity = 1.26f;
            tunnelFillLight.color = new Color(.39f, .62f, .55f);

            nurseryFillLight = new GameObject("Nursery soft fill").AddComponent<Light>();
            nurseryFillLight.transform.SetParent(underground, false);
            nurseryFillLight.transform.localPosition =
                UndergroundChamberCenters[NurseryChamberIndex] + new Vector3(0, 1.8f, 0);
            nurseryFillLight.type = LightType.Point;
            nurseryFillLight.range = 7f;
            nurseryFillLight.intensity = 1.35f;
            nurseryFillLight.color = new Color(.86f, .62f, .39f);

            BuildNestGuideLights();
        }

        void BuildModeledNestNetwork()
        {
            for (int chamber = 0; chamber < UndergroundChamberCenters.Length; chamber++)
                BuildModeledChamber(
                    UndergroundChamberNames[chamber],
                    UndergroundChamberCenters[chamber],
                    UndergroundChamberRadii[chamber],
                    chamber + 1,
                    UndergroundChamberPortals[chamber],
                    UndergroundChamberPortalHalfAngles[chamber]);

            for (int tunnel = 0; tunnel < UndergroundTunnelPaths.Length; tunnel++)
                BuildModeledTunnel(
                    UndergroundTunnelNames[tunnel],
                    UndergroundTunnelVariants[tunnel],
                    UndergroundTunnelPaths[tunnel],
                    UndergroundTunnelRadii[tunnel],
                    UndergroundTunnelHeights[tunnel],
                    UndergroundTunnelBusy[tunnel]);

            Debug.Log(
                $"MOONROOT_MODELED_NEST_READY chambers={UndergroundChamberCenters.Length} " +
                $"tunnels={UndergroundTunnelPaths.Length} floors=solid shells=closed " +
                "colliders=continuous routes=looped source=single-specification");
        }

        void BuildModeledChamber(
            string name,
            Vector3 localPosition,
            Vector3 radii,
            int variant,
            float[] portalAngles,
            float[] portalHalfAngles)
        {
            var chamber = new GameObject(name).transform;
            chamber.SetParent(underground, false);
            chamber.localPosition = localPosition;
            Material soil = VisualFactory.NestSoilMaterial();
            VisualFactory.MeshObject(
                "Uneven excavated chamber floor",
                chamber,
                NestGeometryFactory.ChamberFloor(variant,
                    new Vector2(radii.x * .96f, radii.z * .96f)),
                Vector3.zero,
                Vector3.one,
                soil,
                false);
            GameObject chamberFloorCollision = VisualFactory.MeshObject(
                "Open smooth chamber floor collision without portal rim",
                chamber,
                NestGeometryFactory.ChamberFloor(variant,
                    new Vector2(radii.x * .985f, radii.z * .985f), true),
                Vector3.zero,
                Vector3.one,
                soil,
                true);
            chamberFloorCollision.GetComponent<Renderer>().enabled = false;
            chamberFloorCollision.AddComponent<MovementSurface>().Initialize("Packed nest soil", .94f);
            GameObject chamberShell = VisualFactory.MeshObject(
                "Curved chamber walls and ceiling",
                chamber,
                NestGeometryFactory.ChamberShell(
                    variant, radii, portalAngles, portalHalfAngles),
                Vector3.zero,
                Vector3.one,
                soil,
                true);
            chamber.gameObject.AddComponent<NestChamberMarker>().Initialize(
                name,
                radii,
                portalAngles?.Length ?? 0,
                chamberFloorCollision.GetComponent<Collider>(),
                chamberShell.GetComponent<Collider>());

            // Embedded clods and pebbles protrude from the wall so their depth
            // remains visible during lateral camera movement.
            for (int i = 0; i < 11; i++)
            {
                float angle = i / 11f * Mathf.PI * 2f + variant * .47f;
                float y = .32f + (i % 4) * radii.y * .16f;
                float elevation = Mathf.Asin(Mathf.Clamp01(y / radii.y));
                float radial = Mathf.Cos(elevation) * .9f;
                Vector3 position = new(
                    Mathf.Cos(angle) * radii.x * radial,
                    y,
                    Mathf.Sin(angle) * radii.z * radial);
                GameObject clod = VisualFactory.MeshObject(
                    i % 3 == 0 ? "Embedded nest pebble" : "Excavation soil clod",
                    chamber,
                    i % 3 == 0
                        ? EnvironmentMeshFactory.HeroStone(variant * 17 + i)
                        : WorldAssetMeshFactory.SoilClod(variant * 19 + i),
                    position,
                    new Vector3(.28f + i % 3 * .07f, .19f + i % 2 * .055f,
                        .25f + i % 4 * .045f),
                    i % 3 == 0
                        ? VisualFactory.PbrMaterial("Stone", new Color(.64f, .58f, .47f), .04f, 1.1f,
                            new Vector2(1.7f, 1.7f))
                        : soil,
                    false);
                clod.transform.localRotation = Quaternion.Euler(
                    i * 13f,
                    -angle * Mathf.Rad2Deg,
                    i * 29f);
            }

            for (int rootIndex = 0; rootIndex < 3; rootIndex++)
            {
                float angle = variant * .63f + rootIndex * 1.91f;
                Vector3 side = new(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 tangent = new(-side.z, 0, side.x);
                VisualFactory.TexturedRoot(
                    "Root penetrating modeled wall and ceiling",
                    chamber,
                    new[]
                    {
                        Vector3.Scale(side, new Vector3(radii.x * .94f, 0, radii.z * .94f)) +
                            Vector3.up * .48f,
                        Vector3.Scale(side, new Vector3(radii.x * .78f, 0, radii.z * .78f)) +
                            tangent * .18f + Vector3.up * radii.y * .62f,
                        side * .2f + tangent * .32f + Vector3.up * radii.y * .92f
                    },
                    new[] { .16f + rootIndex * .025f, .12f, .055f },
                    false);
            }
        }

        void BuildModeledTunnel(
            string name,
            int variant,
            IReadOnlyList<Vector3> path,
            float radius,
            float height,
            bool busy)
        {
            var tunnel = new GameObject(name).transform;
            tunnel.SetParent(underground, false);
            Material soil = VisualFactory.NestSoilMaterial();
            VisualFactory.MeshObject(
                "Visible worn tunnel floor",
                tunnel,
                NestGeometryFactory.TunnelFloor(variant, path, radius * .88f),
                Vector3.zero,
                Vector3.one,
                soil,
                false);
            GameObject floorCollision = VisualFactory.MeshObject(
                "Continuous smooth tunnel floor collision",
                tunnel,
                NestGeometryFactory.TunnelFloor(variant, path, radius * .94f, true),
                Vector3.zero,
                Vector3.one,
                soil,
                true);
            floorCollision.GetComponent<Renderer>().enabled = false;
            floorCollision.AddComponent<MovementSurface>().Initialize("Packed tunnel soil", .96f);
            VisualFactory.MeshObject(
                "Curved tunnel walls ceiling and outer thickness",
                tunnel,
                NestGeometryFactory.TunnelShell(variant, path, radius, height, 0, false, .18f),
                Vector3.zero,
                Vector3.one,
                soil,
                false);
            GameObject tunnelCollision = VisualFactory.MeshObject(
                "Junction-trimmed tunnel walls and ceiling collision",
                tunnel,
                // The visual excavation reaches into each chamber, but its
                // collision shell begins only after the junction throat. This
                // prevents the mouth of one tunnel becoming an invisible wall
                // when the player circles around a chamber. The chamber shell
                // remains solid around the authored portal, so this does not
                // disable world collision or expose an escape gap.
                NestGeometryFactory.TunnelShell(variant, path, radius, height, 2, true),
                Vector3.zero,
                Vector3.one,
                soil,
                true);
            tunnelCollision.GetComponent<Renderer>().enabled = false;

            TunnelClearanceMarker marker = tunnel.gameObject.AddComponent<TunnelClearanceMarker>();
            marker.Initialize(name, radius * 2f, height, busy, floorCollision.GetComponent<Collider>(),
                tunnelCollision.GetComponent<Collider>());

            int middle = path.Count / 2;
            Vector3 tangent = (path[Mathf.Min(path.Count - 1, middle + 1)] -
                               path[Mathf.Max(0, middle - 1)]).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
            VisualFactory.TexturedRoot(
                "Exposed tunnel support root",
                tunnel,
                new[]
                {
                    path[middle] - side * radius * .96f + Vector3.up * height * .88f,
                    path[middle] + Vector3.up * height * 1.12f,
                    path[middle] + side * radius * .96f + Vector3.up * height * .88f
                },
                new[] { .075f, .055f, .075f },
                false);
        }

        void BuildQueenChamber()
        {
            var queen = new GameObject("Queen chamber").transform;
            queen.SetParent(underground, false);
            queen.localPosition = UndergroundChamberCenters[QueenChamberIndex] + Vector3.up * .08f;
            for (int i = 0; i < 18; i++)
            {
                float angle = i * 2.399f;
                BroodStage stage = i % 6 == 0 ? BroodStage.Pupa :
                    i % 3 == 0 ? BroodStage.Egg : BroodStage.Larva;
                WorldAssetVisualFactory.Brood(
                    queen,
                    stage,
                    new Vector3(Mathf.Cos(angle) * (2.05f + (i % 3) * .16f),
                        .18f + (i % 2) * .035f,
                        Mathf.Sin(angle) * (1.65f + (i % 2) * .14f)),
                    stage == BroodStage.Egg ? .13f : stage == BroodStage.Pupa ? .25f : .21f,
                    i);
            }
            AntVisual.Create(queen, new Color(.23f, .045f, .012f), 1.28f, AntCaste.Queen)
                .transform.localPosition = new Vector3(0, .28f, -.25f);
            queen.gameObject.AddComponent<QueenBriefing>().Initialize();
        }

        void BuildStorageChambers()
        {
            var storage = new GameObject("Food storage chamber").transform;
            storage.SetParent(underground, false);
            storage.localPosition = UndergroundChamberCenters[FoodChamberIndex] + Vector3.up * .12f;
            for (int i = 0; i < 21; i++)
            {
                float angle = i / 21f * Mathf.PI * 2f;
                float ring = i % 3 == 0 ? 2.05f : 2.35f;
                ResourceNode.CreateCargoVisual(storage,
                    i % 5 == 0 ? ResourceKind.Resin : i % 4 == 0 ? ResourceKind.Protein : ResourceKind.Seed,
                    new Vector3(Mathf.Cos(angle) * ring, .12f + (i % 3) * .08f,
                        Mathf.Sin(angle) * ring * .76f),
                    .24f,
                    i);
            }

            var stationObject = new GameObject("Nursery growth site");
            stationObject.transform.SetParent(underground, false);
            stationObject.transform.localPosition =
                UndergroundChamberCenters[NurseryChamberIndex] + new Vector3(2.6f, .18f, 1.9f);
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
            undergroundUpgrade.transform.localPosition =
                UndergroundChamberCenters[GuardChamberIndex] + new Vector3(.2f, .12f, .15f);
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f;
                VisualFactory.Mushroom(undergroundUpgrade.transform,
                    new Vector3(Mathf.Cos(a) * 1.95f, 0, Mathf.Sin(a) * 1.45f),
                    .42f, new Color(.24f, .12f, .34f));
            }
            undergroundUpgrade.SetActive(false);
        }

        void BuildNurseryAndServiceZones()
        {
            Transform nursery = new GameObject("Living nursery work floor").transform;
            nursery.SetParent(underground, false);
            nursery.localPosition = UndergroundChamberCenters[NurseryChamberIndex] + Vector3.up * .08f;
            BuildBroodZone(nursery, "Warm egg shelves", BroodStage.Egg,
                new Vector3(-2.25f, 0, -1.55f), new Vector2(1.15f, .72f), 18, 310);
            BuildBroodZone(nursery, "Feeding larva shelves", BroodStage.Larva,
                new Vector3(1.85f, 0, -1.55f), new Vector2(1.28f, .74f), 15, 350);
            BuildBroodZone(nursery, "Dry pupa shelves", BroodStage.Pupa,
                new Vector3(2.05f, 0, 1.45f), new Vector2(1.15f, .7f), 12, 390);

            // Low non-blocking berms separate the work zones visually while a
            // broad central loop remains clear for the player and traffic.
            for (int i = 0; i < 3; i++)
            {
                float angle = i / 3f * Mathf.PI * 2f + .55f;
                WorldAssetVisualFactory.ChamberBerm(
                    nursery,
                    "Low nursery brood shelf edge",
                    new Vector3(Mathf.Cos(angle) * 2.7f, .02f, Mathf.Sin(angle) * 2.05f),
                    new Vector3(1.15f, .16f, .32f),
                    430 + i,
                    false);
            }

            Transform eggs = new GameObject("Dedicated egg incubation gallery").transform;
            eggs.SetParent(underground, false);
            eggs.localPosition = UndergroundChamberCenters[EggChamberIndex] + Vector3.up * .08f;
            BuildBroodZone(eggs, "Humidity sorted egg beds", BroodStage.Egg,
                new Vector3(0, 0, -.2f), new Vector2(2.15f, 1.55f), 30, 470);

            Transform pupae = new GameObject("Larva feeding and pupa gallery").transform;
            pupae.SetParent(underground, false);
            pupae.localPosition = UndergroundChamberCenters[PupaChamberIndex] + Vector3.up * .08f;
            BuildBroodZone(pupae, "Larva feeding beds", BroodStage.Larva,
                new Vector3(-1.05f, 0, -.15f), new Vector2(1.25f, 1.55f), 18, 520);
            BuildBroodZone(pupae, "Pupa drying beds", BroodStage.Pupa,
                new Vector3(1.2f, 0, .15f), new Vector2(1.15f, 1.45f), 16, 560);

            Transform sanitation = new GameObject("Sanitation and refuse sorting zone").transform;
            sanitation.SetParent(underground, false);
            sanitation.localPosition = UndergroundChamberCenters[SanitationChamberIndex] + Vector3.up * .04f;
            for (int i = 0; i < 14; i++)
            {
                float angle = i * 2.399f;
                VisualFactory.OrganicPart(
                    "Sorted dry refuse pellet",
                    sanitation,
                    OrganicMeshFactory.BodyShape.Brood,
                    new Vector3(Mathf.Cos(angle) * (1.55f + i % 2 * .28f), .08f,
                        Mathf.Sin(angle) * (1.35f + i % 3 * .12f)),
                    new Vector3(.14f, .09f, .18f),
                    new Color(.21f, .13f, .07f),
                    .04f);
            }

            Transform guard = new GameObject("Entrance guard and tool chamber").transform;
            guard.SetParent(underground, false);
            guard.localPosition = UndergroundChamberCenters[GuardChamberIndex] + Vector3.up * .06f;
            for (int i = 0; i < 9; i++)
            {
                float angle = i / 9f * Mathf.PI * 2f;
                ResourceNode.CreateCargoVisual(
                    guard,
                    i % 3 == 0 ? ResourceKind.Resin : ResourceKind.Seed,
                    new Vector3(Mathf.Cos(angle) * 1.8f, .1f, Mathf.Sin(angle) * 1.45f),
                    .17f,
                    610 + i);
            }
        }

        static void BuildBroodZone(
            Transform parent,
            string name,
            BroodStage stage,
            Vector3 center,
            Vector2 extents,
            int count,
            int variantBase)
        {
            Transform zone = new GameObject(name).transform;
            zone.SetParent(parent, false);
            zone.localPosition = center;
            for (int i = 0; i < count; i++)
            {
                float angle = i * 2.399963f;
                float radial = Mathf.Sqrt((i + .5f) / count);
                Vector3 position = new(
                    Mathf.Cos(angle) * extents.x * radial,
                    .1f + (i % 3) * .018f,
                    Mathf.Sin(angle) * extents.y * radial);
                float scale = stage == BroodStage.Egg ? .105f :
                    stage == BroodStage.Larva ? .17f : .2f;
                WorldAssetVisualFactory.Brood(
                    zone,
                    stage,
                    position,
                    scale * (.9f + (i % 4) * .04f),
                    variantBase + i);
            }
        }

        void BuildLivingColony()
        {
            Vector3 nursery = UndergroundChamberCenters[NurseryChamberIndex];
            Vector3 queen = UndergroundChamberCenters[QueenChamberIndex];
            Vector3 food = UndergroundChamberCenters[FoodChamberIndex];
            Vector3 eggs = UndergroundChamberCenters[EggChamberIndex];
            Vector3 pupae = UndergroundChamberCenters[PupaChamberIndex];
            Vector3 sanitation = UndergroundChamberCenters[SanitationChamberIndex];
            Vector3 entrance = UndergroundChamberCenters[EntranceChamberIndex];
            Vector3 guard = UndergroundChamberCenters[GuardChamberIndex];

            CreateNestWorker("Nursery nurse A", new[]
            {
                nursery + new Vector3(-2.2f, 0, -1.1f),
                nursery + new Vector3(-1.1f, 0, -2.25f),
                nursery + new Vector3(.2f, 0, -2.45f),
                nursery + new Vector3(1.4f, 0, -1.75f),
                nursery + new Vector3(.55f, 0, -.65f)
            }, 1.12f, NestWorkerLoad.Larva, 0);
            CreateNestWorker("Nursery nurse B", new[]
            {
                nursery + new Vector3(2.15f, 0, 1.15f),
                nursery + new Vector3(1.1f, 0, 2.25f),
                nursery + new Vector3(-.5f, 0, 2.35f),
                nursery + new Vector3(-1.8f, 0, 1.25f),
                nursery + new Vector3(-.2f, 0, .55f)
            }, 1.05f, NestWorkerLoad.Egg, 1);
            CreateNestWorker("Egg transfer worker", ComposeNestRoute(
                nursery, eggs, (4, false)), 1.2f, NestWorkerLoad.Egg, 2);
            CreateNestWorker("Pupa transfer worker", ComposeNestRoute(
                nursery, pupae, (5, false)), 1.08f, NestWorkerLoad.Pupa, 3);
            CreateNestWorker("Seed porter", ComposeNestRoute(
                food, nursery, (1, true), (2, false)), 1.22f, NestWorkerLoad.Seed, 4);
            CreateNestWorker("Protein porter", ComposeNestRoute(
                entrance, food, (9, true)), 1.16f, NestWorkerLoad.Protein, 5);
            CreateNestWorker("Royal brood attendant", ComposeNestRoute(
                queen, nursery, (0, true), (2, false)), 1.04f, NestWorkerLoad.Larva, 6);
            CreateNestWorker("Queen chamber attendant", ComposeNestRoute(
                queen + new Vector3(-1.25f, 0, -.8f),
                queen + new Vector3(1.45f, 0, 1.05f)),
                0.88f, NestWorkerLoad.None, 7);
            CreateNestWorker("Sanitation worker", ComposeNestRoute(
                sanitation, queen, (7, false)), 1.08f, NestWorkerLoad.Refuse, 8);
            CreateNestWorker("Egg gallery cleaner", ComposeNestRoute(
                eggs, sanitation, (6, false)), 1.12f, NestWorkerLoad.Refuse, 9);
            CreateNestWorker("Guard relief worker", ComposeNestRoute(
                guard, pupae, (10, true)), 1.18f, NestWorkerLoad.None, 10);
            CreateNestWorker("Entrance traffic worker", ComposeNestRoute(
                entrance, guard, (11, true)), 1.24f, NestWorkerLoad.Seed, 11);
        }

        Vector3[] ComposeNestRoute(
            Vector3 start,
            Vector3 end,
            params (int tunnelIndex, bool reverse)[] sections)
        {
            var route = new List<Vector3> { start };
            foreach ((int tunnelIndex, bool reverse) in sections)
            {
                IReadOnlyList<Vector3> path = UndergroundTunnelPaths[tunnelIndex];
                if (reverse)
                {
                    for (int i = path.Count - 1; i >= 0; i--) route.Add(path[i]);
                }
                else
                {
                    for (int i = 0; i < path.Count; i++) route.Add(path[i]);
                }
            }
            route.Add(end);
            return route.ToArray();
        }

        void CreateNestWorker(
            string name,
            IReadOnlyList<Vector3> localRoute,
            float speed,
            NestWorkerLoad load,
            int phase)
        {
            var worker = new GameObject(name);
            worker.transform.SetParent(underground, false);
            worker.transform.position = UndergroundCenter + localRoute[0] + Vector3.up * .035f;
            AntVisual visual = AntVisual.Create(
                worker.transform,
                new Color(.24f, .055f, .016f),
                load is NestWorkerLoad.Egg or NestWorkerLoad.Larva or NestWorkerLoad.Pupa ? .6f : .66f,
                load is NestWorkerLoad.Egg or NestWorkerLoad.Larva or NestWorkerLoad.Pupa
                    ? AntCaste.Nurse
                    : AntCaste.Worker);
            var body = worker.AddComponent<SphereCollider>();
            body.center = new Vector3(0, .22f, 0);
            body.radius = .17f;
            body.isTrigger = true;
            Vector3[] worldRoute = localRoute
                .Select(point => UndergroundCenter + point + Vector3.up * .035f)
                .ToArray();
            worker.AddComponent<NestWorkerRoutine>().Initialize(
                worldRoute, speed, load, phase, visual, body);
        }

        void BuildNestGuideLights()
        {
            undergroundGuideLights.Clear();
            (int chamber, Color color, float range, float intensity)[] lights =
            {
                (CentralChamberIndex, new Color(.7f,.55f,.36f), 7.5f, 1.45f),
                (FoodChamberIndex, new Color(.74f,.48f,.27f), 6f, 1.16f),
                (EntranceChamberIndex, new Color(.38f,.62f,.55f), 6.5f, 1.28f),
                (EggChamberIndex, new Color(.82f,.67f,.45f), 6f, 1.34f),
                (PupaChamberIndex, new Color(.72f,.5f,.34f), 6f, 1.22f),
                (SanitationChamberIndex, new Color(.4f,.5f,.35f), 5.2f, .9f),
                (GuardChamberIndex, new Color(.5f,.62f,.45f), 5.5f, 1.08f)
            };
            foreach ((int chamber, Color color, float range, float intensity) in lights)
            {
                Light light = new GameObject($"Soft reflected chamber light {chamber}").AddComponent<Light>();
                light.transform.SetParent(underground, false);
                light.transform.localPosition = UndergroundChamberCenters[chamber] + Vector3.up * 1.55f;
                light.type = LightType.Point;
                light.range = range;
                light.intensity = intensity;
                light.color = color;
                light.shadows = LightShadows.None;
                undergroundGuideLights.Add(light);
            }
        }

        void CreateNestDoor(string name, Vector3 position, bool undergroundDoor)
        {
            var door = new GameObject(name);
            door.transform.SetParent(environment, false);
            door.transform.position = position;
            var collider = door.AddComponent<SphereCollider>();
            collider.radius = 1.28f;
            collider.isTrigger = true;
            door.AddComponent<ColonyEntrance>().Initialize(undergroundDoor);
            door.AddComponent<IInteractableHost>().Target = door.GetComponent<ColonyEntrance>();
            GameObject opening = VisualFactory.OrganicPart(
                "Shadowed earthen tunnel throat",
                door.transform,
                OrganicMeshFactory.BodyShape.Eye,
                Vector3.zero,
                new Vector3(2.15f, .42f, 1.7f),
                new Color(.08f, .052f, .031f),
                .025f);
            opening.transform.localRotation = Quaternion.Euler(90, 0, 0);

            Vector3[] archPath =
            {
                new(-1.52f, -.16f, .08f), new(-1.34f, .52f, .015f),
                new(-.92f, 1.05f, -.06f), new(0, 1.42f, -.12f),
                new(.92f, 1.05f, -.06f), new(1.34f, .52f, .015f),
                new(1.52f, -.16f, .08f)
            };
            VisualFactory.MeshObject(
                "Continuous curved root tunnel collar",
                door.transform,
                OrganicMeshFactory.Tube(archPath,
                    new[] { .24f, .22f, .18f, .16f, .18f, .22f, .24f }, 16),
                Vector3.zero,
                Vector3.one,
                VisualFactory.PbrMaterial("Bark", new Color(.54f, .38f, .2f),
                    .05f, 1.1f, new Vector2(1.2f, 2.2f)));
            Light mouthFill = new GameObject("Soft reflected tunnel light").AddComponent<Light>();
            mouthFill.transform.SetParent(door.transform, false);
            mouthFill.transform.localPosition = new Vector3(0, .42f, .32f);
            mouthFill.type = LightType.Point;
            mouthFill.range = 3.2f;
            mouthFill.intensity = undergroundDoor ? .7f : .42f;
            mouthFill.color = undergroundDoor
                ? new Color(.45f, .62f, .49f)
                : new Color(.82f, .59f, .31f);
            mouthFill.shadows = LightShadows.None;
        }

        void BuildLandmarks()
        {
            VisualFactory.HeroTexturedRoot(
                "Long rain-fallen branch landmark",
                environment,
                new[]
                {
                    At(3.4f, 6.6f, .24f), At(5.2f, 7.5f, .42f),
                    At(7.4f, 8.7f, .62f), At(9.3f, 10.1f, .72f),
                    At(12.1f, 11.6f, .68f), At(14.8f, 12.9f, .52f),
                    At(16.7f, 13.8f, .34f), At(18.2f, 14.2f, .16f)
                },
                new[] { .68f, .72f, .7f, .64f, .56f, .43f, .27f, .12f },
                true);
            VisualFactory.HeroTexturedRoot(
                "Broken lateral branch",
                environment,
                new[]
                {
                    At(10.7f, 10.95f, .72f), At(11.8f, 9.85f, .82f),
                    At(13.05f, 8.7f, .7f), At(14.2f, 7.8f, .3f)
                },
                new[] { .32f, .27f, .2f, .07f },
                true);
            VisualFactory.HeroTexturedRoot(
                "Weathered branch fork",
                environment,
                new[]
                {
                    At(10.9f, 11.4f, .22f), At(11.7f, 10.55f, .48f),
                    At(12.55f, 9.45f, .78f), At(13.2f, 8.3f, 1.05f),
                    At(13.75f, 7.2f, 1.18f)
                },
                new[] { .5f, .45f, .35f, .24f, .11f },
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

            VisualFactory.HeroTexturedRoot(
                "Partly buried pond root ridge",
                environment,
                new[]
                {
                    At(-20f, -1f, .12f), At(-18.2f, 2f, .38f),
                    At(-16f, 5f, .58f), At(-13.7f, 8.9f, .84f),
                    At(-11f, 13f, .78f), At(-8.6f, 17f, .42f), At(-7f, 19f, .16f)
                },
                new[] { 1.05f, 1.02f, .92f, .76f, .58f, .38f, .18f },
                true);

            VisualFactory.HeroTexturedRoot(
                "Climbable arcing feeder root bridge",
                environment,
                new[]
                {
                    At(1.5f, 17f, .12f), At(4.1f, 17.55f, .46f),
                    At(6.2f, 18.4f, .82f), At(9.2f, 19.35f, 1.12f),
                    At(12.5f, 20.4f, 1.18f), At(15f, 21.15f, .7f), At(17f, 21.6f, .18f)
                },
                new[] { .7f, .7f, .64f, .55f, .46f, .3f, .12f },
                true);
        }

        void BuildHeroMicrohabitat()
        {
            var habitat = new GameObject("Maximum-quality playable microhabitat").transform;
            habitat.SetParent(environment, false);
            int xSegments = RuntimeQualityProfile.IsFullQuality ? 96 : 54;
            int zSegments = RuntimeQualityProfile.IsFullQuality ? 80 : 46;
            VisualFactory.HeroMicroTerrain(
                habitat,
                HeroMicrohabitatCenter,
                new Vector2(12f, 10f),
                xSegments,
                zSegments,
                GroundHeight);

            // A small root system establishes the terrain's moisture and shelter
            // zones. Branches follow the ground instead of floating above it.
            VisualFactory.HeroTexturedRoot(
                "Fine feeder root beside the ant path",
                habitat,
                new[]
                {
                    At(5.35f, 18.35f, .04f), At(7.35f, 18.18f, .17f),
                    At(9.55f, 18.42f, .24f), At(12.75f, 19.05f, .06f)
                },
                new[] { .23f, .3f, .25f, .08f },
                true);
            VisualFactory.HeroTexturedRoot(
                "Forked feeder root",
                habitat,
                new[]
                {
                    At(9.1f, 18.35f, .13f), At(10.15f, 17.55f, .16f),
                    At(11.2f, 16.85f, .08f), At(12.25f, 16.35f, .025f)
                },
                new[] { .18f, .15f, .11f, .035f },
                true);
            VisualFactory.HeroTexturedRoot(
                "Hair root crossing damp soil",
                habitat,
                new[]
                {
                    At(7.75f, 18.12f, .1f), At(7.2f, 17.25f, .08f),
                    At(6.7f, 16.4f, .035f), At(6.2f, 15.8f, .018f)
                },
                new[] { .13f, .105f, .065f, .022f },
                true);

            Vector3[] stones =
            {
                At(6.15f, 17.72f, .16f), At(7.25f, 18.68f, .13f),
                At(10.85f, 18.55f, .17f), At(12.2f, 17.45f, .14f),
                At(12.55f, 14.55f, .11f), At(10.9f, 13.55f, .09f),
                At(7.15f, 13.55f, .1f), At(5.55f, 14.25f, .12f)
            };
            for (int i = 0; i < stones.Length; i++)
            {
                float size = .38f + (i % 3) * .12f;
                VisualFactory.HeroStone(
                    habitat,
                    stones[i],
                    new Vector3(size * 1.25f, size * .62f, size),
                    260 + i,
                    true);
            }

            Vector3[] mossPositions =
            {
                At(6.55f, 18.2f, .025f), At(7.95f, 18.55f, .025f),
                At(9.15f, 18.72f, .025f), At(10.25f, 18.48f, .025f),
                At(11.5f, 18.75f, .025f), At(6.05f, 17.45f, .025f),
                At(11.45f, 17.25f, .025f), At(5.65f, 15.75f, .02f)
            };
            for (int i = 0; i < mossPositions.Length; i++)
            {
                float scale = .42f + (i % 4) * .075f;
                VisualFactory.MossCushion(
                    habitat,
                    mossPositions[i],
                    new Vector3(scale, .38f + i % 2 * .07f, scale * .8f),
                    310 + i);
            }

            // Leaf litter accumulates downwind and beneath the root. The central
            // one-metre ant lane remains readable for navigation and combat.
            var random = new System.Random(92417);
            for (int i = 0; i < (RuntimeQualityProfile.IsFullQuality ? 24 : 13); i++)
            {
                float angle = Mathf.Lerp(-.35f, 3.35f, (float)random.NextDouble());
                float radius = Mathf.Lerp(1.55f, 5.15f, Mathf.Sqrt((float)random.NextDouble()));
                float x = HeroMicrohabitatCenter.x + Mathf.Cos(angle) * radius;
                float z = HeroMicrohabitatCenter.y + Mathf.Sin(angle) * radius * .78f;
                if (Vector2.Distance(new Vector2(x, z), HeroMicrohabitatCenter) < 1.2f)
                    continue;
                VisualFactory.HeroFallenLeaf(
                    habitat,
                    At(x, z, .035f),
                    new Vector3(
                        Mathf.Lerp(.68f, 1.28f, (float)random.NextDouble()),
                        1f,
                        Mathf.Lerp(.68f, 1.22f, (float)random.NextDouble())),
                    400 + i);
            }
            Vector3[] specimenLeaves =
            {
                At(8.05f, 15.28f, .038f),
                At(8.1f, 16.42f, .036f),
                At(7.35f, 15.88f, .037f)
            };
            for (int i = 0; i < specimenLeaves.Length; i++)
                VisualFactory.HeroFallenLeaf(
                    habitat,
                    specimenLeaves[i],
                    new Vector3(1.05f + i * .08f, 1f, .94f + i * .05f),
                    470 + i);

            // Grass forms light-seeking colonies at the open and damp margins,
            // rather than a uniform random field across the traversal lane.
            int grassCount = RuntimeQualityProfile.IsFullQuality ? 30 : 15;
            for (int i = 0; i < grassCount; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float along = Mathf.Lerp(-4.25f, 4.25f, (float)random.NextDouble());
                float distanceFromLane = Mathf.Lerp(1.55f, 4.75f,
                    Mathf.Pow((float)random.NextDouble(), .72f));
                float x = HeroMicrohabitatCenter.x + side * distanceFromLane;
                float z = HeroMicrohabitatCenter.y + along +
                          Mathf.Sin(x * .8f) * .28f;
                Vector2 local = new(x - HeroMicrohabitatCenter.x, z - HeroMicrohabitatCenter.y);
                if (Mathf.Abs(local.x) > 5.7f || Mathf.Abs(local.y) > 4.65f)
                    continue;
                Vector2 puddleCenter = HeroMicrohabitatCenter + new Vector2(-3.25f, -.85f);
                if (Vector2.Distance(new Vector2(x, z), puddleCenter) < 1.45f)
                    continue;
                Color color = Color.Lerp(
                    new Color(.31f, .46f, .13f),
                    new Color(.55f, .67f, .25f),
                    (float)random.NextDouble());
                GameObject grass = VisualFactory.HeroGrassTuft(
                    habitat,
                    At(x, z, .015f),
                    Mathf.Lerp(.62f, 1.12f, (float)random.NextDouble()),
                    color,
                    500 + i);
                grass.transform.localRotation = Quaternion.Euler(
                    0,
                    Mathf.Lerp(0, 360, (float)random.NextDouble()),
                    Mathf.Lerp(-5f, 5f, (float)random.NextDouble()));
                if (i < 18)
                    grass.AddComponent<ReactiveVegetation>().Initialize();
            }
            Vector3[] specimenGrass =
            {
                At(11.15f, 15.55f, .015f),
                At(11.62f, 15.08f, .015f),
                At(10.92f, 14.88f, .015f),
                At(11.7f, 16.08f, .015f)
            };
            for (int i = 0; i < specimenGrass.Length; i++)
            {
                GameObject grass = VisualFactory.HeroGrassTuft(
                    habitat,
                    specimenGrass[i],
                    .74f + i * .08f,
                    Color.Lerp(new Color(.43f, .58f, .18f),
                        new Color(.68f, .72f, .3f), i / 3f),
                    590 + i);
                grass.transform.localRotation = Quaternion.Euler(0, i * 71f, i - 2f);
                grass.AddComponent<ReactiveVegetation>().Initialize();
            }

            // Four real botanical silhouettes replace the former grass-only
            // wall while keeping the central ant lane open and readable.
            Vector3[] groundcoverPositions =
            {
                At(6.25f, 14.45f, .018f), At(6.75f, 17.15f, .018f),
                At(7.65f, 18.8f, .018f), At(9.15f, 19.05f, .018f),
                At(10.55f, 18.75f, .018f), At(12.15f, 17.8f, .018f),
                At(12.65f, 15.65f, .018f), At(11.8f, 13.85f, .018f),
                At(8.1f, 13.7f, .018f), At(5.7f, 16.25f, .018f)
            };
            for (int i = 0; i < groundcoverPositions.Length; i++)
            {
                GameObject patch = VisualFactory.GroundcoverPatch(
                    habitat,
                    groundcoverPositions[i],
                    .58f + (i % 4) * .11f,
                    Color.Lerp(new Color(.62f, .72f, .43f), Color.white, (i % 3) * .1f),
                    720 + i);
                patch.transform.localRotation = Quaternion.Euler(0, i * 47f, 0);
                if (i < 6) patch.AddComponent<ReactiveVegetation>().Initialize();
            }

            Vector3 puddlePosition = At(
                HeroMicrohabitatCenter.x - 3.25f,
                HeroMicrohabitatCenter.y - .85f,
                .026f);
            VisualFactory.HeroPuddle(
                habitat,
                puddlePosition,
                new Vector3(1.28f, 1f, .82f),
                7);

            Debug.Log(
                $"MOONROOT_HERO_MICROHABITAT_READY ground={xSegments}x{zSegments} " +
                $"grass={grassCount + specimenGrass.Length} groundcover={groundcoverPositions.Length} " +
                $"leaves={(RuntimeQualityProfile.IsFullQuality ? 24 : 13) + specimenLeaves.Length} " +
                "stones=8 moss=8 roots=3 puddles=1");
        }

        Vector3 At(float x, float z, float above = 0) => new(x, GroundHeight(x, z) + above, z);

        void BuildVegetation()
        {
            InstancedVegetation instancedGrass =
                environment.gameObject.AddComponent<InstancedVegetation>();
            int grassCount = RuntimeQualityProfile.GrassCount(GameSettings.Quality);
            Vector2[] lightSeekingColonies =
            {
                new(-8f, 3.5f), new(-8.5f, 15.5f), new(6.8f, 5.8f),
                new(15.5f, 12.8f), new(17f, 1.5f), new(-17f, -3f),
                new(8.5f, -8.5f), new(-8.5f, -11f), new(-14f, 23f),
                new(13.5f, 25f), new(1f, 28f)
            };
            int placedGrass = 0;
            int grassAttempts = 0;
            while (placedGrass < grassCount && grassAttempts++ < grassCount * 5)
            {
                Vector2 p;
                if (Random.value < .78f)
                {
                    Vector2 center = lightSeekingColonies[Random.Range(0, lightSeekingColonies.Length)];
                    Vector2 offset = Random.insideUnitCircle * Random.Range(2.1f, 5.4f);
                    p = center + offset;
                }
                else
                {
                    Vector2 circle = Random.insideUnitCircle * 33f;
                    p = new Vector2(circle.x, circle.y + 5f);
                }
                float x = p.x;
                float z = p.y;
                if (KeepClear(x, z)) continue;
                float exposure = Mathf.PerlinNoise(x * .075f + 17f, z * .075f + 41f);
                if (exposure < .24f && Random.value > .32f) continue;
                float height = Random.Range(.72f, 2.3f);
                float age = Mathf.Clamp01(Random.value * .78f + (1f - exposure) * .25f);
                Color grass = Color.Lerp(
                    new Color(.27f, .45f, .11f),
                    new Color(.58f, .67f, .22f),
                    1f - age);
                Quaternion rotation = Quaternion.Euler(
                    0, Random.Range(0, 360f), Random.Range(-4f, 4f));
                Vector3 scale = new(.82f, height, .82f);
                if (SystemInfo.supportsInstancing)
                    instancedGrass.Add(placedGrass, At(x, z), rotation, scale, grass);
                else
                {
                    GameObject tuft = VisualFactory.GrassTuft(
                        environment, At(x, z), height, grass, placedGrass);
                    tuft.transform.localRotation = rotation;
                }
                placedGrass++;
            }
            instancedGrass.Complete();

            int groundcoverCount = RuntimeQualityProfile.IsFullQuality ? 84 : 34;
            for (int i = 0; i < groundcoverCount; i++)
            {
                Vector2 center = lightSeekingColonies[(i * 7 + 3) % lightSeekingColonies.Length];
                Vector2 offset = Random.insideUnitCircle * Random.Range(1.4f, 5.6f);
                Vector2 p = center + offset;
                if (KeepClear(p.x, p.y)) continue;
                GameObject patch = VisualFactory.GroundcoverPatch(
                    environment,
                    At(p.x, p.y, .016f),
                    Random.Range(.48f, 1.08f),
                    Color.Lerp(new Color(.58f, .68f, .38f), Color.white, Random.Range(0f, .18f)),
                    900 + i);
                patch.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            }

            int leafCount = RuntimeQualityProfile.LeafCount(GameSettings.Quality);
            Vector2[] litterShelters =
            {
                new(11.7f, 10.9f), new(-13.5f, 8.8f), new(9.1f, 19.35f),
                new(-18f, -2f), new(17f, 3f), new(-8f, 24f)
            };
            int placedLeaves = 0;
            int leafAttempts = 0;
            while (placedLeaves < leafCount && leafAttempts++ < leafCount * 6)
            {
                Vector2 p;
                if (Random.value < .74f)
                {
                    Vector2 shelter = litterShelters[Random.Range(0, litterShelters.Length)];
                    Vector2 offset = Random.insideUnitCircle * Random.Range(1.3f, 5.8f);
                    offset.y *= .62f;
                    p = shelter + offset;
                }
                else
                {
                    Vector2 circle = Random.insideUnitCircle * 29f;
                    p = new Vector2(circle.x, circle.y + 5f);
                }
                if (KeepClear(p.x, p.y)) continue;
                VisualFactory.FallenLeaf(environment, At(p.x, p.y, .035f),
                    new Vector3(Random.Range(.8f, 1.65f), 1, Random.Range(.75f, 1.35f)),
                    placedLeaves);
                placedLeaves++;
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
                    .88f + (i % 4) * .18f,
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
            camera.nearClipPlane = .018f;
            camera.farClipPlane = RuntimeQualityProfile.IsFullQuality ? 180f : 125f;
            camera.allowHDR = RuntimeQualityProfile.IsFullQuality;
            camera.allowMSAA = GameSettings.Quality > 0;
            cameraObject.AddComponent<AudioListener>();

            GameObject playerPrefab = Resources.Load<GameObject>("Prefabs/PlayerScoutAnt");
            GameObject playerObject;
            if (playerPrefab)
                playerObject = Instantiate(playerPrefab, environment, false);
            else
            {
                playerObject = new GameObject("Player scout ant");
                playerObject.transform.SetParent(environment, false);
                playerObject.AddComponent<CharacterController>();
                playerObject.AddComponent<PlayerAnt>();
            }
            playerObject.name = "Player scout ant";
            playerObject.transform.position = UndergroundPlayerSpawn;
            Player = playerObject.GetComponent<PlayerAnt>();

            UnitRole[] roles =
            {
                UnitRole.Worker, UnitRole.Worker, UnitRole.Worker, UnitRole.Worker,
                UnitRole.LightSoldier, UnitRole.LightSoldier, UnitRole.LightSoldier, UnitRole.HeavySoldier
            };
            for (int i = 0; i < roles.Length; i++)
            {
                var unit = new GameObject($"{roles[i]} {i + 1}");
                unit.transform.SetParent(environment, false);
                Vector3 offset = new((i % 4 - 1.5f) * .58f, 0, (i / 4 - .5f) * .7f);
                unit.transform.position = UndergroundSquadBay + offset;
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
                // Face back toward the entrance after emerging. The prior
                // inherited +Z heading placed the third-person camera inside
                // the large mound behind the player and filled the WebGL view
                // with soil. This orientation presents the entrance as a clear
                // landmark and keeps the full camera boom on open terrain.
                player.Face(NestPosition + Vector3.up * .32f, 11f);
                squads.Teleport(SurfacePlayerSpawn + Vector3.forward * .8f);
                ShowToast(GameText.Pick("Forest floor — the rain has stopped", "Лесная подстилка — дождь закончился"));
            }
            else
            {
                IsUnderground = true;
                player.Teleport(UndergroundEntrySpawn);
                player.Face(
                    UndergroundCenter + UndergroundChamberCenters[CentralChamberIndex] +
                    Vector3.up * .35f,
                    12f);
                squads.Teleport(
                    UndergroundCenter + UndergroundChamberCenters[EntranceChamberIndex] +
                    new Vector3(1.15f, .035f, -.25f));
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

            if (!string.IsNullOrEmpty(collisionQaCaption))
            {
                GUI.Box(new Rect(width * .5f - 330, 82, 660, 48), "");
                GUI.Label(new Rect(width * .5f - 318, 86, 636, 38),
                    collisionQaCaption,
                    centered);
            }

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

    public sealed class NestChamberMarker : MonoBehaviour
    {
        public string ChamberName { get; private set; }
        public Vector3 ClearRadii { get; private set; }
        public int PortalCount { get; private set; }
        public Collider FloorCollider { get; private set; }
        public Collider ShellCollider { get; private set; }

        public void Initialize(
            string chamberName,
            Vector3 clearRadii,
            int portalCount,
            Collider floorCollider,
            Collider shellCollider)
        {
            ChamberName = chamberName;
            ClearRadii = clearRadii;
            PortalCount = portalCount;
            FloorCollider = floorCollider;
            ShellCollider = shellCollider;
        }
    }

    public sealed class TunnelClearanceMarker : MonoBehaviour
    {
        public string PassageName { get; private set; }
        public float ClearWidth { get; private set; }
        public float ClearHeight { get; private set; }
        public bool IsBusyRoute { get; private set; }
        public Collider FloorCollider { get; private set; }
        public Collider ShellCollider { get; private set; }

        public void Initialize(
            string passageName,
            float clearWidth,
            float clearHeight,
            bool isBusyRoute,
            Collider floorCollider,
            Collider shellCollider)
        {
            PassageName = passageName;
            ClearWidth = clearWidth;
            ClearHeight = clearHeight;
            IsBusyRoute = isBusyRoute;
            FloorCollider = floorCollider;
            ShellCollider = shellCollider;
        }
    }
}
