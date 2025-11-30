using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 라운드 승리 처리
/// 각 라운드 씬에서 승리 시 호출하여 진행 상태 저장 및 Tournament Map으로 복귀
/// 현재 게임 모드에 따라 Story/Free Mode Tournament Map으로 이동
/// </summary>
public class RoundVictoryHandler : MonoBehaviour
{
    [Header("Tournament Map Scenes")]
    public string storyModeTournamentScene = "StoryModeTournamentMap"; // Story Mode Tournament Map 씬 이름
    public string freeModeTournamentScene = "FreeModeTournamentMap"; // Free Mode Tournament Map 씬 이름

    /// <summary>
    /// 라운드 승리 시 호출
    /// </summary>
    public void OnVictory()
    {
        // 현재 라운드 인덱스 가져오기 (TournamentMapManager에서 저장한 값)
        int roundIndex = PlayerPrefs.GetInt("SelectedRound", 0);

        Debug.Log($"[Victory] Round {roundIndex + 1} 클리어!");

        // GameModeManager를 통해 라운드 클리어 처리
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.ClearRound(roundIndex);
        }
        else
        {
            Debug.LogError("[Victory] GameModeManager를 찾을 수 없습니다!");
        }

        // 현재 모드에 맞는 Tournament Map으로 돌아가기
        ReturnToTournamentMap();
    }

    /// <summary>
    /// 라운드 패배 시 호출 (선택적)
    /// </summary>
    public void OnDefeat()
    {
        Debug.Log("[Defeat] 라운드 실패!");

        // 패배 시에도 Tournament Map으로 돌아가거나, 재시도 UI 표시
        // 여기서는 일단 Tournament Map으로 돌아가기
        ReturnToTournamentMap();
    }

    /// <summary>
    /// Tournament Map으로 직접 돌아가기 (중도 포기 등)
    /// 현재 게임 모드에 따라 적절한 Tournament Map 씬으로 이동
    /// </summary>
    public void ReturnToTournamentMap()
    {
        string sceneName = GetTournamentMapSceneName();
        Debug.Log($"[Return] {sceneName}으로 복귀");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 현재 게임 모드에 맞는 Tournament Map 씬 이름 반환
    /// </summary>
    string GetTournamentMapSceneName()
    {
        if (GameModeManager.Instance != null)
        {
            if (GameModeManager.Instance.currentMode == GameMode.StoryMode)
            {
                return storyModeTournamentScene;
            }
            else
            {
                return freeModeTournamentScene;
            }
        }
        else
        {
            Debug.LogWarning("[Victory] GameModeManager를 찾을 수 없습니다. Story Mode로 기본 설정합니다.");
            return storyModeTournamentScene;
        }
    }
}
