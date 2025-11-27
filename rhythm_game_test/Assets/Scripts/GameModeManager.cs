using UnityEngine;

/// <summary>
/// 게임 모드 관리
/// </summary>
public enum GameMode
{
    StoryMode,  // 스토리 모드 (순차 진행)
    FreeMode    // 자유 모드 (자유 선택)
}

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    [Header("Game Mode")]
    public GameMode currentMode = GameMode.StoryMode;

    [Header("Round Data")]
    public RoundData[] allRounds = new RoundData[5]; // 총 5개 라운드

    [Header("Progress")]
    public int currentStoryRound = 0; // Story Mode에서 현재 진행 중인 라운드 (0~4)
    public bool[] roundCleared = new bool[5]; // 각 라운드 클리어 여부

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 게임 모드 설정
    /// </summary>
    public void SetGameMode(GameMode mode)
    {
        currentMode = mode;
        Debug.Log($"[GameMode] {mode} 선택됨!");
    }

    /// <summary>
    /// 라운드가 잠겨있는지 확인
    /// </summary>
    public bool IsRoundLocked(int roundIndex)
    {
        // Free Mode에서는 모든 라운드 열림
        if (currentMode == GameMode.FreeMode)
            return false;

        // Story Mode에서는 현재 라운드까지만 열림
        return roundIndex > currentStoryRound;
    }

    /// <summary>
    /// 라운드 클리어 처리
    /// </summary>
    public void ClearRound(int roundIndex)
    {
        if (roundIndex < 0 || roundIndex >= 5) return;

        roundCleared[roundIndex] = true;

        // Story Mode에서 다음 라운드 해금
        if (currentMode == GameMode.StoryMode && roundIndex == currentStoryRound)
        {
            currentStoryRound = Mathf.Min(currentStoryRound + 1, 4);
            Debug.Log($"[GameMode] Round {roundIndex + 1} 클리어! 다음 라운드 해금: {currentStoryRound + 1}");
        }

        SaveProgress();
    }

    /// <summary>
    /// 진행 상태 저장
    /// </summary>
    public void SaveProgress()
    {
        PlayerPrefs.SetInt("CurrentStoryRound", currentStoryRound);
        for (int i = 0; i < 5; i++)
        {
            PlayerPrefs.SetInt($"RoundCleared_{i}", roundCleared[i] ? 1 : 0);
        }
        PlayerPrefs.Save();
        Debug.Log("[GameMode] 진행 상태 저장됨!");
    }

    /// <summary>
    /// 진행 상태 불러오기
    /// </summary>
    public void LoadProgress()
    {
        currentStoryRound = PlayerPrefs.GetInt("CurrentStoryRound", 0);
        for (int i = 0; i < 5; i++)
        {
            roundCleared[i] = PlayerPrefs.GetInt($"RoundCleared_{i}", 0) == 1;
        }
        Debug.Log($"[GameMode] 진행 상태 불러옴! 현재 라운드: {currentStoryRound + 1}");
    }

    /// <summary>
    /// 진행 상태 초기화 (디버그용)
    /// </summary>
    public void ResetProgress()
    {
        currentStoryRound = 0;
        for (int i = 0; i < 5; i++)
        {
            roundCleared[i] = false;
        }
        SaveProgress();
        Debug.Log("[GameMode] 진행 상태 초기화됨!");
    }
}
