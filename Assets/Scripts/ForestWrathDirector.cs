using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Forest-level authority for timer, resource loss, Demeter's Wrath,
/// victory, and defeat. Do not leave a LevelVictory component active
/// in the Forest scene alongside this director.
/// </summary>
public class ForestWrathDirector : MonoBehaviour
{
    [Header("Clock")]
    [Min(1f)] public float levelSeconds = 360f;
    [Min(0f)] public float wrathAtSecondsRemaining = 60f;
    [Tooltip("When checked, reaching the nature/earth threshold jumps the clock into its last-minute phase.")]
    public bool earlyWrathStartsFinalMinute = true;

    [Header("Environmental Balance")]
    [Min(1)] public int natureOrEarthDepletionThreshold = 3;
    [Tooltip("If checked, ALL finite elemental sources must be depleted to cause resource defeat, regardless of Counts For Wrath.")]
    public bool lossWhenAllSourcesDepleted = true;

    [Header("HUD and End Panels")]
    public TMP_Text timerText;
    public TMP_Text warningText;
    public TMP_Text remainingEnemiesText;
    public TMP_Text remainingSourcesText;
    public TMP_Text wrathProgressText;
    public TMP_Text resultText;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public bool pauseOnFinish = true;

    [Header("Demeter Flower Hazards")]
    public GameObject flowerHazardPrefab;
    public Transform[] hazardSpawnPoints;
    [Min(0.4f)] public float flowerSpawnEvery = 6f;
    [Min(0f)] public float firstFlowerDelay = 1.5f;
    public bool removeActiveFlowersOnFinish = true;

    public bool WrathActive { get; private set; }
    public bool Finished { get; private set; }
    public float SecondsRemaining { get; private set; }
    public int NatureEarthDepleted { get; private set; }
    public int TotalSources => sources.Count;
    public int RemainingSources => CountRemainingSources();
    public int RemainingEnemies { get; private set; }

    public event Action WrathBegan;
    public event Action<string> GameLost;
    public event Action GameWon;

    private readonly List<ElementSource> sources = new List<ElementSource>();
    private readonly HashSet<ElementSource> countedDepletions = new HashSet<ElementSource>();
    private readonly List<GameObject> spawnedFlowers = new List<GameObject>();
    private readonly List<PlayerDurability> players = new List<PlayerDurability>();

    private Coroutine spawning;
    private Coroutine warningRoutine;
    private float nextEnemyCheck;
    private bool foundEnemiesAtStart;
    private bool warnedTwoMinutes;
    private bool warnedThirtySeconds;
    private bool warnedFinalMinute;

    private void Start()
    {
        Time.timeScale = 1f;
        SecondsRemaining = levelSeconds;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (warningText != null) warningText.text = "";

        foreach (ElementSource source in
                 FindObjectsByType<ElementSource>(FindObjectsSortMode.None))
        {
            sources.Add(source);
            source.Depleted += OnSourceDepleted;
            if (source.IsDepleted) RegisterDepletion(source);
        }

        foreach (PlayerDurability player in
                 FindObjectsByType<PlayerDurability>(FindObjectsSortMode.None))
        {
            players.Add(player);
            player.PlayerDefeated += OnPlayerDefeated;
        }

        RemainingEnemies = CountAliveEnemies();
        foundEnemiesAtStart = RemainingEnemies > 0;
        if (!foundEnemiesAtStart)
            Debug.LogWarning("ForestWrathDirector found no active EnemyLayers at Start. Victory won't trigger until at least one Void has been present.", this);
        if (sources.Count == 0)
            Debug.LogWarning("ForestWrathDirector found no ElementSource objects. Resource-loss condition is disabled until sources are set up.", this);

        RefreshHud();
    }

    private void Update()
    {
        if (Finished) return;

        SecondsRemaining = Mathf.Max(0f, SecondsRemaining - Time.deltaTime);

        // Events handle normal source depletion immediately. This secondary
        // scan catches pre-depleted sources and unexpected state changes.
        foreach (ElementSource source in sources)
            if (source != null && source.IsDepleted)
                RegisterDepletion(source);

        // One-time clock warnings. Avoid overwriting the Wrath announcement.
        if (!warnedTwoMinutes && SecondsRemaining <= 120f &&
            levelSeconds > 120f)
        {
            warnedTwoMinutes = true;
            if (!WrathActive) ShowWarning("TWO MINUTES REMAIN!", 2f);
        }

        if (!WrathActive && SecondsRemaining <= wrathAtSecondsRemaining)
        {
            warnedFinalMinute = true;
            BeginWrath();
        }

        if (!warnedThirtySeconds && SecondsRemaining <= 30f)
        {
            warnedThirtySeconds = true;
            if (WrathActive) ShowWarning("30 SECONDS! SURVIVE DEMETER'S WRATH!", 3f);
            else ShowWarning("30 SECONDS REMAIN!", 3f);
        }

        if (SecondsRemaining <= 0f)
        {
            Lose("TIME'S UP - DEMETER'S JUDGMENT");
            return;
        }

        if (lossWhenAllSourcesDepleted && sources.Count > 0 && RemainingSources == 0)
        {
            Lose("ALL ELEMENT SOURCES EXHAUSTED");
            return;
        }

        if (Time.time >= nextEnemyCheck)
        {
            nextEnemyCheck = Time.time + 0.25f;
            RemainingEnemies = CountAliveEnemies();
            if (RemainingEnemies > 0) foundEnemiesAtStart = true;
            if (foundEnemiesAtStart && RemainingEnemies == 0)
            {
                Win();
                return;
            }
        }

        RefreshHud();
    }

