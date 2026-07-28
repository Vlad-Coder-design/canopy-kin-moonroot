using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

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
        float blockedTimer;
        float diagnosticTimer;
        float surfaceSpeedMultiplier = 1f;
        float bufferedW;
        float bufferedA;
        float bufferedS;
        float bufferedD;
        float bufferedSprint;
        Vector2 rawInput;
        Vector3 processedMovement;
        Vector3 actualVelocity;
        Vector3 groundNormal = Vector3.up;
        float slopeAngle;
        string currentSurface = "Soil";
        bool diagnosticsVisible;
        IInteractable nearbyInteraction;
        bool pointerWasLocked;
        bool dying;
        readonly RaycastHit[] castHits = new RaycastHit[32];

        public float Health { get; private set; } = 100;
        public float Stamina { get; private set; } = 100;
        public string CurrentPrompt { get; private set; }
        public Transform CameraTransform => viewCamera ? viewCamera.transform : null;
        public bool TacticalView { get; private set; }
        public Vector2 RawInput => rawInput;
        public Vector3 ProcessedMovement => processedMovement;
        public Vector3 ActualVelocity => actualVelocity;
        public bool Grounded { get; private set; }
        public string CurrentSurface => currentSurface;
        public float SlopeAngle => slopeAngle;
        public bool HasInputFocus =>
            Application.isFocused &&
            (Application.platform != RuntimePlatform.WebGLPlayer ||
             Cursor.lockState == CursorLockMode.Locked);
        public string LocomotionState { get; private set; } = "Idle";
        public string AnimationState => visual ? visual.AnimationState : "Unavailable";

        void Awake()
        {
            body = GetComponent<CharacterController>();
            body.height = .68f;
            body.radius = .23f;
            body.center = new Vector3(0, .34f, 0);
            body.stepOffset = .22f;
            body.slopeLimit = 54f;
            body.skinWidth = .025f;
            body.minMoveDistance = 0;
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

        void OnEnable() => InputSystem.onEvent += BufferKeyboardEvent;

        void OnDisable() => InputSystem.onEvent -= BufferKeyboardEvent;

        void BufferKeyboardEvent(InputEventPtr eventPointer, InputDevice device)
        {
            Keyboard keyboard = device as Keyboard;
            if (keyboard == null ||
                (!eventPointer.IsA<StateEvent>() &&
                 !eventPointer.IsA<DeltaStateEvent>())) return;

            BufferPressed(eventPointer, keyboard.wKey, ref bufferedW);
            BufferPressed(eventPointer, keyboard.aKey, ref bufferedA);
            BufferPressed(eventPointer, keyboard.sKey, ref bufferedS);
            BufferPressed(eventPointer, keyboard.dKey, ref bufferedD);
            BufferPressed(eventPointer, keyboard.leftShiftKey, ref bufferedSprint);
            BufferPressed(eventPointer, keyboard.rightShiftKey, ref bufferedSprint);
        }

        static void BufferPressed(
            InputEventPtr eventPointer,
            KeyControl key,
            ref float buffer)
        {
            if (key.ReadValueFromEvent(eventPointer) > .5f)
                buffer = Mathf.Max(buffer, .085f);
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || WorldBootstrap.Instance == null) return;
            if (keyboard.f10Key.wasPressedThisFrame)
                diagnosticsVisible = !diagnosticsVisible;

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
            if (Cursor.lockState == CursorLockMode.Locked ||
                mouse == null ||
                !mouse.leftButton.wasPressedThisFrame) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
            rawInput = new Vector2(
                DigitalValue(keyboard.dKey, bufferedD) - DigitalValue(keyboard.aKey, bufferedA),
                DigitalValue(keyboard.wKey, bufferedW) - DigitalValue(keyboard.sKey, bufferedS));
            Vector2 input = Vector2.ClampMagnitude(rawInput, 1);
            Vector3 forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            bool sprint =
                (keyboard.leftShiftKey.isPressed ||
                 keyboard.rightShiftKey.isPressed ||
                 bufferedSprint > 0) &&
                Stamina > 2 &&
                input.sqrMagnitude > .1f;
            ConsumeInputBuffers();
            Stamina = Mathf.Clamp(Stamina + (sprint ? -21f : 18f) * Time.deltaTime, 0, 100);

            Grounded = ProbeGround(out RaycastHit groundHit);
            if (Grounded)
            {
                groundNormal = groundHit.normal;
                slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
                MovementSurface surface = groundHit.collider.GetComponentInParent<MovementSurface>();
                currentSurface = surface ? surface.DisplayName : SurfaceName(groundHit.collider);
                surfaceSpeedMultiplier = surface ? surface.SpeedMultiplier : 1f;
            }
            else
            {
                groundNormal = Vector3.up;
                slopeAngle = 0;
                currentSurface = "Air";
                surfaceSpeedMultiplier = 1f;
            }

            float speed = (sprint ? 4.25f : 2.55f) * surfaceSpeedMultiplier;
            Vector3 wishDirection = forward * input.y + right * input.x;
            if (wishDirection.sqrMagnitude > 1f) wishDirection.Normalize();
            if (Grounded && slopeAngle <= body.slopeLimit)
                wishDirection = Vector3.ProjectOnPlane(wishDirection, groundNormal).normalized * input.magnitude;
            processedMovement = wishDirection;
            Vector3 desired = wishDirection * speed;
            float acceleration = Grounded
                ? (desired.sqrMagnitude > planarVelocity.sqrMagnitude ? (sprint ? 18f : 24f) : 30f)
                : 5.5f;
            planarVelocity = Vector3.MoveTowards(planarVelocity, desired, acceleration * Time.deltaTime);

            if (Grounded && vertical <= 0) vertical = -1.8f;
            else vertical -= 15.5f * Time.deltaTime;
            if (keyboard.spaceKey.wasPressedThisFrame && Grounded)
            {
                if (!TryClimb(planarVelocity.normalized))
                    vertical = 3.35f;
            }

            Vector3 before = transform.position;
            CollisionFlags collision = body.Move((planarVelocity + Vector3.up * vertical) * Time.deltaTime);
            Vector3 moved = transform.position - before;
            actualVelocity = moved / Mathf.Max(Time.deltaTime, .0001f);
            Vector3 actualPlanar = Vector3.ProjectOnPlane(actualVelocity, Vector3.up);
            float actualSpeed = actualPlanar.magnitude;
            if ((collision & CollisionFlags.Below) != 0)
                Grounded = true;
            if ((collision & CollisionFlags.Above) != 0 && vertical > 0)
                vertical = 0;

            if (input.sqrMagnitude > .1f && actualSpeed < .08f)
                blockedTimer += Time.deltaTime;
            else
                blockedTimer = 0;
            if (blockedTimer > .22f && Grounded)
            {
                Vector3 escape = Vector3.ProjectOnPlane(wishDirection, groundNormal);
                planarVelocity = Vector3.Lerp(planarVelocity, escape * speed * .65f, .55f);
            }

            footstepTravel += new Vector2(moved.x, moved.z).magnitude;
            if (footstepTravel > (sprint ? .42f : .58f))
            {
                footstepTravel = 0;
                AudioDirector.Instance?.PlayStep(transform.position, currentSurface);
                FxPool.Instance?.Burst(
                    transform.position + Vector3.up * .035f,
                    FootstepColor(currentSurface),
                    sprint ? 5 : 3);
            }
            if (actualPlanar.sqrMagnitude > .003f || wishDirection.sqrMagnitude > .1f)
            {
                Vector3 facing = actualPlanar.sqrMagnitude > .003f ? actualPlanar : wishDirection;
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(facing, Vector3.up),
                    520f * Time.deltaTime);
            }

            float speed01 = Mathf.InverseLerp(0, 4.25f, actualSpeed);
            visual?.SetPlayerMotion(actualSpeed, speed01, Grounded, groundNormal);
            LocomotionState = !Grounded
                ? (vertical > .05f ? "Vault" : "Falling")
                : actualSpeed < .06f
                    ? "Idle"
                    : sprint && actualSpeed > 2.85f ? "Run" : "Walk";
            EmitDiagnostics();
        }

        static float DigitalValue(KeyControl key, float buffered)
            => key.isPressed || key.wasPressedThisFrame || buffered > 0 ? 1f : 0f;

        void ConsumeInputBuffers()
        {
            float step = Time.unscaledDeltaTime;
            bufferedW = Mathf.Max(0, bufferedW - step);
            bufferedA = Mathf.Max(0, bufferedA - step);
            bufferedS = Mathf.Max(0, bufferedS - step);
            bufferedD = Mathf.Max(0, bufferedD - step);
            bufferedSprint = Mathf.Max(0, bufferedSprint - step);
        }

        bool ProbeGround(out RaycastHit best)
        {
            best = default;
            Vector3 origin = transform.position + Vector3.up * .42f;
            int count = Physics.SphereCastNonAlloc(
                origin,
                .18f,
                Vector3.down,
                castHits,
                .72f,
                ~0,
                QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = castHits[i];
                if (!candidate.collider || IgnoreMovementCollider(candidate.collider)) continue;
                if (candidate.distance >= nearest) continue;
                nearest = candidate.distance;
                best = candidate;
            }
            return best.collider != null && Vector3.Angle(best.normal, Vector3.up) <= body.slopeLimit + 8f;
        }

        bool TryFilteredSphereCast(
            Vector3 origin,
            float radius,
            Vector3 direction,
            float distance,
            out RaycastHit best)
        {
            best = default;
            int count = Physics.SphereCastNonAlloc(
                origin,
                radius,
                direction.normalized,
                castHits,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = castHits[i];
                if (!candidate.collider || IgnoreMovementCollider(candidate.collider)) continue;
                if (candidate.distance >= nearest) continue;
                nearest = candidate.distance;
                best = candidate;
            }
            return best.collider != null;
        }

        bool IgnoreMovementCollider(Collider collider)
        {
            if (!collider) return true;
            Transform candidate = collider.transform;
            if (candidate == transform || candidate.IsChildOf(transform)) return true;
            return collider.GetComponentInParent<SquadUnit>() != null;
        }

        static string SurfaceName(Collider collider)
        {
            if (!collider) return "Unknown";
            string value = collider.name.ToLowerInvariant();
            if (value.Contains("root") || value.Contains("bark") || value.Contains("leaf"))
                return "Wood";
            if (value.Contains("stone") || value.Contains("pebble") || value.Contains("rock"))
                return "Stone";
            if (value.Contains("moss")) return "Moss";
            if (value.Contains("mud") || value.Contains("pool") || value.Contains("water"))
                return "Wet soil";
            return "Soil";
        }

        static Color FootstepColor(string surface)
        {
            if (surface.Contains("Wood")) return new Color(.38f, .2f, .075f);
            if (surface.Contains("Stone")) return new Color(.5f, .52f, .46f);
            if (surface.Contains("Moss")) return new Color(.25f, .47f, .13f);
            if (surface.Contains("Wet")) return new Color(.22f, .16f, .09f);
            return new Color(.48f, .3f, .13f);
        }

        void EmitDiagnostics()
        {
            if (!diagnosticsVisible) return;
            diagnosticTimer -= Time.unscaledDeltaTime;
            if (diagnosticTimer > 0) return;
            diagnosticTimer = .5f;
            Debug.Log(
                $"MOONROOT_MOVEMENT_SAMPLE pos={transform.position:F2} raw={rawInput:F2} " +
                $"processed={processedMovement:F2} velocity={actualVelocity:F2} " +
                $"grounded={Grounded} state={LocomotionState} animation={AnimationState} " +
                $"focus={Application.isFocused} pointer={Cursor.lockState} " +
                $"surface={currentSurface} slope={slopeAngle:F1}");
        }

        bool TryClimb(Vector3 direction)
        {
            if (direction.sqrMagnitude < .1f) direction = transform.forward;
            Vector3 low = transform.position + Vector3.up * .18f;
            bool obstacle = TryFilteredSphereCast(low, .12f, direction, .58f, out _);
            bool clear = !TryFilteredSphereCast(
                transform.position + Vector3.up * .64f,
                .1f,
                direction,
                .66f,
                out _);
            if (!obstacle || !clear) return false;
            vertical = 2.15f;
            planarVelocity = direction * Mathf.Max(1.45f, planarVelocity.magnitude);
            return true;
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

        public void BiteForQa()
        {
            visual?.PlayAttack();
            AudioDirector.Instance?.PlayBite(transform.position + transform.forward * .45f);
            ResolveBite();
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
            Vector3 target = transform.position + Vector3.up * Mathf.Lerp(.38f, .2f, tacticalBlend);
            float tacticalPitch = Mathf.Lerp(pitch, 68f, tacticalBlend);
            float distance = Mathf.Lerp(2.72f, 9.2f, tacticalBlend);
            float height = Mathf.Lerp(.36f, 3.4f, tacticalBlend);
            Quaternion orbit = Quaternion.Euler(tacticalPitch, yaw, 0);
            Vector3 wanted = target + orbit * new Vector3(0, height, -distance);
            Vector3 direction = wanted - target;
            if (TryFilteredSphereCast(
                    target,
                    .13f,
                    direction,
                    direction.magnitude,
                    out RaycastHit hit))
                wanted = hit.point - direction.normalized * .16f;
            cameraShake = Mathf.MoveTowards(cameraShake, 0, Time.unscaledDeltaTime * .8f);
            if (cameraShake > 0) wanted += UnityEngine.Random.insideUnitSphere * cameraShake;
            float smoothing = WorldBootstrap.Instance.IsPlaying ? 16f : 5f;
            viewCamera.transform.position = Vector3.Lerp(viewCamera.transform.position, wanted, smoothing * Time.unscaledDeltaTime);
            viewCamera.transform.rotation = Quaternion.Slerp(
                viewCamera.transform.rotation,
                Quaternion.LookRotation(target - viewCamera.transform.position, Vector3.up),
                (smoothing + 2f) * Time.unscaledDeltaTime);
            viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, Mathf.Lerp(GameSettings.FieldOfView, 53f, tacticalBlend), Time.unscaledDeltaTime * 5f);
        }

        public void SnapCamera()
        {
            if (!viewCamera) viewCamera = Camera.main;
            if (!viewCamera) return;
            Vector3 target = transform.position + Vector3.up * .38f;
            viewCamera.transform.position = target + Quaternion.Euler(pitch, yaw, 0) * new Vector3(0, .36f, -2.72f);
            viewCamera.transform.rotation = Quaternion.LookRotation(target - viewCamera.transform.position);
        }

        public void Teleport(Vector3 position)
        {
            bool enabledBefore = body.enabled;
            body.enabled = false;
            transform.position = position;
            body.enabled = enabledBefore;
            planarVelocity = Vector3.zero;
            actualVelocity = Vector3.zero;
            vertical = 0;
            SnapCamera();
        }

        public void Face(Vector3 target, float cameraPitch = 10f)
        {
            Vector3 direction = target - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude < .001f) return;
            yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(cameraPitch, -18f, 62f);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
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

        public void RequestPointerCapture()
        {
            if (WorldBootstrap.Instance == null ||
                !WorldBootstrap.Instance.IsPlaying ||
                WorldBootstrap.Instance.IsPaused ||
                TacticalView) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            pointerWasLocked = true;
        }

        public void SetMovementDiagnostics(string value)
        {
            diagnosticsVisible =
                string.Equals(value, "1", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "true", System.StringComparison.OrdinalIgnoreCase);
            diagnosticTimer = 0;
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                rawInput = Vector2.zero;
                planarVelocity = Vector3.zero;
                actualVelocity = Vector3.zero;
                return;
            }

            if (Application.platform != RuntimePlatform.WebGLPlayer &&
                WorldBootstrap.Instance != null &&
                WorldBootstrap.Instance.IsPlaying &&
                !WorldBootstrap.Instance.IsPaused)
                RequestPointerCapture();
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody rigidbody = hit.collider.attachedRigidbody;
            if (!rigidbody || rigidbody.isKinematic || hit.moveDirection.y < -.25f) return;
            Vector3 force = new(hit.moveDirection.x, .06f, hit.moveDirection.z);
            rigidbody.AddForceAtPosition(force * 1.35f, hit.point, ForceMode.Impulse);
        }

        void OnGUI()
        {
            if (!diagnosticsVisible) return;
            const float width = 440f;
            Rect panel = new(16, Screen.height - 236, width, 220);
            Color previous = GUI.color;
            GUI.color = new Color(.02f, .035f, .025f, .92f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(
                new Rect(panel.x + 12, panel.y + 10, width - 24, 200),
                "MOVEMENT DIAGNOSTICS  [F10 hide]\n" +
                $"Position        {transform.position:F2}\n" +
                $"Raw input       {rawInput:F2}\n" +
                $"Processed       {processedMovement:F2}\n" +
                $"Actual velocity {actualVelocity:F2}\n" +
                $"Grounded        {Grounded}   slope {slopeAngle:F1}°\n" +
                $"Locomotion      {LocomotionState} / {AnimationState}\n" +
                $"Input focus     {Application.isFocused}   pointer {Cursor.lockState}\n" +
                $"Surface         {currentSurface}   speed x{surfaceSpeedMultiplier:F2}");
            GUI.color = previous;
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
