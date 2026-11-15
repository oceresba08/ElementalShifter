using UnityEngine;

/// <summary>
/// Visual-only bridge for the existing EnemyLayers component.
/// Keeps the permanent core untouched, tints a TRANSPARENT shell,
/// and swaps hand-authored particle aura prefabs on elemental layer changes.
/// </summary>
[RequireComponent(typeof(EnemyLayers))]
public class ElementalVoidAppearance : MonoBehaviour
{
    [Header("Assign the outer translucent sphere here")]
    [SerializeField] private Renderer elementalShellRenderer;
    [Range(0f, 1f)][SerializeField] private float shellOpacity = 0.28f;

    [Header("Particle mount (child Empty at 0,0,0)")]
    [SerializeField] private Transform effectsAnchor;
    [SerializeField] private GameObject persistentVoidAuraPrefab;
    [SerializeField] private GameObject defeatBurstPrefab;

    [Header("One-shot effect lifetime")]
    [Min(0.1f)][SerializeField] private float burstLifetime = 3f;

    private EnemyLayers enemy;
    private GameObject elementalAuraInstance;
    private GameObject voidAuraInstance;
    private ElementData previousElement;
    private int previousLayerCount = -1;
    private MaterialPropertyBlock shellProperties;

    private void Awake()
    {
        enemy = GetComponent<EnemyLayers>();
        shellProperties = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (enemy == null) enemy = GetComponent<EnemyLayers>();
        enemy.LayerChanged += OnLayerChanged;
        enemy.Defeated += OnDefeated;
    }

    private void Start()
    {
        // EnemyLayers.Start initializes random elements; this is safe regardless
        // of Start method ordering because LayerChanged also refreshes visuals.
        if (enemy.CurrentLayer != null && previousLayerCount < 0)
            RefreshElement();

        if (persistentVoidAuraPrefab != null && voidAuraInstance == null)
            voidAuraInstance = Instantiate(persistentVoidAuraPrefab, Anchor());
    }

    private Transform Anchor() => effectsAnchor != null ? effectsAnchor : transform;

    private void OnLayerChanged(EnemyLayers source)
    {
        if (source == null || source.CurrentLayer == null) return;
        // LayerChanged also fires when health changes. Rebuild only when a
        // layer was actually removed, or the current element changed.
        if (previousLayerCount >= 0 && source.RemainingLayers < previousLayerCount)
            SpawnBurst(previousElement != null ? previousElement.transformationBurstPrefab : null);

        if (previousLayerCount != source.RemainingLayers || previousElement != source.CurrentLayer)
            RefreshElement();
    }

    private void RefreshElement()
    {
        ElementData element = enemy.CurrentLayer;
        if (element == null) return;
        previousElement = element;
        previousLayerCount = enemy.RemainingLayers;

        if (elementalAuraInstance != null)
            Destroy(elementalAuraInstance);
        elementalAuraInstance = null;

        if (element.loopingAuraPrefab != null)
            elementalAuraInstance = Instantiate(element.loopingAuraPrefab, Anchor());

        if (elementalShellRenderer != null)
        {
            Color tint = Color.white;
            if (element.appearanceMaterial != null)
            {
                Material m = element.appearanceMaterial;
                if (m.HasProperty("_BaseColor")) tint = m.GetColor("_BaseColor");
                else if (m.HasProperty("_Color")) tint = m.GetColor("_Color");
            }
            tint.a = shellOpacity;
            elementalShellRenderer.GetPropertyBlock(shellProperties);
            shellProperties.SetColor("_BaseColor", tint);
            shellProperties.SetColor("_Color", tint);
            elementalShellRenderer.SetPropertyBlock(shellProperties);
        }
    }

    private void OnDefeated(EnemyLayers source)
    {
        SpawnBurst(previousElement != null ? previousElement.transformationBurstPrefab : null);
        SpawnBurst(defeatBurstPrefab);
    }

    private void SpawnBurst(GameObject prefab)
    {
        if (prefab == null) return;
        GameObject spawned = Instantiate(prefab, transform.position, Quaternion.identity);
        Destroy(spawned, burstLifetime);
    }

    private void OnDisable()
    {
        if (enemy == null) return;
        enemy.LayerChanged -= OnLayerChanged;
        enemy.Defeated -= OnDefeated;
    }
}
