using System.Collections;
using UnityEngine;
[RequireComponent(typeof(SphereCollider))]
public class DemeterFlower : MonoBehaviour
{
    public float armSeconds = 1.2f;
    public float blastRadius = 3f;
    public float push = 10f;
    public float damage = 8f;
    public float lifetime = 3f;
    public GameObject burstVFX;
    void Start() { GetComponent<SphereCollider>().isTrigger = true; StartCoroutine(Burst()); }
    IEnumerator Burst()
    {
        yield return new WaitForSeconds(armSeconds);
        foreach (Collider c in Physics.OverlapSphere(transform.position, blastRadius))
        {
            PlayerDurability p = c.GetComponentInParent<PlayerDurability>();
            if (p == null) continue;
            Vector3 direction = p.transform.position - transform.position;
            direction.y = .35f;
            p.TakeDamage(damage, direction.normalized * push);
            break;
        }
        if (burstVFX != null) Instantiate(burstVFX, transform.position, Quaternion.identity);
        Destroy(gameObject, Mathf.Max(.1f, lifetime - armSeconds));
    }
}
