using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ScoreResultUI : MonoBehaviour
{
    [Header("Rank Display")]
    public TextMeshProUGUI rankText;

    [Header("Stats Display")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI maxComboText;
    public TextMeshProUGUI accuracyText;

    [Header("Judge Counts")]
    public TextMeshProUGUI perfectText;
    public TextMeshProUGUI greatText;
    public TextMeshProUGUI goodText;
    public TextMeshProUGUI missText;
    public TextMeshProUGUI parryText;  // 패링 개수 표시 (옵셔널)

    [Header("Parry Display")]
    public bool showParryCount = false;  // 패링 개수 표시 여부 (Inspector에서 설정)
    public GameObject parryLabel;  // "Parry:" 레이블 오브젝트

    [Header("Buttons")]
    public Button retryButton;
    public Button backToTournamentButton;  // 토너먼트 맵으로 복귀 (모드에 따라 자동 선택)
    public Button exitToMenuButton;        // 메인 메뉴(GameStart)로 복귀

    [Header("Pass/Fail")]
    public TextMeshProUGUI resultText;           // "CLEAR" 또는 "FAILED" 표시
    public string passingRank = "C";             // 이 등급 이상이면 성공
    public Color passColor = new Color(0.4f, 1f, 0.4f);   // 성공 색상 (초록)
    public Color failColor = new Color(1f, 0.4f, 0.4f);   // 실패 색상 (빨강)

    [Header("Rank Colors")]
    public Color rankS = new Color(1f, 0.84f, 0f);      // Gold
    public Color rankA = new Color(0.6f, 0.8f, 0.6f);   // Light Green
    public Color rankB = new Color(0.5f, 0.7f, 1f);     // Light Blue
    public Color rankC = new Color(0.8f, 0.6f, 0.8f);   // Light Purple
    public Color rankD = new Color(0.8f, 0.8f, 0.8f);   // Gray
    public Color rankF = new Color(1f, 0.4f, 0.4f);     // Red

    void Start()
    {
        DisplayResults();

        // Story Mode: 클리어 판정 및 라운드 해금 처리
        ProcessStoryModeClear();

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetry);

        if (backToTournamentButton != null)
            backToTournamentButton.onClick.AddListener(OnBackToTournament);

        if (exitToMenuButton != null)
            exitToMenuButton.onClick.AddListener(OnExitToMenu);
    }

    void DisplayResults()
    {
        // 등급 표시
        string rank = GameResultData.GetRank();
        if (rankText != null)
        {
            rankText.text = rank;
            rankText.color = GetRankColor(rank);
        }

        // 점수 표시
        if (scoreText != null)
            scoreText.text = $"{GameResultData.Score:N0}";

        // 최대 콤보 표시
        if (maxComboText != null)
            maxComboText.text = $"{GameResultData.MaxCombo}";

        // 정확도 표시
        if (accuracyText != null)
            accuracyText.text = $"{GameResultData.Accuracy:F1}%";

        // 판정별 개수 표시
        if (perfectText != null)
            perfectText.text = $"{GameResultData.PerfectCount}";

        if (greatText != null)
            greatText.text = $"{GameResultData.GreatCount}";

        if (goodText != null)
            goodText.text = $"{GameResultData.GoodCount}";

        if (missText != null)
            missText.text = $"{GameResultData.MissCount}";

        // 패링 개수 표시 (옵션이 켜져있을 때만)
        if (showParryCount)
        {
            if (parryText != null)
            {
                parryText.text = $"{GameResultData.ParryCount}";
                parryText.gameObject.SetActive(true);
            }
            if (parryLabel != null)
            {
                parryLabel.SetActive(true);
            }
        }
        else
        {
            if (parryText != null)
            {
                parryText.gameObject.SetActive(false);
            }
            if (parryLabel != null)
            {
                parryLabel.SetActive(false);
            }
        }

        // 성공/실패 표시
        if (resultText != null)
        {
            bool passed = IsPassingRank(rank);
            resultText.text = passed ? "CLEAR" : "FAILED";
            resultText.color = passed ? passColor : failColor;
        }
    }

    // 등급이 통과 기준 이상인지 확인
    bool IsPassingRank(string rank)
    {
        int rankValue = GetRankValue(rank);
        int passingValue = GetRankValue(passingRank);
        return rankValue >= passingValue;
    }

    // 등급을 숫자로 변환 (S=6, A=5, B=4, C=3, D=2, F=1)
    int GetRankValue(string rank)
    {
        switch (rank)
        {
            case "S": return 6;
            case "A": return 5;
            case "B": return 4;
            case "C": return 3;
            case "D": return 2;
            case "F": return 1;
            default: return 0;
        }
    }

    Color GetRankColor(string rank)
    {
        switch (rank)
        {
            case "S": return rankS;
            case "A": return rankA;
            case "B": return rankB;
            case "C": return rankC;
            case "D": return rankD;
            default: return rankF;
        }
    }

    void OnRetry()
    {
        // RhythmTest 씬으로 다시 이동
        SceneManager.LoadScene("RhythmTest");
    }

    /// <summary>
    /// 토너먼트 맵으로 복귀 (모드에 따라 자동 선택)
    /// </summary>
    void OnBackToTournament()
    {
        if (GameModeManager.Instance != null)
        {
            if (GameModeManager.Instance.currentMode == GameMode.StoryMode)
            {
                Debug.Log("[ScoreResultUI] Story Mode → StoryMapScene으로 복귀");
                SceneManager.LoadScene("StoryMapScene");
            }
            else if (GameModeManager.Instance.currentMode == GameMode.FreeMode)
            {
                Debug.Log("[ScoreResultUI] Free Mode → FreeMapScene으로 복귀");
                SceneManager.LoadScene("FreeMapScene");
            }
            else
            {
                Debug.LogWarning("[ScoreResultUI] 알 수 없는 모드. GameStart로 이동합니다.");
                SceneManager.LoadScene("GameStart");
            }
        }
        else
        {
            Debug.LogWarning("[ScoreResultUI] GameModeManager.Instance가 null입니다. GameStart로 이동합니다.");
            SceneManager.LoadScene("GameStart");
        }
    }

    /// <summary>
    /// 메인 메뉴(GameStart)로 복귀
    /// </summary>
    void OnExitToMenu()
    {
        Debug.Log("[ScoreResultUI] GameStart로 이동");
        SceneManager.LoadScene("GameStart");
    }

    /// <summary>
    /// Story Mode 클리어 판정 및 라운드 해금 처리
    /// </summary>
    void ProcessStoryModeClear()
    {
        // Story Mode가 아니면 처리 안 함
        if (GameModeManager.Instance == null || GameModeManager.Instance.currentMode != GameMode.StoryMode)
        {
            return;
        }

        // 현재 플레이한 라운드 가져오기 (PlayerPrefs에서)
        int currentRoundIndex = PlayerPrefs.GetInt("SelectedRound", 0);

        // 등급 가져오기
        string rank = GameResultData.GetRank();

        // 클리어 판정 (passingRank 이상이면 클리어)
        bool cleared = IsPassingRank(rank);

        if (cleared)
        {
            Debug.Log($"[ScoreResultUI] Story Mode Round {currentRoundIndex + 1} 클리어! (등급: {rank})");

            // GameModeManager에 클리어 정보 저장
            GameModeManager.Instance.ClearRound(currentRoundIndex);

            Debug.Log($"[ScoreResultUI] 다음 라운드 해금됨! 현재 진행: {GameModeManager.Instance.currentStoryRound + 1}/5");
        }
        else
        {
            Debug.Log($"[ScoreResultUI] Story Mode Round {currentRoundIndex + 1} 실패... (등급: {rank}, 필요: {passingRank} 이상)");
        }
    }
}
