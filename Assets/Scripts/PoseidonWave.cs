using System.Collections;
using UnityEngine;

/// <summary>Telegraphed one-way surge. Use a short wave-shaped VFX prefab for the visual.</summary>
public class PoseidonWave : MonoBehaviour
{
    [Header("Timing")]
    [Min(0.1f)] public float warningSeconds = 1.5f;
    [Min(0.1f)] public float travelSeconds = 2.3f;
    [Header("Wave")]
    public float travelDistance = 12f;
    public float halfWidth = 2.5f;
    public float hitDepth = 1.2f;
    public float damage = 10f;
    public float knockback = 9f;
    [Header("VFX")]
    public GameObject warningPrefab;
    public GameObject waveVisualPrefab;
    public float effectLifetime = 4f;

    private IEnumerator Start()
    {
        SpawnEffect(warningPrefab, transform.position, transform.rotation);
        yield return new WaitForSeconds(warningSeconds);
        Vector3 start = transform.position;
        Vector3 direction = transform.forward;
        direction.y = 0f;
        direction.Normalize();
        if (direction.sqrMagnitude < 0.001f) direction = Vector3.forward;
        GameObject visual = waveVisualPrefab != null
            ? Instantiate(waveVisualPrefab, start, transform.rotation)
            : null;
        bool struck = false;
        float elapsed = 0f;
        while (elapsed < travelSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelSeconds);
            Vector3 center = start + direction * (travelDistance * t);
            if (visual != null) visual.transform.position = center;
            if (!struck)
            {
                Collider[] hits = Physics.OverlapBox(center + Vector3.up * 0.6f,
                    new Vector3(halfWidth, 1.1f, hitDepth),
                    Quaternion.LookRotation(direction), ~0, QueryTriggerInteraction.Collide);
                foreach (Collider c in hits)
                {
                    PlayerDurability health = c.GetComponentInParent<PlayerDurability>();
                    if (health == null) continue;
                    health.TakeDamage(damage, (direction + Vector3.up * 0.15f).normalized * knockback);
                    struck = true;
                    break;
                }
            }
            yield return null;
        }
        if (visual != null) Destroy(visual);
        Destroy(gameObject);
    }

    private void SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab != null) Destroy(Instantiate(prefab, position, rotation), effectLifetime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * travelDistance);
    }
}
