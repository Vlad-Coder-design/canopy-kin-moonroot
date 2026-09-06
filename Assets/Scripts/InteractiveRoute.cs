using UnityEngine;

namespace CanopyKin
{
    public enum NestWorkerLoad
    {
        None,
        Egg,
        Larva,
        Pupa,
        Seed,
        Protein,
        Refuse
    }

    public sealed class MovementSurface : MonoBehaviour
    {
        public string DisplayName { get; private set; } = "Soil";
        public float SpeedMultiplier { get; private set; } = 1f;

        public MovementSurface Initialize(string displayName, float speedMultiplier = 1f)
        {
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Soil" : displayName;
            SpeedMultiplier = Mathf.Clamp(speedMultiplier, .55f, 1.08f);
            return this;
        }
    }

    public sealed class ReactiveVegetation : MonoBehaviour
    {
        Quaternion restRotation;
        Vector3 restPosition;
        float reaction;
        float pollenCooldown;
        float bendDirection;

        public void Initialize()
        {
            restRotation = transform.localRotation;
            restPosition = transform.localPosition;
            bendDirection = Random.value > .5f ? 1f : -1f;
        }

        void Update()
        {
            PlayerAnt player = WorldBootstrap.Instance ? WorldBootstrap.Instance.Player : null;
            if (!player)
            {
                reaction = Mathf.MoveTowards(reaction, 0, Time.deltaTime * 2.2f);
                ApplyPose();
                return;
            }

            Vector3 offset = transform.position - player.transform.position;
            offset.y = 0;
            float target = 1f - Mathf.Clamp01(offset.magnitude / 1.25f);
            reaction = Mathf.MoveTowards(reaction, target, Time.deltaTime * 7.5f);
            pollenCooldown -= Time.deltaTime;
            if (target > .68f && pollenCooldown <= 0)
            {
                pollenCooldown = 1.1f;
                FxPool.Instance?.Burst(
                    transform.position + Vector3.up * 1.1f,
                    new Color(.75f, .74f, .38f),
                    4);
            }
            ApplyPose();
        }

        void ApplyPose()
        {
            float wind = Mathf.Sin(Time.time * 1.7f + transform.position.x * .43f) * 2.5f;
            transform.localRotation = restRotation *
                Quaternion.Euler(reaction * 16f, wind, bendDirection * reaction * 23f);
            transform.localPosition = restPosition +
                new Vector3(bendDirection * reaction * .08f, 0, reaction * .045f);
        }
    }

    public sealed class AmbientAntPatrol : MonoBehaviour
    {
        Vector3 pointA;
        Vector3 pointB;
        Vector3 target;
        float speed;
        float wait;

        public void Initialize(Vector3 a, Vector3 b, float movementSpeed)
        {
            pointA = a;
            pointB = b;
            target = b;
            speed = Mathf.Max(.3f, movementSpeed);
        }

        void Update()
        {
            WorldBootstrap world = WorldBootstrap.Instance;
            if (!world || !world.IsPlaying || world.IsPaused || world.IsUnderground) return;
            if (wait > 0)
            {
                wait -= Time.deltaTime;
                return;
            }

            Vector3 direction = target - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude < .08f)
            {
                target = (target - pointA).sqrMagnitude < .1f ? pointB : pointA;
                wait = Random.Range(.35f, .85f);
                return;
            }

