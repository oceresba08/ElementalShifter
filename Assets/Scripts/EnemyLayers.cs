using System;
using System.Collections;
using UnityEngine;
public class EnemyLayers : MonoBehaviour
{
    [Header("Outermost layer first")]
    public ElementData[] layers;
    public bool randomizeOnStart = true;
    public ElementData[] allowedRandomElements;
    [Range(1, 5)] public int randomMinLayers = 1;
    [Range(1, 5)] public int randomMaxLayers = 3;
    [Header("Stats")]
    public float defaultHealthPerLayer = 35f;
    public Renderer bodyRenderer;
    public float extraSizePerLayer = .2f;
    public float wetFireLayerSeconds = 3f;
    public event Action<EnemyLayers> LayerChanged;
    public event Action<EnemyLayers> Defeated;
    public int RemainingLayers => layers == null ? 0 : Mathf.Max(0, layers.Length - currentIndex);
    public ElementData CurrentLayer => RemainingLayers > 0 ? layers[currentIndex] : null;
    public float HealthFraction => Mathf.Clamp01(health / Mathf.Max(1f, layerMaxHealth));
    public float CurrentHealth => health;
    public float CurrentMaxHealth => layerMaxHealth;
    int currentIndex;
    float health, layerMaxHealth;
    Vector3 initialScale;
    Coroutine rainRoutine;
    bool defeated;
    void Start()
    {
        initialScale = transform.localScale;
        if (randomizeOnStart && allowedRandomElements != null && allowedRandomElements.Length > 0)
        {
            int min = Mathf.Clamp(randomMinLayers, 1, 5);
            int max = Mathf.Clamp(randomMaxLayers, min, 5);
            layers = new ElementData[UnityEngine.Random.Range(min, max + 1)];
            for (int i = 0; i < layers.Length; i++)
                layers[i] = allowedRandomElements[UnityEngine.Random.Range(0, allowedRandomElements.Length)];
        }
        if (layers == null || layers.Length == 0 || layers[0] == null)
        { Debug.LogWarning("Configure layers or allowedRandomElements", this); enabled = false; return; }
        transform.localScale = initialScale * (1f + extraSizePerLayer * (layers.Length - 1));
        RefreshLayer();
        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.WeatherChanged += OnWeatherChanged;
            OnWeatherChanged(WeatherManager.Instance.Current);
        }
    }
    public bool TakeHit(float damage, ElementData attacker, bool isElemental)
    {
        if (defeated || CurrentLayer == null) return false;
        float matchup = ElementMatchups.Advantage(attacker, CurrentLayer);
        float weight = isElemental ? .85f : .35f;
        float power = attacker != null ? attacker.attackMultiplier : 1f;
        health -= damage * Mathf.Lerp(1f, matchup, weight) * power;
        if (health <= 0) AdvanceLayer(); else LayerChanged?.Invoke(this);
        return true;
    }
    void AdvanceLayer()
    {
        if (defeated) return;
        currentIndex++;
        if (RemainingLayers <= 0)
        {
            defeated = true;
            Defeated?.Invoke(this);
            Destroy(gameObject);
            return;
        }
        RefreshLayer();
    }
    void RefreshLayer()
    {
        layerMaxHealth = CurrentLayer != null ? CurrentLayer.enemyLayerHealth : defaultHealthPerLayer;
        layerMaxHealth = Mathf.Max(1f, layerMaxHealth);
        health = layerMaxHealth;
        if (bodyRenderer != null && CurrentLayer != null && CurrentLayer.appearanceMaterial != null)
            bodyRenderer.sharedMaterial = CurrentLayer.appearanceMaterial;
        LayerChanged?.Invoke(this);
    }
    void OnWeatherChanged(WeatherType w)
    {
        if (rainRoutine != null) StopCoroutine(rainRoutine);
        rainRoutine = null;
        if (w == WeatherType.Rain || w == WeatherType.Snow || w == WeatherType.Storm)
            rainRoutine = StartCoroutine(WetFireReaction());
    }
    IEnumerator WetFireReaction()
    {
        while (!defeated)
        {
            yield return new WaitForSeconds(wetFireLayerSeconds);
            if (CurrentLayer == null || CurrentLayer.primaryFamily != ElementFamily.Fire) break;
            AdvanceLayer();
        }
        rainRoutine = null;
    }
    void OnDestroy()
    { if (WeatherManager.Instance != null) WeatherManager.Instance.WeatherChanged -= OnWeatherChanged; }
}
