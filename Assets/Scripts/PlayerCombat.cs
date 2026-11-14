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
    public float hitDamage = 17f;
    public float spinDamage = 12f;
    public float slamDamage = 26f;
    [Header("Special attacks")]
    public float skillDamage = 25f;
    public float ultimateDamage = 58f;
    public float skillCooldown = 5f;
    public float hitCooldown = 0.35f;
    public float spinCooldown = 1.3f;
    public float slamCooldown = 2.5f;

    public float UltimateCharge { get; private set; }
    public event Action<string> AttackFeedback;

    private Rigidbody rb;
    private float nextHit;
    private float nextSpin;
    private float nextSlam;
    private float nextSkill;
    private bool midSlam;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (elements == null) elements = GetComponent<PlayerElements>();
    }

    private void Update()
    {
        if (Keyboard.current == null || Time.timeScale == 0f) return;
        if (Keyboard.current.jKey.wasPressedThisFrame && Time.time >= nextHit)
        {
            nextHit = Time.time + hitCooldown;
            Strike(transform.position + FlatForward() * 1.3f, 1.4f, hitDamage, false);
            AttackFeedback?.Invoke("HIT!");
        }
        if (Keyboard.current.kKey.wasPressedThisFrame && Time.time >= nextSpin)
        {
            nextSpin = Time.time + spinCooldown;
            Strike(transform.position, 2.2f, spinDamage, false);
            AttackFeedback?.Invoke("SPIN!");
        }
        if (Keyboard.current.lKey.wasPressedThisFrame && Time.time >= nextSlam && !midSlam)
        {
            nextSlam = Time.time + slamCooldown;
            StartCoroutine(Slam());
        }
        if (Keyboard.current.qKey.wasPressedThisFrame && Time.time >= nextSkill &&
            elements != null && elements.Current != null)
        {
            nextSkill = Time.time + skillCooldown;
            Strike(transform.position + FlatForward() * 1.2f, 3.1f, skillDamage, true);
            AttackFeedback?.Invoke(elements.Current.skillName.ToUpperInvariant());
        }
        if (Keyboard.current.rKey.wasPressedThisFrame && UltimateCharge >= 100f &&
            elements != null && elements.Current != null)
        {
            UltimateCharge = 0f;
            Strike(transform.position, 5f, ultimateDamage, true);
            AttackFeedback?.Invoke(elements.Current.ultimateName.ToUpperInvariant());
        }
    }

    private IEnumerator Slam()
    {
        midSlam = true;
        AttackFeedback?.Invoke("SLAM!");
        if (combatCamera != null) combatCamera.PlaySlamBeat();
        rb.AddForce(Vector3.up * 5.5f, ForceMode.VelocityChange);
        yield return new WaitForSeconds(0.23f);
        rb.AddForce(Vector3.down * 11f, ForceMode.VelocityChange);
        yield return new WaitForSeconds(0.24f);
        Strike(transform.position, 2.4f, slamDamage, false);
        midSlam = false;
    }

    private Vector3 FlatForward()
    {
        Vector3 direction = combatCamera != null ? combatCamera.transform.forward : transform.forward;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
    }

    private void Strike(Vector3 center, float radius, float damage, bool elemental)
    {
        HashSet<EnemyLayers> hit = new HashSet<EnemyLayers>();
        foreach (Collider collider in Physics.OverlapSphere(center, radius))
        {
            EnemyLayers enemy = collider.GetComponentInParent<EnemyLayers>();
            if (enemy != null && hit.Add(enemy) &&
                enemy.TakeHit(damage, elements != null ? elements.Current : null, elemental))
                UltimateCharge = Mathf.Min(100f, UltimateCharge + (elemental ? 10f : 8f));
        }
    }
}
