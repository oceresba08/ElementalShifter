using UnityEngine;

/// <summary>
/// Toggles PRE-MADE particle effect GameObjects based on existing WeatherManager.
/// Build all particle systems by hand in the editor and assign their roots below.
/// </summary>
public class WeatherVFXController : MonoBehaviour
{
    [Header("Weather effect roots - existing scene objects")]
    public GameObject rainEffects;
    public GameObject snowEffects;
    public GameObject stormEffects;
    public GameObject clearEffects;

    [Header("Optional follow player")]
    public Transform followTarget;
    public bool followPlayer = true;
    public Vector3 followOffset = new Vector3(0f, 9f, 0f);

    private WeatherManager subscribed;

    private void Start()
    {
        subscribed = WeatherManager.Instance;
        if (subscribed != null)
        {
            subscribed.WeatherChanged += Refresh;
            Refresh(subscribed.Current);
        }
        else
        {
            Debug.LogWarning("WeatherVFXController: WeatherManager not found.", this);
            Refresh(WeatherType.Clear);
        }
    }

    private void LateUpdate()
    {
        if (!followPlayer) return;
        if (followTarget == null)
        {
            PlayerElements player = FindFirstObjectByType<PlayerElements>();
            if (player != null) followTarget = player.transform;
        }
        if (followTarget != null)
            transform.position = followTarget.position + followOffset;
    }

    private void Refresh(WeatherType weather)
    {
        if (clearEffects != null) clearEffects.SetActive(weather == WeatherType.Clear);
        if (rainEffects != null) rainEffects.SetActive(weather == WeatherType.Rain);
        if (snowEffects != null) snowEffects.SetActive(weather == WeatherType.Snow);
        if (stormEffects != null) stormEffects.SetActive(weather == WeatherType.Storm);
    }

    private void OnDestroy()
    {
        if (subscribed != null)
            subscribed.WeatherChanged -= Refresh;
    }
}
