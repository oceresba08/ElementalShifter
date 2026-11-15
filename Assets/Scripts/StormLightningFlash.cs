
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class StormLightningFlash : MonoBehaviour
{
    [Header("Lightning Timing")]
    public float minimumInterval = 5f;
    public float maximumInterval = 12f;

    [Header("Sky Lighting")]
    public float flashIntensity = 12f;
    public float firstFlashSeconds = 0.10f;
    public float secondFlashSeconds = 0.07f;
    public float gapBetweenFlashes = 0.08f;

    [Header("Visible Lightning Bolt")]
    public LineRenderer lightningBolt;

    [Header("Optional Bolt Variation")]
    public bool randomizeBoltPosition = true;
    public float horizontalOffset = 3f;

    private Light flashLight;
    private Coroutine flashRoutine;
    private Vector3 originalBoltPosition;

    private void Awake()
    {
        flashLight = GetComponent<Light>();

        if (lightningBolt != null)
        {
            originalBoltPosition =
                lightningBolt.transform.localPosition;
        }

        HideLightning();
    }

    private void OnEnable()
    {
        if (flashLight == null)
            flashLight = GetComponent<Light>();

        HideLightning();
        flashRoutine = StartCoroutine(LightningLoop());
    }

    private IEnumerator LightningLoop()
    {
        while (true)
        {
            float delay = Random.Range(
                Mathf.Max(0.1f, minimumInterval),
                Mathf.Max(minimumInterval, maximumInterval)
            );

            yield return new WaitForSeconds(delay);

            if (lightningBolt != null &&
                randomizeBoltPosition)
            {
                Vector3 offset = new Vector3(
                    Random.Range(
                        -horizontalOffset,
                        horizontalOffset
                    ),
                    0f,
                    Random.Range(
                        -horizontalOffset,
                        horizontalOffset
                    )
                );

                lightningBolt.transform.localPosition =
                    originalBoltPosition + offset;
            }

            // First bright strike.
            ShowLightning(flashIntensity);
            yield return new WaitForSeconds(
                firstFlashSeconds
            );

            HideLightning();

            // Brief darkness.
            yield return new WaitForSeconds(
                gapBetweenFlashes
            );

            // Smaller second flash.
            ShowLightning(flashIntensity * 0.65f);
            yield return new WaitForSeconds(
                secondFlashSeconds
            );

            HideLightning();
        }
    }

    private void ShowLightning(float intensity)
    {
        if (flashLight != null)
            flashLight.intensity = intensity;

        if (lightningBolt != null)
            lightningBolt.enabled = true;
    }

    private void HideLightning()
    {
        if (flashLight != null)
            flashLight.intensity = 0f;

        if (lightningBolt != null)
            lightningBolt.enabled = false;
    }

    private void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        HideLightning();
    }
}
