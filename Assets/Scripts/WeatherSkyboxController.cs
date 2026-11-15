
using System.Collections;
using UnityEngine;

public class WeatherSkyboxController : MonoBehaviour
{
    [Header("Weather Skyboxes")]
    public Material clearSky;
    public Material rainSky;
    public Material snowSky;
    public Material stormSky;

    [Header("Optional Scene Lighting")]
    public Light directionalLight;

    public float clearLightIntensity = 1.2f;
    public float rainLightIntensity = 0.65f;
    public float snowLightIntensity = 0.85f;
    public float stormLightIntensity = 0.3f;

    [Header("Storm Sky Flashes")]
    public bool enableStormFlashes = true;
    public float minimumFlashInterval = 5f;
    public float maximumFlashInterval = 12f;
    public float flashExposureMultiplier = 2f;
    public float flashLightMultiplier = 2.5f;

    private Material runtimeStormSky;
    private Material originalSky;

    private Coroutine stormRoutine;
    private float normalStormExposure = 1f;
    private WeatherType currentWeather;
    private float currentLightIntensity;

    private void Awake()
    {
        originalSky = RenderSettings.skybox;

        // Duplicate the storm material so flashing never
        // permanently modifies the imported material asset.
        if (stormSky != null)
        {
            runtimeStormSky = new Material(stormSky);

            if (runtimeStormSky.HasProperty("_Exposure"))
            {
                normalStormExposure =
                    runtimeStormSky.GetFloat("_Exposure");
            }
        }
    }

    private void Start()
    {
        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.WeatherChanged +=
                OnWeatherChanged;

            OnWeatherChanged(
                WeatherManager.Instance.Current
            );
        }
        else
        {
            OnWeatherChanged(WeatherType.Clear);

            Debug.LogWarning(
                "WeatherSkyboxController: " +
                "No WeatherManager found.",
                this
            );
        }
    }

    private void OnWeatherChanged(WeatherType weather)
    {
        currentWeather = weather;

        if (stormRoutine != null)
        {
            StopCoroutine(stormRoutine);
            stormRoutine = null;
        }

        ResetStormFlash();

        Material selectedSky = clearSky;

        switch (weather)
        {
            case WeatherType.Clear:
                selectedSky = clearSky;
                currentLightIntensity =
                    clearLightIntensity;
                break;

            case WeatherType.Rain:
                selectedSky = rainSky;
                currentLightIntensity =
                    rainLightIntensity;
                break;

            case WeatherType.Snow:
                selectedSky = snowSky;
                currentLightIntensity =
                    snowLightIntensity;
                break;

            case WeatherType.Storm:
                selectedSky = runtimeStormSky;
                currentLightIntensity =
                    stormLightIntensity;
                break;
        }

        if (selectedSky != null)
        {
            RenderSettings.skybox = selectedSky;

            // Update ambient lighting after changing sky.
            DynamicGI.UpdateEnvironment();
        }

        if (directionalLight != null)
        {
            directionalLight.intensity =
                currentLightIntensity;
        }

        if (weather == WeatherType.Storm &&
            enableStormFlashes)
        {
            stormRoutine = StartCoroutine(
                StormFlashLoop()
            );
        }
    }

    private IEnumerator StormFlashLoop()
    {
        while (currentWeather == WeatherType.Storm)
        {
            float min = Mathf.Max(
                0.1f, minimumFlashInterval);

            float max = Mathf.Max(
                min, maximumFlashInterval);

            yield return new WaitForSeconds(
                Random.Range(min, max)
            );

            // First flash.
            SetFlash(1f);
            yield return new WaitForSeconds(0.1f);

            ResetStormFlash();
            yield return new WaitForSeconds(0.08f);

            // Weaker second flash.
            SetFlash(0.55f);
            yield return new WaitForSeconds(0.12f);

            ResetStormFlash();
        }
    }

    private void SetFlash(float strength)
    {
        if (runtimeStormSky != null &&
            runtimeStormSky.HasProperty("_Exposure"))
        {
            float multiplier = Mathf.Lerp(
                1f,
                flashExposureMultiplier,
                strength
            );

            runtimeStormSky.SetFloat(
                "_Exposure",
                normalStormExposure * multiplier
            );
        }

        if (directionalLight != null)
        {
            directionalLight.intensity =
                currentLightIntensity *
                Mathf.Lerp(
                    1f,
                    flashLightMultiplier,
                    strength
                );
        }
    }

    private void ResetStormFlash()
    {
        if (runtimeStormSky != null &&
            runtimeStormSky.HasProperty("_Exposure"))
        {
            runtimeStormSky.SetFloat(
                "_Exposure",
                normalStormExposure
            );
        }

        if (directionalLight != null)
        {
            directionalLight.intensity =
                currentLightIntensity;
        }
    }

    private void OnDestroy()
    {
        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.WeatherChanged -=
                OnWeatherChanged;
        }

        if (stormRoutine != null)
            StopCoroutine(stormRoutine);

        if (runtimeStormSky != null)
        {
            if (RenderSettings.skybox == runtimeStormSky)
                RenderSettings.skybox = originalSky;

            Destroy(runtimeStormSky);
        }
    }
}
