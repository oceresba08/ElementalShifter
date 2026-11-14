using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeatherChanger : MonoBehaviour
{
    public WeatherType newWeather = WeatherType.Storm;
    [Min(0f)] public float lastsSeconds = 45f;
    [Min(0f)] public float pickupCooldown = 2f;
    private float nextAllowedTime;

    private void Awake() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerElements>() == null ||
            Time.time < nextAllowedTime || WeatherManager.Instance == null) return;

        nextAllowedTime = Time.time + pickupCooldown;
        WeatherManager.Instance.ChangeWeather(newWeather, lastsSeconds);
    }
}
