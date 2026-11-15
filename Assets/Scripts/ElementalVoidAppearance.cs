
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyLayers))]
public class ElementalVoidAppearance : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Renderer elementalShellRenderer;

    [Range(0f, 1f)]
    [SerializeField] private float shellOpacity = 0.5f;

    [SerializeField] private Transform effectsAnchor;
    [SerializeField] private GameObject persistentVoidAuraPrefab;
    [SerializeField] private GameObject defeatBurstPrefab;

    [Min(0.1f)]
    [SerializeField] private float burstLifetime = 3f;

    [Header("Absorption Flashes")]
    [SerializeField] private float flashSeconds = 0.6f;

    [Min(1)]
    [SerializeField] private int flashes = 2;

    private EnemyLayers enemy;
    private ElementalVoidSiphon siphon;

    private MaterialPropertyBlock properties;

    private GameObject elementalAura;
    private GameObject voidAura;

    private ElementData displayedElement;
    private int displayedLayerCount = -1;

    private Color permanentColor = Color.white;

    private ElementData absorbingElement;
    private float absorptionProgress;
    private bool isAbsorbing;

    private void Awake()
    {
        enemy = GetComponent<EnemyLayers>();
        siphon = GetComponent<ElementalVoidSiphon>();

        properties = new MaterialPropertyBlock();

        if (elementalShellRenderer == null)
        {
            Debug.LogWarning(
                "ElementalShell renderer is not assigned!",
                this
            );
        }
    }

    private void OnEnable()
    {
        if (enemy != null)
        {
            enemy.LayerChanged += OnLayerChanged;
            enemy.Defeated += OnDefeated;
        }

        if (siphon != null)
        {
            siphon.AbsorptionPulse += OnAbsorptionPulse;
        }
    }

    private void Start()
    {
        RefreshElement();

        if (persistentVoidAuraPrefab != null &&
            voidAura == null)
        {
            Transform anchor =
                effectsAnchor != null
                ? effectsAnchor
                : transform;

            voidAura = Instantiate(
                persistentVoidAuraPrefab,
                anchor
            );

            voidAura.transform.localPosition = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (enemy == null ||
            enemy.CurrentLayer == null)
            return;

        // Normally display the current outer layer.
        if (!isAbsorbing || absorbingElement == null)
        {
            SetShellColor(permanentColor);
            return;
        }

        // Blend between the current and target colors
        // repeatedly WHILE an element is being absorbed.
        float frequency =
            Mathf.Max(1, flashes) /
            Mathf.Max(0.1f, flashSeconds);

        float oscillation = 0.5f + 0.5f *
            Mathf.Sin(Time.time * frequency *
                      Mathf.PI * 2f);

        // Make the target color increasingly prominent
        // as the enemy approaches a complete charge.
        float strength = Mathf.Lerp(
            0.45f,
            0.95f,
            absorptionProgress
        );

        float blend = oscillation * strength;

        Color targetColor =
            GetElementColor(absorbingElement);

        Color result = Color.Lerp(
            permanentColor,
            targetColor,
            blend
        );

        SetShellColor(result);
    }

    private void OnAbsorptionPulse(
        ElementData targetElement,
        float progress)
    {
        if (targetElement == null)
        {
            isAbsorbing = false;
            absorbingElement = null;
            absorptionProgress = 0f;

            SetShellColor(permanentColor);
            return;
        }

        absorbingElement = targetElement;
        absorptionProgress = Mathf.Clamp01(progress);
        isAbsorbing = true;
    }

    private void OnLayerChanged(EnemyLayers source)
    {
        if (source == null ||
            source.CurrentLayer == null)
            return;

        bool layerWasRemoved =
            displayedLayerCount >= 0 &&
            source.RemainingLayers < displayedLayerCount;

        bool elementChanged =
            displayedElement != source.CurrentLayer;

        bool layerCountChanged =
            displayedLayerCount != source.RemainingLayers;

        if (layerWasRemoved && displayedElement != null)
        {
            SpawnBurst(
                displayedElement.transformationBurstPrefab
            );
        }

        if (elementChanged || layerCountChanged)
        {
            RefreshElement();
        }
    }

    private void RefreshElement()
    {
        if (enemy == null ||
            enemy.CurrentLayer == null)
            return;

        ElementData current = enemy.CurrentLayer;

        displayedElement = current;
        displayedLayerCount = enemy.RemainingLayers;

        permanentColor = GetElementColor(current);

        isAbsorbing = false;
        absorbingElement = null;
        absorptionProgress = 0f;

        SetShellColor(permanentColor);

        // Remove the previous elemental aura.
        if (elementalAura != null)
        {
            Destroy(elementalAura);
            elementalAura = null;
        }

        // Spawn the new outer layer's handmade aura.
        if (current.loopingAuraPrefab != null)
        {
            Transform anchor =
                effectsAnchor != null
                ? effectsAnchor
                : transform;

            elementalAura = Instantiate(
                current.loopingAuraPrefab,
                anchor
            );

            elementalAura.transform.localPosition =
                Vector3.zero;
        }
    }

    private static Color GetElementColor(
        ElementData element)
    {
        if (element == null ||
            element.appearanceMaterial == null)
        {
            return Color.gray;
        }

        Material material = element.appearanceMaterial;

        if (material.HasProperty("_BaseColor"))
        {
            return material.GetColor("_BaseColor");
        }

        if (material.HasProperty("_Color"))
        {
            return material.GetColor("_Color");
        }

        return Color.gray;
    }

    private void SetShellColor(Color color)
    {
        if (elementalShellRenderer == null)
            return;

        color.a = shellOpacity;

        elementalShellRenderer.GetPropertyBlock(properties);

        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);

        elementalShellRenderer.SetPropertyBlock(
            properties
        );
    }

    private void OnDefeated(EnemyLayers source)
    {
        isAbsorbing = false;

        if (displayedElement != null)
        {
            SpawnBurst(
                displayedElement.transformationBurstPrefab
            );
        }

        SpawnBurst(defeatBurstPrefab);
    }

    private void SpawnBurst(GameObject prefab)
    {
        if (prefab == null)
            return;

        Vector3 position =
            effectsAnchor != null
            ? effectsAnchor.position
            : transform.position;

        GameObject effect = Instantiate(
            prefab,
            position,
            Quaternion.identity
        );

        Destroy(effect, burstLifetime);
    }

    private void OnDisable()
    {
        if (enemy != null)
        {
            enemy.LayerChanged -= OnLayerChanged;
            enemy.Defeated -= OnDefeated;
        }

        if (siphon != null)
        {
            siphon.AbsorptionPulse -= OnAbsorptionPulse;
        }
    }
}
