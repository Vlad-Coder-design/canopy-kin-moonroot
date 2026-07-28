using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CanopyKin
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerAnt : MonoBehaviour
    {
        CharacterController body;
        Camera viewCamera;
        AntVisual visual;
        Vector3 planarVelocity;
        float yaw;
        float pitch = 18f;
        float vertical;
        float attackCooldown;
        float pendingAttack;
        float promptTimer;
        float footstepTravel;
        float cameraShake;
        float tacticalBlend;
        IInteractable nearbyInteraction;
        bool pointerWasLocked;
        bool dying;

        public float Health { get; private set; } = 100;
        public float Stamina { get; private set; } = 100;
        public string CurrentPrompt { get; private set; }
        public Transform CameraTransform => viewCamera ? viewCamera.transform : null;
        public bool TacticalView { get; private set; }

        void Awake()
        {
            body = GetComponent<CharacterController>();
            body.height = .74f;
            body.radius = .25f;
            body.center = new Vector3(0, .37f, 0);
            body.stepOffset = .3f;
            body.slopeLimit = 58f;
            body.skinWidth = .035f;
            visual = AntVisual.Create(transform, new Color(.16f, .035f, .012f), .92f, AntCaste.Scout);
        }

        void Start()
        {
            viewCamera = Camera.main;
            if (viewCamera)
            {
                viewCamera.fieldOfView = GameSettings.FieldOfView;
                viewCamera.nearClipPlane = .025f;
            }
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
                if (TacticalView) SetTactical(false);
                else world.TogglePause();
                return;
            }
            if (world.IsPaused || dying) return;

            if (keyboard.tabKey.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame)
                SetTactical(!TacticalView);

            if (TacticalView)
            {
                HandleTacticalInput(mouse);
                UpdateAttack();
                return;
            }

            CapturePointer(mouse);
            ReadCamera(mouse);
            Move(keyboard);
            UpdateInteractions(keyboard);
            UpdateAttackInput(mouse);
            UpdateAttack();
        }

        void CapturePointer(Mouse mouse)
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer ||
                Cursor.lockState == CursorLockMode.Locked ||
                mouse == null ||
                !mouse.leftButton.wasPressedThisFrame) return;
            Cursor.lockState = CursorLockMode.Locked;
            pointerWasLocked = true;
        }

        void ReadCamera(Mouse mouse)
        {
            if (Cursor.lockState != CursorLockMode.Locked || mouse == null) return;
            Vector2 delta = mouse.delta.ReadValue() * GameSettings.Sensitivity;
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, 6f, 39f);
            pointerWasLocked = true;
        }

        void Move(Keyboard keyboard)
        {
            Vector2 input = new(
                (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            input = Vector2.ClampMagnitude(input, 1);
            Vector3 forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            bool sprint = keyboard.leftShiftKey.isPressed && Stamina > 2 && input.sqrMagnitude > .1f;
            float speed = sprint ? 5.1f : 3.15f;
            Stamina = Mathf.Clamp(Stamina + (sprint ? -23f : 17f) * Time.deltaTime, 0, 100);

            Vector3 desired = (forward * input.y + right * input.x) * speed;
            float acceleration = body.isGrounded ? (sprint ? 11f : 15f) : 4f;
            planarVelocity = Vector3.MoveTowards(planarVelocity, desired, acceleration * Time.deltaTime);

            if (body.isGrounded) vertical = -.65f;
            else vertical -= 13f * Time.deltaTime;
            if (keyboard.spaceKey.wasPressedThisFrame && body.isGrounded)
            {
                if (!TryClimb(planarVelocity.normalized))
                    vertical = 3.15f;
            }
            if (body.isGrounded && input.sqrMagnitude > .12f)
                AutoTraverse(planarVelocity.normalized);

            Vector3 before = transform.position;
            body.Move((planarVelocity + Vector3.up * vertical) * Time.deltaTime);
            Vector3 moved = transform.position - before;
            footstepTravel += new Vector2(moved.x, moved.z).magnitude;
            if (footstepTravel > (sprint ? .5f : .72f))
            {
                footstepTravel = 0;
                AudioDirector.Instance?.PlayStep(transform.position);
            }
            if (planarVelocity.sqrMagnitude > .08f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(planarVelocity), 10.5f * Time.deltaTime);
        }

        bool TryClimb(Vector3 direction)
        {
            if (direction.sqrMagnitude < .1f) direction = transform.forward;
            Vector3 low = transform.position + Vector3.up * .18f;
            bool obstacle = Physics.SphereCast(low, .12f, direction, out _, .65f, ~0, QueryTriggerInteraction.Ignore);
            bool clear = !Physics.SphereCast(transform.position + Vector3.up * .72f, .11f, direction, out _, .72f, ~0, QueryTriggerInteraction.Ignore);
            if (!obstacle || !clear) return false;
            body.Move(Vector3.up * .28f + direction * .18f);
            vertical = 1.1f;
            return true;
        }

        void AutoTraverse(Vector3 direction)
        {
            if (direction.sqrMagnitude < .1f) return;
            Vector3 low = transform.position + Vector3.up * .12f;
            if (Physics.Raycast(low, direction, out RaycastHit hit, .34f, ~0, QueryTriggerInteraction.Ignore) &&
                !Physics.Raycast(transform.position + Vector3.up * .48f, direction, .48f, ~0, QueryTriggerInteraction.Ignore) &&
                hit.normal.y < .45f)
                body.Move(Vector3.up * Time.deltaTime * 1.3f);
        }

        void UpdateInteractions(Keyboard keyboard)
        {
            promptTimer -= Time.deltaTime;
            if (promptTimer <= 0)
            {
                promptTimer = .12f;
                FindInteraction();
            }
            if (keyboard.eKey.wasPressedThisFrame) Interact();
        }

        void UpdateAttackInput(Mouse mouse)
        {
            attackCooldown -= Time.deltaTime;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || attackCooldown > 0 ||
                Cursor.lockState != CursorLockMode.Locked) return;
            attackCooldown = .52f;
            pendingAttack = .13f;
            visual?.PlayAttack();
            AudioDirector.Instance?.PlayBite(transform.position + transform.forward * .45f);
        }

        void UpdateAttack()
        {
            if (pendingAttack <= 0) return;
            pendingAttack -= Time.deltaTime;
            if (pendingAttack > 0) return;
            ResolveBite();
        }

        void ResolveBite()
        {
            Vector3 point = transform.position + Vector3.up * .34f + transform.forward * .68f;
            bool hitSomething = false;
            foreach (Collider hit in Physics.OverlapSphere(point, .48f, ~0, QueryTriggerInteraction.Ignore))
            {
                Creature creature = hit.GetComponentInParent<Creature>();
                if (!creature || !creature.IsActive) continue;
                Vector3 toTarget = creature.transform.position - transform.position;
                if (Vector3.Dot(transform.forward, toTarget.normalized) < .25f) continue;
                creature.Damage(22, transform.position);
                hitSomething = true;
            }
            if (hitSomething)
            {
                FxPool.Instance?.Burst(point, new Color(.78f, .24f, .08f), 12);
                cameraShake = GameSettings.Shake * .14f;
            }
            WorldBootstrap.Instance.FlashCrosshair(hitSomething);
        }

        void HandleTacticalInput(Mouse mouse)
        {
            if (!viewCamera || mouse == null) return;
            if (mouse.rightButton.wasPressedThisFrame || mouse.leftButton.wasPressedThisFrame)
            {
                Ray ray = viewCamera.ScreenPointToRay(mouse.position.ReadValue());
                if (!Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore)) return;
                ResourceNode resource = hit.collider.GetComponentInParent<ResourceNode>();
                Creature creature = hit.collider.GetComponentInParent<Creature>();
                if (resource)
                    WorldBootstrap.Instance.CommandSquad(SquadOrder.Gather, resource.transform.position, resource, null);
                else if (creature && creature.IsActive)
                    WorldBootstrap.Instance.CommandSquad(SquadOrder.Attack, creature.transform.position, null, creature);
                else
                    WorldBootstrap.Instance.CommandSquad(SquadOrder.Move, hit.point, null, null);
            }
        }

        void SetTactical(bool active)
        {
            TacticalView = active;
            if (active)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                WorldBootstrap.Instance.ShowToast(GameText.Pick("Tactical view: click terrain or a target", "Тактический режим: выберите землю или цель"));
            }
            else if (Application.platform == RuntimePlatform.WebGLPlayer || pointerWasLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void LateUpdate()
        {
            if (!viewCamera || WorldBootstrap.Instance == null) return;
            if (WorldBootstrap.Instance.IsCinematic) return;
            tacticalBlend = Mathf.MoveTowards(tacticalBlend, TacticalView ? 1 : 0, Time.unscaledDeltaTime * 2.5f);
            Vector3 target = transform.position + Vector3.up * Mathf.Lerp(.5f, .2f, tacticalBlend);
            float tacticalPitch = Mathf.Lerp(pitch, 68f, tacticalBlend);
            float distance = Mathf.Lerp(3.55f, 9.2f, tacticalBlend);
            float height = Mathf.Lerp(.58f, 3.4f, tacticalBlend);
            Quaternion orbit = Quaternion.Euler(tacticalPitch, yaw, 0);
            Vector3 wanted = target + orbit * new Vector3(0, height, -distance);
            Vector3 direction = wanted - target;
            if (Physics.SphereCast(target, .16f, direction.normalized, out RaycastHit hit, direction.magnitude, ~0, QueryTriggerInteraction.Ignore))
                wanted = hit.point - direction.normalized * .2f;
            cameraShake = Mathf.MoveTowards(cameraShake, 0, Time.unscaledDeltaTime * .8f);
            if (cameraShake > 0) wanted += UnityEngine.Random.insideUnitSphere * cameraShake;
            float smoothing = WorldBootstrap.Instance.IsPlaying ? 10.5f : 4f;
            viewCamera.transform.position = Vector3.Lerp(viewCamera.transform.position, wanted, smoothing * Time.unscaledDeltaTime);
            viewCamera.transform.rotation = Quaternion.Slerp(
                viewCamera.transform.rotation,
                Quaternion.LookRotation(target - viewCamera.transform.position, Vector3.up),
                smoothing * Time.unscaledDeltaTime);
            viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, Mathf.Lerp(GameSettings.FieldOfView, 53f, tacticalBlend), Time.unscaledDeltaTime * 5f);
        }

        public void SnapCamera()
        {
            if (!viewCamera) viewCamera = Camera.main;
            if (!viewCamera) return;
            Vector3 target = transform.position + Vector3.up * .5f;
            viewCamera.transform.position = target + Quaternion.Euler(pitch, yaw, 0) * new Vector3(0, .58f, -3.55f);
            viewCamera.transform.rotation = Quaternion.LookRotation(target - viewCamera.transform.position);
        }

        public void Teleport(Vector3 position)
        {
            bool enabledBefore = body.enabled;
            body.enabled = false;
            transform.position = position;
            body.enabled = enabledBefore;
            planarVelocity = Vector3.zero;
            SnapCamera();
        }

        void FindInteraction()
        {
            nearbyInteraction = null;
            CurrentPrompt = null;
            float best = float.MaxValue;
            Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up * .3f, 1.55f, ~0, QueryTriggerInteraction.Collide);
            foreach (Collider hit in hits)
            {
                IInteractableHost host = hit.GetComponentInParent<IInteractableHost>();
                if (!host || host.Target == null) continue;
                float distance = (hit.ClosestPoint(transform.position) - transform.position).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                nearbyInteraction = host.Target;
            }
            if (nearbyInteraction != null)
                CurrentPrompt = nearbyInteraction.Prompt + "  [E]";
        }

        void Interact()
        {
            nearbyInteraction?.Interact(this);
            FindInteraction();
        }

        public void Damage(float value)
        {
            if (dying) return;
            Health = Mathf.Max(0, Health - value);
            visual?.PlayStagger();
            AudioDirector.Instance?.PlayHit(transform.position);
            FxPool.Instance?.Burst(transform.position + Vector3.up * .3f, new Color(.42f, .13f, .05f), 8);
            cameraShake = Mathf.Max(cameraShake, GameSettings.Shake * .2f);
            WorldBootstrap.Instance.ShowToast(GameText.Pick($"Hit! -{value:0} health", $"Удар! -{value:0} здоровья"));
            if (Health <= 0) StartCoroutine(Respawn());
        }

        IEnumerator Respawn()
        {
            dying = true;
            visual?.PlayDeath();
            yield return new WaitForSeconds(1.3f);
            Health = 100;
            Stamina = 100;
            Teleport(WorldBootstrap.Instance.PlayerRespawn);
            dying = false;
            WorldBootstrap.Instance.ShowToast(GameText.Pick("Moonroot carried you home", "Лунный Корень вернул вас домой"));
        }

        public void UnlockPointer()
        {
            if (pointerWasLocked || Cursor.lockState == CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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
