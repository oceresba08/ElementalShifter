using System.Collections;
using UnityEngine;

/// <summary>
/// Optional weather schedule using the existing WeatherManager API.
/// Do not add this if your scene already has another automatic weather controller.
/// </summary>
public class ForestWeatherCycle : MonoBehaviour
{
    [Header("Optional automatic forest weather")]
    public bool runCycle = true;
    public WeatherType[] sequence = { WeatherType.Clear, WeatherType.Rain, WeatherType.Clear, WeatherType.Storm };
    [Min(1f)] public float secondsPerWeather = 35f;
    [Min(0f)] public float initialDelay = 10f;

    private IEnumerator Start()
    {
        if (!runCycle) yield break;
        yield return new WaitForSeconds(initialDelay);
        int index = 0;
        while (enabled && runCycle)
        {
            WeatherManager manager = WeatherManager.Instance;
            if (manager != null && sequence != null && sequence.Length > 0)
            {
                manager.ChangeWeather(sequence[index % sequence.Length], 0f);
                index++;
            }
            yield return new WaitForSeconds(Mathf.Max(1f, secondsPerWeather));
        }
    }
}
