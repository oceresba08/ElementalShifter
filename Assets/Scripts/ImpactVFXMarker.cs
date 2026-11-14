using System.Collections;
using UnityEngine;

/// <summary>
/// Add to Demeter's warning flower. Shows telltale particles and a timed blast
/// synced to the existing DemeterFlower.armSeconds value.
/// </summary>
public class ImpactVFXMarker : MonoBehaviour
{
    public Color color = new Color(1f, .3f, .6f);
    public float emissionRate = 9f;
    public float auraRadius = .65f;
    ParticleSystem aura;
    void Start()
    {
        aura = ElementalVFX.Aura("HAZARD_WARNING", transform, color, auraRadius, emissionRate, .17f);
        DemeterFlower flower = GetComponent<DemeterFlower>();
        if (flower != null) StartCoroutine(BurstWhenArmed(flower.armSeconds));
    }
    IEnumerator BurstWhenArmed(float delay)
    {
        yield return new WaitForSeconds(delay);
        ElementalVFX.Burst("DEMETER_FLOWER_BLAST", transform.position, color, 90, 1.2f, 8f, .25f);
        if (aura != null) { Destroy(aura.gameObject); aura = null; }
    }
    void OnDestroy() { if (aura != null) Destroy(aura.gameObject); }
}
