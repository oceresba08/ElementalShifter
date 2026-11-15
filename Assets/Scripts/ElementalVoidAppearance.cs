using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyLayers))]
public class ElementalVoidAppearance : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] Renderer elementalShellRenderer;
    [Range(0f, 1f)][SerializeField] float shellOpacity = .28f;
    [SerializeField] Transform effectsAnchor;
    [SerializeField] GameObject persistentVoidAuraPrefab;
    [SerializeField] GameObject defeatBurstPrefab;
    [Min(.1f)][SerializeField] float burstLifetime = 3f;

    [Header("Absorption flashes")]
    [SerializeField] float flashSeconds = .5f;
    [SerializeField] int flashes = 2;

    EnemyLayers enemy;
    ElementalVoidSiphon siphon;
    GameObject elementalAura;
    GameObject voidAura;
    ElementData previousElement;
    int previousLayerCount = -1;
    MaterialPropertyBlock properties;
    Color baseColor = Color.white;
    Coroutine flashRoutine;

    void Awake()
    {
        enemy = GetComponent<EnemyLayers>();
        siphon = GetComponent<ElementalVoidSiphon>();
        properties = new MaterialPropertyBlock();
    }
    void OnEnable()
    {
        enemy.LayerChanged += OnLayerChanged;
        enemy.Defeated += OnDefeated;
        if (siphon != null) siphon.ChargeStolen += FlashAbsorption;
    }
    void Start()
    {
        if (enemy.CurrentLayer != null) RefreshElement();
        if (persistentVoidAuraPrefab != null && voidAura == null)
            voidAura = Instantiate(persistentVoidAuraPrefab, effectsAnchor != null ? effectsAnchor : transform);
    }
    void OnLayerChanged(EnemyLayers source)
    {
        if (source == null || source.CurrentLayer == null) return;
        if (previousLayerCount >= 0 && source.RemainingLayers < previousLayerCount)
            SpawnBurst(previousElement != null ? previousElement.transformationBurstPrefab : null);
        if (previousLayerCount != source.RemainingLayers || previousElement != source.CurrentLayer)
            RefreshElement();
    }
    void RefreshElement()
    {
        ElementData element = enemy.CurrentLayer;
        if (element == null) return;
        previousElement = element;
        previousLayerCount = enemy.RemainingLayers;
        if (elementalAura != null) Destroy(elementalAura);
        elementalAura = element.loopingAuraPrefab != null
            ? Instantiate(element.loopingAuraPrefab, effectsAnchor != null ? effectsAnchor : transform) : null;
        baseColor = ElementTint(element);
        if (flashRoutine != null) { StopCoroutine(flashRoutine); flashRoutine = null; }
        SetShell(baseColor);
    }
    static Color ElementTint(ElementData element)
    {
        if (element != null && element.appearanceMaterial != null)
        {
            Material m = element.appearanceMaterial;
            if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
            if (m.HasProperty("_Color")) return m.GetColor("_Color");
        }
        return Color.white;
    }
    void SetShell(Color color)
    {
        if (elementalShellRenderer == null) return;
        color.a = shellOpacity;
        elementalShellRenderer.GetPropertyBlock(properties);
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);
        elementalShellRenderer.SetPropertyBlock(properties);
    }
    void FlashAbsorption(ElementData stolen)
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(Flash(stolen));
    }
    IEnumerator Flash(ElementData stolen)
    {
        Color stolenColor = ElementTint(stolen);
        float half = Mathf.Max(.03f, flashSeconds / (Mathf.Max(1, flashes) * 2));
        for (int i = 0; i < Mathf.Max(1, flashes); i++)
        {
            float t = 0;
            while (t < half) { t += Time.deltaTime; SetShell(Color.Lerp(baseColor, stolenColor, Mathf.Clamp01(t / half))); yield return null; }
            t = 0;
            while (t < half) { t += Time.deltaTime; SetShell(Color.Lerp(stolenColor, baseColor, Mathf.Clamp01(t / half))); yield return null; }
        }
        SetShell(baseColor);
        flashRoutine = null;
    }
    void OnDefeated(EnemyLayers source)
    {
        SpawnBurst(previousElement != null ? previousElement.transformationBurstPrefab : null);
        SpawnBurst(defeatBurstPrefab);
    }
    void SpawnBurst(GameObject prefab)
    {
        if (prefab == null) return;
        GameObject effect = Instantiate(prefab, transform.position, Quaternion.identity);
        Destroy(effect, burstLifetime);
    }
    void OnDisable()
    {
        if (enemy != null) { enemy.LayerChanged -= OnLayerChanged; enemy.Defeated -= OnDefeated; }
        if (siphon != null) siphon.ChargeStolen -= FlashAbsorption;
    }
}
