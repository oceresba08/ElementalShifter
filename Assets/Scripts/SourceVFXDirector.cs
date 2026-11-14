using UnityEngine;

/// <summary>Attach to every elemental pickup. Reflects source charge depletion visibly.</summary>
[RequireComponent(typeof(ElementSource))]
public class SourceVFXDirector : MonoBehaviour
{
    public ElementSource source;
    public Transform vfxAnchor;
    public float auraEmissionRate = 10f;
    ParticleSystem aura;
    int previousCharges;
    void Awake() { if (source == null) source = GetComponent<ElementSource>(); }
    void OnEnable() { if (source != null) source.Depleted += OnDepleted; }
    void Start()
    {
        previousCharges = source != null ? source.RemainingCharges : 0;
        if (source != null && source.grantsElement != null)
            aura = ElementalVFX.Aura("SOURCE_ENERGY", vfxAnchor != null ? vfxAnchor : transform,
                ElementalVFX.ColorFor(source.grantsElement), .4f, auraEmissionRate, .13f);
    }
    void Update()
    {
        if (source == null) return;
        if (source.RemainingCharges < previousCharges)
        {
            ElementalVFX.Burst("SOURCE_ABSORB", transform.position,
                ElementalVFX.ColorFor(source.grantsElement), 25, .3f, 4f, .13f);
            previousCharges = source.RemainingCharges;
        }
        if (aura != null)
        {
            bool available = !source.IsDepleted && source.GetComponent<Collider>().enabled;
            var emission = aura.emission;
            emission.rateOverTime = available ? auraEmissionRate * source.RemainingCharges /
                Mathf.Max(1f, source.startingCharges) : 0f;
        }
    }
    void OnDepleted(ElementSource s)
    {
        ElementalVFX.Burst("SOURCE_DEPLETED", transform.position,
            ElementalVFX.ColorFor(s.grantsElement), 70, .65f, 5.5f, .17f);
    }
    void OnDisable()
    {
        if (source != null) source.Depleted -= OnDepleted;
        if (aura != null) Destroy(aura.gameObject);
    }
}
