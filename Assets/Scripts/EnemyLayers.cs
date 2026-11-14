using System;
using System.Collections;
using UnityEngine;

public class EnemyLayers : MonoBehaviour
{
    [Header("Element layers, outermost first")]
    public ElementData[] layers;
    public bool randomizeOnStart;
    public ElementData[] allowedRandomElements;
    [Range(1, 5)] public int randomMinLayers = 1;
    [Range(1, 5)] public int randomMaxLayers = 3;

    [Header("Stats and appearance")]
    [Min(1f)] public float healthPerLayer = 35f;
    public Renderer bodyRenderer;
    public float extraSizePerLayer = 0.2f;
    [Min(0.2f)] public float wetFireLayerSeconds = 3f;

    public event Action<EnemyLayers> LayerChanged;
    public event Action<EnemyLayers> Defeated;
    public int RemainingLayers => layers == null ? 0 : Mathf.Max(0, layers.Length - currentIndex);
    public ElementData CurrentLayer => RemainingLayers > 0 ? layers[currentIndex] : null;
    public float HealthFraction => Mathf.Clamp01(health / Mathf.Max(1f, healthPerLayer));

    private int currentIndex;
    private float health;
    private Vector3 initialScale;
    private Coroutine rainRoutine;
    private bool defeated;

    private void Start()
    {
        initialScale = transform.localScale;
        if (randomizeOnStart && allowedRandomElements != null && allowedRandomElements.Length > 0)
        {
            int count = UnityEngine.Random.Range(randomMinLayers, randomMaxLayers + 1);
            layers = new ElementData[count];
            for (int i = 0; i < count; i++)
                layers[i] = allowedRandomElements[UnityEngine.Random.Range(0, allowedRandomElements.Length)];
        }
        if (layers == null || layers.Length == 0)
        {
            Debug.LogWarning("Enemy needs at least one elemental layer.", this);
            enabled = false;
            return;
        }

        health = healthPerLayer;
        transform.localScale = initialScale * (1f + extraSizePerLayer * (layers.Length - 1));
        SetAppearance();
        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.WeatherChanged += OnWeatherChanged;
            OnWeatherChanged(WeatherManager.Instance.Current);
        }
    }

    public bool TakeHit(float baseDamage, ElementData attackElement, bool isElementalAttack)
    {
        if (defeated || CurrentLayer == null) return false;
        float matchup = ElementMatchups.Advantage(attackElement, CurrentLayer);
        float matchupWeight = isElementalAttack ? 0.85f : 0.35f;
        float attackScale = attackElement != null ? attackElement.attackMultiplier : 1f;
        health -= baseDamage * Mathf.Lerp(1f, matchup, matchupWeight) * attackScale;
        if (health <= 0f) AdvanceLayer();
        else LayerChanged?.Invoke(this);
        return true;
    }

    private void AdvanceLayer()
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
        health = healthPerLayer;
        SetAppearance();
    }

    private void SetAppearance()
    {
        if (bodyRenderer != null && CurrentLayer != null && CurrentLayer.appearanceMaterial != null)
            bodyRenderer.sharedMaterial = CurrentLayer.appearanceMaterial;
        LayerChanged?.Invoke(this);
    }

    private void OnWeatherChanged(WeatherType weather)
    {
        if (rainRoutine != null) StopCoroutine(rainRoutine);
        rainRoutine = null;
        if (weather == WeatherType.Rain || weather == WeatherType.Snow || weather == WeatherType.Storm)
            rainRoutine = StartCoroutine(WetFireReaction());
    }

    private IEnumerator WetFireReaction()
    {
        while (!defeated)
        {
            yield return new WaitForSeconds(wetFireLayerSeconds);
            if (CurrentLayer == null || CurrentLayer.primaryFamily != ElementFamily.Fire)
                break;
            AdvanceLayer();
        }
        rainRoutine = null;
    }

    private void OnDestroy()
    {
        if (WeatherManager.Instance != null)
            WeatherManager.Instance.WeatherChanged -= OnWeatherChanged;
    }
}
