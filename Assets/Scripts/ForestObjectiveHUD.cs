
using UnityEngine;
using TMPro;

/// <summary>
/// Displays remaining enemies and elemental resources.
/// </summary>
public class ForestObjectiveHUD : MonoBehaviour
{
    public TMP_Text voidObjectiveText;
    public TMP_Text sourceObjectiveText;

    public float refreshSeconds = 0.3f;

    private float nextRefresh;

    private void Update()
    {
        if (voidObjectiveText == null ||
            Time.time < nextRefresh)
            return;

        nextRefresh =
            Time.time + Mathf.Max(0.1f, refreshSeconds);

        int enemies =
            FindObjectsByType<EnemyLayers>(
                FindObjectsSortMode.None).Length;

        ElementSource[] sources =
            FindObjectsByType<ElementSource>(
                FindObjectsSortMode.None);

        int available = 0;

        foreach (ElementSource source in sources)
        {
            if (source != null && !source.IsDepleted)
                available++;
        }

        voidObjectiveText.text =
            "Voids Remaining: " + enemies;

        sourceObjectiveText.text = 
              "Sources Remaining: " + available;
    }
}
