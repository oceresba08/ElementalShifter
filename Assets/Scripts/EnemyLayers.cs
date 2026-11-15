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
    public float extraSizePerLayer = 0.2f;
    public float wetFireLayerSeconds = 3f;

    [Header("Optional: Visual-only growth (recommended)")]
    [Tooltip("Assign VisualRoot so the NavMeshAgent and collider root never scale.")]
    public Transform visualRoot;

    public event Action<EnemyLayers> LayerChanged;
    public event Action<EnemyLayers> Defeated;

    public int RemainingLayers => layers == null ? 0 :
        Mathf.Max(0, layers.Length - currentIndex);
    public ElementData CurrentLayer => RemainingLayers > 0 ? layers[currentIndex] : null;
    public float HealthFraction => Mathf.Clamp01(health / Mathf.Max(1f, layerMaxHealth));
    public float CurrentHealth => health;
    public float CurrentMaxHealth => layerMaxHealth;

    // Index 0 is the outer layer. Index 1 is the next inner layer.
    public ElementData GetRemainingLayer(int index)
    {
        if (layers == null || index < 0 || index >= RemainingLayers) return null;
        return layers[currentIndex + index];
    }

    private int currentIndex;
    private float health, layerMaxHealth;
    private Vector3 initialRootScale;
    private Vector3 initialVisualScale;
    private Coroutine rainRoutine;
    private bool defeated;

    private void Awake()
    {
        initialRootScale = transform.localScale;
        if (visualRoot != null) initialVisualScale = visualRoot.localScale;
    }

    private void Start()
    {
        if (randomizeOnStart && allowedRandomElements != null &&
            allowedRandomElements.Length > 0)
        {
            int min = Mathf.Clamp(randomMinLayers, 1, 5);
            int max = Mathf.Clamp(randomMaxLayers, min, 5);
            layers = new ElementData[UnityEngine.Random.Range(min, max + 1)];
            for (int i = 0; i < layers.Length; i++)
                layers[i] = allowedRandomElements[
                    UnityEngine.Random.Range(0, allowedRandomElements.Length)];
        }

        if (layers == null || layers.Length == 0 || layers[0] == null)
        {
            Debug.LogWarning("Configure layers or allowedRandomElements", this);
            enabled = false;
            return;
        }

        UpdateVisualSize();
        RefreshLayer();

        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.WeatherChanged += OnWeatherChanged;
            OnWeatherChanged(WeatherManager.Instance.Current);
        }
    }

    // Optional staggerSeconds = -1 preserves all existing projectile/skill callers.
    public bool TakeHit(float damage, ElementData attacker, bool isElemental,
                        float staggerSeconds = -1f)
    {
        if (defeated || CurrentLayer == null) return false;

        if (damage > 0f)
        {
            ElementalVoidSiphon siphon = GetComponent<ElementalVoidSiphon>();
            if (siphon != null)
            {
                if (staggerSeconds >= 0f) siphon.InterruptAbsorption(staggerSeconds);
                else siphon.InterruptAbsorption();
            }
        }

        float matchup = ElementMatchups.Advantage(attacker, CurrentLayer);
        float weight = isElemental ? 0.85f : 0.35f;
        float power = attacker != null ? attacker.attackMultiplier : 1f;
        health -= damage * Mathf.Lerp(1f, matchup, weight) * power;

        if (health <= 0f) AdvanceLayer();
        else LayerChanged?.Invoke(this);
        return true;
    }

    public bool TryAddOuterLayer(ElementData element, int maximumLayers)
    {
        if (defeated || element == null || CurrentLayer == null) return false;
        if (RemainingLayers >= Mathf.Max(1, maximumLayers)) return false;

        ElementData[] updated = new ElementData[RemainingLayers + 1];
        updated[0] = element;
        for (int i = 0; i < RemainingLayers; i++)
            updated[i + 1] = layers[currentIndex + i];

        layers = updated;
        currentIndex = 0;
        UpdateVisualSize();
        RefreshLayer();
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
        UpdateVisualSize();
        RefreshLayer();
    }

    private void UpdateVisualSize()
    {
        float multiplier = 1f + extraSizePerLayer * Mathf.Max(0, RemainingLayers - 1);
        if (visualRoot != null)
        {
            transform.localScale = initialRootScale;
            visualRoot.localScale = initialVisualScale * multiplier;
        }
        else
        {
            // Legacy behavior: if VisualRoot has not been assigned, preserve
            // the original scaling so existing prefab visuals do not change.
            transform.localScale = initialRootScale * multiplier;
        }
    }

    private void RefreshLayer()
    {
        layerMaxHealth = CurrentLayer != null
            ? CurrentLayer.enemyLayerHealth : defaultHealthPerLayer;
        layerMaxHealth = Mathf.Max(1f, layerMaxHealth);
        health = layerMaxHealth;
        if (bodyRenderer != null && CurrentLayer != null &&
            CurrentLayer.appearanceMaterial != null)
            bodyRenderer.sharedMaterial = CurrentLayer.appearanceMaterial;
        LayerChanged?.Invoke(this);
    }

    private void OnWeatherChanged(WeatherType weather)
    {
        if (rainRoutine != null) StopCoroutine(rainRoutine);
        rainRoutine = null;
        if (weather == WeatherType.Rain || weather == WeatherType.Snow ||
            weather == WeatherType.Storm)
            rainRoutine = StartCoroutine(WetFireReaction());
    }

    private IEnumerator WetFireReaction()
    {
        while (!defeated)
        {
            yield return new WaitForSeconds(wetFireLayerSeconds);
            if (CurrentLayer == null ||
                CurrentLayer.primaryFamily != ElementFamily.Fire) break;
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
