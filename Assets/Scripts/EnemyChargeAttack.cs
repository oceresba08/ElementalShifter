using System.Collections;
using UnityEngine;
using UnityEngine.AI;
public class EnemyChargeAttack : MonoBehaviour
{
    public Transform player;
    public float attackRange = 3.3f;
    public float chargeSpeed = 10f;
    public float damage = 12f;
    public float knockback = 6f;
    public float cooldown = 2.5f;
    public float windup = .35f;
    public float chargeDuration = .5f;
    public float contactRadius = 1.4f;
    NavMeshAgent agent;
    float nextAttack;
    bool charging;
    void Awake() { agent = GetComponent<NavMeshAgent>(); }
    void Update()
    {
        if (charging || player == null || Time.time < nextAttack) return;
        if ((player.position - transform.position).sqrMagnitude <= attackRange * attackRange)
        { nextAttack = Time.time + cooldown; StartCoroutine(Charge()); }
    }
    IEnumerator Charge()
    {
        charging = true;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        yield return new WaitForSeconds(windup);
        Vector3 direction = player.position - transform.position;
        direction.y = 0; direction = direction.sqrMagnitude > .001f ? direction.normalized : transform.forward;
        bool hit = false;
        for (float t = 0; t < chargeDuration; t += Time.fixedDeltaTime)
        {
            if (agent != null && agent.isOnNavMesh)
                agent.Move(direction * chargeSpeed * Time.fixedDeltaTime);
            else transform.position += direction * chargeSpeed * Time.fixedDeltaTime;
            if (!hit && Vector3.Distance(transform.position, player.position) <= contactRadius)
            {
                PlayerDurability health = player.GetComponentInParent<PlayerDurability>();
                if (health != null) health.TakeDamage(damage, direction * knockback + Vector3.up * 1.2f);
                hit = true;
            }
            yield return new WaitForFixedUpdate();
        }
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        charging = false;
    }
}
