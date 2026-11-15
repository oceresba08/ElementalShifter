using UnityEngine;

/// <summary>Steals finite ElementSource charges and slowly grows an outer elemental layer.</summary>
[RequireComponent(typeof(EnemyLayers))]
public class ElementalVoidSiphon : MonoBehaviour
{
    [Header("Absorption")]
    [Min(0.5f)] public float drainRange = 3.5f;
    [Min(0.5f)] public float secondsPerCharge = 8f;
    [Min(0.1f)] public float rescanInterval = 1f;
    [Min(1)] public int chargesToEvolve = 3;
    [Min(1)] public int maximumLayers = 3;
    [Min(0f)] public float evolutionCooldown = 12f;
    [Tooltip("Useful for balancing: Voids pause absorption while the player is nearby.")]
    public bool onlyStealWhenPlayerFar = true;
    [Min(0f)] public float playerSafeDistance = 5f;
    public Transform player;
    [Header("Optional world-space status")]
    public TMPro.TMP_Text statusText;

    private EnemyLayers layers;
    private ElementSource target;
    private float nextScan;
    private float nextDrain;
    private float nextEvolution;
    private int storedCharges;
    private ElementData lastStolenElement;
    public int StoredCharges => storedCharges;
    public ElementSource TargetSource => target;
    public bool CanSeekResource => layers != null && layers.CurrentLayer != null && layers.RemainingLayers < maximumLayers;
    public event System.Action<ElementData> ChargeStolen;

    private void Awake() { layers = GetComponent<EnemyLayers>(); }
    private void Start() { nextDrain = Time.time + secondsPerCharge; }

    private void Update()
    {
        if (layers.CurrentLayer == null) return;
        if (player == null)
        {
            PlayerElements found = FindFirstObjectByType<PlayerElements>();
            if (found != null) player = found.transform;
        }
        if (onlyStealWhenPlayerFar && player != null &&
            Vector3.Distance(transform.position, player.position) < playerSafeDistance)
        {
            target = null;
            SetStatus("VOID ALERT");
            return;
        }
        if (Time.time >= nextScan)
        {
            nextScan = Time.time + rescanInterval;
            target = FindClosestAvailableSource();
        }
        if (target == null)
        {
            SetStatus("PATROLLING");
            return;
        }
        if (layers.RemainingLayers >= maximumLayers)
        {
            SetStatus("MAX LAYERS");
            return; // stops stealing resources when fully grown
        }
        float sourceDistance = Vector3.Distance(transform.position, target.transform.position);
        if (sourceDistance > drainRange)
        {
            SetStatus("SEEKING SOURCE");
            nextDrain = Time.time + secondsPerCharge;
            return;
        }
        SetStatus("ABSORBING " + storedCharges + "/" + chargesToEvolve);
        if (Time.time < nextDrain) return;
        nextDrain = Time.time + secondsPerCharge;
        ElementData stolen = target.grantsElement;
        if (!target.TryDrainCharge()) { target = null; return; }
        // Energy comes from the actual source and its own elemental family.
        lastStolenElement = stolen;
        storedCharges++;
        ChargeStolen?.Invoke(stolen);
        if (target.IsDepleted) target = null;
        if (storedCharges >= chargesToEvolve && Time.time >= nextEvolution)
        {
            if (layers.TryAddOuterLayer(lastStolenElement, maximumLayers))
            {
                storedCharges = 0;
                nextEvolution = Time.time + evolutionCooldown;
                SetStatus("VOID EVOLVED!");
            }
        }
    }

    private ElementSource FindClosestAvailableSource()
    {
        ElementSource closest = null;
        float bestSq = float.PositiveInfinity; // Allow the patrol agent to travel toward distant resources.
        foreach (ElementSource source in FindObjectsByType<ElementSource>(FindObjectsSortMode.None))
        {
            if (source == null || !source.isActiveAndEnabled || source.IsDepleted) continue;
            float ds = (source.transform.position - transform.position).sqrMagnitude;
            if (ds < bestSq) { bestSq = ds; closest = source; }
        }
        return closest;
    }
    private void SetStatus(string value)
    {
        if (statusText != null && statusText.text != value) statusText.text = value;
    }
}
