using UnityEngine;

/// <summary>Attach to WindZone. Only emits when the existing timed wind zone is active.</summary>
[RequireComponent(typeof(WindCurrentZone))]
public class GustVFXDirector : MonoBehaviour
{
    public WindCurrentZone wind;
    public Vector3 effectCenter = Vector3.zero;
    public float gustEmission = 36f;
    ParticleSystem gust;
    void Awake() { if (wind == null) wind = GetComponent<WindCurrentZone>(); }
    void Start()
    {
        gust = ElementalVFX.Aura("WIND_STREAMS", transform,
            new Color(.4f, 1f, .88f), 2f, gustEmission, .09f);
        gust.transform.localPosition = effectCenter;
        var velocity = gust.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(wind.windDirection.normalized.x * 5f);
        velocity.z = new ParticleSystem.MinMaxCurve(wind.windDirection.normalized.z * 5f);
    }
    void Update()
    {
        if (gust == null || wind == null) return;
        var emission = gust.emission;
        emission.rateOverTime = wind.IsGusting ? gustEmission : 0;
    }
    void OnDestroy() { if (gust != null) Destroy(gust.gameObject); }
}
