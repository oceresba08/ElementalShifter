using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ElementSource : MonoBehaviour
{
    public ElementData grantsElement;
    public bool requiresWeather;
    public WeatherType onlyDuring = WeatherType.Storm;
    public bool extinguishedByRainOrSnow;
    public Renderer[] renderersToHide;

    private Collider trigger;
    private bool available;

    private void Awake()
    {
        trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
        if (renderersToHide == null || renderersToHide.Length == 0)
            renderersToHide = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        if (WeatherManager.Instance != null)
            WeatherManager.Instance.WeatherChanged += Refresh;
        Refresh(WeatherManager.Instance != null ? WeatherManager.Instance.Current : WeatherType.Clear);
    }

    private void OnDestroy()
    {
        if (WeatherManager.Instance != null)
            WeatherManager.Instance.WeatherChanged -= Refresh;
    }

    private void Refresh(WeatherType weather)
    {
        available = (!requiresWeather || weather == onlyDuring) &&
                    (!extinguishedByRainOrSnow ||
                     (weather != WeatherType.Rain && weather != WeatherType.Snow));
        if (trigger != null) trigger.enabled = available;
        if (renderersToHide != null)
            foreach (Renderer r in renderersToHide)
                if (r != null) r.enabled = available;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!available) return;
        PlayerElements player = other.GetComponentInParent<PlayerElements>();
        if (player != null) player.Absorb(grantsElement);
    }
}
