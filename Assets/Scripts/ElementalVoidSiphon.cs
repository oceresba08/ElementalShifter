
using System;
using UnityEngine;

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

    [Header("Player Interruption")]
    public bool onlyStealWhenPlayerFar = true;
    [Min(0f)] public float playerSafeDistance = 5f;
    public Transform player;


    [Header("Absorption Interruption")]
    [Min(0f)] public float interruptionSeconds = 3f;

    private float interruptedUntil;

    public bool IsInterrupted =>
        Time.time < interruptedUntil;

    public event Action AbsorptionInterrupted;


    [Header("Optional Status")]
    public TMPro.TMP_Text statusText;

    private EnemyLayers layers;
    private ElementSource target;

    private ElementData pendingElement;
    private int storedCharges;

    private bool isSeeking;
    private float nextScan;
    private float nextDrain;
    private float nextEvolution;

    // EnemyPatrol reads these properties.
    public ElementSource TargetSource => target;
    public ElementData PendingElement => pendingElement;
    public ElementData TargetElement => pendingElement;
    public int StoredCharges => storedCharges;


    public bool CanSeekResource =>
        layers != null &&
        layers.CurrentLayer != null &&
        layers.RemainingLayers < maximumLayers &&
        !IsInterrupted;


    public bool IsSeeking => isSeeking;

    // Fired after a source charge is successfully stolen.
    public event Action<ElementData> ChargeStolen;

    // Fired continuously while absorption is happening.
    // Progress is between 0 and 1.
    // A null element means stop the absorption visual.
    public event Action<ElementData, float> AbsorptionPulse;

    private void Awake()
    {
        layers = GetComponent<EnemyLayers>();
    }

    private void Start()
    {
        nextDrain = Time.time + secondsPerCharge;
    }

    // Called by EnemyPatrol when its activity changes.
    public void SetSeeking(bool allowed)
    {
        if (isSeeking == allowed)
            return;

        isSeeking = allowed;

        if (!allowed)
        {
            target = null;
            AbsorptionPulse?.Invoke(null, 0f);
        }
        else
        {
            nextScan = 0f;
            nextDrain = Time.time + secondsPerCharge;
        }
    }



    public void InterruptAbsorption()
    {
        if (layers == null || layers.CurrentLayer == null)
            return;

        // Interrupt even if the Void has just switched
        // from absorbing to chasing the player.
        interruptedUntil = Mathf.Max(
            interruptedUntil,
            Time.time + interruptionSeconds
        );

        // Reset unfinished progress toward the next charge.
        // Keep storedCharges and pendingElement unchanged.
        nextDrain = interruptedUntil + secondsPerCharge;

        target = null;
        isSeeking = false;
        nextScan = 0f;

        AbsorptionPulse?.Invoke(null, 0f);
        AbsorptionInterrupted?.Invoke();

        SetStatus("DISRUPTED!");

        Debug.Log(
            name + " was interrupted until " +
            interruptedUntil,
            this
        );
    }



    private void Update()
    {
        if (!CanSeekResource)
        {
            StopAbsorbing();
            SetStatus("MAX LAYERS");
            return;
        }

        if (!isSeeking)
        {
            AbsorptionPulse?.Invoke(null, 0f);
            return;
        }


        if (IsInterrupted)
        {
            AbsorptionPulse?.Invoke(null, 0f);
            SetStatus("DISRUPTED");
            return;
        }


        FindPlayerIfNeeded();

        if (onlyStealWhenPlayerFar && player != null)
        {
            float playerDistance = Vector3.Distance(
                transform.position,
                player.position
            );

            if (playerDistance < playerSafeDistance)
            {
                StopAbsorbing();
                SetStatus("VOID ALERT");
                return;
            }
        }

        // Weather changes may disable an elemental source.
        if (target != null && !IsSourceUsable(target))
        {
            target = null;
            nextScan = 0f;
            nextDrain = Time.time + secondsPerCharge;
        }

        // Choose a source if we don't currently have one.
        if (target == null && Time.time >= nextScan)
        {
            nextScan = Time.time + rescanInterval;
            ChooseTargetSource();
        }

        if (target == null)
        {
            AbsorptionPulse?.Invoke(null, 0f);
            SetStatus("SEARCHING");
            return;
        }

        // Finish a completed layer after its cooldown.
        if (storedCharges >= chargesToEvolve)
        {
            if (Time.time >= nextEvolution)
                TryCompleteLayer();
            else
                SetStatus("EVOLUTION COOLDOWN");

            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            target.transform.position
        );

        // The patrol agent handles navigation.
        if (distance > drainRange)
        {
            nextDrain = Time.time + secondsPerCharge;
            AbsorptionPulse?.Invoke(null, 0f);
            SetStatus("SEEKING " + pendingElement.name);
            return;
        }

        // The Void is close enough to absorb energy.
        float remainingTime = Mathf.Max(
            0f,
            nextDrain - Time.time
        );

        float progress = 1f - remainingTime /
            Mathf.Max(0.1f, secondsPerCharge);

        progress = Mathf.Clamp01(progress);

        // This event makes the shell pulse DURING absorption.
        AbsorptionPulse?.Invoke(pendingElement, progress);

        SetStatus(
            "ABSORBING " + pendingElement.name +
            " " + storedCharges + "/" + chargesToEvolve
        );

        if (Time.time < nextDrain)
            return;

        nextDrain = Time.time + secondsPerCharge;

        // Source might have become unavailable this frame.
        if (!IsSourceUsable(target))
        {
            target = null;
            nextScan = 0f;
            return;
        }

        ElementData stolenElement = target.grantsElement;

        if (stolenElement != pendingElement)
        {
            target = null;
            nextScan = 0f;
            return;
        }

        if (!target.TryDrainCharge())
        {
            target = null;
            nextScan = 0f;
            return;
        }

        storedCharges++;

        bool depleted = target.IsDepleted;

        if (depleted)
        {
            target = null;
            nextScan = 0f;
        }

        // Complete this specific element if enough energy exists.
        if (storedCharges >= chargesToEvolve &&
            Time.time >= nextEvolution)
        {
            TryCompleteLayer();
        }

        // EnemyPatrol receives this event and returns to patrol.
        ChargeStolen?.Invoke(stolenElement);
    }

    private bool IsSourceUsable(ElementSource source)
    {
        return source != null &&
               source.IsAvailable &&
               source.grantsElement != null;
    }

    private void ChooseTargetSource()
    {
        // Prefer another source of the same target element.
        ElementSource chosen = FindClosestSource(pendingElement);

        if (chosen == null)
        {
            // No more sources of the desired element.
            // Select a different available element.
            chosen = FindClosestSource(null);

            if (chosen != null &&
                pendingElement != chosen.grantsElement)
            {
                pendingElement = chosen.grantsElement;
                storedCharges = 0;

                Debug.Log(
                    name + " changed target element to " +
                    pendingElement.name,
                    this
                );
            }
        }

        target = chosen;

        if (target != null)
        {
            if (pendingElement == null)
                pendingElement = target.grantsElement;

            nextDrain = Time.time + secondsPerCharge;

            SetStatus("TARGET: " + pendingElement.name);
        }
    }

    // Searches all usable elemental sources.
    // When desired is null, any element is allowed.
    private ElementSource FindClosestSource(ElementData desired)
    {
        ElementSource bestSource = null;
        float bestDistance = float.PositiveInfinity;

        ElementSource[] sources =
            FindObjectsByType<ElementSource>(
                FindObjectsSortMode.None
            );

        foreach (ElementSource source in sources)
        {
            if (!IsSourceUsable(source))
                continue;

            if (desired != null &&
                source.grantsElement != desired)
                continue;

            float distance = (
                source.transform.position -
                transform.position
            ).sqrMagnitude;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestSource = source;
            }
        }

        return bestSource;
    }

    private void TryCompleteLayer()
    {
        if (pendingElement == null)
            return;

        if (layers.TryAddOuterLayer(
            pendingElement,
            maximumLayers))
        {
            Debug.Log(
                name + " evolved into " + pendingElement.name,
                this
            );

            SetStatus("VOID EVOLVED!");

            storedCharges = 0;
            pendingElement = null;
            target = null;

            nextEvolution = Time.time + evolutionCooldown;
            nextScan = 0f;

            AbsorptionPulse?.Invoke(null, 0f);
        }
    }

    private void StopAbsorbing()
    {
        target = null;
        AbsorptionPulse?.Invoke(null, 0f);
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null)
            return;

        PlayerElements found =
            FindFirstObjectByType<PlayerElements>();

        if (found != null)
            player = found.transform;
    }

    private void SetStatus(string message)
    {
        if (statusText != null &&
            statusText.text != message)
        {
            statusText.text = message;
        }
    }
}
