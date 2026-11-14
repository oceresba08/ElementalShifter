using TMPro;
using UnityEngine;

public class PrototypeHUD : MonoBehaviour
{
    public PlayerElements player;
    public PlayerCombat combat;
    public TMP_Text elementLabel;
    public TMP_Text mixLabel;
    public TMP_Text weatherLabel;
    public TMP_Text chargeLabel;
    public TMP_Text messageLabel;

    private string message;
    private float messageExpires;

    private void OnEnable()
    {
        if (player != null) player.Feedback += ShowMessage;
        if (combat != null) combat.AttackFeedback += ShowMessage;
    }

    private void OnDisable()
    {
        if (player != null) player.Feedback -= ShowMessage;
        if (combat != null) combat.AttackFeedback -= ShowMessage;
    }

    private void Update()
    {
        if (elementLabel != null)
            elementLabel.text = "FORM: " + (player != null && player.Current != null ? player.Current.id.ToString() : "NEUTRAL");
        if (mixLabel != null)
            mixLabel.text = "FUSION: " + (player != null && player.MixingEnabled ? "ON" : "OFF");
        if (weatherLabel != null)
            weatherLabel.text = "WEATHER: " + (WeatherManager.Instance != null ? WeatherManager.Instance.Current.ToString() : "CLEAR");
        if (chargeLabel != null)
            chargeLabel.text = "ULT: " + (combat != null ? Mathf.RoundToInt(combat.UltimateCharge) : 0) + "%";
        if (messageLabel != null) messageLabel.text = Time.unscaledTime < messageExpires ? message : "";
    }

    private void ShowMessage(string text)
    {
        message = text;
        messageExpires = Time.unscaledTime + 1.2f;
    }
}