    private int CountAliveEnemies()
    {
        return FindObjectsByType<EnemyLayers>(FindObjectsSortMode.None).Length;
    }

    private int CountRemainingSources()
    {
        int count = 0;
        foreach (ElementSource source in sources)
            if (source != null && !source.IsDepleted) count++;
        return count;
    }

    private void OnSourceDepleted(ElementSource source)
    {
        RegisterDepletion(source);
        if (!Finished && lossWhenAllSourcesDepleted && sources.Count > 0 && RemainingSources == 0)
            Lose("ALL ELEMENT SOURCES EXHAUSTED");
    }

    private void RegisterDepletion(ElementSource source)
    {
        if (Finished || source == null || !source.IsDepleted || !countedDepletions.Add(source))
            return;

        // Counts For Wrath affects the EARLY wrath trigger only.
        // All sources still contribute to the total-resource-loss rule.
        if (source.countsForWrath && (source.DefaultElement ?? source.grantsElement) != null &&
            ((source.DefaultElement ?? source.grantsElement).primaryFamily == ElementFamily.Nature ||
             (source.DefaultElement ?? source.grantsElement).primaryFamily == ElementFamily.Earth))
        {
            NatureEarthDepleted++;
            if (!WrathActive && NatureEarthDepleted >= natureOrEarthDepletionThreshold)
            {
                if (earlyWrathStartsFinalMinute)
                    SecondsRemaining = Mathf.Min(SecondsRemaining, wrathAtSecondsRemaining);
                BeginWrath();
            }
        }

        RefreshHud();
    }

    private void BeginWrath()
    {
        if (Finished || WrathActive) return;
        WrathActive = true;
        WrathBegan?.Invoke();
        ShowWarning("DEMETER'S WRATH!\nAvoid explosive flowers!", 4f);

        if (flowerHazardPrefab != null && hazardSpawnPoints != null && hazardSpawnPoints.Length > 0)
            spawning = StartCoroutine(SpawnFlowers());
        else
            Debug.LogWarning("Demeter's Wrath started, but the flower prefab or hazard spawn points are missing.", this);
    }

    private IEnumerator SpawnFlowers()
    {
        yield return new WaitForSeconds(firstFlowerDelay);
        while (!Finished && WrathActive)
        {
            List<Transform> validPoints = new List<Transform>();
            foreach (Transform point in hazardSpawnPoints)
                if (point != null) validPoints.Add(point);

            if (validPoints.Count > 0)
            {
                Transform point = validPoints[UnityEngine.Random.Range(0, validPoints.Count)];
                GameObject flower = Instantiate(flowerHazardPrefab, point.position, point.rotation);
                spawnedFlowers.Add(flower);
            }
            yield return new WaitForSeconds(Mathf.Max(0.4f, flowerSpawnEvery));
        }
    }

    private void ShowWarning(string message, float seconds)
    {
        if (warningText == null) return;
        warningText.text = message;
        if (warningRoutine != null) StopCoroutine(warningRoutine);
        warningRoutine = StartCoroutine(ClearWarningAfter(seconds));
    }

    private IEnumerator ClearWarningAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (!Finished && warningText != null) warningText.text = "";
        warningRoutine = null;
    }

    private void RefreshHud()
    {
        if (timerText != null)
        {
            int t = Mathf.CeilToInt(SecondsRemaining);
            timerText.text = string.Format("{0:00}:{1:00}", t / 60, t % 60);
        }
        if (remainingEnemiesText != null)
            remainingEnemiesText.text = "VOIDS: " + RemainingEnemies;
        if (remainingSourcesText != null)
            remainingSourcesText.text = "SOURCES: " + RemainingSources + "/" + TotalSources;
        if (wrathProgressText != null)
            wrathProgressText.text = WrathActive
                ? "DEMETER'S WRATH ACTIVE"
                : "NATURE / EARTH LOST: " + NatureEarthDepleted + "/" + natureOrEarthDepletionThreshold;
    }

    private void OnPlayerDefeated() { Lose("ELEMENTAL CONNECTION LOST"); }

    public void Lose(string reason)
    {
        if (Finished) return;
        Finished = true;
        StopHazards();
        if (resultText != null) resultText.text = reason;
        if (warningText != null) warningText.text = reason;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        EndPlayerControl();
        GameLost?.Invoke(reason);
    }

    public void Win()
    {
        if (Finished) return;
        Finished = true;
        RemainingEnemies = 0;
        StopHazards();
        if (resultText != null) resultText.text = "FOREST RESTORED!";
        if (warningText != null) warningText.text = "FOREST RESTORED!";
        if (victoryPanel != null) victoryPanel.SetActive(true);
        EndPlayerControl();
        GameWon?.Invoke();
    }

    private void EndPlayerControl()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (pauseOnFinish) Time.timeScale = 0f;
    }

    private void StopHazards()
    {
        if (spawning != null) { StopCoroutine(spawning); spawning = null; }
        if (warningRoutine != null) { StopCoroutine(warningRoutine); warningRoutine = null; }
        if (!removeActiveFlowersOnFinish) return;
        foreach (GameObject flower in spawnedFlowers)
            if (flower != null) Destroy(flower);
        spawnedFlowers.Clear();
    }

    // Assign to UI Buttons via On Click(), no additional menu script needed.
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadLevel(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void OnDestroy()
    {
        foreach (ElementSource source in sources)
            if (source != null) source.Depleted -= OnSourceDepleted;
        foreach (PlayerDurability player in players)
            if (player != null) player.PlayerDefeated -= OnPlayerDefeated;
        // Important if changing scenes after a frozen victory/defeat.
        if (Finished && pauseOnFinish) Time.timeScale = 1f;
    }
}
