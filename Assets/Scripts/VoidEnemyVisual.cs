using UnityEngine;

/// <summary>
/// Visual identity for Void enemies. Add with EnemyLayers and optional
/// EnemyPatrol / EnemyChargeAttack. Does not yet consume ElementSource charges.
/// </summary>
[RequireComponent(typeof(EnemyLayers))]
public class VoidEnemyVisual : MonoBehaviour
{
    public Transform voidCore;
    public float pulseAmplitude = .07f;
    public float pulseSpeed = 3f;
    public float voidAuraRadius = .9f;
    public float voidAuraRate = 25f;
    ParticleSystem voidAura;
    Vector3 originalCoreScale;
    void Start()
    {
        if (voidCore != null) originalCoreScale = voidCore.localScale;
        voidAura = ElementalVFX.Aura("VOID_HAZE", transform, new Color(.57f, .20f, .91f),
            voidAuraRadius, voidAuraRate, .22f);
    }
    void Update()
    {
        if (voidCore != null)
            voidCore.localScale = originalCoreScale * (1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude);
    }
    void OnDestroy() { if (voidAura != null) Destroy(voidAura.gameObject); }
}
