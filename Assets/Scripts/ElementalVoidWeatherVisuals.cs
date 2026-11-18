
using UnityEngine;
using UnityEngine.AI;

public class ElementalVoidWeatherVisuals : MonoBehaviour
{
    [Header("Existing Enemy Components")]
    public EnemyLayers enemyLayers;
    public NavMeshAgent agent;

    [Header("Visual Mounts")]
    public Transform visualMount;
    public Transform auraMount;
    public Renderer elementShellRenderer;

    [Header("Weather Transformation Elements")]
    public ElementData mudElement;
    public ElementData iceElement;

    [Header("Visual Settings")]
    public bool useElementVisualPrefabs = true;
    public bool useElementAuras = true;

    [Tooltip("Size of the spawned elemental model.")]
    public float visualScale = 1f;

    [Tooltip("Keep the original shell visible behind models.")]
    public bool keepOriginalShell = false;

    [Header("Weather Movement Effects")]
    [Range(0.1f, 1f)]
    public float fireWetSpeedMultiplier = 0.7f;

    [Range(0.1f, 1f)]
    public float mudSpeedMultiplier = 0.8f;

    [Range(0.1f, 1f)]
    public float iceSpeedMultiplier = 0.85f;

    [Header("Debug")]
    public bool printChanges = false;

    private GameObject currentModel;
    private GameObject currentAura;

    private ElementData displayedElement;
    private ElementData actualElement;

    private WeatherType currentWeather = WeatherType.Clear;

    private float originalAgentSpeed;
    private bool hasOriginalSpeed;
    private bool subscribedToWeather;

    public ElementData ActualElement => actualElement;
    public ElementData EffectiveElement => displayedElement;

    public bool IsExtinguished
    {
        get
        {
            return actualElement != null &&
                actualElement.id == ElementId.Fire &&
                IsWetWeather(currentWeather);
        }
    }

    private void Awake()
    {
        if (enemyLayers == null)
            enemyLayers = GetComponent<EnemyLayers>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            originalAgentSpeed = agent.speed;
            hasOriginalSpeed = true;
        }
    }

    private void Start()
    {
        if (enemyLayers != null)
            enemyLayers.LayerChanged += OnLayerChanged;

        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.WeatherChanged += OnWeatherChanged;
            subscribedToWeather = true;

            currentWeather = WeatherManager.Instance.Current;
        }

        RefreshVisuals();
    }

    private void OnDestroy()
    {
        if (enemyLayers != null)
            enemyLayers.LayerChanged -= OnLayerChanged;

        if (subscribedToWeather &&
            WeatherManager.Instance != null)
        {
            WeatherManager.Instance.WeatherChanged -= OnWeatherChanged;
        }

        if (agent != null && hasOriginalSpeed && agent.enabled)
            agent.speed = originalAgentSpeed;
    }

    private void OnLayerChanged(EnemyLayers changedEnemy)
    {
        RefreshVisuals();
    }

    private void OnWeatherChanged(WeatherType newWeather)
    {
        currentWeather = newWeather;
        RefreshVisuals();
    }

    private static bool IsWetWeather(WeatherType weather)
    {
        return weather == WeatherType.Rain ||
               weather == WeatherType.Snow ||
               weather == WeatherType.Storm;
    }

    private ElementData ResolveWeatherElement(ElementData source)
    {
        if (source == null)
            return null;

        switch (source.id)
        {
            case ElementId.Fire:
                // Fire loses its elemental shell in wet weather.
                if (IsWetWeather(currentWeather))
                    return null;
                break;

            case ElementId.Earth:
                // Earth becomes Mud in Rain or Storm.
                if (currentWeather == WeatherType.Rain ||
                    currentWeather == WeatherType.Storm)
                {
                    if (mudElement != null)
                        return mudElement;
                }
                break;

            case ElementId.Water:
                // Water becomes Ice during Snow.
                if (currentWeather == WeatherType.Snow)
                {
                    if (iceElement != null)
                        return iceElement;
                }
                break;

            case ElementId.Lightning:
                // Lightning visuals only activate during Storm.
                if (currentWeather != WeatherType.Storm)
                    return null;
                break;
        }

        return source;
    }

    public void RefreshVisuals()
    {
        actualElement = enemyLayers != null
            ? enemyLayers.CurrentLayer
            : null;

        displayedElement = ResolveWeatherElement(actualElement);

        ClearVisuals();

        if (displayedElement != null)
        {
            SpawnElementModel(displayedElement);
            SpawnElementAura(displayedElement);
        }

        UpdateOriginalShell();
        UpdateWeatherMovement();

        if (printChanges)
        {
            string originalName = actualElement != null
                ? actualElement.name : "None";

            string displayedName = displayedElement != null
                ? displayedElement.name : "Extinguished";

            Debug.Log(
                $"{name}: {originalName} -> {displayedName} " +
                $"during {currentWeather}",
                this
            );
        }
    }

    private void SpawnElementModel(ElementData element)
    {
        if (!useElementVisualPrefabs ||
            element == null ||
            element.visualPrefab == null)
        {
            return;
        }

        Transform parent = visualMount != null
            ? visualMount : transform;

        currentModel = Instantiate(
            element.visualPrefab,
            parent,
            false
        );

        currentModel.name = element.name + "_VoidModel";

        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        currentModel.transform.localScale =
            Vector3.one * Mathf.Max(0.01f, visualScale);

        // Models are decorative only.
        // The Void's original collider handles combat.
        foreach (Collider c in
            currentModel.GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

        foreach (Rigidbody body in
            currentModel.GetComponentsInChildren<Rigidbody>())
        {
            body.isKinematic = true;
            body.detectCollisions = false;
        }
    }

    private void SpawnElementAura(ElementData element)
    {
        if (!useElementAuras ||
            element == null ||
            element.loopingAuraPrefab == null)
        {
            return;
        }

        Transform parent = auraMount != null
            ? auraMount : visualMount != null
                ? visualMount : transform;

        currentAura = Instantiate(
            element.loopingAuraPrefab,
            parent,
            false
        );

        currentAura.name = element.name + "_VoidAura";

        currentAura.transform.localPosition = Vector3.zero;
        currentAura.transform.localRotation = Quaternion.identity;

        foreach (Collider c in
            currentAura.GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }
    }

    private void UpdateOriginalShell()
    {
        if (elementShellRenderer == null)
            return;

        bool hasModel = currentModel != null;

        // Show the original shell as fallback if no prefab exists.
        elementShellRenderer.enabled =
            keepOriginalShell || !hasModel;
    }

    private void UpdateWeatherMovement()
    {
        if (agent == null ||
            !agent.enabled ||
            !hasOriginalSpeed)
        {
            return;
        }

        float multiplier = 1f;

        if (IsExtinguished)
        {
            multiplier = fireWetSpeedMultiplier;
        }
        else if (actualElement != null)
        {
            if (actualElement.id == ElementId.Earth &&
                (currentWeather == WeatherType.Rain ||
                 currentWeather == WeatherType.Storm))
            {
                multiplier = mudSpeedMultiplier;
            }

            if (actualElement.id == ElementId.Water &&
                currentWeather == WeatherType.Snow)
            {
                multiplier = iceSpeedMultiplier;
            }
        }

        agent.speed = originalAgentSpeed * multiplier;
    }

    private void ClearVisuals()
    {
        if (currentModel != null)
        {
            currentModel.SetActive(false);
            Destroy(currentModel);
            currentModel = null;
        }

        if (currentAura != null)
        {
            currentAura.SetActive(false);
            Destroy(currentAura);
            currentAura = null;
        }
    }
}
