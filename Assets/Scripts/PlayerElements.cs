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
    public event Action<ElementData> ElementChanged;
    public event Action<string> Feedback;

    private GameObject activeAura;
    private GameObject activeVisual;

    private void Start()
    {
        if (startingElement != null) ApplyElement(startingElement);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            MixingEnabled = !MixingEnabled;
            Feedback?.Invoke(MixingEnabled ? "FUSION MODE: ON" : "FUSION MODE: OFF");
        }
    }

    public bool Absorb(ElementData incoming)
    {
        if (incoming == null) return false;
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
