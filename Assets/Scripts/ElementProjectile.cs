using UnityEngine;

// Visible projectile for the Wind Ranger. Works with a prefab OR the fallback sphere.
public class ElementProjectile : MonoBehaviour
{
    public float collisionRadius = 0.30f;
    public float maxLifetime = 2.4f;
    PlayerCombat owner;
    ElementData element;
    Vector3 direction;
    float speed;
    float damage;
    float remaining;
    bool initialized;

    public void Initialize(PlayerCombat source, ElementData sourceElement, Vector3 heading,
        float travelSpeed, float attackDamage, float lifetime)
    {
        owner = source;
        element = sourceElement;
        direction = heading.sqrMagnitude > .001f ? heading.normalized : Vector3.forward;
        speed = Mathf.Max(.1f, travelSpeed);
        damage = attackDamage;
        remaining = lifetime;
        initialized = true;
        // Detection is handled by SphereCastAll to avoid tunneling at high speeds.
        foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
    }
    void Update()
    {
        if (!initialized) return;
        float step = speed * Time.deltaTime;
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, collisionRadius, direction,
            step, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (owner != null && hit.collider.transform.IsChildOf(owner.transform)) continue;
            EnemyLayers enemy = hit.collider.GetComponentInParent<EnemyLayers>();
            if (enemy != null)
            {
                if (enemy.TakeHit(damage, element, true)) owner?.AddUltimateCharge(10f);
                Destroy(gameObject);
                return;
            }
            // Walls and ground stop the projectile too.
            Destroy(gameObject);
            return;
        }
        transform.position += direction * step;
        remaining -= Time.deltaTime;
        if (remaining <= 0) Destroy(gameObject);
    }
}
