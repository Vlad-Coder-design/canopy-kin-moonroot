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
        GameObject beacon;

        public string Prompt => Remaining > 0
            ? GameText.Pick($"Collect {Kind.ToString().ToLowerInvariant()}", Kind switch
            {
                ResourceKind.Seed => "Собрать лунное семя",
                ResourceKind.Resin => "Собрать янтарную смолу",
                _ => "Собрать белок"
            })
            : GameText.Pick("Depleted", "Источник исчерпан");

        public void Initialize(ResourceKind kind, int amount)
        {
            Kind = kind;
            Remaining = amount;
            var collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = .72f;
            collider.center = new Vector3(0, .35f, 0);
            collider.isTrigger = true;
            var host = gameObject.AddComponent<IInteractableHost>();
            host.Target = this;

            Color color = kind switch
            {
                ResourceKind.Seed => new Color(.76f, .42f, .08f),
                ResourceKind.Resin => new Color(1f, .38f, .035f),
                _ => new Color(.46f, .12f, .08f)
            };
            for (int i = 0; i < amount; i++)
            {
                float angle = i * 2.399f;
                GameObject piece = VisualFactory.Primitive(
                    PrimitiveType.Sphere,
                    kind.ToString(),
                    transform,
                    new Vector3(Mathf.Cos(angle) * .28f, .2f + i * .035f, Mathf.Sin(angle) * .28f),
                    kind == ResourceKind.Resin ? Vector3.one * .22f : new Vector3(.18f, .13f, .34f),
                    color,
                    false,
                    .68f);
                piece.transform.localRotation = Quaternion.Euler(12f, i * 71f, 28f);
                pieces.Add(piece);
            }
            beacon = VisualFactory.Primitive(
                PrimitiveType.Sphere,
                "Resource glow marker",
                transform,
                new Vector3(0, 1.05f, 0),
                Vector3.one * .09f,
                Color.Lerp(color, Color.white, .35f),
                false,
                .8f);
        }

        public void Interact(PlayerAnt player) => CollectOne();

        public bool CollectOne()
        {
            if (Remaining <= 0) return false;
            Remaining--;
            WorldBootstrap.Instance.Colony.Add(Kind, 1);
            WorldBootstrap.Instance.Mission.NotifyGather();
            if (Remaining < pieces.Count && pieces[Remaining]) pieces[Remaining].SetActive(false);
            if (Remaining == 0 && beacon) beacon.SetActive(false);
            WorldBootstrap.Instance.ShowToast(Kind switch
            {
                ResourceKind.Seed => GameText.Pick("Moonseed secured", "Лунное семя собрано"),
                ResourceKind.Resin => GameText.Pick("Amber resin secured", "Янтарная смола собрана"),
                _ => GameText.Pick("Protein secured", "Белок собран")
            });
            return true;
        }
    }

    public sealed class ColonyEntrance : MonoBehaviour, IInteractable
    {
        bool underground;

        public void Initialize(bool isUnderground) => underground = isUnderground;

        public string Prompt
        {
            get
            {
                WorldBootstrap world = WorldBootstrap.Instance;
                if (underground) return GameText.Pick("Return to the forest floor", "Вернуться на поверхность");
                if (world.Mission.Step == 4 && world.Colony.Level < 2)
                    return GameText.Pick(
                        $"Grow nursery ({ColonyState.UpgradeSeedCost} seed, {ColonyState.UpgradeResinCost} resin)",
                        $"Расширить ясли ({ColonyState.UpgradeSeedCost} семени, {ColonyState.UpgradeResinCost} смолы)");
                return GameText.Pick("Enter the Moonroot nursery", "Войти в ясли Лунного Корня");
            }
        }

        public void Interact(PlayerAnt player)
        {
            WorldBootstrap world = WorldBootstrap.Instance;
            if (!underground && world.Mission.Step == 4 && world.Colony.Level < 2)
            {
                if (world.Colony.Upgrade())
                {
                    world.ApplyNestUpgrade();
                    world.Mission.NotifyUpgrade();
                    world.ShowToast(GameText.Pick("The nursery has grown", "Ясли расширены"));
                }
                else
                {
                    world.ShowToast(GameText.Pick("More seed and resin are needed", "Нужно больше семян и смолы"));
                }
            }
            else
            {
                world.ToggleNest(player, underground);
            }
        }
    }

    public sealed class Creature : MonoBehaviour
    {
        public enum Species { Beetle, Spider, RivalAnt }

        public Species Kind { get; private set; }
        public float Health { get; private set; }
        public bool IsActive => WorldBootstrap.Instance &&
                                WorldBootstrap.Instance.IsPlaying &&
                                WorldBootstrap.Instance.Mission.Step >= requiredMissionStep &&
                                Health > 0;

        float speed;
        float aggro;
        float attackDamage;
        float attackCooldown;
        int requiredMissionStep;
        Vector3 home;
        Vector3 wanderTarget;
        float wanderTimer;
        Renderer[] renderers;

        public void Initialize(Species species, int missionStep)
        {
            Kind = species;
            requiredMissionStep = missionStep;
            Health = species switch
            {
                Species.Spider => 150,
                Species.RivalAnt => 72,
                _ => 78
            };
            speed = species switch
            {
                Species.Spider => 1.7f,
                Species.RivalAnt => 2.25f,
                _ => 1.45f
            };
            aggro = species == Species.Spider ? 11f : 7.5f;
            attackDamage = species == Species.Spider ? 17f : 9f;

            var collider = gameObject.AddComponent<SphereCollider>();
            collider.center = new Vector3(0, .42f, 0);
            collider.radius = species == Species.Spider ? .72f : .52f;
            if (species == Species.RivalAnt) AntVisual.Create(transform, new Color(.42f, .055f, .025f), 1.2f);
            else if (species == Species.Beetle) CreatureVisuals.BuildBeetle(transform);
            else CreatureVisuals.BuildSpider(transform);
            renderers = GetComponentsInChildren<Renderer>();
            home = transform.position;
            wanderTarget = home;
        }

        void Update()
        {
            WorldBootstrap world = WorldBootstrap.Instance;
            if (!world || !world.IsPlaying || world.IsPaused || Health <= 0) return;

            PlayerAnt player = world.Player;
            float distance = Vector3.Distance(transform.position, player.transform.position);
            Vector3 target;
            if (world.Mission.Step < requiredMissionStep)
            {
                target = home;
            }
            else if (distance < aggro)
            {
                target = player.transform.position;
            }
            else
            {
                wanderTimer -= Time.deltaTime;
                if (wanderTimer <= 0)
                {
                    wanderTimer = Random.Range(2.5f, 5f);
                    Vector2 circle = Random.insideUnitCircle * 2.4f;
                    wanderTarget = home + new Vector3(circle.x, 0, circle.y);
                }
                target = wanderTarget;
            }

            target.y = WorldBootstrap.GroundHeight(target.x, target.z) + .03f;
            Vector3 flat = target - transform.position;
            flat.y = 0;
            if (flat.sqrMagnitude > .16f)
            {
                Vector3 next = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                next.y = WorldBootstrap.GroundHeight(next.x, next.z) + .03f;
                transform.position = next;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), 6f * Time.deltaTime);
            }

            attackCooldown -= Time.deltaTime;
            if (IsActive && distance < (Kind == Species.Spider ? 1.45f : 1.05f) && attackCooldown <= 0)
            {
                player.Damage(attackDamage);
                attackCooldown = Kind == Species.Spider ? 1.15f : 1.35f;
            }
        }

        public void Damage(float amount)
        {
            if (!IsActive) return;
            Health -= amount;
            StopAllCoroutines();
            StartCoroutine(HitFlash());
            if (Health > 0)
            {
                WorldBootstrap.Instance.ShowToast(GameText.Pick(
                    $"{DisplayName}: {Health:0} health",
                    $"{DisplayName}: {Health:0} здоровья"));
                return;
            }

            WorldBootstrap.Instance.Colony.Add(ResourceKind.Protein, Kind == Species.Spider ? 3 : 1);
            WorldBootstrap.Instance.Mission.NotifyKill(Kind);
            WorldBootstrap.Instance.ShowToast(GameText.Pick($"{DisplayName} defeated", $"{DisplayName} повержен"));
            gameObject.SetActive(false);
        }

        System.Collections.IEnumerator HitFlash()
        {
            foreach (Renderer item in renderers) item.enabled = false;
            yield return new WaitForSeconds(.06f);
            foreach (Renderer item in renderers) item.enabled = true;
        }

        public string DisplayName => Kind switch
        {
            Species.Beetle => GameText.Pick("Bark beetle", "Жук-короед"),
            Species.RivalAnt => GameText.Pick("Emberjaw scout", "Разведчик Огненных Жвал"),
            _ => GameText.Pick("Ashback spider", "Паук Пепельноспин")
        };
    }

    public sealed class SquadController : MonoBehaviour
    {
        sealed class Unit
        {
            public Transform Transform;
            public float Cooldown;
        }

        public SquadOrder Order { get; private set; } = SquadOrder.Follow;
        readonly List<Unit> units = new();

        public void Add(Transform unit) => units.Add(new Unit { Transform = unit });

        public void Set(SquadOrder order)
        {
            Order = order;
            WorldBootstrap.Instance.ShowToast(order switch
            {
                SquadOrder.Gather => GameText.Pick("Workers: gather", "Рабочие: сбор ресурсов"),
                SquadOrder.Attack => GameText.Pick("Soldiers: attack", "Солдаты: атаковать"),
                SquadOrder.Defend => GameText.Pick("Squad: defend Moonroot", "Отряд: защищать Лунный Корень"),
                SquadOrder.Retreat => GameText.Pick("Squad: retreat", "Отряд: отступить"),
                _ => GameText.Pick("Squad: follow", "Отряд: следовать")
            });
        }

        void Update()
        {
            WorldBootstrap world = WorldBootstrap.Instance;
            if (!world || !world.IsPlaying || world.IsPaused || !world.Player) return;
            for (int i = 0; i < units.Count; i++)
            {
                Unit unit = units[i];
                if (!unit.Transform) continue;
                unit.Cooldown -= Time.deltaTime;
                Vector3[] formation =
                {
                    new(-1.0f, 0, -.15f), new(1.0f, 0, -.15f),
                    new(-1.25f, 0, .62f), new(1.25f, 0, .62f),
                    new(-.72f, 0, 1.28f), new(.72f, 0, 1.28f)
                };
                Vector3 offset = formation[i % formation.Length];
                Vector3 goal = world.Player.transform.TransformPoint(offset);

                if (Order == SquadOrder.Defend || Order == SquadOrder.Retreat)
                    goal = world.NestPosition + offset;
                else if (Order == SquadOrder.Gather)
                {
                    ResourceNode resource = world.FindNearestResource(unit.Transform.position);
                    if (resource)
                    {
                        goal = resource.transform.position;
                        if (Vector3.Distance(unit.Transform.position, goal) < .75f && unit.Cooldown <= 0)
                        {
                            resource.CollectOne();
                            unit.Cooldown = 1.3f;
                        }
                    }
                }
                else if (Order == SquadOrder.Attack)
                {
                    Creature creature = world.FindNearestActiveCreature(unit.Transform.position);
                    if (creature)
                    {
                        goal = creature.transform.position;
                        if (Vector3.Distance(unit.Transform.position, goal) < .9f && unit.Cooldown <= 0)
                        {
                            creature.Damage(8f);
                            unit.Cooldown = .85f;
                        }
                    }
                }

                goal.y = WorldBootstrap.GroundHeight(goal.x, goal.z) + .02f;
                Vector3 previous = unit.Transform.position;
                unit.Transform.position = Vector3.MoveTowards(previous, goal, 2.7f * Time.deltaTime);
                Vector3 movement = unit.Transform.position - previous;
                movement.y = 0;
                if (movement.sqrMagnitude > .0001f)
                    unit.Transform.rotation = Quaternion.Slerp(unit.Transform.rotation, Quaternion.LookRotation(movement), 9f * Time.deltaTime);
            }
        }
    }
}
