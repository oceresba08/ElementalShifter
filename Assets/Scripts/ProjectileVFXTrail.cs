using UnityEngine;

/// <summary>Optional visual companion for the Wind projectile prefab.</summary>
public class ProjectileVFXTrail : MonoBehaviour
{
    public Color trailColor = new Color(.35f, 1f, .86f);
    public float trailRadius = .18f;
    public float rate = 35f;
    ParticleSystem trail;
    void Start()
    {
        trail = ElementalVFX.Aura("PROJECTILE_TRAIL", transform, trailColor, trailRadius, rate, .10f);
    }
    void OnDisable()
    {
        if (trail != null) Destroy(trail.gameObject);
        // A projectile can disappear after impact or after lifetime; both use a short puff.
        if (Application.isPlaying && gameObject.scene.IsValid())
            ElementalVFX.Burst("PROJECTILE_END", transform.position, trailColor, 22, .18f, 3.3f, .11f);
    }
}
