using System;
using System.Collections;
using UnityEngine;

public enum WeatherType { Clear, Rain, Snow, Storm }

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }
    public WeatherType startingWeather = WeatherType.Clear;
    public WeatherType Current { get; private set; }
    public event Action<WeatherType> WeatherChanged;

    private Coroutine resetRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Use one WeatherManager per scene.", this);
            enabled = false;
            return;
        }
        Instance = this;
        Current = startingWeather;
    }

    public void ChangeWeather(WeatherType weather, float durationSeconds)
    {
        if (resetRoutine != null) StopCoroutine(resetRoutine);
        Current = weather;
        WeatherChanged?.Invoke(Current);
        if (durationSeconds > 0f)
            resetRoutine = StartCoroutine(ReturnToClear(durationSeconds));
    }

    private IEnumerator ReturnToClear(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        resetRoutine = null;
        Current = WeatherType.Clear;
        WeatherChanged?.Invoke(Current);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
