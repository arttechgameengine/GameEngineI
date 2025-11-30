using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // 씬 이름으로 로드
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    
    // 씬 인덱스로 로드
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
    
    // 현재 씬 재시작
    public void RestartCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
    
    public void LoadSceneWithFade(string sceneName)
    {
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeToScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
    
    /// <summary>
    /// 토너먼트 맵으로 복귀 (모드에 따라 자동 선택)
    /// Story Mode → TournamentMap, Free Mode → FreeMapScene
    /// </summary>
    public void LoadTournamentMap()
    {
        if (GameModeManager.Instance != null)
        {
            if (GameModeManager.Instance.currentMode == GameMode.StoryMode)
            {
                SceneManager.LoadScene("TournamentMap");
            }
            else if (GameModeManager.Instance.currentMode == GameMode.FreeMode)
            {
                SceneManager.LoadScene("FreeMapScene");
            }
            else
            {
                SceneManager.LoadScene("GameStart");
            }
        }
        else
        {
            Debug.LogWarning("[SceneLoader] GameModeManager.Instance가 null입니다. GameStart로 이동합니다.");
            SceneManager.LoadScene("GameStart");
        }
    }

    // 게임 종료
    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}