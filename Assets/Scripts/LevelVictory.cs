using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelVictory : MonoBehaviour
{
    public GameObject victoryPanel;
    public float checkEverySeconds = 0.5f;
    private float nextCheck;
    private bool finished;

    private void Start()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        nextCheck = Time.time + 1f;
    }

    private void Update()
    {
        if (finished || Time.time < nextCheck) return;
        nextCheck = Time.time + checkEverySeconds;
        if (FindObjectsByType<EnemyLayers>(FindObjectsSortMode.None).Length == 0)
        {
            finished = true;
            if (victoryPanel != null) victoryPanel.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void LoadLevel(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
