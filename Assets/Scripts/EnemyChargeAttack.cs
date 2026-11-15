
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChargeAttack : MonoBehaviour
{
    [Header("Attack")]
    public Transform player;
    public float attackRange = 3.3f;
    public float chargeSpeed = 8f;
    public float damage = 12f;
    public float knockback = 4f;
    public float cooldown = 2.5f;
    public float windup = 0.35f;
    public float chargeDuration = 0.45f;
    public float contactRadius = 1.35f;
    public float safeStopDistance = 1.3f;

    [Header("Disruption Reaction")]
    public Transform visualRoot;
    public float recoilDistance = 0.3f;
    public float recoilDuration = 0.4f;
    public float recoilSquash = 0.85f;
    public float recoveryAttackDelay = 0.7f;

    [Header("Handmade Particle Effects")]
    public GameObject disruptionBurstPrefab;
    public Transform effectsAnchor;
    public float effectLifetime = 3f;

    public bool IsCharging { get; private set; }

    private NavMeshAgent agent;
    private ElementalVoidSiphon siphon;

    private Coroutine chargeRoutine;
    private Coroutine recoilRoutine;
    private float nextAttack;

    private Vector3 originalVisualPosition;
    private Vector3 originalVisualScale;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        siphon = GetComponent<ElementalVoidSiphon>();

        if (visualRoot != null)
        {
            originalVisualPosition = visualRoot.localPosition;
            originalVisualScale = visualRoot.localScale;
        }
    }

    private void OnEnable()
    {
        if (siphon != null)
        {
            siphon.AbsorptionInterrupted +=
                OnAbsorptionInterrupted;
        }
    }

    private void OnDisable()
    {
        if (siphon != null)
        {
            siphon.AbsorptionInterrupted -=
                OnAbsorptionInterrupted;
        }

        if (chargeRoutine != null)
        {
            StopCoroutine(chargeRoutine);
            chargeRoutine = null;
        }

        if (recoilRoutine != null)
        {
            StopCoroutine(recoilRoutine);
            recoilRoutine = null;
        }

        ResetVisual();
        IsCharging = false;
    }

    private void Update()
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        // Do not move or attack during disruption.
        if (siphon != null && siphon.IsInterrupted)
        {
            agent.isStopped = true;
            return;
        }

        if (IsCharging || Time.time < nextAttack)
            return;

        if (player == null)
        {
            PlayerElements found =
                FindFirstObjectByType<PlayerElements>();

            if (found != null)
                player = found.transform;
        }

        if (player == null)
            return;

        Vector3 gap = player.position - transform.position;
        gap.y = 0f;

        if (gap.sqrMagnitude <= attackRange * attackRange)
        {
            nextAttack = Time.time + cooldown;
            chargeRoutine = StartCoroutine(Charge());
        }
    }

    private IEnumerator Charge()
    {
        IsCharging = true;

        agent.isStopped = true;
        agent.ResetPath();

        // Telegraph the attack before lunging.
        float elapsed = 0f;

        while (elapsed < windup)
        {
            if (IsInterrupted())
            {
                FinishCharge();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (player == null || IsInterrupted())
        {
            FinishCharge();
            yield break;
        }

        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        direction = direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : transform.forward;

        bool hit = false;
        elapsed = 0f;

        while (elapsed < chargeDuration)
        {
            if (player == null ||
                !agent.isOnNavMesh ||
                IsInterrupted())
            {
                break;
            }

            Vector3 gap =
                player.position - transform.position;

            gap.y = 0f;

            float distance = gap.magnitude;

            if (!hit && distance <= contactRadius)
            {
                PlayerDurability health =
                    player.GetComponentInParent<PlayerDurability>();

                if (health != null)
                {
                    health.TakeDamage(
                        damage,
                        direction * knockback
                    );
                }

                hit = true;
            }

            // Never move through the player.
            if (distance <= safeStopDistance)
                break;

            float step = Mathf.Min(
                chargeSpeed * Time.fixedDeltaTime,
                Mathf.Max(0f, distance - safeStopDistance)
            );

            agent.Move(direction * step);

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        FinishCharge();
    }

    private bool IsInterrupted()
    {
        return siphon != null && siphon.IsInterrupted;
    }

    private void OnAbsorptionInterrupted()
    {
        // Immediately cancel an active charge.
        if (chargeRoutine != null)
        {
            StopCoroutine(chargeRoutine);
            chargeRoutine = null;
        }

        IsCharging = false;

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        float interruptionEnd =
            siphon != null
            ? Time.time + siphon.interruptionSeconds
            : Time.time;

        nextAttack = Mathf.Max(
            nextAttack,
            interruptionEnd + recoveryAttackDelay
        );

        PlayDisruptionEffect();
        StartRecoil();
    }

    private void PlayDisruptionEffect()
    {
        if (disruptionBurstPrefab == null)
            return;

        Vector3 position = effectsAnchor != null
            ? effectsAnchor.position
            : transform.position + Vector3.up * 0.5f;

        GameObject effect = Instantiate(
            disruptionBurstPrefab,
            position,
            Quaternion.identity
        );

        Destroy(effect, effectLifetime);
    }

    private void StartRecoil()
    {
        if (visualRoot == null)
            return;

        if (recoilRoutine != null)
            StopCoroutine(recoilRoutine);

        ResetVisual();

        recoilRoutine = StartCoroutine(Recoil());
    }

    private IEnumerator Recoil()
    {
        Vector3 away = -transform.forward;

        if (player != null)
        {
            away = transform.position - player.position;
            away.y = 0f;

            if (away.sqrMagnitude > 0.001f)
                away.Normalize();
            else
                away = -transform.forward;
        }

        Vector3 localAway =
            transform.InverseTransformDirection(away);

        Vector3 recoilPosition =
            originalVisualPosition +
            localAway * recoilDistance;

        Vector3 squashedScale = new Vector3(
            originalVisualScale.x * 1.08f,
            originalVisualScale.y * recoilSquash,
            originalVisualScale.z * 1.08f
        );

        float half = Mathf.Max(0.05f, recoilDuration / 2f);
        float elapsed = 0f;

        // Move backward and squash.
        while (elapsed < half)
        {
            float t = Mathf.Clamp01(elapsed / half);
            t = t * t * (3f - 2f * t);

            visualRoot.localPosition = Vector3.Lerp(
                originalVisualPosition,
                recoilPosition,
                t
            );

            visualRoot.localScale = Vector3.Lerp(
                originalVisualScale,
                squashedScale,
                t
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;

        // Recover to the original appearance.
        while (elapsed < half)
        {
            float t = Mathf.Clamp01(elapsed / half);
            t = t * t * (3f - 2f * t);

            visualRoot.localPosition = Vector3.Lerp(
                recoilPosition,
                originalVisualPosition,
                t
            );

            visualRoot.localScale = Vector3.Lerp(
                squashedScale,
                originalVisualScale,
                t
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        ResetVisual();
        recoilRoutine = null;
    }

    private void ResetVisual()
    {
        if (visualRoot == null)
            return;

        visualRoot.localPosition = originalVisualPosition;
        visualRoot.localScale = originalVisualScale;
    }

    private void FinishCharge()
    {
        IsCharging = false;
        chargeRoutine = null;

        // Patrol controls movement after the charge.
        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh &&
            !IsInterrupted())
        {
            agent.isStopped = false;
        }
    }
}
