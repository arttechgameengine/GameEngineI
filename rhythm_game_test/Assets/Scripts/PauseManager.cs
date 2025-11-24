using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    public static bool IsPaused { get; private set; } = false;

    [Header("UI References")]
    public Button pauseButton;
    public GameObject pauseOverlay;
    public Button resumeButton;
    public Button retryButton;
    public Button exitButton;

    [Header("Audio")]
    public AudioSource bgmSource;

    [Header("References")]
    public NoteSpawner noteSpawner;

    // 일시정지 시점의 DSP 시간 저장
    private double pausedDspTime;

    void Awake()
    {
        Instance = this;
        IsPaused = false;
    }

    void Start()
    {
        // 버튼 이벤트 연결
        if (pauseButton != null)
            pauseButton.onClick.AddListener(Pause);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (retryButton != null)
            retryButton.onClick.AddListener(Retry);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitToGameStart);

        // 시작 시 오버레이 숨기기
        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);
    }

    void Update()
    {
        // ESC 키로도 일시정지 토글 가능
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        Pause(showOverlay: true);
    }

    // showOverlay: false면 게임만 멈추고 pause overlay는 안 띄움
    public void Pause(bool showOverlay)
    {
        if (IsPaused) return;

        IsPaused = true;

        // 일시정지 시점의 DSP 시간 기록
        pausedDspTime = AudioSettings.dspTime;

        // 음악 일시정지
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Pause();
        }

        // 오버레이 표시 (옵션)
        if (showOverlay)
        {
            if (pauseOverlay != null)
                pauseOverlay.SetActive(true);

            // 일시정지 버튼 숨기기
            if (pauseButton != null)
                pauseButton.gameObject.SetActive(false);
        }

        // 게임 시간 정지
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        Debug.Log($"[Resume] Called. IsPaused: {IsPaused}");

        if (!IsPaused) return;

        // 일시정지 동안 흐른 DSP 시간을 songStartDspTime에 보정
        if (noteSpawner != null)
        {
            double pausedDuration = AudioSettings.dspTime - pausedDspTime;
            noteSpawner.songStartDspTime += pausedDuration;
            Debug.Log($"[Resume] pausedDuration: {pausedDuration:F2}");
        }

        IsPaused = false;

        // 게임 시간 재개
        Time.timeScale = 1f;

        // 음악 재개
        if (bgmSource != null)
        {
            bgmSource.UnPause();
        }

        // 오버레이 숨기기
        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);

        // 일시정지 버튼 다시 표시
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(true);

        Debug.Log("[Resume] Done");
    }

    public void Retry()
    {
        // 게임 시간 복원 후 현재 씬 재시작
        Time.timeScale = 1f;
        IsPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitToGameStart()
    {
        // 게임 시간 복원 후 씬 이동
        Time.timeScale = 1f;
        IsPaused = false;
        SceneManager.LoadScene("GameStart");
    }

    void OnDestroy()
    {
        // 씬 전환 시 상태 초기화
        IsPaused = false;
        Time.timeScale = 1f;
    }
}
