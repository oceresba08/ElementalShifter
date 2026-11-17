using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Phase 10: Q and R abilities; J/K/L remain in PlayerCombat.
// Particle effects are user-authored prefabs, never procedurally created here.
[RequireComponent(typeof(PlayerCombat))]
public class ElementalAbilityController : MonoBehaviour
{
    [System.Serializable]
    public class AbilityEffects
    {
        public ElementId element;
        public GameObject skillEffect;
        public GameObject ultimateEffect;
    }

    [System.Serializable]
    public class BalanceOverride
    {
        public ElementId element;
        [Min(0.1f)] public float skillDamageMultiplier = 1f;
        [Min(0.1f)] public float ultimateDamageMultiplier = 1f;
        [Min(0f)] public float skillCooldownOverride = 0f; // 0 means use default
        [Min(0f)] public float skillHealMultiplier = 1f;
        [Min(0f)] public float ultimateHealMultiplier = 1f;
        [Min(0f)] public float passiveHealMultiplier = 1f;

        [Tooltip("-1 = use this move's original healing; 0 = no healing; any positive number = exact HP healed by Q.")]
        public float skillHealHPOverride = -1f;
        [Tooltip("-1 = use original healing; 0 = no healing; positive = HP per ultimate healing pulse.")]
        public float ultimateHealHPOverride = -1f;
        [Tooltip("-1 = use original passive healing; 0 = no passive healing; positive = HP healed per second while eligible.")]
        public float passiveHealHPPerSecondOverride = -1f;
    }

    [Header("Per-element balance (expand each element)")]
    [Tooltip("Each entry has separate damage, cooldown, skill healing, ultimate healing and passive healing settings.")]
    public BalanceOverride[] balanceOverrides;

