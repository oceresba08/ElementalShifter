using UnityEngine;

/// <summary>Attach to each Enemy prefab. Tracks the actual layer and health changes.</summary>
[RequireComponent(typeof(EnemyLayers))]
public class EnemyVFXDirector : MonoBehaviour
{
    public EnemyLayers enemy;
    public bool auraEnabled = true;
    public float auraRadius = .75f;
    public Transform auraAnchor;
    ParticleSystem aura;
    int previousLayers = -1;
    float previousHealth = -1f;
    ElementData previousElement;

    void Awake() { if (enemy == null) enemy = GetComponent<EnemyLayers>(); }
    void OnEnable()
    {
        if (enemy != null) { enemy.LayerChanged += OnLayerChanged; enemy.Defeated += OnDefeated; }
    }
    void OnDisable()
    {
        if (enemy != null) { enemy.LayerChanged -= OnLayerChanged; enemy.Defeated -= OnDefeated; }
        if (aura != null) Destroy(aura.gameObject);
    }
    void Start()
    {
        // EnemyLayers.Start may run before or after us; next event handles initial state.
        if (enemy != null && enemy.CurrentLayer != null && previousLayers < 0) OnLayerChanged(enemy);
    }
    void OnLayerChanged(EnemyLayers e)
    {
        if (e.CurrentLayer == null) return;
        bool newLayer = previousLayers >= 0 && e.RemainingLayers < previousLayers;
        bool hit = !newLayer && previousLayers >= 0 && e.CurrentHealth < previousHealth;
        if (newLayer)
            ElementalVFX.Burst("LAYER_SHATTER", transform.position, ElementalVFX.ColorFor(previousElement), 65, .9f, 6.5f, .2f);
        else if (hit)
            ElementalVFX.Burst("ENEMY_HIT", transform.position, ElementalVFX.ColorFor(e.CurrentLayer), 17, .4f, 3.2f, .13f);
        if (previousElement != e.CurrentLayer || aura == null)
        {
            if (aura != null) Destroy(aura.gameObject);
            if (auraEnabled) aura = ElementalVFX.Aura("ENEMY_LAYER_AURA", auraAnchor != null ? auraAnchor : transform,
                ElementalVFX.ColorFor(e.CurrentLayer), auraRadius, 10f, .14f);
        }
        previousElement = e.CurrentLayer;
        previousLayers = e.RemainingLayers;
        previousHealth = e.CurrentHealth;
    }
    void OnDefeated(EnemyLayers e)
    {
        ElementalVFX.Burst("VOID_DEFEATED", transform.position, new Color(.55f, .25f, .9f), 95, 1.1f, 7f, .27f);
    }
}
