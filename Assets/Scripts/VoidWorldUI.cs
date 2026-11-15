using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Attach to the World Space Canvas, NOT VisualRoot.
public class VoidWorldUI : MonoBehaviour
{
    [Header("Gameplay References")]
    [SerializeField] private EnemyLayers enemy;
    [SerializeField] private ElementalVoidSiphon siphon;
    [Header("Canvas")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private float visibleDistance = 18f;
    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text currentLayerText;
    [SerializeField] private TMP_Text layerCountText;
    [SerializeField] private TMP_Text layerListText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private TMP_Text chargeText;
    [SerializeField] private TMP_Text statusText;
    [Header("Filled Images (Image Type = Filled, Horizontal)")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Image totalChargeFill;
    [SerializeField] private Image currentAbsorptionFill;
    [SerializeField] private GameObject absorptionProgressGroup;

    private Camera cam;
    private float absorptionProgress;
    private bool activelyAbsorbing;

    private void Awake()
    {
        if (enemy == null) enemy = GetComponentInParent<EnemyLayers>();
        if (siphon == null) siphon = GetComponentInParent<ElementalVoidSiphon>();
        if (worldCanvas == null) worldCanvas = GetComponent<Canvas>();
        cam = Camera.main;
    }

    private void OnEnable()
    {
        if (siphon != null) siphon.AbsorptionPulse += OnAbsorptionPulse;
    }

    private void OnDisable()
    {
        if (siphon != null) siphon.AbsorptionPulse -= OnAbsorptionPulse;
    }

    private void OnAbsorptionPulse(ElementData element, float progress)
    {
        activelyAbsorbing = element != null;
        absorptionProgress = activelyAbsorbing ? Mathf.Clamp01(progress) : 0f;
    }

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        if (worldCanvas != null)
        {
            worldCanvas.enabled = Vector3.Distance(cam.transform.position,
                transform.position) <= visibleDistance;
            if (!worldCanvas.enabled) return;
        }

        // Unity world-space Canvas faces its positive Z forward toward the camera.
        Vector3 towardCamera = transform.position - cam.transform.position;
        if (towardCamera.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(towardCamera);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (enemy == null) return;
        ElementData outer = enemy.CurrentLayer;

        if (titleText != null) titleText.text = "ELEMENTAL VOID";
        if (currentLayerText != null)
        {
            currentLayerText.text = "OUTER: " + NameOf(outer);
            currentLayerText.color = ColorOf(outer);
        }
        if (layerCountText != null)
            layerCountText.text = enemy.RemainingLayers + " LAYERS";

        if (layerListText != null)
        {
            string list = "";
            for (int i = 0; i < enemy.RemainingLayers; i++)
            {
                if (i > 0) list += "  >  ";
                list += NameOf(enemy.GetRemainingLayer(i));
            }
            layerListText.text = list;
        }
        if (healthText != null)
            healthText.text = Mathf.CeilToInt(Mathf.Max(0f, enemy.CurrentHealth)) +
                " / " + Mathf.CeilToInt(enemy.CurrentMaxHealth) + " HP";
        if (healthFill != null)
        {
            healthFill.fillAmount = enemy.HealthFraction;
            healthFill.color = ColorOf(outer);
        }
        if (siphon == null) return;

        ElementData desired = siphon.TargetElement;
        if (targetText != null)
        {
            targetText.text = desired != null ? "TARGET: " + NameOf(desired) : "NO TARGET";
            targetText.color = ColorOf(desired);
        }
        int required = Mathf.Max(1, siphon.chargesToEvolve);
        int stored = Mathf.Clamp(siphon.StoredCharges, 0, required);
        if (chargeText != null) chargeText.text = stored + "/" + required + " CHARGES";
        if (totalChargeFill != null)
        {
            totalChargeFill.fillAmount = (float)stored / required;
            totalChargeFill.color = ColorOf(desired);
        }
        bool channeling = activelyAbsorbing && !siphon.IsInterrupted;
        if (absorptionProgressGroup != null)
            absorptionProgressGroup.SetActive(channeling);
        if (currentAbsorptionFill != null)
        {
            currentAbsorptionFill.fillAmount = absorptionProgress;
            currentAbsorptionFill.color = ColorOf(desired);
        }
        if (statusText != null)
        {
            if (siphon.IsInterrupted)
            {
                statusText.text = "DISRUPTED!";
                statusText.color = new Color(0.84f, 0.55f, 1f);
            }
            else if (channeling)
            {
                statusText.text = "ABSORBING " + Mathf.RoundToInt(absorptionProgress * 100f) + "%";
                statusText.color = Color.white;
            }
            else
            {
                statusText.text = desired != null ? "BUILDING " + NameOf(desired) : "PATROLLING";
                statusText.color = Color.white;
            }
        }
    }

    private static string NameOf(ElementData e)
    {
        if (e == null) return "NONE";
        string name = e.name;
        if (name.StartsWith("E_")) name = name.Substring(2);
        return name.ToUpperInvariant();
    }

    private static Color ColorOf(ElementData e)
    {
        if (e == null || e.appearanceMaterial == null)
            return new Color(0.72f, 0.6f, 1f);
        Material m = e.appearanceMaterial;
        if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
        if (m.HasProperty("_Color")) return m.GetColor("_Color");
        return Color.white;
    }
}
