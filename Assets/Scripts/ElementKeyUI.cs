using System.Collections.Generic;
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
    public TMP_Text pageLabel;
    [Range(1, 8)] public int entriesPerPage = 4;
    bool open;
    int page;
    List<string> entries = new List<string>();
    void Start() { if (dictionaryPanel != null) dictionaryPanel.SetActive(false); }
    void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.tabKey.wasPressedThisFrame) Toggle();
        if (!open) return;
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame) NextPage();
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame) PreviousPage();
    }
    public void Toggle()
    {
        open = !open;
        if (dictionaryPanel != null) dictionaryPanel.SetActive(open);
        Time.timeScale = open ? 0f : 1f;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
        if (open) { BuildEntries(); page = 0; Refresh(); }
    }
    void BuildEntries()
    {
        entries.Clear();
        if (player == null || database == null) return;
        foreach (ElementData e in database.Elements)
        {
            if (e == null) continue;
            entries.Add(player.HasDiscovered(e)
              ? $"{e.id} · {e.combatClass}\nSkill: {e.skillName}\nUltimate: {e.ultimateName}\n{e.description}"
              : "??? · NOT DISCOVERED");
        }
        if (database.recipes != null)
            foreach (ElementRecipe r in database.recipes)
            {
                if (r == null || r.result == null || r.ingredientA == null || r.ingredientB == null) continue;
                entries.Add(player.HasDiscovered(r.result)
                  ? $"FUSION: {r.ingredientA.id} + {r.ingredientB.id} = {r.result.id}"
                  : "FUSION: ??? + ??? = ???");
            }
    }
    void Refresh()
    {
        if (dictionaryText == null) return;
        int max = Mathf.Max(1, Mathf.CeilToInt(entries.Count / (float)entriesPerPage));
        page = Mathf.Clamp(page, 0, max - 1);
        StringBuilder s = new StringBuilder("ELEMENTAL DICTIONARY\n\n");
        int first = page * entriesPerPage;
        for (int i = first; i < Mathf.Min(entries.Count, first + entriesPerPage); i++)
            s.AppendLine(entries[i] + "\n");
        dictionaryText.text = s.ToString();
        if (pageLabel != null) pageLabel.text = $"PAGE {page + 1} / {max}";
    }
    public void NextPage() { page++; Refresh(); }
    public void PreviousPage() { page--; Refresh(); }
    void OnDisable()
    {
        if (!open) return;
        open = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