    // Run from the component's three-dot menu in the Inspector.
    // This adds missing entries without overwriting existing balance entries.
    [ContextMenu("Create Missing Element Balance Entries")]
    private void CreateMissingElementBalanceEntries()
    {
        var list = balanceOverrides != null
            ? new List<BalanceOverride>(balanceOverrides)
            : new List<BalanceOverride>();
        foreach (ElementId id in System.Enum.GetValues(typeof(ElementId)))
        {
            if (list.Exists(x => x != null && x.element == id)) continue;
            list.Add(new BalanceOverride { element = id });
        }
        balanceOverrides = list.ToArray();
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void Reset() => CreateMissingElementBalanceEntries();

    [Header("References")]
    public PlayerElements elements;
    public PlayerCombat combat;
    public Transform effectsAnchor;
    public ElementProjectile projectilePrefab;

    [Header("Handmade VFX (optional)")]
    public AbilityEffects[] effects;
    public float effectLifetime = 4f;

    [Header("Balance")]
    public float skillBaseDamage = 25f;
    public float ultimateBaseDamage = 58f;
    public float projectileSpeed = 17f;
    public float skillStagger = 1.2f;
    public float ultimateStagger = 2.6f;
    public bool requireMatchingWeatherForLightningAndIce = true;

    [Header("Passives")]
    public float natureHealPerSecond = 1.2f;
    public float waterHealPerSecondInRain = 0.7f;
    public float paperMovementBonus = 1.1f;
    public float windMovementBonus = 1.16f;
    public float fireMovementBonus = 1.08f;
    public float mudMovementMultiplier = 0.91f;

    private float SkillDamage => skillBaseDamage * DamageMultiplier(CurrentId, false);
    private float UltimateDamage => ultimateBaseDamage * DamageMultiplier(CurrentId, true);
    private float DamageMultiplier(ElementId id, bool ultimate)
    {
        if (balanceOverrides != null)
            foreach (BalanceOverride setting in balanceOverrides)
                if (setting != null && setting.element == id)
                    return ultimate ? setting.ultimateDamageMultiplier : setting.skillDamageMultiplier;
        return 1f;
    }

    // Set these independently for each ElementId in the Balance Overrides array.
    // Healing multipliers scale healing already present in an ability.
    private float HealMultiplier(ElementId id, bool ultimate, bool passive = false)
    {
        if (balanceOverrides != null)
            foreach (BalanceOverride setting in balanceOverrides)
                if (setting != null && setting.element == id)
                    return passive ? setting.passiveHealMultiplier :
                        ultimate ? setting.ultimateHealMultiplier : setting.skillHealMultiplier;
        return 1f;
    }

    private BalanceOverride BalanceFor(ElementId id)
    {
        if (balanceOverrides == null) return null;
        foreach (BalanceOverride entry in balanceOverrides)
            if (entry != null && entry.element == id) return entry;
        return null;
    }

    private float HealAmount(ElementId id, bool ultimate, float originalAmount)
    {
        BalanceOverride b = BalanceFor(id);
        if (b == null) return originalAmount;
        float exact = ultimate ? b.ultimateHealHPOverride : b.skillHealHPOverride;
        float amount = exact >= 0f ? exact : originalAmount;
        return amount * HealMultiplier(id, ultimate);
    }

    private float PassiveHealAmount(ElementId id, float originalPerSecond)
    {
        BalanceOverride b = BalanceFor(id);
        if (b == null) return originalPerSecond;
        float amount = b.passiveHealHPPerSecondOverride >= 0f
            ? b.passiveHealHPPerSecondOverride : originalPerSecond;
        return amount * b.passiveHealMultiplier;
    }

    private readonly Dictionary<ElementId, float> cooldownEnds = new Dictionary<ElementId, float>();
    private float nextPassiveTick;
    private bool casting;
    public bool IsCasting => casting;
    public string LastMessage { get; private set; } = "";
    public float LastMessageUntil { get; private set; }
    public ElementId CurrentId => elements != null && elements.Current != null ? elements.Current.id : ElementId.Fire;
    public bool HasElement => elements != null && elements.Current != null;
    public float SkillCooldown => HasElement ? CooldownFor(CurrentId) : 0f;
    public float SkillRemaining => HasElement && cooldownEnds.TryGetValue(CurrentId, out float end) ? Mathf.Max(0f, end - Time.time) : 0f;
    public bool WeatherLocked => HasElement && !WeatherAllows(CurrentId);
    public bool CanSkill => HasElement && !casting && SkillRemaining <= 0f && !WeatherLocked;
    public bool CanUltimate => HasElement && !casting && combat != null && combat.UltimateCharge >= 100f && WeatherAllows(CurrentId);

    private void Awake()
    {
        if (elements == null) elements = GetComponent<PlayerElements>();
        if (combat == null) combat = GetComponent<PlayerCombat>();
        if (effectsAnchor == null && elements != null) effectsAnchor = elements.effectsAnchor;
    }

    private void Update()
    {
        if (Time.time < nextPassiveTick || !HasElement) return;
        nextPassiveTick = Time.time + 1f;
        PlayerDurability hp = GetComponent<PlayerDurability>();
        if (hp == null) return;
        if (CurrentId == ElementId.Nature || CurrentId == ElementId.Bloom)
            hp.Heal(PassiveHealAmount(CurrentId, natureHealPerSecond));
        else if (CurrentId == ElementId.Water && WeatherManager.Instance != null &&
                 (WeatherManager.Instance.Current == WeatherType.Rain || WeatherManager.Instance.Current == WeatherType.Storm))
            hp.Heal(PassiveHealAmount(CurrentId, waterHealPerSecondInRain));
    }

    // Optional modifier read by BallMotor. All other ElementData multipliers still apply.
    public float MovementMultiplier
    {
        get
        {
            if (!HasElement) return 1f;
            switch (CurrentId)
            {
                case ElementId.Wind: return windMovementBonus;
                case ElementId.Paper: return paperMovementBonus;
                case ElementId.Fire: return fireMovementBonus;
                case ElementId.Mud: return mudMovementMultiplier;
                default: return 1f;
            }
        }
    }

    public bool TryUseSkill()
    {
        if (!HasElement || casting) return false;
        if (!WeatherAllows(CurrentId)) { Message("NEEDS " + (CurrentId == ElementId.Ice ? "SNOW" : "STORM")); return false; }
        if (SkillRemaining > 0f) { Message("SKILL COOLING DOWN"); return false; }
        ElementId element = CurrentId;
        cooldownEnds[element] = Time.time + CooldownFor(element);
        SpawnEffect(element, false);
        StartCoroutine(SkillRoutine(element, elements.Current));
        return true;
    }

    public bool TryUseUltimate()
    {
        if (!CanUltimate) { Message(!WeatherAllows(CurrentId) ? "WEATHER LOCKED" : "ULTIMATE NOT READY"); return false; }
        if (!combat.TrySpendUltimateCharge(100f)) return false;
        ElementId element = CurrentId;
        SpawnEffect(element, true);
        StartCoroutine(UltimateRoutine(element, elements.Current));
        return true;
    }

    private IEnumerator SkillRoutine(ElementId id, ElementData element)
    {
        casting = true;
        Vector3 forward = FlatForward();
        switch (id)
        {
            case ElementId.Fire: // Flare Rush: offensive forward burst
                Message("FLARE RUSH");
                GetComponent<Rigidbody>().AddForce(forward * 7f, ForceMode.VelocityChange);
                yield return new WaitForSeconds(0.14f);
                DamageNear(transform.position + forward * 1.3f, 2.7f, SkillDamage * 1.2f, element, 1.5f);
                break;
            case ElementId.Water: // Restorative jet
                Message("RESTORATIVE JET");
                Heal(11f, id, false);
                DamageCone(5.5f, 0.48f, SkillDamage * 0.85f, element, 1f);
                break;
            case ElementId.Earth: // Ground shatter
                Message("STONE CRASH");
                yield return new WaitForSeconds(0.33f);
                DamageNear(transform.position, 3.2f, SkillDamage * 1.2f, element, 2.4f);
                break;
            case ElementId.Nature: // Heal & roots
                Message("ROOT RENEWAL");
                Heal(16f, id, false);
                DamageNear(transform.position, 2.6f, SkillDamage * 0.55f, element, 2.5f);
                break;
            case ElementId.Wind: // Air cutter
                Message("AIR CUTTER");
                LaunchProjectile(element, forward, SkillDamage, 1f);
                break;
            case ElementId.Paper: // Two razor pages
                Message("RAZOR PAGES");
                LaunchProjectile(element, Quaternion.Euler(0, -8f, 0) * forward, SkillDamage * 0.62f, 1f);
                LaunchProjectile(element, Quaternion.Euler(0, 8f, 0) * forward, SkillDamage * 0.62f, 1f);
                break;
            case ElementId.Mud: // Slow pool
                Message("MUD TRAP");
                DamageNear(transform.position + forward * 2f, 3f, SkillDamage * 0.6f, element, 2.5f);
                StartCoroutine(SlowNearby(transform.position + forward * 2f, 3f, 3.5f, 0.55f));
                break;
            case ElementId.Steam: // Steam screen, repeated pulses
                Message("STEAM SCREEN");
                yield return Pulses(element, 2, 0.45f, 3.2f, SkillDamage * 0.38f, 1.2f);
                break;
            case ElementId.Bloom: // Bloom burst: healing + damage
                Message("BLOOM BURST");
                Heal(14f, id, false);
                DamageNear(transform.position, 3.3f, SkillDamage * 0.75f, element, 2f);
                break;
            case ElementId.Lightning: // Chain zap
                Message("CHAIN LIGHTNING");
                DamageNearest(3, 7f, SkillDamage * 0.78f, element, 1.7f);
                break;
            case ElementId.Ice: // Freeze enemies
                Message("FROST LOCK");
                DamageCone(5f, 0.45f, SkillDamage * 0.8f, element, 3f);
                break;
        }
        casting = false;
    }

    private IEnumerator UltimateRoutine(ElementId id, ElementData element)
    {
        casting = true;
        switch (id)
        {
            case ElementId.Fire:
                Message("INFERNO NOVA");
                yield return Pulses(element, 4, .35f, 5f, UltimateDamage * .31f, 2f);
                break;
            case ElementId.Water:
                Message("TIDAL REGENESIS"); Heal(30f, id, true);
                yield return Pulses(element, 3, .5f, 5.7f, UltimateDamage * .28f, 2.2f);
                break;
            case ElementId.Earth:
                Message("TITAN QUAKE"); Heal(12f, id, true);
                yield return new WaitForSeconds(.4f);
                DamageNear(transform.position, 6.3f, UltimateDamage, element, 4f);
                break;
            case ElementId.Nature:
                Message("LIVING SANCTUARY");
                for (int i = 0; i < 5; i++)
                {
                    Heal(8f, id, true); DamageNear(transform.position, 4f, UltimateDamage * .12f, element, 1.5f);
                    yield return new WaitForSeconds(.55f);
                }
                break;
            case ElementId.Wind:
                Message("TEMPEST FAN");
                for (int i = -3; i <= 3; i++)
                    LaunchProjectile(element, Quaternion.Euler(0, i * 12f, 0) * FlatForward(), UltimateDamage * .35f, 1f);
                break;
            case ElementId.Paper:
                Message("PAPER CYCLONE");
                yield return Pulses(element, 5, .3f, 4.5f, UltimateDamage * .22f, 1.5f);
                break;
            case ElementId.Mud:
                Message("QUAGMIRE");
                StartCoroutine(SlowNearby(transform.position, 5.5f, 5f, .35f));
                yield return Pulses(element, 3, .6f, 5.5f, UltimateDamage * .26f, 3f);
                break;
            case ElementId.Steam:
                Message("PRESSURE RELEASE");
                yield return new WaitForSeconds(.5f);
                DamageNear(transform.position, 6f, UltimateDamage * 1.1f, element, 3f);
                break;
            case ElementId.Bloom:
                Message("SPRING AWAKENING"); Heal(38f, id, true);
                yield return Pulses(element, 4, .45f, 5f, UltimateDamage * .20f, 2.2f);
                break;
            case ElementId.Lightning:
                Message("THUNDER JUDGMENT");
                yield return Pulses(element, 3, .28f, 7.5f, UltimateDamage * .31f, 3.5f);
                break;
            case ElementId.Ice:
                Message("ABSOLUTE ZERO");
                DamageNear(transform.position, 6f, UltimateDamage * .85f, element, 5f);
                break;
        }
        casting = false;
    }

    private IEnumerator Pulses(ElementData e, int count, float delay, float radius, float damage, float stagger)
    {
        for (int i = 0; i < count; i++)
        {
            DamageNear(transform.position, radius, damage, e, stagger);
            if (i < count - 1) yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator SlowNearby(Vector3 center, float radius, float duration, float factor)
    {
        Dictionary<NavMeshAgent, float> changed = new Dictionary<NavMeshAgent, float>();
        foreach (EnemyLayers enemy in FindObjectsByType<EnemyLayers>(FindObjectsSortMode.None))
        {
            if (enemy == null || Vector3.Distance(enemy.transform.position, center) > radius) continue;
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled || changed.ContainsKey(agent)) continue;
            changed.Add(agent, agent.speed);
            agent.speed *= Mathf.Clamp(factor, .1f, 1f);
        }
        yield return new WaitForSeconds(duration);
        foreach (var pair in changed)
            if (pair.Key != null) pair.Key.speed = pair.Value;
    }

    private void DamageNear(Vector3 center, float radius, float damage, ElementData e, float stagger)
    {
        HashSet<EnemyLayers> seen = new HashSet<EnemyLayers>();
        foreach (Collider collider in Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            EnemyLayers enemy = collider.GetComponentInParent<EnemyLayers>();
            if (enemy != null && seen.Add(enemy) && enemy.TakeHit(damage, e, true, stagger))
                combat?.AddUltimateCharge(8f);
        }
    }

    private void DamageCone(float radius, float minDot, float damage, ElementData e, float stagger)
    {
        HashSet<EnemyLayers> seen = new HashSet<EnemyLayers>();
        Vector3 forward = FlatForward();
        foreach (Collider c in Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            EnemyLayers enemy = c.GetComponentInParent<EnemyLayers>();
            if (enemy == null || !seen.Add(enemy)) continue;
            Vector3 direction = enemy.transform.position - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude > 0.01f && Vector3.Dot(forward, direction.normalized) >= minDot &&
                enemy.TakeHit(damage, e, true, stagger)) combat?.AddUltimateCharge(8f);
        }
    }

    private void DamageNearest(int maxCount, float radius, float damage, ElementData e, float stagger)
    {
        var targets = new List<EnemyLayers>();
        foreach (EnemyLayers enemy in FindObjectsByType<EnemyLayers>(FindObjectsSortMode.None))
            if (enemy != null && Vector3.Distance(transform.position, enemy.transform.position) <= radius) targets.Add(enemy);
        targets.Sort((a, b) => Vector3.SqrMagnitude(a.transform.position - transform.position)
            .CompareTo(Vector3.SqrMagnitude(b.transform.position - transform.position)));
        for (int i = 0; i < Mathf.Min(maxCount, targets.Count); i++)
            if (targets[i].TakeHit(damage, e, true, stagger)) combat?.AddUltimateCharge(8f);
    }

    private void LaunchProjectile(ElementData e, Vector3 direction, float damage, float size)
    {
        Vector3 point = transform.position + Vector3.up * .45f + direction.normalized * 1.05f;
        ElementProjectile projectile;
        if (projectilePrefab != null)
            projectile = Instantiate(projectilePrefab, point, Quaternion.LookRotation(direction));
        else
        {
            // Existing ElementProjectile behavior supports a primitive fallback.
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "AbilityProjectile_Fallback";
            sphere.transform.position = point;
            sphere.transform.localScale = Vector3.one * (.35f * size);
            projectile = sphere.AddComponent<ElementProjectile>();
        }
        projectile.Initialize(combat, e, direction, projectileSpeed, damage, 2.5f);
    }

    private void Heal(float amount, ElementId id, bool ultimate)
    {
        float adjustedAmount = HealAmount(id, ultimate, amount);
        if (adjustedAmount > 0f)
            GetComponent<PlayerDurability>()?.Heal(adjustedAmount);
    }

    private Vector3 FlatForward()
    {
        Transform camera = Camera.main != null ? Camera.main.transform : transform;
        Vector3 forward = camera.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > .01f ? forward.normalized : Vector3.forward;
    }

    private void SpawnEffect(ElementId id, bool ultimate)
    {
        if (effects == null) return;
        foreach (AbilityEffects entry in effects)
        {
            if (entry == null || entry.element != id) continue;
            GameObject prefab = ultimate ? entry.ultimateEffect : entry.skillEffect;
            if (prefab == null) return;
            Transform anchor = effectsAnchor != null ? effectsAnchor : transform;
            GameObject fx = Instantiate(prefab, anchor.position, Quaternion.identity);
            Destroy(fx, Mathf.Max(.1f, effectLifetime));
            return;
        }
    }

    private bool WeatherAllows(ElementId id)
    {
        if (!requireMatchingWeatherForLightningAndIce || WeatherManager.Instance == null) return true;
        if (id == ElementId.Lightning) return WeatherManager.Instance.Current == WeatherType.Storm;
        if (id == ElementId.Ice) return WeatherManager.Instance.Current == WeatherType.Snow;
        return true;
    }

    private float CooldownFor(ElementId id)
    {
        if (balanceOverrides != null)
            foreach (BalanceOverride setting in balanceOverrides)
                if (setting != null && setting.element == id && setting.skillCooldownOverride > 0f)
                    return setting.skillCooldownOverride;
        switch (id)
        {
            case ElementId.Fire: return 4.5f;
            case ElementId.Water: return 5f;
            case ElementId.Nature: return 7f;
            case ElementId.Earth: return 6.5f;
            case ElementId.Wind: return 3f;
            case ElementId.Paper: return 3.3f;
            case ElementId.Mud: return 7.5f;
            case ElementId.Steam: return 6.5f;
            case ElementId.Bloom: return 7.5f;
            case ElementId.Lightning: return 5.5f;
            case ElementId.Ice: return 6f;
            default: return 5f;
        }
    }

    public string SkillLabel => !HasElement ? "NO FORM" : CurrentId.ToString().ToUpperInvariant() + " SKILL";
    public string UltimateLabel => !HasElement ? "NO FORM" : CurrentId.ToString().ToUpperInvariant() + " ULT";
    private void Message(string message) { LastMessage = message; LastMessageUntil = Time.time + 1.6f; }
}