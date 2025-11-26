using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI scoreText;

    [Header("Score Settings")]
    public int perfectScore = 1000;
    public int greatScore = 750;
    public int goodScore = 500;

    [Header("Stats")]
    public int currentScore = 0;
    public int currentCombo = 0;
    public int maxCombo = 0;
    public int perfectCount = 0;
    public int greatCount = 0;
    public int goodCount = 0;
    public int missCount = 0;
    public int parryCount = 0;  // 패링 성공 횟수

    void Awake()
    {
        Instance = this;
        ResetStats();
    }

    public void ResetStats()
    {
        currentScore = 0;
        currentCombo = 0;
        maxCombo = 0;
        perfectCount = 0;
        greatCount = 0;
        goodCount = 0;
        missCount = 0;
        parryCount = 0;
        UpdateComboUI();
        UpdateScoreUI();
    }

    // 패링 성공 시 호출
    public void AddParrySuccess()
    {
        parryCount++;
    }

    public void AddJudge(string judge)
    {
        switch (judge)
        {
            case "PERFECT":
                perfectCount++;
                AddScore(perfectScore);
                IncreaseCombo();
                break;
            case "GREAT":
                greatCount++;
                AddScore(greatScore);
                IncreaseCombo();
                break;
            case "GOOD":
                goodCount++;
                AddScore(goodScore);
                IncreaseCombo();
                break;
            case "MISS":
                missCount++;
                ResetCombo();
                break;
        }
    }

    void AddScore(int baseScore)
    {
        currentScore += baseScore;
        UpdateScoreUI();
    }

    void IncreaseCombo()
    {
        currentCombo++;
        if (currentCombo > maxCombo)
        {
            maxCombo = currentCombo;
        }
        UpdateComboUI();
    }

    void ResetCombo()
    {
        currentCombo = 0;
        UpdateComboUI();
    }

    void UpdateComboUI()
    {
        if (comboText != null)
        {
            if (currentCombo > 0)
            {
                comboText.text = $"{currentCombo} COMBO";
            }
            else
            {
                comboText.text = "";
            }
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore}";
        }
    }

    public int GetTotalNotes()
    {
        return perfectCount + greatCount + goodCount + missCount;
    }

    public void PrintStats()
    {
        Debug.Log($"=== Game Stats ===");
        Debug.Log($"Score: {currentScore}");
        Debug.Log($"PERFECT: {perfectCount}");
        Debug.Log($"GREAT: {greatCount}");
        Debug.Log($"GOOD: {goodCount}");
        Debug.Log($"MISS: {missCount}");
        Debug.Log($"Max Combo: {maxCombo}");
        Debug.Log($"Total Notes: {GetTotalNotes()}");
    }

    // 결과 화면으로 이동
    public void GoToResultScene()
    {
        // 결과 데이터 저장
        GameResultData.SaveFromScoreManager(this);
        // ScoreScene으로 이동
        SceneManager.LoadScene("ScoreScene");
    }
}
