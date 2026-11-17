
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ElementalAbilityHUD : MonoBehaviour
{
    [Header("Player References")]
    public ElementalAbilityController abilities;
    public PlayerCombat combat;

    [Header("Text Labels")]
    public TMP_Text formLabel;
    public TMP_Text skillLabel;
    public TMP_Text skillTimer;
    public TMP_Text ultimateLabel;
    public TMP_Text ultimatePercent;
    public TMP_Text feedbackLabel;

    [Header("Progress Bars")]
    public Image skillFill;
    public Image ultimateFill;

    [Header("Colors")]
    public Color readyColor = new Color(
        0.25f, 0.85f, 0.95f
    );

    public Color cooldownColor = new Color(
        0.65f, 0.55f, 0.95f
    );

    public Color lockedColor = new Color(
        0.45f, 0.45f, 0.5f
    );

    public Color ultimateReadyColor = new Color(
        1f, 0.8f, 0.35f
    );

    private void Awake()
    {
        FindReferences();
    }

    private void Start()
    {
        FindReferences();
        RefreshHUD();
    }

    private void Update()
    {
        // Recover references if the Player was
        // created after this HUD initialized.
        if (abilities == null || combat == null)
        {
            FindReferences();
        }

        RefreshHUD();
    }

    private void FindReferences()
    {
        if (abilities == null)
        {
            abilities =
                FindFirstObjectByType<
                    ElementalAbilityController>();
        }

        if (combat == null)
        {
            combat =
                FindFirstObjectByType<PlayerCombat>();
        }
    }

    private void RefreshHUD()
    {
        if (abilities == null || combat == null)
        {
            ShowUnavailable();
            return;
        }

        // ---------------------------
        // CURRENT ELEMENT
        // ---------------------------

        if (formLabel != null)
        {
            formLabel.text = abilities.HasElement
                ? "FORM: " +
                  abilities.CurrentId.ToString()
                      .ToUpperInvariant()
                : "FORM: NEUTRAL";
        }

        // ---------------------------
        // Q SKILL
        // ---------------------------

        if (skillLabel != null)
        {
            skillLabel.text =
                "Q: " + abilities.SkillLabel;
        }

        float remaining =
            Mathf.Max(0f, abilities.SkillRemaining);

        float cooldown =
            Mathf.Max(0.01f, abilities.SkillCooldown);

        bool hasForm = abilities.HasElement;
        bool weatherLocked = abilities.WeatherLocked;

        bool skillReady =
            hasForm &&
            !weatherLocked &&
            remaining <= 0f;

        if (skillTimer != null)
        {
            if (!hasForm)
            {
                skillTimer.text = "NO ELEMENT";
            }
            else if (weatherLocked)
            {
                skillTimer.text = "WEATHER LOCKED";
            }
            else if (remaining > 0f)
            {
                skillTimer.text =
                    remaining.ToString("0.0") + "s";
            }
            else
            {
                skillTimer.text = "READY";
            }
        }

        if (skillFill != null)
        {
            float fill = hasForm
                ? 1f - Mathf.Clamp01(
                    remaining / cooldown)
                : 0f;

            skillFill.fillAmount =
                weatherLocked ? 0f : fill;

            skillFill.color =
                !hasForm || weatherLocked
                ? lockedColor
                : skillReady
                    ? readyColor
                    : cooldownColor;
        }

        // ---------------------------
        // R ULTIMATE
        // ---------------------------

        if (ultimateLabel != null)
        {
            ultimateLabel.text =
                "R: " + abilities.UltimateLabel;
        }

        float ultimateCharge =
            Mathf.Clamp(
                combat.UltimateCharge,
                0f,
                100f
            );

        if (ultimatePercent != null)
        {
            ultimatePercent.text =
                Mathf.RoundToInt(ultimateCharge)
                + "%";

            if (ultimateCharge >= 100f)
            {
                ultimatePercent.text = "READY!";
            }
        }

        if (ultimateFill != null)
        {
            ultimateFill.fillAmount =
                ultimateCharge / 100f;

            ultimateFill.color =
                ultimateCharge >= 100f
                    ? ultimateReadyColor
                    : cooldownColor;
        }

        // ---------------------------
        // COMBAT FEEDBACK
        // ---------------------------

        if (feedbackLabel != null)
        {
            if (Time.time <
                abilities.LastMessageUntil)
            {
                feedbackLabel.text =
                    abilities.LastMessage;
            }
            else
            {
                feedbackLabel.text = "";
            }
        }
    }

    private void ShowUnavailable()
    {
        if (formLabel != null)
            formLabel.text = "FORM: NEUTRAL";

        if (skillLabel != null)
            skillLabel.text = "Q: NO SKILL";

        if (skillTimer != null)
            skillTimer.text = "UNAVAILABLE";

        if (ultimateLabel != null)
            ultimateLabel.text = "R: NO ULTIMATE";

        if (ultimatePercent != null)
            ultimatePercent.text = "0%";

        if (feedbackLabel != null)
            feedbackLabel.text = "";

        if (skillFill != null)
            skillFill.fillAmount = 0f;

        if (ultimateFill != null)
            ultimateFill.fillAmount = 0f;
    }
}
