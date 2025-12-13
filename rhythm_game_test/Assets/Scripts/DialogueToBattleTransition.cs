using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 기존 DialogueManager와 BattleDialogueManager를 연결하는 브릿지 스크립트
///
/// 사용 방법:
/// 1. 마지막 대화가 있는 씬에 빈 GameObject를 만들고 이 스크립트 추가
/// 2. Inspector에서 battleDialogueSceneName 설정 (예: "BattleDialogueScene")
/// 3. DialogueManager의 onDialogueEnd 이벤트에 이 스크립트의 OnDialogueEnd() 메서드 연결
///
/// 또는:
/// Unity Inspector에서 직접 연결:
/// DialogueManager → onDialogueEnd → + 버튼 클릭 →
/// 이 GameObject 드래그 → DialogueToBattleTransition.OnDialogueEnd 선택
/// </summary>
public class DialogueToBattleTransition : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("전환할 전투 대화 씬 이름 (BattleDialogueManager가 있는 씬)")]
    public string battleDialogueSceneName = "BattleDialogueScene";

    [Tooltip("Fade 전환 사용 여부")]
    public bool useFadeTransition = true;

    /// <summary>
    /// DialogueManager의 onDialogueEnd 이벤트에서 호출될 메서드
    /// </summary>
    public void OnDialogueEnd()
    {
        Debug.Log($"[DialogueToBattleTransition] Dialogue ended! Transitioning to {battleDialogueSceneName}...");
        TransitionToBattleDialogue();
    }

    void TransitionToBattleDialogue()
    {
        if (useFadeTransition && SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeToScene(battleDialogueSceneName);
        }
        else
        {
            SceneManager.LoadScene(battleDialogueSceneName);
        }
    }
}
