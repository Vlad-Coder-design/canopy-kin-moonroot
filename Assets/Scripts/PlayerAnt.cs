using UnityEngine;
using UnityEngine.InputSystem;

namespace CanopyKin
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerAnt : MonoBehaviour
    {
        CharacterController body;
        Camera viewCamera;
        float yaw;
        float pitch = 17f;
        float vertical;
        float attackCooldown;
        float promptTimer;
        IInteractable nearbyInteraction;
        bool pointerWasLocked;

        public float Health { get; private set; } = 100;
        public float Stamina { get; private set; } = 100;
        public string CurrentPrompt { get; private set; }
        public Transform CameraTransform => viewCamera ? viewCamera.transform : null;

        void Awake()
        {
            body = GetComponent<CharacterController>();
            body.height = .78f;
            body.radius = .27f;
            body.center = new Vector3(0, .39f, 0);
            body.stepOffset = .28f;
            body.slopeLimit = 55f;
            AntVisual.Create(transform, new Color(.28f, .105f, .028f));
        }

        void Start()
        {
            viewCamera = Camera.main;
            SnapCamera();
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || WorldBootstrap.Instance == null) return;

            WorldBootstrap world = WorldBootstrap.Instance;
            bool startPressed = keyboard.enterKey.wasPressedThisFrame ||
                                keyboard.spaceKey.wasPressedThisFrame ||
                                (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (!world.IsPlaying)
            {
                if (startPressed) world.BeginPlay();
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                world.TogglePause();
                return;
            }
            if (world.IsPaused) return;

            if (Application.platform == RuntimePlatform.WebGLPlayer &&
                Cursor.lockState != CursorLockMode.Locked &&
                mouse != null &&
                mouse.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                pointerWasLocked = true;
                return;
            }

            if (Cursor.lockState == CursorLockMode.Locked && mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue() * GameSettings.Sensitivity;
                yaw += delta.x;
                pitch = Mathf.Clamp(pitch - delta.y, 7f, 42f);
                pointerWasLocked = true;
            }

            Vector2 input = new(
                (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            input = Vector2.ClampMagnitude(input, 1);

            Vector3 forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            bool sprint = keyboard.leftShiftKey.isPressed && Stamina > 1 && input.sqrMagnitude > .1f;
            float speed = sprint ? 5.4f : 3.35f;
            Stamina = Mathf.Clamp(Stamina + (sprint ? -25f : 18f) * Time.deltaTime, 0, 100);
            Vector3 planar = (forward * input.y + right * input.x) * speed;

            if (body.isGrounded) vertical = -.8f;
            else vertical -= 12f * Time.deltaTime;
            if (keyboard.spaceKey.wasPressedThisFrame && body.isGrounded) vertical = 3.4f;
            body.Move((planar + Vector3.up * vertical) * Time.deltaTime);

            if (planar.sqrMagnitude > .05f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(planar), 13f * Time.deltaTime);

            attackCooldown -= Time.deltaTime;
            promptTimer -= Time.deltaTime;
            if (promptTimer <= 0)
            {
                promptTimer = .12f;
                FindInteraction();
            }
            if (keyboard.eKey.wasPressedThisFrame) Interact();
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && attackCooldown <= 0) Attack();
        }

        void LateUpdate()
        {
            if (!viewCamera || WorldBootstrap.Instance == null) return;
            Vector3 target = transform.position + Vector3.up * .52f;
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0);
            Vector3 wanted = target + orbit * new Vector3(0, .65f, -3.25f);
            Vector3 direction = wanted - target;
            if (Physics.SphereCast(target, .16f, direction.normalized, out RaycastHit hit, direction.magnitude, ~0, QueryTriggerInteraction.Ignore))
                wanted = hit.point - direction.normalized * .18f;
            float smoothing = WorldBootstrap.Instance.IsPlaying ? 12f : 5f;
            viewCamera.transform.position = Vector3.Lerp(viewCamera.transform.position, wanted, smoothing * Time.deltaTime);
            viewCamera.transform.rotation = Quaternion.Slerp(
                viewCamera.transform.rotation,
                Quaternion.LookRotation(target - viewCamera.transform.position),
                smoothing * Time.deltaTime);
        }

        public void SnapCamera()
        {
            if (!viewCamera) viewCamera = Camera.main;
            if (!viewCamera) return;
            Vector3 target = transform.position + Vector3.up * .52f;
            viewCamera.transform.position = target + Quaternion.Euler(pitch, yaw, 0) * new Vector3(0, .65f, -3.25f);
            viewCamera.transform.rotation = Quaternion.LookRotation(target - viewCamera.transform.position);
        }

        public void Teleport(Vector3 position)
        {
            bool enabledBefore = body.enabled;
            body.enabled = false;
            transform.position = position;
            body.enabled = enabledBefore;
            SnapCamera();
        }

        void FindInteraction()
        {
            nearbyInteraction = null;
            CurrentPrompt = null;
            float best = float.MaxValue;
            Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up * .3f, 1.65f, ~0, QueryTriggerInteraction.Collide);
            foreach (Collider hit in hits)
            {
                IInteractableHost host = hit.GetComponentInParent<IInteractableHost>();
                if (!host || host.Target == null) continue;
                float distance = (hit.transform.position - transform.position).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                nearbyInteraction = host.Target;
            }
            if (nearbyInteraction != null)
                CurrentPrompt = nearbyInteraction.Prompt + GameText.Pick("  [E]", "  [E]");
        }

        void Interact()
        {
            nearbyInteraction?.Interact(this);
            FindInteraction();
        }

        void Attack()
        {
            attackCooldown = .48f;
            Vector3 direction = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            transform.rotation = Quaternion.LookRotation(direction);
            Vector3 point = transform.position + Vector3.up * .35f + direction * .78f;
            bool hitSomething = false;
            foreach (Collider hit in Physics.OverlapSphere(point, .72f, ~0, QueryTriggerInteraction.Ignore))
            {
                Creature creature = hit.GetComponentInParent<Creature>();
                if (!creature || !creature.IsActive) continue;
                creature.Damage(24);
                hitSomething = true;
            }
            WorldBootstrap.Instance.FlashCrosshair(hitSomething);
        }

        public void Damage(float value)
        {
            Health = Mathf.Max(0, Health - value);
            WorldBootstrap.Instance.ShowToast(GameText.Pick($"Hit! -{value:0} health", $"Удар! -{value:0} здоровья"));
            if (Health > 0) return;
            Health = 100;
            Stamina = 100;
            Teleport(WorldBootstrap.Instance.PlayerSpawn);
            WorldBootstrap.Instance.ShowToast(GameText.Pick("Moonroot carried you home", "Лунный Корень вернул вас домой"));
        }

        public void UnlockPointer()
        {
            if (pointerWasLocked || Cursor.lockState == CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.None;
        }
    }

    public interface IInteractable
    {
        string Prompt { get; }
        void Interact(PlayerAnt player);
    }

    public sealed class IInteractableHost : MonoBehaviour
    {
        public IInteractable Target;
        public void Use(PlayerAnt player) => Target?.Interact(player);
    }
}
