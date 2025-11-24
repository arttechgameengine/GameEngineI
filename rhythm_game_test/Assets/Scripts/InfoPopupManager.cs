using UnityEngine;
using UnityEngine.UI;

public class InfoPopupManager : MonoBehaviour
{
    [Header("UI References")]
    public Button infoButton;
    public GameObject infoPopup;
    public Button closeButton;

    private bool wasPlayingBeforeInfo = false;

    void Start()
    {
        if (infoButton != null)
            infoButton.onClick.AddListener(ShowInfo);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInfo);

        // 시작 시 팝업 숨기기
        if (infoPopup != null)
            infoPopup.SetActive(false);
    }

    public void ShowInfo()
    {
        // 이미 일시정지 상태였는지 기록
        wasPlayingBeforeInfo = !PauseManager.IsPaused;

        // 일시정지 (overlay 없이 게임만 멈춤)
        if (PauseManager.Instance != null && !PauseManager.IsPaused)
        {
            PauseManager.Instance.Pause(showOverlay: false);
        }

        // 팝업 표시
        if (infoPopup != null)
            infoPopup.SetActive(true);
    }

    public void CloseInfo()
    {
        // 팝업 숨기기
        if (infoPopup != null)
            infoPopup.SetActive(false);

        // Info 열기 전에 플레이 중이었으면 재개
        // IsPaused 체크 없이 Resume 호출 (Info로 인한 pause 해제)
        if (wasPlayingBeforeInfo && PauseManager.Instance != null && PauseManager.IsPaused)
        {
            PauseManager.Instance.Resume();
        }

        Debug.Log($"[CloseInfo] wasPlayingBeforeInfo: {wasPlayingBeforeInfo}, IsPaused: {PauseManager.IsPaused}");
    }
}