            direction.Normalize();
            Collider body = GetComponent<Collider>();
            CollisionSafety.MoveSphere(
                transform,
                body,
                direction,
                speed * Time.deltaTime,
                .22f,
                .15f);
            transform.position = world.ConstrainActorPosition(transform.position, .15f);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                420f * Time.deltaTime);
        }
    }

    /// <summary>
    /// Lightweight, collision-aware nest traffic.  These workers are not squad
    /// units: they make the home feel occupied, carry visible loads between
    /// rooms, yield to the player and use authored routes through the same
    /// tunnel centre lines that generate collision geometry.
    /// </summary>
    public sealed class NestWorkerRoutine : MonoBehaviour
    {
        static readonly System.Collections.Generic.List<NestWorkerRoutine> Active = new();
        Vector3[] route;
        int targetIndex;
        int direction = 1;
        float speed;
        float wait;
        float stuck;
        Vector3 lastPosition;
        NestWorkerLoad load;
        GameObject loadVisual;
        AntVisual visual;
        SphereCollider body;
        bool carrying = true;
        float totalTravelDistance;

        public bool Carrying => carrying && load != NestWorkerLoad.None;
        public NestWorkerLoad Load => load;
        public int RoutePointCount => route?.Length ?? 0;
        public float TotalTravelDistance => totalTravelDistance;

        public void Initialize(
            Vector3[] worldRoute,
            float movementSpeed,
            NestWorkerLoad carriedLoad,
            int phase,
            AntVisual antVisual,
            SphereCollider bodyCollider)
        {
            route = worldRoute;
            speed = Mathf.Max(.55f, movementSpeed);
            load = carriedLoad;
            visual = antVisual;
            body = bodyCollider;
            targetIndex = route == null || route.Length < 2
                ? 0
                : Mathf.Clamp(1 + phase % (route.Length - 1), 1, route.Length - 1);
            if (route != null && route.Length > 0)
                transform.position = route[Mathf.Max(0, targetIndex - 1)];
            lastPosition = transform.position;
            CreateLoadVisual(phase);
        }

        void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
        }

        void OnDisable() => Active.Remove(this);

        void CreateLoadVisual(int variant)
        {
            if (load == NestWorkerLoad.None) return;
            switch (load)
            {
                case NestWorkerLoad.Egg:
                case NestWorkerLoad.Larva:
                case NestWorkerLoad.Pupa:
                    BroodStage stage = load == NestWorkerLoad.Egg
                        ? BroodStage.Egg
                        : load == NestWorkerLoad.Larva ? BroodStage.Larva : BroodStage.Pupa;
                    loadVisual = WorldAssetVisualFactory.Brood(
                        transform,
                        stage,
                        new Vector3(0, .49f, -.03f),
                        stage == BroodStage.Egg ? .085f : stage == BroodStage.Larva ? .12f : .14f,
                        700 + variant);
                    break;
                case NestWorkerLoad.Seed:
                case NestWorkerLoad.Protein:
                    loadVisual = ResourceNode.CreateCargoVisual(
                        transform,
                        load == NestWorkerLoad.Seed ? ResourceKind.Seed : ResourceKind.Protein,
                        new Vector3(0, .47f, -.04f),
                        .2f,
                        730 + variant);
                    break;
                default:
                    loadVisual = VisualFactory.OrganicPart(
                        "Carried dry refuse",
                        transform,
                        OrganicMeshFactory.BodyShape.Brood,
                        new Vector3(0, .47f, -.04f),
                        new Vector3(.11f, .08f, .14f),
                        new Color(.2f, .12f, .06f),
                        .03f);
                    break;
            }
            visual?.SetCarrying(true);
        }

        void Update()
        {
            WorldBootstrap world = WorldBootstrap.Instance;
            if (!world || !world.IsPlaying || world.IsPaused || !world.IsUnderground ||
                route == null || route.Length < 2) return;
            if (wait > 0)
            {
                wait -= Time.deltaTime;
                visual?.SetPlayerMotion(0, 0, true, Vector3.up);
                return;
            }

            Vector3 toTarget = route[targetIndex] - transform.position;
            toTarget.y = 0;
            if (toTarget.sqrMagnitude < .09f)
            {
                AdvanceRoute();
                return;
            }

            Vector3 wanted = toTarget.normalized;
            Vector3 avoidance = Vector3.zero;
            foreach (NestWorkerRoutine other in Active)
            {
                if (!other || other == this) continue;
                Vector3 away = transform.position - other.transform.position;
                away.y = 0;
                float distance = away.magnitude;
                if (distance > .01f && distance < .62f)
                    avoidance += away.normalized * (1f - distance / .62f);
            }
            if (world.Player)
            {
                Vector3 fromPlayer = transform.position - world.Player.transform.position;
                fromPlayer.y = 0;
                float distance = fromPlayer.magnitude;
                if (distance > .01f && distance < 1.05f)
                    avoidance += fromPlayer.normalized * (1f - distance / 1.05f) * 2.8f;
            }

            Vector3 movement = wanted + avoidance * .92f;
            if (movement.sqrMagnitude < .04f)
            {
                wait = .18f;
                return;
            }
            movement.Normalize();
            Vector3 before = transform.position;
            CollisionSafety.MoveSphere(
                transform,
                body,
                movement,
                speed * Time.deltaTime,
                .22f,
                .145f);
            transform.position = world.ConstrainActorPosition(transform.position, .18f);
            Vector3 actual = transform.position - before;
            actual.y = 0;
            totalTravelDistance += actual.magnitude;
            float actualSpeed = actual.magnitude / Mathf.Max(Time.deltaTime, .0001f);
            if (actual.sqrMagnitude > .00003f)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(actual.normalized, Vector3.up),
                    460f * Time.deltaTime);
            visual?.SetPlayerMotion(actualSpeed, Mathf.InverseLerp(0, speed, actualSpeed), true, Vector3.up);

            if ((transform.position - lastPosition).sqrMagnitude < .00006f) stuck += Time.deltaTime;
            else stuck = Mathf.Max(0, stuck - Time.deltaTime * 2f);
            lastPosition = transform.position;
            if (stuck > 1.1f)
            {
                // Re-enter the authored centre line instead of teleporting
                // through a wall or pushing the player.
                Vector3 routeDirection = toTarget.normalized;
                Vector3 side = Vector3.Cross(Vector3.up, routeDirection) *
                               (((targetIndex + GetInstanceID()) & 1) == 0 ? .28f : -.28f);
                CollisionSafety.MoveSphere(transform, body, side, .24f, .22f, .145f);
                transform.position = world.ConstrainActorPosition(transform.position, .2f);
                stuck = 0;
                wait = .12f;
            }
        }

        void AdvanceRoute()
        {
            bool endpoint = targetIndex == 0 || targetIndex == route.Length - 1;
            if (targetIndex == route.Length - 1) direction = -1;
            else if (targetIndex == 0) direction = 1;
            targetIndex += direction;
            targetIndex = Mathf.Clamp(targetIndex, 0, route.Length - 1);
            if (!endpoint) return;
            wait = Random.Range(.45f, 1.1f);
            if (loadVisual)
            {
                carrying = !carrying;
                loadVisual.SetActive(carrying);
                visual?.SetCarrying(carrying);
            }
        }
    }

    public sealed class PheromonePulse : MonoBehaviour
    {
        Vector3 restScale;
        Vector3 restPosition;
        float phase;

        public void Initialize(float seed)
        {
            restScale = transform.localScale;
            restPosition = transform.localPosition;
            phase = seed * 1.73f;
        }

        void Update()
        {
            float pulse = .92f + Mathf.Sin(Time.time * 3.2f + phase) * .11f;
            transform.localScale = restScale * pulse;
            transform.localPosition = restPosition +
                Vector3.up * (.025f + Mathf.Sin(Time.time * 2.1f + phase) * .018f);
        }
    }
}
