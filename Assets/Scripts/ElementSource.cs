using System;
using UnityEngine;
[RequireComponent(typeof(Collider))]
public class ElementSource : MonoBehaviour
{
    public ElementData grantsElement;
    public bool requiresWeather;
    public WeatherType onlyDuring = WeatherType.Storm;
    public bool extinguishedByRainOrSnow;
    public Renderer[] renderersToHide;
    [Header("Resource depletion")]
    [Min(1)] public int startingCharges = 3;
    public float absorbCooldown = .8f;
    public bool countsForWrath = true;
    public event Action<ElementSource> Depleted;
    public int RemainingCharges { get; private set; }
    public bool IsDepleted => RemainingCharges <= 0;
    Collider trigger;
    Renderer[] visuals;
    bool weatherAvailable;
    float nextAbsorb;
    void Awake()
    {
        trigger = GetComponent<Collider>(); trigger.isTrigger = true;
        visuals = renderersToHide != null && renderersToHide.Length > 0 ? renderersToHide : GetComponentsInChildren<Renderer>();
        RemainingCharges = startingCharges;
    }
    void Start()
    {
        if (WeatherManager.Instance != null) WeatherManager.Instance.WeatherChanged += Refresh;
        Refresh(WeatherManager.Instance != null ? WeatherManager.Instance.Current : WeatherType.Clear);
    }
    void OnDestroy()
    { if (WeatherManager.Instance != null) WeatherManager.Instance.WeatherChanged -= Refresh; }
    void Refresh(WeatherType weather)
    {
        weatherAvailable = (!requiresWeather || weather == onlyDuring) &&
          (!extinguishedByRainOrSnow || (weather != WeatherType.Rain && weather != WeatherType.Snow && weather != WeatherType.Storm));
        bool shown = weatherAvailable && !IsDepleted;
        if (trigger != null) trigger.enabled = shown;
        if (visuals != null) foreach (Renderer r in visuals) if (r != null) r.enabled = shown;
    }
    void OnTriggerEnter(Collider other)
    {
        if (!weatherAvailable || IsDepleted || Time.time < nextAbsorb) return;
        PlayerElements player = other.GetComponentInParent<PlayerElements>();
        if (player == null || !player.Absorb(grantsElement)) return;
        RemainingCharges--;
        nextAbsorb = Time.time + absorbCooldown;
        if (IsDepleted)
        {
            Depleted?.Invoke(this);
            Refresh(WeatherManager.Instance != null ? WeatherManager.Instance.Current : WeatherType.Clear);
        }
    }
}
