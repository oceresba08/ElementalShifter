using UnityEngine;

/// <summary>
/// Add to the Player alongside PlayerElements and PlayerCombat.
/// Reacts to existing events without replacing either existing component.
/// </summary>
[RequireComponent(typeof(PlayerElements))]
public class PlayerVFXDirector : MonoBehaviour
{
    public PlayerElements elements;
    public PlayerCombat combat;
    [Header("VFX switches")]
    public bool transformationBursts = true;
    public bool persistentAuras = true;
    public bool combatBursts = true;
    [Range(.25f, 2f)] public float visualScale = 1f;
    public Transform auraAnchor;
    ParticleSystem activeAura;

    void Awake()
    {
        if (elements == null) elements = GetComponent<PlayerElements>();
        if (combat == null) combat = GetComponent<PlayerCombat>();
    }
    void OnEnable()
    {
        if (elements != null) elements.ElementChanged += OnTransform;
        if (combat != null) combat.AttackFeedback += OnAttack;
    }
    void Start() { if (elements != null && elements.Current != null) SetAura(elements.Current); }
    void OnDisable()
    {
        if (elements != null) elements.ElementChanged -= OnTransform;
        if (combat != null) combat.AttackFeedback -= OnAttack;
        if (activeAura != null) Destroy(activeAura.gameObject);
    }
    void OnTransform(ElementData next)
    {
        if (transformationBursts)
        {
            ElementalVFX.Burst("TRANSFORM_" + next.id, transform.position, ElementalVFX.ColorFor(next),
                Mathf.RoundToInt(46 * visualScale), .6f * visualScale, 6f, .23f * visualScale);
        }
        SetAura(next);
    }
    void SetAura(ElementData current)
    {
        if (activeAura != null) Destroy(activeAura.gameObject);
        activeAura = null;
        // Avoid duplicating an artist-authored looping aura already assigned on ElementData.
        if (!persistentAuras || current == null || current.loopingAuraPrefab != null) return;
        activeAura = ElementalVFX.Aura("AURA_" + current.id,
            auraAnchor != null ? auraAnchor : transform, ElementalVFX.ColorFor(current),
            .58f * visualScale, 13f, .12f * visualScale);
    }
    void OnAttack(string feedback)
    {
        if (!combatBursts || string.IsNullOrEmpty(feedback)) return;
        Color color = ElementalVFX.ColorFor(elements != null ? elements.Current : null);
        // These are cue/anticipation bursts. Actual enemy hit particles are handled by EnemyVFXDirector.
        if (feedback.Contains("HIT"))
            ElementalVFX.Burst("HIT_DASH", transform.position + transform.forward * .4f, color, 13, .2f, 3f, .12f);
        else if (feedback.Contains("SPIN"))
            ElementalVFX.Burst("SPIN_START", transform.position, color, 23, .5f, 4.2f, .14f);
        else if (feedback.Contains("SLAM"))
            ElementalVFX.Burst("SLAM_LAUNCH", transform.position, color, 22, .35f, 3f, .13f);
        else if (feedback.Contains("SMASH"))
            ElementalVFX.Burst("SLAM_IMPACT", transform.position - Vector3.up * .4f, color, 85, 1.25f, 8f, .25f);
        else
            ElementalVFX.Burst("SKILL_" + feedback, transform.position, color, 28, .5f, 4.5f, .19f);
    }
}
