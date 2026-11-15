
using UnityEngine;
using TMPro;

/// <summary>
/// Displays remaining enemies and elemental resources.
/// </summary>
public class ForestObjectiveHUD : MonoBehaviour
{
    public TMP_Text objectiveText;
    public float refreshSeconds = 0.3f;

    private float nextRefresh;

    private void Update()
    {
        if (objectiveText == null ||
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

        objectiveText.text =
            "VOIDS REMAINING: " + enemies +
            "\nSOURCES REMAINING: " + available;
    }
}
