using UnityEngine;

/// <summary>Attach to ForestWrathSystem. Visual cue when the gods' wrath begins.</summary>
[RequireComponent(typeof(ForestWrathDirector))]
public class WrathVFXDirector : MonoBehaviour
{
    public ForestWrathDirector wrath;
    public Transform player;
    public Color warningColor = new Color(1f, .28f, .43f);
    void Awake() { if (wrath == null) wrath = GetComponent<ForestWrathDirector>(); }
    void OnEnable()
    {
        if (wrath != null) wrath.WrathBegan += OnWrath;
    }
    void OnDisable()
    {
        if (wrath != null) wrath.WrathBegan -= OnWrath;
    }
    void OnWrath()
    {
        Vector3 origin = player != null ? player.position : transform.position;
        ElementalVFX.Burst("DEMETER_WRATH_WARNING", origin + Vector3.up * 1.5f, warningColor,
            130, 3f, 9f, .27f);
    }
}
