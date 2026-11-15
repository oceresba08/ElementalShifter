using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerElements : MonoBehaviour
{
    public ElementDatabase database;
    public ElementData startingElement;
    public Renderer[] visualRenderers;
    public Transform visualMount;
    public Transform effectsAnchor;

    public ElementData Current { get; private set; }
    public bool MixingEnabled { get; private set; }
    [Header("Transformation permissions")]
    public bool shiftingEnabledAtStart = true;
    public bool ShiftingEnabled { get; private set; }
    public event Action<ElementData> ElementChanged;
    public event Action<string> Feedback;

    private GameObject activeAura;
    private GameObject activeVisual;

    [Header("Transformation burst settings")]
    [Min(0.1f)] public float transformationBurstLifetime = 3f;

    private void Start()
    {
        ShiftingEnabled = shiftingEnabledAtStart;
        if (startingElement != null) ApplyElement(startingElement);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
        {
            ShiftingEnabled = !ShiftingEnabled;
            Feedback?.Invoke(ShiftingEnabled ? "SHIFTING: ON" : "SHIFTING: OFF");
        }
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            MixingEnabled = !MixingEnabled;
            Feedback?.Invoke(MixingEnabled ? "FUSION MODE: ON" : "FUSION MODE: OFF");
        }
    }

    public bool Absorb(ElementData incoming)
    {
        if (incoming == null || !ShiftingEnabled) return false;
        if (Current == null || !MixingEnabled)
        {
            ApplyElement(incoming);
            return true;
        }

        if (database != null && database.TryFuse(Current, incoming, out ElementData fused))
        {
            ApplyElement(fused);
            Feedback?.Invoke("FUSION: " + fused.name.ToUpperInvariant());
            return true;
        }

        Feedback?.Invoke("NO MATCHING FUSION");
        return false;
    }

    public void ApplyElement(ElementData next)
    {
        if (next == null) return;
        // Touching a source of the same element should not replay the burst.
        bool changedForm = Current == null || Current.id != next.id;
        Current = next;
        if (activeVisual != null) Destroy(activeVisual);
        bool usesCustomMesh = next.visualPrefab != null;
        if (visualRenderers != null)
        {
            foreach (Renderer r in visualRenderers)
            {
                if (r == null) continue;
                r.enabled = !usesCustomMesh;
                if (!usesCustomMesh && next.appearanceMaterial != null)
                    r.sharedMaterial = next.appearanceMaterial;
            }
        }
        if (usesCustomMesh)
            activeVisual = Instantiate(next.visualPrefab,
                visualMount != null ? visualMount : transform);

        if (activeAura != null) Destroy(activeAura);
        if (next.loopingAuraPrefab != null)
            activeAura = Instantiate(next.loopingAuraPrefab,
                effectsAnchor != null ? effectsAnchor : transform);

        // The burst is intentionally NOT parented to the rolling ball.
        if (changedForm && next.transformationBurstPrefab != null)
        {
            Vector3 position = effectsAnchor != null ? effectsAnchor.position : transform.position;
            GameObject burst = Instantiate(next.transformationBurstPrefab, position, Quaternion.identity);
            Destroy(burst, transformationBurstLifetime);
        }

        PlayerPrefs.SetInt("ElementUnlocked_" + next.id, 1);
        PlayerPrefs.Save();
        ElementChanged?.Invoke(next);
        Feedback?.Invoke("ABSORBED: " + next.id.ToString().ToUpperInvariant());
    }

    public bool HasDiscovered(ElementData element)
    {
        return element != null && PlayerPrefs.GetInt("ElementUnlocked_" + element.id, 0) == 1;
    }
}
