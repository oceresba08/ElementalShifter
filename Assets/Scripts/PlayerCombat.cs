using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody))]
public class PlayerCombat : MonoBehaviour
{
    public PlayerElements elements;
    [Header("Elemental Abilities")]
    public ElementalAbilityController elementalAbilities;
    public OrbitCombatCamera combatCamera;
    [Header("Physical attacks")]
    public float hitDamage = 17f, spinDamage = 12f, slamDamage = 26f;
    [Header("Attack Stagger Durations")]
    [Min(0f)] public float hitStaggerSeconds = 1.5f;
    [Min(0f)] public float spinStaggerSeconds = 2.5f;
    [Min(0f)] public float slamStaggerSeconds = 4f;
    [Header("Elemental attacks")]
    public float skillDamage = 25f, ultimateDamage = 58f;
    public float skillCooldown = 5f, hitCooldown = .65f, spinCooldown = 1.3f, slamCooldown = 2.5f;
    [Header("Lunge")]
    public float lockOnRange = 9f;
    public float lungeSpeed = 17f;
    public float lungeDuration = .23f;
    public float collisionDamageRadius = 1.7f;
    public float hitKnockback = 5f;
    [Header("C-curve Beyblade Spin (K)")]
    public float spinRange = 8f;
    public float spinDuration = 0.68f;
    public float spinSideOffset = 2.8f;
    public float spinExitDistance = 2.7f;
    public float spinTurnSpeed = 24f;
    public float spinHitRadius = 1.65f;
    public TrailRenderer spinTrail;
    [Header("elemental skills")]
    public ElementProjectile windProjectilePrefab;
    public float projectileSpeed = 18f;
    public float earthWindupSeconds = 0.4f;
    public GameObject windCastVFX, earthImpactVFX, naturePulseVFX;
    [Header("Nature regeneration")]
    public float natureHealPerSecond = 1.4f;
    public float sanctuaryDuration = 6f;
    public float sanctuaryHealPerSecond = 7f;
    float nextNatureRegen;
    public float UltimateCharge { get; private set; }
    public event Action<string> AttackFeedback;
    Rigidbody rb;
    float nextHit, nextSpin, nextSlam, nextSkill;
    [Header("Slam: jump, short apex slow motion, then impact")]
    public float slamJumpVelocity = 10.5f;
    public float slamFallVelocity = 17f;
    public float apexSlowMotion = 0.42f;
    public float apexHoldRealSeconds = 0.18f;
    public float slamGroundCheckDistance = 0.8f;
    public GameObject slamImpactVFX;
    bool busy;
    float previousFixedStep;
    bool ownsSlowMotion;
    void OnDisable() { RestoreTime(); combatCamera?.EndSlamFollow(); if (spinTrail != null) spinTrail.emitting = false; }
    void SetSlowMotion(float scale)
    {
        if (!ownsSlowMotion) { previousFixedStep = Time.fixedDeltaTime; ownsSlowMotion = true; }
        Time.timeScale = Mathf.Clamp(scale, .05f, 1f);
        Time.fixedDeltaTime = previousFixedStep * Time.timeScale;
    }
    void RestoreTime()
    {
        if (!ownsSlowMotion) return;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = previousFixedStep;
        ownsSlowMotion = false;
    }
    bool SlamGrounded()
    {
        Collider myCollider = GetComponent<Collider>();
        float radius = myCollider is SphereCollider s ? s.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) : 0.5f;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        foreach (RaycastHit h in Physics.SphereCastAll(origin, radius * .75f, Vector3.down, slamGroundCheckDistance, ~0, QueryTriggerInteraction.Ignore))
            if (h.collider != myCollider && !h.transform.IsChildOf(transform)) return true;
        return false;
    }
    void Awake()
    { rb = GetComponent<Rigidbody>(); if (elements == null) elements = GetComponent<PlayerElements>(); if (elementalAbilities == null) elementalAbilities = GetComponent<ElementalAbilityController>(); }
    void Update()
    {
        if (elementalAbilities == null && elements != null && elements.Current != null && elements.Current.combatClass == ElementClass.Healer && Time.time >= nextNatureRegen)
        {
            nextNatureRegen = Time.time + 1f;
            GetComponent<PlayerDurability>()?.Heal(natureHealPerSecond);
        }
        if (Keyboard.current == null || Time.timeScale == 0) return;
        if (Keyboard.current.jKey.wasPressedThisFrame && Time.time >= nextHit && !busy && (elementalAbilities == null || !elementalAbilities.IsCasting))
        { nextHit = Time.time + hitCooldown; StartCoroutine(LungeHit()); }
        if (Keyboard.current.kKey.wasPressedThisFrame && Time.time >= nextSpin && !busy && (elementalAbilities == null || !elementalAbilities.IsCasting))
        { nextSpin = Time.time + spinCooldown; StartCoroutine(CurvedSpin()); }
        if (Keyboard.current.lKey.wasPressedThisFrame && Time.time >= nextSlam && !busy && (elementalAbilities == null || !elementalAbilities.IsCasting))
        { nextSlam = Time.time + slamCooldown; StartCoroutine(Slam()); }
        if (Keyboard.current.qKey.wasPressedThisFrame && !busy && elements != null && elements.Current != null)
        {
            if (elementalAbilities != null) elementalAbilities.TryUseSkill();
            else if (Time.time >= nextSkill) { nextSkill = Time.time + skillCooldown; UseSkill(); }
        }
        if (Keyboard.current.rKey.wasPressedThisFrame && !busy && elements != null && elements.Current != null)
        {
            if (elementalAbilities != null) elementalAbilities.TryUseUltimate();
            else if (UltimateCharge >= 100f) { UltimateCharge = 0f; StartCoroutine(UseUltimate()); }
        }
    }
    EnemyLayers NearestEnemy(float range)
    {
        EnemyLayers best = null; float bestScore = float.MaxValue;
        foreach (EnemyLayers e in FindObjectsByType<EnemyLayers>(FindObjectsSortMode.None))
        {
            if (!e.isActiveAndEnabled) continue;
            Vector3 d = e.transform.position - transform.position; d.y = 0;
            if (d.magnitude > range) continue;
            float facing = Vector3.Dot(FlatForward(), d.normalized);
            if (facing < .1f) continue;
            float score = d.magnitude - facing * 2f;
            if (score < bestScore) { bestScore = score; best = e; }
        }
        return best;
    }
    IEnumerator LungeHit()
    {
        busy = true;
        EnemyLayers target = NearestEnemy(lockOnRange);
        Vector3 direction = target != null ? target.transform.position - transform.position : FlatForward();
        direction.y = 0; direction = direction.sqrMagnitude > .001f ? direction.normalized : FlatForward();
        combatCamera?.AttackBeat(.8f);
        AttackFeedback?.Invoke("HIT!");
        bool connected = false;
        float elapsed = 0;
        while (elapsed < lungeDuration)
        {
            elapsed += Time.fixedDeltaTime;
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(direction.x * lungeSpeed, v.y, direction.z * lungeSpeed);
            if (!connected)
            {
                Vector3 center = transform.position + direction * 1f;
                connected = Strike(center, collisionDamageRadius, hitDamage, false, direction, hitStaggerSeconds);
                if (connected) { combatCamera?.Shake(.2f, .15f); break; }
            }
            yield return new WaitForFixedUpdate();
        }
        busy = false;
    }
    IEnumerator Slam()
    {
        busy = true;
        AttackFeedback?.Invoke("SLAM!");
        combatCamera?.BeginSlamFollow();
        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(v.x * .5f, slamJumpVelocity, v.z * .5f);

        // Let the ball rise naturally; no camera swoop or rotation.
        float elapsed = 0f;
        while (rb.linearVelocity.y > 1f && elapsed < 1.3f)
        {
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Brief apex pause. Use real-time waiting so it stays short.
        SetSlowMotion(apexSlowMotion);
        yield return new WaitForSecondsRealtime(apexHoldRealSeconds);
        RestoreTime();
        rb.linearVelocity = new Vector3(rb.linearVelocity.x * .35f, -slamFallVelocity, rb.linearVelocity.z * .35f);

        // Apply damage on contact instead of an arbitrary time after launch.
        elapsed = 0f;
        while (elapsed < 2f)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
            if (rb.linearVelocity.y <= 0.1f && SlamGrounded()) break;
        }
        Strike(transform.position, 2.7f, slamDamage, false, null, slamStaggerSeconds);
        if (slamImpactVFX != null) Destroy(Instantiate(slamImpactVFX, transform.position + Vector3.down * .4f, Quaternion.identity), 3f);
        combatCamera?.EndSlamFollow();
        AttackFeedback?.Invoke("SMASH!");
        busy = false;
    }
    // One smooth C-shaped attack with the enemy at the midpoint. Camera yaw is unchanged.
    IEnumerator CurvedSpin()
    {
        busy = true;
        EnemyLayers enemy = NearestEnemy(spinRange);
        Vector3 start = rb.position;
        Vector3 forward = FlatForward();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        // Start on one side, curve around, land the hit at the bend, then exit on the other side.
        Vector3 aim = enemy != null ? enemy.transform.position : start + forward * 4f;
        aim.y = start.y;
        Vector3 impact = aim - forward * 0.95f;
        Vector3 controlIn = Vector3.Lerp(start, impact, .5f) + right * spinSideOffset;
        Vector3 exit = aim + forward * spinExitDistance + right * 1.2f;
        Vector3 controlOut = Vector3.Lerp(impact, exit, .5f) + right * spinSideOffset;
        bool struck = false;
        Vector3 oldAngularVelocity = rb.angularVelocity;
        if (spinTrail != null) { spinTrail.Clear(); spinTrail.emitting = true; }
        AttackFeedback?.Invoke("CURVE SPIN!");
        combatCamera?.AttackBeat(.35f);
        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(.1f, spinDuration));
            float u = t < .5f ? t * 2f : (t - .5f) * 2f;
            Vector3 point = t < .5f ? Bezier(start, controlIn, impact, u) : Bezier(impact, controlOut, exit, u);
            // Preserve vertical physics: spin follows the floor and doesn't teleport vertically.
            point.y = rb.position.y;
            rb.MovePosition(point);
            rb.angularVelocity = Vector3.up * spinTurnSpeed;
            if (!struck && t >= .46f)
            {
                struck = true;
                if (Strike(impact + forward * .95f, spinHitRadius, spinDamage, false, forward, spinStaggerSeconds))
                    combatCamera?.Shake(.07f, .09f);
            }
        }
        if (spinTrail != null) spinTrail.emitting = false;
        rb.angularVelocity = oldAngularVelocity;
        busy = false;
    }
    static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
    { float m = 1f - t; return m * m * a + 2f * m * t * b + t * t * c; }

    void UseSkill()
    {
        ElementData e = elements.Current;
        switch (e.combatClass)
        {
            case ElementClass.Ranger:
                FireProjectile(e, FlatForward(), skillDamage);
                AttackFeedback?.Invoke("AIR CUTTER!");
                break;
            case ElementClass.Heavy:
            case ElementClass.Tank:
                StartCoroutine(EarthWindup(e, skillDamage * 1.2f, 3.5f));
                AttackFeedback?.Invoke("STONE CRASH!");
                break;
            case ElementClass.Healer:
            case ElementClass.Support:
                GetComponent<PlayerDurability>()?.Heal(20f);
                SpawnEffect(naturePulseVFX, transform.position);
                Strike(transform.position, 2.6f, skillDamage * .35f, true);
                AttackFeedback?.Invoke("RENEWAL PULSE!");
                break;
            case ElementClass.Assassin:
            case ElementClass.Trickster:
                StartCoroutine(LungeHit());
                AttackFeedback?.Invoke(e.skillName.ToUpperInvariant());
                break;
            default:
                Strike(transform.position + FlatForward() * 1.8f, 3f, skillDamage, true);
                AttackFeedback?.Invoke(e.skillName.ToUpperInvariant());
                break;
        }
        combatCamera?.AttackBeat(.5f);
    }
    IEnumerator UseUltimate()
    {
        ElementData e = elements.Current;
        AttackFeedback?.Invoke(e.ultimateName.ToUpperInvariant());
        if (e.combatClass == ElementClass.Ranger)
        {
            Vector3 direction = FlatForward();
            for (int i = -2; i <= 2; i++)
                FireProjectile(e, Quaternion.Euler(0, i * 11f, 0) * direction, ultimateDamage * .48f);
        }
        else if (e.combatClass == ElementClass.Heavy || e.combatClass == ElementClass.Tank)
        {
            yield return EarthWindup(e, ultimateDamage, 5.5f);
        }
        else if (e.combatClass == ElementClass.Healer || e.combatClass == ElementClass.Support)
        {
            GetComponent<PlayerDurability>()?.Heal(30f);
            SpawnEffect(naturePulseVFX, transform.position);
            Strike(transform.position, 4f, ultimateDamage * .45f, true);
            float time = 0f;
            while (time < sanctuaryDuration)
            {
                yield return new WaitForSeconds(1f);
                time += 1f;
                GetComponent<PlayerDurability>()?.Heal(sanctuaryHealPerSecond);
            }
        }
        else
        {
            Strike(transform.position, 5f, ultimateDamage, true);
        }
        combatCamera?.AttackBeat(.65f);
    }
    IEnumerator EarthWindup(ElementData e, float damage, float radius)
    {
        busy = true;
        yield return new WaitForSeconds(earthWindupSeconds);
        SpawnEffect(earthImpactVFX, transform.position);
        Strike(transform.position, radius, damage, true);
        combatCamera?.Shake(.10f, .10f);
        busy = false;
    }
    void FireProjectile(ElementData e, Vector3 direction, float damage)
    {
        SpawnEffect(windCastVFX, transform.position + Vector3.up * .3f);
        Vector3 spawn = transform.position + Vector3.up * .4f + direction.normalized * .85f;
        ElementProjectile projectile;
        if (windProjectilePrefab != null)
            projectile = Instantiate(windProjectilePrefab, spawn, Quaternion.LookRotation(direction));
        else
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "AirCutter_Prototype";
            sphere.transform.position = spawn;
            sphere.transform.localScale = Vector3.one * .45f;
            projectile = sphere.AddComponent<ElementProjectile>();
        }
        projectile.Initialize(this, e, direction, projectileSpeed, damage, 2.4f);
    }
    void SpawnEffect(GameObject effect, Vector3 pos)
    {
        if (effect == null) return;
        Destroy(Instantiate(effect, pos, Quaternion.identity), 4f);
    }
    public bool TrySpendUltimateCharge(float amount)
    {
        if (UltimateCharge < amount) return false;
        UltimateCharge = Mathf.Max(0f, UltimateCharge - amount);
        return true;
    }
    public void AddUltimateCharge(float value)
    { UltimateCharge = Mathf.Clamp(UltimateCharge + value, 0f, 100f); }
    Vector3 FlatForward()
    {
        Vector3 v = combatCamera != null ? combatCamera.transform.forward : transform.forward;
        v.y = 0; return v.sqrMagnitude > .001f ? v.normalized : Vector3.forward;
    }
    bool Strike(Vector3 center, float radius, float damage, bool elemental, Vector3? shoveDirection = null, float staggerSeconds = -1f)
    {
        bool any = false;
        HashSet<EnemyLayers> seen = new HashSet<EnemyLayers>();
        foreach (Collider c in Physics.OverlapSphere(center, radius))
        {
            EnemyLayers enemy = c.GetComponentInParent<EnemyLayers>();
            if (enemy == null || !seen.Add(enemy) || !enemy.TakeHit(damage, elements != null ? elements.Current : null, elemental, staggerSeconds)) continue;
            any = true;
            UltimateCharge = Mathf.Min(100, UltimateCharge + (elemental ? 10 : 8));
            Rigidbody targetBody = enemy.GetComponent<Rigidbody>();
            if (targetBody != null && !targetBody.isKinematic)
            {
                Vector3 dir = shoveDirection ?? (enemy.transform.position - transform.position);
                dir.y = .2f; targetBody.AddForce(dir.normalized * hitKnockback, ForceMode.VelocityChange);
            }
        }
        return any;
    }
}