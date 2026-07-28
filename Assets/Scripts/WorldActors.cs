using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CanopyKin
{
    public sealed class ResourceNode : MonoBehaviour, IInteractable
    {
        public ResourceKind Kind { get; private set; }
        public int Remaining { get; private set; }
        public bool Available => Remaining > 0;
        readonly List<GameObject> pieces = new();

        public string Prompt => Remaining > 0
            ? GameText.Pick($"Pick up {Kind.ToString().ToLowerInvariant()}", Kind switch
            {
                ResourceKind.Seed => "Поднять лесное семя",
                ResourceKind.Resin => "Поднять янтарную смолу",
                _ => "Поднять белковую пищу"
            })
            : GameText.Pick("This source is depleted", "Источник исчерпан");

        public void Initialize(ResourceKind kind, int amount)
        {
            Kind = kind;
            Remaining = amount;
            var collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = .72f;
            collider.center = new Vector3(0, .3f, 0);
            collider.isTrigger = true;
            gameObject.AddComponent<IInteractableHost>().Target = this;

            for (int i = 0; i < amount; i++)
            {
                float angle = i * 2.399f;
                Vector3 position = new(Mathf.Cos(angle) * .32f, .18f + i * .025f, Mathf.Sin(angle) * .32f);
                GameObject piece = CreateCargoVisual(transform, kind, position, .95f);
                piece.transform.localRotation = Quaternion.Euler(12f + i * 9f, i * 71f, 24f);
                pieces.Add(piece);
            }
            BuildNaturalMarker(kind);
        }

        void BuildNaturalMarker(ResourceKind kind)
        {
            Color glow = kind switch
            {
                ResourceKind.Seed => new Color(.93f, .69f, .18f),
                ResourceKind.Resin => new Color(1f, .31f, .035f),
                _ => new Color(.7f, .2f, .08f)
            };
            for (int i = 0; i < 3; i++)
            {
                Transform mote = VisualFactory.OrganicPart(
                    "Pheromone firefly",
                    transform,
                    OrganicMeshFactory.BodyShape.Eye,
                    new Vector3(Mathf.Cos(i * 2.1f) * .42f, .66f + i * .12f, Mathf.Sin(i * 2.1f) * .42f),
                    Vector3.one * .09f,
                    glow,
                    .9f).transform;
                mote.gameObject.AddComponent<HoverMote>().Initialize(i * 2.1f);
            }
        }

        public void Interact(PlayerAnt player)
        {
            if (!TryTake(out ResourceKind cargo)) return;
            WorldBootstrap.Instance.Colony.Add(cargo, 1);
            WorldBootstrap.Instance.Mission.NotifyGather();
            WorldBootstrap.Instance.ShowToast(CargoMessage(cargo));
        }

        public bool TryTake(out ResourceKind cargo)
        {
            cargo = Kind;
            if (Remaining <= 0) return false;
            Remaining--;
            if (Remaining < pieces.Count && pieces[Remaining]) pieces[Remaining].SetActive(false);
            return true;
        }

        public static GameObject CreateCargoVisual(Transform parent, ResourceKind kind, Vector3 localPosition, float scale)
        {
            Color color = kind switch
            {
                ResourceKind.Seed => new Color(.6f, .31f, .055f),
                ResourceKind.Resin => new Color(1f, .25f, .018f),
                _ => new Color(.62f, .18f, .075f)
            };
            OrganicMeshFactory.BodyShape shape = kind == ResourceKind.Resin
                ? OrganicMeshFactory.BodyShape.SpiderBody
                : OrganicMeshFactory.BodyShape.Brood;
            Vector3 dimensions = kind switch
            {
                ResourceKind.Seed => new Vector3(.32f, .25f, .52f),
                ResourceKind.Resin => new Vector3(.31f, .22f, .3f),
                _ => new Vector3(.34f, .24f, .42f)
            };
            return VisualFactory.OrganicPart(
                $"{kind} cargo",
                parent,
                shape,
                localPosition,
                dimensions * scale,
                color,
                kind == ResourceKind.Resin ? .78f : .32f);
        }

        static string CargoMessage(ResourceKind kind) => kind switch
        {
            ResourceKind.Seed => GameText.Pick("Forest seed delivered", "Лесное семя доставлено"),
            ResourceKind.Resin => GameText.Pick("Amber resin delivered", "Янтарная смола доставлена"),
            _ => GameText.Pick("Protein delivered", "Белковая пища доставлена")
        };
    }

    public sealed class HoverMote : MonoBehaviour
    {
        float phase;
        Vector3 origin;
        public void Initialize(float value)
        {
            phase = value;
            origin = transform.localPosition;
        }
        void Update()
        {
            transform.localPosition = origin + new Vector3(
                Mathf.Sin(Time.time * 1.2f + phase) * .08f,
                Mathf.Sin(Time.time * 2f + phase) * .07f,
                Mathf.Cos(Time.time * 1.1f + phase) * .08f);
        }
    }

    public sealed class ColonyEntrance : MonoBehaviour, IInteractable
    {
        bool underground;
        public void Initialize(bool isUnderground) => underground = isUnderground;
        public string Prompt => underground
            ? GameText.Pick("Climb to the forest floor", "Подняться на лесную подстилку")
            : GameText.Pick("Enter the Moonroot colony", "Войти в колонию Лунного Корня");

        public void Interact(PlayerAnt player)
        {
            WorldBootstrap world = WorldBootstrap.Instance;
            world.ToggleNest(player, underground);
            if (underground) world.Mission.NotifyNestExit();
            else world.Mission.NotifyReturnedToNest();
        }
    }

    public sealed class UpgradeStation : MonoBehaviour, IInteractable
    {
        public string Prompt
        {
            get
            {
                ColonyState colony = WorldBootstrap.Instance.Colony;
                if (colony.IsConstructing)
                    return GameText.Pick($"Nursery growing — {colony.ConstructionProgress:P0}", $"Ясли растут — {colony.ConstructionProgress:P0}");
                if (colony.Level >= 2)
                    return GameText.Pick("Expanded nursery chamber", "Расширенная камера яслей");
                return GameText.Pick(
                    $"Expand nursery ({ColonyState.UpgradeSeedCost} seed, {ColonyState.UpgradeResinCost} resin, {ColonyState.UpgradeProteinCost} protein)",
                    $"Расширить ясли ({ColonyState.UpgradeSeedCost} семян, {ColonyState.UpgradeResinCost} смолы, {ColonyState.UpgradeProteinCost} белка)");
            }
        }

        public void Initialize()
        {
            var collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = 1.1f;
            collider.isTrigger = true;
            gameObject.AddComponent<IInteractableHost>().Target = this;
        }

        public void Interact(PlayerAnt player)
        {
            WorldBootstrap world = WorldBootstrap.Instance;
            if (world.Mission.Step != MissionDirector.UpgradeStep)
            {
                world.ShowToast(GameText.Pick("The nursery does not need work yet", "Пока ясли не требуют работ"));
                return;
            }
            if (world.Colony.BeginUpgrade(world))
            {
                world.ShowToast(GameText.Pick("Workers begin shaping the new chamber", "Рабочие начали расширять камеру"));
                AudioDirector.Instance?.PlayOrder(transform.position);
            }
            else
            {
                world.ShowToast(GameText.Pick("The colony lacks construction resources", "Колонии не хватает строительных ресурсов"));
            }
        }
    }

    public sealed class ScoutGuide : MonoBehaviour, IInteractable
    {
        bool met;
        public string Prompt => met
            ? GameText.Pick("Scout: the seed trail lies beyond the mushrooms", "Разведчик: тропа семян лежит за грибами")
            : GameText.Pick("Meet the Moonroot scout", "Поговорить с разведчиком Лунного Корня");

        public void Initialize()
        {
            var collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = .8f;
            collider.isTrigger = true;
            gameObject.AddComponent<IInteractableHost>().Target = this;
        }

        public void Interact(PlayerAnt player)
        {
            if (!met)
            {
                met = true;
                WorldBootstrap.Instance.Mission.NotifyScoutReached();
                WorldBootstrap.Instance.ShowToast(GameText.Pick("Scout: workers answer pheromone order 1", "Разведчик: рабочие подчиняются феромонному приказу 1"));
                AudioDirector.Instance?.PlayOrder(transform.position);
            }
        }
    }

    public sealed class QueenBriefing : MonoBehaviour, IInteractable
    {
        bool heard;

        public string Prompt => heard
            ? GameText.Pick(
                "Queen: bring food home before the Emberjaws find us",
                "Королева: принесите пищу до того, как нас найдут Огненные Жвала")
            : GameText.Pick(
                "Listen to the Moonroot queen",
                "Выслушать королеву Лунного Корня");

        public void Initialize()
        {
            var collider = gameObject.AddComponent<SphereCollider>();
            collider.center = new Vector3(0, .46f, -.35f);
            collider.radius = 1.25f;
            collider.isTrigger = true;
            gameObject.AddComponent<IInteractableHost>().Target = this;
        }

        public void Interact(PlayerAnt player)
        {
            if (heard) return;
            heard = true;
            WorldBootstrap world = WorldBootstrap.Instance;
            world.Mission.NotifyQueenBriefed();
            world.ShowToast(GameText.Pick(
                "Queen: the rain broke our stores. Find food, protect the workers, and return alive.",
                "Королева: дождь уничтожил запасы. Найдите пищу, защитите рабочих и вернитесь живыми."));
            AudioDirector.Instance?.PlayOrder(transform.position);
        }
    }

    public sealed class CapturePoint : MonoBehaviour
    {
        float progress;
        float pulse;
        Transform marker;

        public void Initialize()
        {
            marker = transform.Find("Capture marker");
        }

        void Update()
        {
            WorldBootstrap world = WorldBootstrap.Instance;
            if (!world || world.Mission.Step != MissionDirector.CaptureStep || world.IsPaused) return;
            int friendly = 0;
            if (Vector3.Distance(world.Player.transform.position, transform.position) < 3.2f) friendly++;
            foreach (SquadUnit unit in FindObjectsByType<SquadUnit>(FindObjectsSortMode.None))
                if (unit.IsAvailable && Vector3.Distance(unit.transform.position, transform.position) < 3.2f) friendly++;
            float rate = friendly > 0 ? Mathf.Lerp(.022f, .065f, Mathf.InverseLerp(1, 7, friendly)) : -.018f;
            progress = Mathf.Clamp01(progress + rate * Time.deltaTime);
            world.Mission.SetCaptureProgress(progress);
            pulse += Time.deltaTime;
            if (marker)
            {
                marker.localScale = Vector3.one * (1f + Mathf.Sin(pulse * 3f) * .08f + progress * .35f);
                marker.localRotation = Quaternion.Euler(0, pulse * 15f, 0);
            }
        }
    }

    public sealed class ThreatRevealTrigger : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            if (!other.GetComponentInParent<PlayerAnt>()) return;
            WorldBootstrap world = WorldBootstrap.Instance;
            if (world.Mission.Step != MissionDirector.OverlookStep) return;
            world.Mission.NotifyOverlookReached();
            world.BeginThreatReveal();
        }
    }

    public sealed class SquadUnit : MonoBehaviour
    {
        public UnitRole Role { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool Selected { get; private set; }
        public bool IsAvailable => Health > 0 && !recovering && gameObject.activeSelf;
        public bool HasCargo { get; private set; }
        public ResourceKind Cargo { get; private set; }
        public AntVisual Visual { get; private set; }

        GameObject selectionMarker;
        GameObject injuryMarker;
        GameObject cargoVisual;
        bool recovering;

        public void Initialize(UnitRole role)
        {
            Role = role;
            AntDefinition definition = GameDefinitions.Ant(role);
            MaxHealth = definition.maxHealth;
            Health = MaxHealth;
            Visual = AntVisual.Create(transform, definition.shell, definition.visualScale, definition.caste);
            BuildStateMarkers();
        }

        void BuildStateMarkers()
        {
            selectionMarker = BuildRing("Selected pheromone ring", new Color(.35f, .9f, .38f), .5f);
            injuryMarker = BuildRing("Injured pheromone ring", new Color(1f, .18f, .045f), .38f);
            selectionMarker.SetActive(false);
            injuryMarker.SetActive(false);
        }

        GameObject BuildRing(string name, Color color, float radius)
        {
            var points = new List<Vector3>();
            var radii = new List<float>();
            for (int i = 0; i <= 18; i++)
            {
                float angle = i / 18f * Mathf.PI * 2f;
                points.Add(new Vector3(Mathf.Cos(angle) * radius, .025f, Mathf.Sin(angle) * radius));
                radii.Add(.012f);
            }
            return VisualFactory.MeshObject(name, transform, OrganicMeshFactory.Tube(points, radii, 5), Vector3.zero, Vector3.one, VisualFactory.Material(color, .8f));
        }

        public void SetSelected(bool selected)
        {
            Selected = selected;
            if (selectionMarker) selectionMarker.SetActive(selected);
        }

        public void TakeCargo(ResourceKind kind)
        {
            DropCargoVisual();
            HasCargo = true;
            Cargo = kind;
            cargoVisual = ResourceNode.CreateCargoVisual(transform, kind, new Vector3(0, .78f, -.08f), .78f);
            Visual?.SetCarrying(true);
        }

        public ResourceKind DeliverCargo()
        {
            ResourceKind kind = Cargo;
            HasCargo = false;
            DropCargoVisual();
            Visual?.SetCarrying(false);
            return kind;
        }

        void DropCargoVisual()
        {
            if (cargoVisual) Destroy(cargoVisual);
            cargoVisual = null;
        }

        public void Damage(float amount)
        {
            if (Health <= 0) return;
            Health = Mathf.Max(0, Health - amount);
            Visual?.PlayStagger();
            if (injuryMarker) injuryMarker.SetActive(Health > 0 && Health < MaxHealth * .38f);
            if (Health <= 0) StartCoroutine(Recover());
        }

        IEnumerator Recover()
        {
            recovering = true;
            Visual?.PlayDeath();
            yield return new WaitForSeconds(1.1f);
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            yield return new WaitForSeconds(7f);
            Health = MaxHealth;
            transform.position = WorldBootstrap.Instance.NestPosition + Vector3.up * .05f;
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = true;
            if (injuryMarker) injuryMarker.SetActive(false);
            recovering = false;
        }
    }

    public sealed class Creature : MonoBehaviour
    {
        public enum Species { Beetle, Spider, RivalAnt }
        enum BrainState { Dormant, Wander, Chase, Telegraph, Recover, Retreat, Dead }

        public Species Kind { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public int DamageEvents { get; private set; }
        public int AttackEvents { get; private set; }
        public int SuccessfulAttacks { get; private set; }
        public bool IsActive => state != BrainState.Dead &&
                                WorldBootstrap.Instance &&
                                WorldBootstrap.Instance.IsPlaying &&
                                WorldBootstrap.Instance.Mission.Step >= requiredMissionStep &&
                                gameObject.activeSelf;

        EnemyDefinition definition;
        BrainState state;
        int requiredMissionStep;
        Vector3 home;
        Vector3 wanderTarget;
        Vector3 attackTarget;
        float stateTimer;
        float wanderTimer;
        float stuckTimer;
        Vector3 lastPosition;
        Collider bodyCollider;
        Renderer[] renderers;
        AntVisual rivalVisual;
        SpiderVisual spiderVisual;
        bool qaFrozen;

        public void FreezeForQa()
        {
            qaFrozen = true;
            spiderVisual?.SetTelegraphing(false);
        }

        public void Initialize(Species species, int missionStep)
        {
            Kind = species;
            requiredMissionStep = missionStep;
            definition = GameDefinitions.Enemy(species);
            MaxHealth = definition.maxHealth;
            Health = MaxHealth;
            var collider = gameObject.AddComponent<SphereCollider>();
            collider.center = new Vector3(0, .42f, 0);
            collider.radius = species == Species.Spider ? .88f : species == Species.Beetle ? .66f : .42f;
            bodyCollider = collider;
            if (species == Species.RivalAnt)
                rivalVisual = AntVisual.Create(transform, new Color(.43f, .035f, .012f), 1.16f, AntCaste.Rival);
            else if (species == Species.Beetle)
                CreatureVisuals.BuildBeetle(transform);
            else
                spiderVisual = CreatureVisuals.BuildSpider(transform);
            renderers = GetComponentsInChildren<Renderer>();
            home = transform.position;
            wanderTarget = home;
            lastPosition = transform.position;
            state = BrainState.Dormant;
        }

        void Update()
        {
            WorldBootstrap world = WorldBootstrap.Instance;
            if (!world || !world.IsPlaying || world.IsPaused || state == BrainState.Dead) return;
            if (qaFrozen) return;
            if (world.Mission.Step < requiredMissionStep)
            {
                state = BrainState.Dormant;
                return;
            }
            if (state == BrainState.Dormant) state = BrainState.Wander;

            stateTimer -= Time.deltaTime;
            spiderVisual?.SetTelegraphing(state == BrainState.Telegraph);
            Transform target = ChooseTarget();
            float distance = target ? Vector3.Distance(transform.position, target.position) : float.MaxValue;
            switch (state)
            {
                case BrainState.Wander:
                    if (distance < definition.aggroRadius) state = BrainState.Chase;
                    Wander();
                    break;
                case BrainState.Chase:
                    if (!target) { state = BrainState.Wander; break; }
                    if (distance <= definition.attackRange)
                    {
                        state = BrainState.Telegraph;
                        stateTimer = Kind == Species.Spider ? .62f : .42f;
                        attackTarget = target.position;
                        rivalVisual?.PlayAttack();
                    }
                    else
                        MoveTowards(target.position, definition.speed);
                    break;
                case BrainState.Telegraph:
                    TelegraphPose();
                    if (stateTimer <= 0)
                    {
                        ResolveAttack(target);
                        state = BrainState.Recover;
                        stateTimer = definition.attackInterval;
                    }
                    break;
                case BrainState.Recover:
                    if (stateTimer <= 0) state = BrainState.Chase;
                    break;
                case BrainState.Retreat:
                    MoveTowards(home, definition.speed * 1.2f);
                    if (Vector3.Distance(transform.position, home) < 1f) state = BrainState.Wander;
                    break;
            }
            DetectStuck();
        }

        Transform ChooseTarget()
        {
            WorldBootstrap world = WorldBootstrap.Instance;
            Transform best = world.Player ? world.Player.transform : null;
            float distance = best ? (best.position - transform.position).sqrMagnitude : float.MaxValue;
            foreach (SquadUnit unit in FindObjectsByType<SquadUnit>(FindObjectsSortMode.None))
            {
                if (!unit.IsAvailable) continue;
                float candidate = (unit.transform.position - transform.position).sqrMagnitude;
                if (candidate >= distance) continue;
                distance = candidate;
                best = unit.transform;
            }
            return best;
        }

        void Wander()
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0)
            {
                wanderTimer = Random.Range(2.4f, 4.8f);
                Vector2 circle = Random.insideUnitCircle * 2.7f;
                wanderTarget = home + new Vector3(circle.x, 0, circle.y);
            }
            MoveTowards(wanderTarget, definition.speed * .52f);
        }

        void MoveTowards(Vector3 target, float speed)
        {
            target.y = WorldBootstrap.GroundHeight(target.x, target.z) + .035f;
            Vector3 direction = target - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude < .06f) return;
            direction.Normalize();
            Vector3 origin = transform.position + Vector3.up * .3f;
            if (Physics.SphereCast(origin, .14f, direction, out RaycastHit hit, .62f, ~0, QueryTriggerInteraction.Ignore) &&
                hit.collider != bodyCollider)
            {
                Vector3 side = Vector3.Cross(Vector3.up, hit.normal).normalized;
                if (Vector3.Dot(side, direction) < 0) side = -side;
                direction = Vector3.Slerp(direction, side, .72f);
            }
            Vector3 next = transform.position + direction * speed * Time.deltaTime;
            next.y = WorldBootstrap.GroundHeight(next.x, next.z) + .035f;
            transform.position = next;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 6.5f * Time.deltaTime);
        }

        void TelegraphPose()
        {
            float pulse = 1f + Mathf.Sin(Time.time * 24f) * .035f;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * pulse, Time.deltaTime * 12f);
            Vector3 facing = attackTarget - transform.position;
            facing.y = 0;
            if (facing.sqrMagnitude > .1f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing), Time.deltaTime * 9f);
        }

        void ResolveAttack(Transform target)
        {
            transform.localScale = Vector3.one;
            spiderVisual?.PlayAttack();
            AttackEvents++;
            if (!target || Vector3.Distance(transform.position, target.position) > definition.attackRange * 1.28f) return;
            SuccessfulAttacks++;
            if (target.TryGetComponent(out PlayerAnt player)) player.Damage(definition.damage);
            else target.GetComponent<SquadUnit>()?.Damage(definition.damage);
            AudioDirector.Instance?.PlayHit(target.position);
            FxPool.Instance?.Burst(target.position + Vector3.up * .3f, new Color(.48f, .13f, .04f), 9);
        }

        void DetectStuck()
        {
            if ((transform.position - lastPosition).sqrMagnitude < .0001f) stuckTimer += Time.deltaTime;
            else stuckTimer = 0;
            lastPosition = transform.position;
            if (stuckTimer > 1.1f)
            {
                transform.position += transform.right * (Random.value > .5f ? .35f : -.35f);
                stuckTimer = 0;
            }
        }

        public void Damage(float amount) => Damage(amount, WorldBootstrap.Instance.Player.transform.position);

        public void Damage(float amount, Vector3 sourcePosition)
        {
            if (!IsActive) return;
            DamageEvents++;
            Vector3 incoming = (sourcePosition - transform.position).normalized;
            float front = Vector3.Dot(transform.forward, incoming);
            float weakPointMultiplier = Kind switch
            {
                Species.Beetle => front < .15f ? 1.65f : .68f,
                Species.Spider => front < -.25f ? 1.45f : 1f,
                _ => 1f
            };
            Health -= amount * weakPointMultiplier;
            AudioDirector.Instance?.PlayHit(transform.position);
            FxPool.Instance?.Burst(transform.position + Vector3.up * .42f, new Color(.65f, .24f, .06f), 10);
            rivalVisual?.PlayStagger();
            spiderVisual?.PlayStagger();
            StartCoroutine(HitFlash());
            if (Health > 0)
            {
                state = Health < MaxHealth * .18f && Kind != Species.RivalAnt ? BrainState.Retreat : BrainState.Chase;
                WorldBootstrap.Instance.ShowCreatureStatus(DisplayName, Health, MaxHealth, weakPointMultiplier > 1.1f);
                return;
            }
            StartCoroutine(Die());
        }

        IEnumerator HitFlash()
        {
            foreach (Renderer item in renderers)
            {
                if (!item) continue;
                item.enabled = false;
            }
            yield return new WaitForSeconds(.045f);
            foreach (Renderer item in renderers)
            {
                if (!item) continue;
                item.enabled = true;
            }
        }

        IEnumerator Die()
        {
            state = BrainState.Dead;
            bodyCollider.enabled = false;
            rivalVisual?.PlayDeath();
            spiderVisual?.PlayDeath();
            WorldBootstrap.Instance.Colony.Add(ResourceKind.Protein, Kind == Species.Spider ? 3 : 1);
            WorldBootstrap.Instance.Mission.NotifyKill(Kind);
            WorldBootstrap.Instance.ShowToast(GameText.Pick($"{DisplayName} defeated", $"{DisplayName} повержен"));
            float elapsed = 0;
            Quaternion start = transform.rotation;
            while (elapsed < .8f)
            {
                elapsed += Time.deltaTime;
                if (!spiderVisual)
                    transform.rotation = Quaternion.Slerp(
                        start,
                        start * Quaternion.Euler(0, 0, 82f),
                        elapsed / .8f);
                yield return null;
            }
            yield return new WaitForSeconds(1.8f);
            gameObject.SetActive(false);
        }

        public string DisplayName => Kind switch
        {
            Species.Beetle => GameText.Pick("Barkshield beetle", "Жук Кора-Щит"),
            Species.RivalAnt => GameText.Pick("Emberjaw raider", "Налётчик Огненных Жвал"),
            _ => GameText.Pick("Ashback spider", "Паук Пепельноспин")
        };
    }

    public sealed class SquadController : MonoBehaviour
    {
        sealed class Unit
        {
            public SquadUnit Actor;
            public float Cooldown;
            public float Stuck;
            public Vector3 Last;
        }

        public SquadOrder Order { get; private set; } = SquadOrder.Follow;
        public Vector3 CommandPosition { get; private set; }
        public string SelectedGroup { get; private set; }
        public string StatusText => GameText.Pick(
            $"{SelectedGroup} · {Order}",
            $"{SelectedGroup} · {OrderName(Order)}");
        readonly List<Unit> units = new();
        ResourceNode targetResource;
        Creature targetCreature;
        Vector3 patrolA;
        Vector3 patrolB;
        bool patrolToggle;

        public void Add(Transform unitTransform, UnitRole role)
        {
            SquadUnit actor = unitTransform.gameObject.AddComponent<SquadUnit>();
            actor.Initialize(role);
            units.Add(new Unit { Actor = actor, Last = actor.transform.position });
            SelectAll();
        }

        public void SelectAll()
        {
            foreach (Unit unit in units)
                unit.Actor.SetSelected(unit.Actor.gameObject.activeSelf);
            SelectedGroup = GameText.Pick("All squads", "Все отряды");
        }

        public void SelectWorkers()
        {
            foreach (Unit unit in units) unit.Actor.SetSelected(unit.Actor.Role == UnitRole.Worker);
            SelectedGroup = GameText.Pick("Workers", "Рабочие");
        }

        public void SelectSoldiers()
        {
            foreach (Unit unit in units)
                unit.Actor.SetSelected(
                    unit.Actor.gameObject.activeSelf &&
                    unit.Actor.Role != UnitRole.Worker);
            SelectedGroup = GameText.Pick("Soldiers", "Солдаты");
        }

        public void SetSoldiersUnlocked(bool unlocked)
        {
            foreach (Unit unit in units)
            {
                if (!unit.Actor || unit.Actor.Role == UnitRole.Worker) continue;
                bool wasActive = unit.Actor.gameObject.activeSelf;
                unit.Actor.gameObject.SetActive(unlocked);
                if (!unlocked)
                    unit.Actor.SetSelected(false);
                else
                {
                    WorldBootstrap world = WorldBootstrap.Instance;
                    if (!wasActive && world && world.Player)
                        unit.Actor.transform.position =
                            world.Player.transform.position +
                            FormationOffset(units.IndexOf(unit), unit.Actor.Role);
                }
            }
            if (!unlocked) SelectWorkers();
        }

        public void Command(SquadOrder order, Vector3 position, ResourceNode resource = null, Creature creature = null)
        {
            Order = order;
            CommandPosition = position;
            targetResource = resource;
            targetCreature = creature;
            if (order == SquadOrder.Patrol)
            {
                patrolA = WorldBootstrap.Instance.NestPosition;
                patrolB = position;
            }
            AudioDirector.Instance?.PlayOrder(WorldBootstrap.Instance.Player.transform.position);
            WorldBootstrap.Instance.ShowToast(OrderMessage(order));
            bool workersSelected = units.Exists(unit =>
                unit.Actor && unit.Actor.gameObject.activeSelf &&
                unit.Actor.Selected && unit.Actor.Role == UnitRole.Worker);
            bool soldiersSelected = units.Exists(unit =>
                unit.Actor && unit.Actor.gameObject.activeSelf &&
                unit.Actor.Selected && unit.Actor.Role != UnitRole.Worker);
            WorldBootstrap.Instance.Mission.NotifySquadCommand(
                order, workersSelected, soldiersSelected);
        }

        public void Set(SquadOrder order)
        {
            Vector3 position = order switch
            {
                SquadOrder.Defend or SquadOrder.ReturnToNest or SquadOrder.Retreat => WorldBootstrap.Instance.NestPosition,
                _ => WorldBootstrap.Instance.Player.transform.position
            };
            Command(order, position);
        }

        public void Teleport(Vector3 center)
        {
            for (int i = 0; i < units.Count; i++)
            {
                Unit unit = units[i];
                if (!unit.Actor) continue;
                Vector3 position = center + FormationOffset(i, unit.Actor.Role) * 1.15f;
                if (position.y > -2f)
                    position.y = WorldBootstrap.GroundHeight(position.x, position.z) + .025f;
                unit.Actor.transform.position = position;
                unit.Last = position;
                unit.Stuck = 0;
            }
        }

        void Update()
        {
            WorldBootstrap world = WorldBootstrap.Instance;
            if (!world || !world.IsPlaying || world.IsPaused || !world.Player) return;
            if (Order == SquadOrder.Patrol && Vector3.Distance(units[0].Actor.transform.position, patrolToggle ? patrolA : patrolB) < 1.2f)
                patrolToggle = !patrolToggle;

            for (int i = 0; i < units.Count; i++)
            {
                Unit unit = units[i];
                if (!unit.Actor || !unit.Actor.IsAvailable) continue;
                unit.Cooldown -= Time.deltaTime;
                Vector3 goal = DetermineGoal(unit, i);
                MoveUnit(unit, goal, i);
            }
        }

        Vector3 DetermineGoal(Unit unit, int index)
        {
            WorldBootstrap world = WorldBootstrap.Instance;
            SquadUnit actor = unit.Actor;
            Vector3 offset = FormationOffset(index, actor.Role);
            if (actor.HasCargo)
            {
                if (Vector3.Distance(actor.transform.position, world.NestPosition) < 1.45f)
                {
                    ResourceKind delivered = actor.DeliverCargo();
                    world.Colony.Add(delivered, 1);
                    world.Mission.NotifyGather();
                    world.ShowToast(GameText.Pick($"{delivered} delivered by worker", $"Рабочий доставил: {delivered}"));
                }
                return world.NestPosition + offset * .45f;
            }
            if (!actor.Selected)
                return world.Player.transform.TransformPoint(offset);

            switch (Order)
            {
                case SquadOrder.Gather:
                    if (actor.Role != UnitRole.Worker)
                        return targetResource ? targetResource.transform.position + offset * .6f : world.Player.transform.TransformPoint(offset);
                    ResourceNode resource = targetResource && targetResource.Available
                        ? targetResource
                        : world.FindNearestResource(actor.transform.position);
                    if (resource)
                    {
                        if (Vector3.Distance(actor.transform.position, resource.transform.position) < .72f && unit.Cooldown <= 0 &&
                            resource.TryTake(out ResourceKind cargo))
                        {
                            actor.TakeCargo(cargo);
                            unit.Cooldown = 1.15f;
                        }
                        return resource.transform.position;
                    }
                    return world.NestPosition + offset;

                case SquadOrder.Attack:
                    if (actor.Role == UnitRole.Worker)
                        return world.Player.transform.TransformPoint(offset * 1.25f);
                    Creature creature = targetCreature && targetCreature.IsActive
                        ? targetCreature
                        : world.FindNearestActiveCreature(actor.transform.position);
                    if (creature)
                    {
                        float range = GameDefinitions.Ant(actor.Role).attackRange;
                        if (Vector3.Distance(actor.transform.position, creature.transform.position) < range && unit.Cooldown <= 0)
                        {
                            actor.Visual?.PlayAttack();
                            creature.Damage(GameDefinitions.Ant(actor.Role).damage, actor.transform.position);
                            unit.Cooldown = actor.Role == UnitRole.HeavySoldier ? 1.15f : .78f;
                        }
                        return creature.transform.position + offset.normalized * .55f;
                    }
                    return world.Player.transform.TransformPoint(offset);

                case SquadOrder.Defend:
                    Creature threat = world.FindNearestActiveCreature(world.NestPosition);
                    if (threat && Vector3.Distance(threat.transform.position, world.NestPosition) < 8f && actor.Role != UnitRole.Worker)
                    {
                        targetCreature = threat;
                        return threat.transform.position + offset.normalized * .55f;
                    }
                    return CommandPosition + offset;
                case SquadOrder.Move:
                    return CommandPosition + offset;
                case SquadOrder.Patrol:
                    return (patrolToggle ? patrolA : patrolB) + offset;
                case SquadOrder.Retreat:
                case SquadOrder.ReturnToNest:
                    return world.NestPosition + offset;
                default:
                    return world.Player.transform.TransformPoint(offset);
            }
        }

        void MoveUnit(Unit unit, Vector3 goal, int index)
        {
            SquadUnit actor = unit.Actor;
            goal.y = WorldBootstrap.GroundHeight(goal.x, goal.z) + .025f;
            Vector3 direction = goal - actor.transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude < .045f) return;
            direction.Normalize();

            Vector3 separation = Vector3.zero;
            foreach (Unit other in units)
            {
                if (other == unit || !other.Actor || !other.Actor.IsAvailable) continue;
                Vector3 away = actor.transform.position - other.Actor.transform.position;
                away.y = 0;
                float distance = away.magnitude;
                if (distance > .01f && distance < .52f)
                    separation += away.normalized * (1f - distance / .52f);
            }
            direction = (direction + separation * .85f).normalized;
            Vector3 origin = actor.transform.position + Vector3.up * .26f;
            if (Physics.SphereCast(origin, .1f, direction, out RaycastHit hit, .42f, ~0, QueryTriggerInteraction.Ignore) &&
                !hit.collider.GetComponentInParent<SquadUnit>())
            {
                Vector3 left = Vector3.Cross(Vector3.up, hit.normal).normalized;
                if (Vector3.Dot(left, direction) < 0) left = -left;
                direction = Vector3.Slerp(direction, left, .75f);
            }

            float speed = GameDefinitions.Ant(actor.Role).speed;
            Vector3 before = actor.transform.position;
            Vector3 next = before + direction * speed * Time.deltaTime;
            next.y = WorldBootstrap.GroundHeight(next.x, next.z) + .025f;
            actor.transform.position = next;
            actor.transform.rotation = Quaternion.Slerp(actor.transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 9f);

            if ((actor.transform.position - unit.Last).sqrMagnitude < .00008f) unit.Stuck += Time.deltaTime;
            else unit.Stuck = 0;
            unit.Last = actor.transform.position;
            if (unit.Stuck > 1f)
            {
                actor.transform.position += actor.transform.right * ((index & 1) == 0 ? .38f : -.38f);
                unit.Stuck = 0;
            }
        }

        static Vector3 FormationOffset(int index, UnitRole role)
        {
            Vector3[] formation =
            {
                new(-.95f, 0, -.25f), new(.95f, 0, -.25f),
                new(-1.3f, 0, .55f), new(1.3f, 0, .55f),
                new(-.78f, 0, 1.25f), new(.78f, 0, 1.25f),
                new(0, 0, 1.75f), new(0, 0, -.92f)
            };
            Vector3 result = formation[index % formation.Length];
            if (role == UnitRole.HeavySoldier) result *= .82f;
            return result;
        }

        static string OrderMessage(SquadOrder order) => order switch
        {
            SquadOrder.Gather => GameText.Pick("Workers gather and carry; soldiers escort", "Рабочие собирают и несут; солдаты прикрывают"),
            SquadOrder.Attack => GameText.Pick("Soldiers surround the marked threat", "Солдаты окружают отмеченную угрозу"),
            SquadOrder.Move => GameText.Pick("Squad moving to the marked position", "Отряд движется к отмеченной позиции"),
            SquadOrder.Defend => GameText.Pick("Squad defending Moonroot", "Отряд защищает Лунный Корень"),
            SquadOrder.Patrol => GameText.Pick("Squad patrolling the route", "Отряд патрулирует маршрут"),
            SquadOrder.Retreat => GameText.Pick("Squad breaks contact and retreats", "Отряд выходит из боя и отступает"),
            SquadOrder.ReturnToNest => GameText.Pick("Squad returning to the colony", "Отряд возвращается в колонию"),
            _ => GameText.Pick("Squad following the scout", "Отряд следует за разведчиком")
        };

        static string OrderName(SquadOrder order) => order switch
        {
            SquadOrder.Follow => "следовать",
            SquadOrder.Move => "перемещение",
            SquadOrder.Attack => "атака",
            SquadOrder.Gather => "сбор",
            SquadOrder.Defend => "оборона",
            SquadOrder.Patrol => "патруль",
            SquadOrder.Retreat => "отступление",
            _ => "домой"
        };
    }
}
