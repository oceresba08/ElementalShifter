using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ElementSource : MonoBehaviour
{
    [Header("Default element")]
    public ElementData grantsElement;
    public bool requiresWeather;
    public WeatherType onlyDuring = WeatherType.Storm;
    public bool extinguishedByRainOrSnow;

    [Header("Permanent object (never hidden)")]
    public Renderer[] persistentRenderers;
    [Tooltip("Only temporary flame/energy mesh renderers. NEVER logs, rocks, water basin, etc.")]
    public Renderer[] renderersToHide;

    [Header("Existing Rain / Storm replacement")]
    public ElementData wetReplacementElement;
    public bool replaceDuringRain = true;
    public bool replaceDuringStorm = true;
    public bool replaceDuringSnow = false;
    public bool replacementOverridesWeatherRestriction = true;

    [Header("Additional weather replacements")]
    [Tooltip("Optional override, e.g. Water -> Ice in snow.")]
    public ElementData snowReplacementElement;
    [Tooltip("Optional explicit rain element. If empty, uses Wet Replacement Element.")]
    public ElementData rainReplacementElement;
    [Tooltip("Optional explicit storm element. If empty, uses Wet Replacement Element.")]
    public ElementData stormReplacementElement;

    [Header("Element and collectible effects")]
    public Transform effectsAnchor;
    public GameObject defaultAuraPrefab;
    public GameObject wetAuraPrefab;
    public GameObject snowAuraPrefab;
    public GameObject stormAuraPrefab;
    [Tooltip("e.g. steam/smoke above extinguished logs. Visible when element cannot be collected.")]
    public GameObject unavailableEffectPrefab;
    public GameObject collectibleMarkerPrefab;
    public bool markerOnlyWhenAvailable = true;

    [Header("Optional source mesh tint")]
    public Renderer tintRenderer;
    public bool tintWhenWet;
    public Color normalColor = Color.white;
    public Color wetColor = new Color(.43f, .29f, .18f, 1f);
    public bool tintWhenSnow;
    public Color snowColor = new Color(.7f, .9f, 1f, 1f);
    public bool tintWhenStorm;
    public Color stormColor = new Color(.38f, .43f, .6f, 1f);

    [Header("Depleted source appearance")]
    [Tooltip("Keep the physical source visible but give it a depleted appearance at zero charges.")]
    public bool tintWhenDepleted = true;
    [Tooltip("Editable depleted tint. Dark gray by default.")]
    public Color depletedColor = new Color(0.22f, 0.22f, 0.24f, 1f);

    [Header("Resource depletion")]
    [Min(1)] public int startingCharges = 3;
    [Min(0f)] public float absorbCooldown = .8f;
    public bool countsForWrath = true;
    [Header("Debugging")]
    public bool logCollectionAttempts = false;

    public event Action<ElementSource> Depleted;
    public int RemainingCharges { get; private set; }
    public bool IsDepleted => RemainingCharges <= 0;
    public bool IsAvailable => isActiveAndEnabled && !IsDepleted &&
                               weatherAvailable && grantsElement != null;
    public bool IsWetForm { get; private set; }
    public ElementData DefaultElement => originalElement;
    public WeatherType CurrentWeather { get; private set; }

    private Collider sourceTrigger;
    private bool weatherAvailable;
    private float nextAbsorb;
    private ElementData originalElement;
    private GameObject auraInstance, markerInstance, unavailableInstance;
    private GameObject activeAuraPrefab;
    private MaterialPropertyBlock tintBlock;
    private bool subscribed;
    private bool collectedDuringContact;
    private GameObject activeMarkerPrefab;
    private GameObject activeUnavailablePrefab;

    private void Awake()
    {
        sourceTrigger = GetComponent<Collider>();
        sourceTrigger.isTrigger = true;
        originalElement = grantsElement;
        RemainingCharges = Mathf.Max(1, startingCharges);
        tintBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.WeatherChanged += Refresh;
            subscribed = true;
        }
        Refresh(WeatherManager.Instance != null
            ? WeatherManager.Instance.Current : WeatherType.Clear);
    }

    private void OnDestroy()
    {
        if (subscribed && WeatherManager.Instance != null)
            WeatherManager.Instance.WeatherChanged -= Refresh;
    }

    public void Refresh(WeatherType weather)
    {
        CurrentWeather = weather;
        // A changed weather-form may be collected on the next valid contact.
        ElementData previousElement = grantsElement;
        bool rain = weather == WeatherType.Rain;
        bool snow = weather == WeatherType.Snow;
        bool storm = weather == WeatherType.Storm;

        ElementData replacement = null;
        if (rain)
        {
            replacement = rainReplacementElement != null
                ? rainReplacementElement
                : replaceDuringRain ? wetReplacementElement : null;
        }
        else if (snow)
        {
            replacement = snowReplacementElement != null
                ? snowReplacementElement
                : replaceDuringSnow ? wetReplacementElement : null;
        }
        else if (storm)
        {
            replacement = stormReplacementElement != null
                ? stormReplacementElement
                : replaceDuringStorm ? wetReplacementElement : null;
        }

        IsWetForm = replacement != null;
        grantsElement = replacement != null ? replacement : originalElement;
        if (grantsElement != previousElement)
        {
            collectedDuringContact = false;
            nextAbsorb = 0f;
        }

        bool defaultAllowed =
            (!requiresWeather || weather == onlyDuring) &&
            (!extinguishedByRainOrSnow || (!rain && !snow && !storm));
        weatherAvailable = (replacement != null &&
            replacementOverridesWeatherRestriction) || defaultAllowed;

        bool available = IsAvailable;
        if (sourceTrigger != null) sourceTrigger.enabled = available;

        // Persistent source meshes remain visible regardless of availability.
        // Hide ONLY transient visual meshes, never the physical object.
        if (renderersToHide != null)
            foreach (Renderer r in renderersToHide)
                if (r != null) r.enabled = available;

        GameObject aura = ChooseAura(weather, replacement);
        SetEffect(ref auraInstance, available ? aura : null, ref activeAuraPrefab);

        GameObject marker = collectibleMarkerPrefab != null &&
            (!markerOnlyWhenAvailable || available) && !IsDepleted
            ? collectibleMarkerPrefab : null;
        SetEffect(ref markerInstance, marker, ref activeMarkerPrefab);
        SetEffect(ref unavailableInstance,
            !available && !IsDepleted ? unavailableEffectPrefab : null,
            ref activeUnavailablePrefab);
        UpdateTint(weather);
    }

    private GameObject ChooseAura(WeatherType weather, ElementData replacement)
    {
        if (replacement == null) return defaultAuraPrefab != null
            ? defaultAuraPrefab : originalElement != null ? originalElement.loopingAuraPrefab : null;

        if (weather == WeatherType.Snow)
            return snowAuraPrefab != null ? snowAuraPrefab : replacement.loopingAuraPrefab;
        if (weather == WeatherType.Storm && stormReplacementElement != null)
            return stormAuraPrefab != null ? stormAuraPrefab : replacement.loopingAuraPrefab;
        if (weather == WeatherType.Storm && stormAuraPrefab != null)
            return stormAuraPrefab;
        return wetAuraPrefab != null ? wetAuraPrefab : replacement.loopingAuraPrefab;
    }

    private void SetEffect(ref GameObject instance, GameObject desired, ref GameObject previous)
    {
        if (instance != null && previous != desired)
        {
            Destroy(instance);
            instance = null;
        }
        if (desired == null && instance != null)
        {
            Destroy(instance);
            instance = null;
        }
        if (instance == null && desired != null)
            instance = Spawn(desired);
        previous = desired;
    }

    private GameObject Spawn(GameObject prefab)
    {
        Transform parent = effectsAnchor != null ? effectsAnchor : transform;
        GameObject obj = Instantiate(prefab, parent);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        return obj;
    }

    private void UpdateTint(WeatherType weather)
    {
        if (tintRenderer == null) return;
        // Depletion overrides weather tint and stays until the source is reset.
        Color color = IsDepleted && tintWhenDepleted ? depletedColor : normalColor;
        if (!IsDepleted || !tintWhenDepleted)
        {
            if (weather == WeatherType.Snow && tintWhenSnow) color = snowColor;
            else if (weather == WeatherType.Storm && tintWhenStorm) color = stormColor;
            else if (weather == WeatherType.Storm && tintWhenWet && IsWetForm) color = wetColor;
            else if (weather == WeatherType.Rain && tintWhenWet) color = wetColor;
        }
        tintRenderer.GetPropertyBlock(tintBlock);
        tintBlock.SetColor("_BaseColor", color);
        tintBlock.SetColor("_Color", color);
        tintRenderer.SetPropertyBlock(tintBlock);
    }

    public bool TryDrainCharge()
    {
        if (!IsAvailable) return false;
        ConsumeCharge();
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCollectFromPlayer(other);
    }

    // If Absorb rejected the initial contact (e.g. shifting was disabled),
    // retry while the player stays within the trigger. One successful
    // collection is allowed per contact, so standing still cannot empty it.
    private void OnTriggerStay(Collider other)
    {
        TryCollectFromPlayer(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerElements>() != null)
            collectedDuringContact = false;
    }

    private void TryCollectFromPlayer(Collider other)
    {

        PlayerElements player = other.GetComponentInParent<PlayerElements>();
        if (player == null) return;


        ElementData equipped = player.Current;

        if (equipped != null && grantsElement != null &&
            (equipped == grantsElement ||
             equipped.id == grantsElement.id))
        {
            return;
        }


        if (!IsAvailable)
        {
            if (logCollectionAttempts && Time.time >= nextAbsorb)
            {
                Debug.Log($"{name}: unavailable (weather={CurrentWeather}, depleted={IsDepleted}, element={grantsElement})", this);
                nextAbsorb = Time.time + 1f;
            }
            return;
        }

        if (collectedDuringContact || Time.time < nextAbsorb)
            return;

        bool accepted = player.Absorb(grantsElement);
        nextAbsorb = Time.time + Mathf.Max(0.15f, absorbCooldown);

        if (logCollectionAttempts)
            Debug.Log($"{name}: player Absorb({grantsElement.name}) returned {accepted}; charges before={RemainingCharges}", this);

        if (!accepted) return;

        collectedDuringContact = true;
        ConsumeCharge();
    }

    private void ConsumeCharge()
    {
        if (!IsAvailable) return;
        RemainingCharges--;
        if (IsDepleted)
        {
            Depleted?.Invoke(this);
            Refresh(WeatherManager.Instance != null
                ? WeatherManager.Instance.Current : WeatherType.Clear);
        }
    }
}
