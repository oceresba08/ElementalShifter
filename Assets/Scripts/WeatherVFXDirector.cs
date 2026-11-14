using UnityEngine;

/// <summary>
/// Add to WeatherSystem. Creates prototype precipitation around the player.
/// Particle simulations are world-space; emission box follows target.
/// </summary>
public class WeatherVFXDirector : MonoBehaviour
{
    public Transform followTarget;
    public WeatherManager weatherManager;
    public float coverage = 12f;
    public float particlesPerSecond = 120f;
    ParticleSystem precipitation;
    void Start()
    {
        if (weatherManager == null) weatherManager = WeatherManager.Instance;
        if (weatherManager != null)
        {
            weatherManager.WeatherChanged += OnWeatherChanged;
            OnWeatherChanged(weatherManager.Current);
        }
    }
    void LateUpdate()
    {
        if (precipitation != null && followTarget != null)
            precipitation.transform.position = followTarget.position + Vector3.up * 7f;
    }
    void OnWeatherChanged(WeatherType weather)
    {
        if (precipitation != null) Destroy(precipitation.gameObject);
        precipitation = null;
        if (weather == WeatherType.Clear) return;
        bool snow = weather == WeatherType.Snow;
        Color color = snow ? new Color(.8f, .95f, 1f, .9f) : new Color(.35f, .7f, 1f, .75f);
        precipitation = ElementalVFX.Make(snow ? "SNOWFALL" : "RAINFALL", null,
            followTarget != null ? followTarget.position + Vector3.up * 7f : transform.position,
            color, true, 150, 1f, snow ? 3.2f : 1.6f, 0f, snow ? .13f : .075f,
            snow ? particlesPerSecond * .5f : particlesPerSecond);
        var shape = precipitation.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(coverage, .2f, coverage);
        var velocity = precipitation.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(snow ? -2.4f : -10f);
        if (weather == WeatherType.Storm)
            ElementalVFX.Burst("STORM_WARNING", followTarget != null ? followTarget.position : transform.position,
                new Color(1f, .88f, .24f), 55, 2f, 6f, .2f);
    }
    void OnDestroy()
    {
        if (weatherManager != null) weatherManager.WeatherChanged -= OnWeatherChanged;
        if (precipitation != null) Destroy(precipitation.gameObject);
    }
}
