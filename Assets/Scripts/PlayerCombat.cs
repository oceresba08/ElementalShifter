using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody))]
public class PlayerCombat : MonoBehaviour
{
    public PlayerElements elements;
    public OrbitCombatCamera combatCamera;
    [Header("Physical attacks")]
    public float hitDamage = 17f, spinDamage = 12f, slamDamage = 26f;
    [Header("Elemental attacks")]
    public float skillDamage = 25f, ultimateDamage = 58f;
    public float skillCooldown = 5f, hitCooldown = .65f, spinCooldown = 1.3f, slamCooldown = 2.5f;
    [Header("Lunge")]
    public float lockOnRange = 9f;
    public float lungeSpeed = 17f;
    public float lungeDuration = .23f;
    public float collisionDamageRadius = 1.7f;
    public float hitKnockback = 5f;
    public float UltimateCharge { get; private set; }
    public event Action<string> AttackFeedback;
    Rigidbody rb;
    float nextHit, nextSpin, nextSlam, nextSkill;
    bool busy;
    void Awake()
    { rb = GetComponent<Rigidbody>(); if (elements == null) elements = GetComponent<PlayerElements>(); }
    void Update()
    {
        if (Keyboard.current == null || Time.timeScale == 0) return;
        if (Keyboard.current.jKey.wasPressedThisFrame && Time.time >= nextHit && !busy)
        { nextHit = Time.time + hitCooldown; StartCoroutine(LungeHit()); }
        if (Keyboard.current.kKey.wasPressedThisFrame && Time.time >= nextSpin && !busy)
        { nextSpin = Time.time + spinCooldown; Strike(transform.position, 2.4f, spinDamage, false); combatCamera?.AttackBeat(.5f); AttackFeedback?.Invoke("SPIN!"); }
        if (Keyboard.current.lKey.wasPressedThisFrame && Time.time >= nextSlam && !busy)
        { nextSlam = Time.time + slamCooldown; StartCoroutine(Slam()); }
        if (Keyboard.current.qKey.wasPressedThisFrame && Time.time >= nextSkill && !busy && elements != null && elements.Current != null)
        { nextSkill = Time.time + skillCooldown; UseSkill(); }
        if (Keyboard.current.rKey.wasPressedThisFrame && UltimateCharge >= 100f && !busy && elements != null && elements.Current != null)
        {
            UltimateCharge = 0;
            Strike(transform.position, 5f, ultimateDamage, true);
            combatCamera?.AttackBeat(1f);
            AttackFeedback?.Invoke(elements.Current.ultimateName.ToUpperInvariant());
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
                connected = Strike(center, collisionDamageRadius, hitDamage, false, direction);
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
        combatCamera?.PlaySlamBeat();
        rb.AddForce(Vector3.up * 6.5f, ForceMode.VelocityChange);
        yield return new WaitForSeconds(.28f);
        rb.AddForce(Vector3.down * 13.5f, ForceMode.VelocityChange);
        yield return new WaitForSeconds(.27f);
        Strike(transform.position, 2.7f, slamDamage, false);
        busy = false;
    }
    void UseSkill()
    {
        ElementData e = elements.Current;
        // Class archetypes, not yet distinct per-element particle/animation prefabs.
        switch (e.combatClass)
        {
            case ElementClass.Ranger:
                StartCoroutine(RangedSkill(e)); break;
            case ElementClass.Healer:
            case ElementClass.Support:
                PlayerDurability h = GetComponent<PlayerDurability>();
                if (h != null) h.Heal(20f);
                Strike(transform.position, 2.5f, skillDamage * .35f, true);
                break;
            case ElementClass.Heavy:
            case ElementClass.Tank:
                Strike(transform.position, 3.5f, skillDamage * 1.2f, true); break;
            case ElementClass.Assassin:
            case ElementClass.Trickster:
                StartCoroutine(LungeHit()); break;
            default:
                Strike(transform.position + FlatForward() * 1.8f, 3f, skillDamage, true); break;
        }
        combatCamera?.AttackBeat(.85f);
        AttackFeedback?.Invoke(e.skillName.ToUpperInvariant());
    }
    IEnumerator RangedSkill(ElementData e)
    {
        Vector3 origin = transform.position + Vector3.up * .5f;
        Vector3 direction = FlatForward();
        EnemyLayers target = NearestEnemy(14f);
        if (target != null) { direction = target.transform.position - origin; direction.Normalize(); }
        // A real visible projectile can be added later. For now, delayed ranged damage.
        yield return new WaitForSeconds(.17f);
        RaycastHit[] hits = Physics.SphereCastAll(origin, .65f, direction, 14f, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            EnemyLayers enemy = hit.collider.GetComponentInParent<EnemyLayers>();
            if (enemy != null) { if (enemy.TakeHit(skillDamage, e, true)) UltimateCharge = Mathf.Min(100, UltimateCharge + 10); break; }
        }
    }
    Vector3 FlatForward()
    {
        Vector3 v = combatCamera != null ? combatCamera.transform.forward : transform.forward;
        v.y = 0; return v.sqrMagnitude > .001f ? v.normalized : Vector3.forward;
    }
    bool Strike(Vector3 center, float radius, float damage, bool elemental, Vector3? shoveDirection = null)
    {
        bool any = false;
        HashSet<EnemyLayers> seen = new HashSet<EnemyLayers>();
        foreach (Collider c in Physics.OverlapSphere(center, radius))
        {
            EnemyLayers enemy = c.GetComponentInParent<EnemyLayers>();
            if (enemy == null || !seen.Add(enemy) || !enemy.TakeHit(damage, elements != null ? elements.Current : null, elemental)) continue;
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
