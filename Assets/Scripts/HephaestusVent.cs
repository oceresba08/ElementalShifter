using System.Collections;
using UnityEngine;

/// <summary>Warns, then produces one damaging volcanic eruption.</summary>
public class HephaestusVent : MonoBehaviour
{
    [Header("Timing")]
    [Min(0.1f)] public float warningSeconds = 1.5f;
    [Min(0.1f)] public float eruptionSeconds = 0.8f;
    [Header("Area / Damage")]
    [Min(0.1f)] public float radius = 2.6f;
    public float damage = 12f;
    public float push = 7f;
    [Header("VFX")]
    public GameObject warningPrefab;
    public GameObject eruptionPrefab;
    public float vfxLifetime = 4f;

    private IEnumerator Start()
    {
        SpawnEffect(warningPrefab);
        yield return new WaitForSeconds(warningSeconds);
        SpawnEffect(eruptionPrefab);
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Collide);
        foreach (Collider c in hits)
        {
            PlayerDurability health = c.GetComponentInParent<PlayerDurability>();
            if (health == null) continue;
            Vector3 away = health.transform.position - transform.position;
            away.y = 0.3f;
            health.TakeDamage(damage, away.normalized * push);
            break;
        }
        yield return new WaitForSeconds(eruptionSeconds);
        Destroy(gameObject);
    }

    private void SpawnEffect(GameObject prefab)
    {
        if (prefab != null)
            Destroy(Instantiate(prefab, transform.position, Quaternion.identity), vfxLifetime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
