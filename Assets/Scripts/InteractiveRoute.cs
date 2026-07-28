using UnityEngine;

namespace CanopyKin
{
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
            Vector3 next = transform.position + direction * speed * Time.deltaTime;
            next.y = WorldBootstrap.GroundHeight(next.x, next.z) + .035f;
            transform.position = next;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                420f * Time.deltaTime);
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
