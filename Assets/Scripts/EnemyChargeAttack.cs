using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChargeAttack : MonoBehaviour
{
    public Transform player;
    public float attackRange = 3.3f;
    public float chargeSpeed = 8f;
    public float damage = 12f;
    public float knockback = 4f;
    public float cooldown = 2.5f;
    public float windup = .35f;
    public float chargeDuration = .45f;
    public float contactRadius = 1.35f;
    public float safeStopDistance = 1.3f;
    public bool IsCharging { get; private set; }

    NavMeshAgent agent;
    float nextAttack;
    void Awake() => agent = GetComponent<NavMeshAgent>();
    void Update()
    {
        if (!agent.isOnNavMesh || IsCharging || Time.time < nextAttack) return;
        if (player == null)
        {
            PlayerElements p = FindFirstObjectByType<PlayerElements>();
            if (p != null) player = p.transform;
        }
        if (player == null) return;
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0;
        if (toPlayer.sqrMagnitude <= attackRange * attackRange)
        {
            nextAttack = Time.time + cooldown;
            StartCoroutine(Charge());
        }
    }

    IEnumerator Charge()
    {
        IsCharging = true;
        agent.isStopped = true;
        agent.ResetPath();
        yield return new WaitForSeconds(windup);
        if (player == null) { FinishCharge(); yield break; }
        Vector3 direction = player.position - transform.position;
        direction.y = 0;
        direction = direction.sqrMagnitude > .001f ? direction.normalized : transform.forward;
        bool hit = false;
        for (float t = 0; t < chargeDuration; t += Time.fixedDeltaTime)
        {
            if (player == null || !agent.isOnNavMesh) break;
            Vector3 gap = player.position - transform.position;
            gap.y = 0;
            float distance = gap.magnitude;
            if (!hit && distance <= contactRadius)
            {
                PlayerDurability health = player.GetComponentInParent<PlayerDurability>();
                if (health != null) health.TakeDamage(damage, direction * knockback);
                hit = true;
            }
            if (distance <= safeStopDistance) break; // Do not ram through the ball.
            float step = Mathf.Min(chargeSpeed * Time.fixedDeltaTime, distance - safeStopDistance);
            agent.Move(direction * step);
            yield return new WaitForFixedUpdate();
        }
        FinishCharge();
    }
    void FinishCharge()
    {
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        IsCharging = false;
    }
}
