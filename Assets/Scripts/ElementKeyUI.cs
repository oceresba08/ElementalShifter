using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ElementKeyUI : MonoBehaviour
{
    public PlayerElements player;
    public ElementDatabase database;
    public GameObject dictionaryPanel;
    public TMP_Text dictionaryText;
    private bool open;

    private void Start()
    {
        if (dictionaryPanel != null) dictionaryPanel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.tabKey.wasPressedThisFrame) return;
        open = !open;
        if (dictionaryPanel != null) dictionaryPanel.SetActive(open);
        Time.timeScale = open ? 0f : 1f;
        Cursor.visible = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        if (open) RefreshText();
    }

    private void RefreshText()
    {
        if (dictionaryText == null || player == null || database == null) return;
        StringBuilder s = new StringBuilder("ELEMENTAL DICTIONARY\n\n");
        foreach (ElementData data in database.Elements)
        {
            if (data == null) continue;
            if (player.HasDiscovered(data))
                s.AppendLine(data.id + " | " + data.skillName + " | " + data.description);
            else
                s.AppendLine("??? | NOT DISCOVERED");
        }
        s.AppendLine("\nFUSION RECIPES\n");
        if (database.recipes != null)
        {
            foreach (ElementRecipe r in database.recipes)
            {
                if (r == null || r.result == null || r.ingredientA == null || r.ingredientB == null) continue;
                if (player.HasDiscovered(r.result))
                    s.AppendLine(r.ingredientA.id + " + " + r.ingredientB.id + " = " + r.result.id);
                else
                    s.AppendLine("??? + ??? = ???");
            }
        }
        dictionaryText.text = s.ToString();
    }

    private void OnDisable()
    {
        if (open)
        {
            open = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
