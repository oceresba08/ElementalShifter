using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WindCurrentZone : MonoBehaviour
{
    [Header("Gust timing")]
    public float calmSeconds = 8f;
    public float gustSeconds = 4f;
    public bool startWithGust = false;
    public bool stormOnly = false;
    [Header("Wind physics")]
    public Vector3 windDirection = Vector3.right;
    public float windAcceleration = 14f;
    [Header("Wind transformation")]
    public ElementData windElement;
    public Renderer[] activeGustVisuals;
    public bool IsGusting { get; private set; }

    float phaseRemaining;
    readonly HashSet<PlayerElements> receivedThisGust = new HashSet<PlayerElements>();
    Collider zone;

    void Awake()
    {
        zone = GetComponent<Collider>();
        zone.isTrigger = true;
        IsGusting = startWithGust;
        phaseRemaining = Mathf.Max(.2f, IsGusting ? gustSeconds : calmSeconds);
        UpdateVisuals();
    }

    void Update()
    {
        bool allowed = !stormOnly || (WeatherManager.Instance != null && WeatherManager.Instance.Current == WeatherType.Storm);
        if (!allowed)
        {
            if (IsGusting) { IsGusting = false; receivedThisGust.Clear(); UpdateVisuals(); }
            phaseRemaining = Mathf.Max(.2f, calmSeconds);
            return;
        }
        phaseRemaining -= Time.deltaTime;
        if (phaseRemaining > 0) return;
        IsGusting = !IsGusting;
        phaseRemaining = Mathf.Max(.2f, IsGusting ? gustSeconds : calmSeconds);
        receivedThisGust.Clear();
        UpdateVisuals();
    }
    void UpdateVisuals()
    {
        foreach (Renderer r in activeGustVisuals)
            if (r != null) r.enabled = IsGusting;
    }
    void OnTriggerStay(Collider other)
    {
        if (!IsGusting) return;
        Rigidbody body = other.attachedRigidbody;
        if (body != null && !body.isKinematic)
            body.AddForce(windDirection.normalized * windAcceleration, ForceMode.Acceleration);

        PlayerElements player = other.GetComponentInParent<PlayerElements>();
        // Shifting OFF means wind can still move the player but cannot change form.
        if (player != null && windElement != null && !receivedThisGust.Contains(player) && player.ShiftingEnabled)
            if (player.Absorb(windElement)) receivedThisGust.Add(player);
    }
}
