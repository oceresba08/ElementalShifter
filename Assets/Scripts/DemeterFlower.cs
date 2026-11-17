using System.Collections;
using UnityEngine;

/// <summary>Telegraphed single-hit flower hazard for Demeter's Wrath.</summary>
[RequireComponent(typeof(SphereCollider))]
public class DemeterFlower : MonoBehaviour
{
    [Header("Timing")]
    [Min(0.1f)] public float armSeconds = 1.2f;
    [Min(0.1f)] public float lifetime = 3f;

    [Header("Explosion")]
    [Min(0.1f)] public float blastRadius = 3f;
    public float push = 10f;
    public float damage = 8f;
    public LayerMask affectedLayers = ~0;

    [Header("VFX")]
    public GameObject warningVFX;
    public GameObject burstVFX;
    [Min(0.1f)] public float effectsLifetime = 4f;

    private void Awake()
    {
        GetComponent<SphereCollider>().isTrigger = true;
    }

    private void Start()
    {
        if (warningVFX != null)
            Destroy(Instantiate(warningVFX, transform.position, Quaternion.identity), effectsLifetime);
        StartCoroutine(Burst());
        Destroy(gameObject, Mathf.Max(lifetime, armSeconds + 0.1f));
    }

    private IEnumerator Burst()
    {
        yield return new WaitForSeconds(armSeconds);
        foreach (Collider collider in Physics.OverlapSphere(
                     transform.position, blastRadius, affectedLayers,
                     QueryTriggerInteraction.Collide))
        {
            PlayerDurability player = collider.GetComponentInParent<PlayerDurability>();
            if (player == null) continue;
            Vector3 away = player.transform.position - transform.position;
            away.y = 0.35f;
            player.TakeDamage(damage, away.normalized * push);
            break; // Only one player can be hit by one flower.
        }

        if (burstVFX != null)
            Destroy(Instantiate(burstVFX, transform.position, Quaternion.identity), effectsLifetime);

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.6f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, blastRadius);
    }
}
