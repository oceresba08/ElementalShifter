using System;
using System.Collections;
using UnityEngine;
using TMPro;
public class ForestWrathDirector : MonoBehaviour
{
    [Header("Clock")]
    public float levelSeconds = 360f;
    public float wrathAtSecondsRemaining = 60f;
    [Header("Environmental balance")]
    public int natureOrEarthDepletionThreshold = 3;
    public TMP_Text timerText;
    public TMP_Text warningText;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    [Header("Demeter flower hazards")]
    public GameObject flowerHazardPrefab;
    public Transform[] hazardSpawnPoints;
    public float flowerSpawnEvery = 6f;
    public bool WrathActive { get; private set; }
    public bool Finished { get; private set; }
    public float SecondsRemaining { get; private set; }
    public event Action WrathBegan;
    public event Action<string> GameLost;
    int natureEarthDepleted;
    ElementSource[] sources;
    Coroutine spawning;
    void Start()
    {
        SecondsRemaining = levelSeconds;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (warningText != null) warningText.text = "";
        sources = FindObjectsByType<ElementSource>(FindObjectsSortMode.None);
        foreach (ElementSource s in sources) if (s != null) s.Depleted += OnSourceDepleted;
        foreach (PlayerDurability p in FindObjectsByType<PlayerDurability>(FindObjectsSortMode.None))
            p.PlayerDefeated += OnPlayerDefeated;
    }
    void Update()
    {
        if (Finished) return;
        if (victoryPanel != null && victoryPanel.activeSelf)
        { Finished = true; StopHazards(); return; }
        SecondsRemaining = Mathf.Max(0, SecondsRemaining - Time.deltaTime);
        if (timerText != null)
        { int t = Mathf.CeilToInt(SecondsRemaining); timerText.text = $"{t / 60:00}:{t % 60:00}"; }
        if (!WrathActive && SecondsRemaining <= wrathAtSecondsRemaining) BeginWrath();
        if (SecondsRemaining <= 0) Lose("TIME'S UP · DEMETER'S JUDGMENT");
    }
    void OnSourceDepleted(ElementSource s)
    {
        if (Finished || !s.countsForWrath) return;
        if (s.grantsElement != null && (s.grantsElement.primaryFamily == ElementFamily.Nature || s.grantsElement.primaryFamily == ElementFamily.Earth))
            natureEarthDepleted++;
        if (!WrathActive && natureEarthDepleted >= natureOrEarthDepletionThreshold)
        {
            SecondsRemaining = Mathf.Min(SecondsRemaining, wrathAtSecondsRemaining);
            BeginWrath();
        }
        // Total depletion is a separate terminal condition regardless of family.
        bool anyRemaining = false;
        bool eligibleFound = false;
        foreach (ElementSource source in sources)
            if (source != null && source.countsForWrath)
            { eligibleFound = true; if (!source.IsDepleted) { anyRemaining = true; break; } }
        if (eligibleFound && !anyRemaining) Lose("ALL ELEMENT SOURCES EXHAUSTED");
    }
    void BeginWrath()
    {
        if (Finished || WrathActive) return;
        WrathActive = true;
        WrathBegan?.Invoke();
        if (warningText != null) warningText.text = "DEMETER'S WRATH!\nAvoid explosive flowers!";
        StartCoroutine(HideWarning());
        if (flowerHazardPrefab != null && hazardSpawnPoints != null && hazardSpawnPoints.Length > 0)
            spawning = StartCoroutine(SpawnFlowers());
    }
    IEnumerator HideWarning() { yield return new WaitForSeconds(4f); if (warningText != null) warningText.text = ""; }
    IEnumerator SpawnFlowers()
    {
        while (!Finished)
        {
            Transform point = hazardSpawnPoints[UnityEngine.Random.Range(0, hazardSpawnPoints.Length)];
            if (point != null) Instantiate(flowerHazardPrefab, point.position, Quaternion.identity);
            yield return new WaitForSeconds(Mathf.Max(.4f, flowerSpawnEvery));
        }
    }
    void OnPlayerDefeated() { Lose("ELEMENTAL CONNECTION LOST"); }
    public void Lose(string reason)
    {
        if (Finished) return;
        Finished = true;
        StopHazards();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (warningText != null) warningText.text = reason;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        GameLost?.Invoke(reason);
        Time.timeScale = 0f;
    }
    void StopHazards() { if (spawning != null) StopCoroutine(spawning); }
    void OnDestroy()
    {
        if (sources != null) foreach (ElementSource s in sources) if (s != null) s.Depleted -= OnSourceDepleted;
        foreach (PlayerDurability p in FindObjectsByType<PlayerDurability>(FindObjectsSortMode.None))
            p.PlayerDefeated -= OnPlayerDefeated;
    }
}
