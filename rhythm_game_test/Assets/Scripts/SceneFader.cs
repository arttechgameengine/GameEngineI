using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance;  // 싱글톤 패턴

    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;               // 페이드용 이미지
    [SerializeField] private float fadeOutDuration = 0.5f;  // Fade Out 시간 (초)
    [SerializeField] private float fadeInDuration = 1.5f;   // Fade In 시간 (초)
    [SerializeField] private Color fadeColor = Color.black; // 페이드 색상

    [Header("Auto Fade In")]
    [SerializeField] private bool autoFadeInOnStart = true; // 씬 시작 시 자동 Fade In

    private bool isFading = false;
    private Canvas canvas;
    private bool isFirstScene = true;  // 첫 씬 로드 여부 추적

    // ⚠️ 게임 시작 시 한 번 계산된 프레임 수 (기기 FPS 기반)
    private int calculatedFadeOutFrames = -1;
    private int calculatedFadeInFrames = -1;
    private bool framesCalculated = false;

    // ⚠️ Fade In이 완료되었는지 여부 (게임 로직 시작 허용)
    public static bool IsFadeInComplete { get; private set; } = false;

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Canvas 설정 (자식에서 찾기)
            canvas = GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 9999; // 최상위에 표시
            }
            else
            {
                Debug.LogWarning("[SceneFader] Canvas를 찾을 수 없습니다. 자식 오브젝트에 Canvas를 추가해주세요.");
            }

            // ⚠️ 핵심: Awake에서 fadeImage를 검은색으로 초기화
            // 이렇게 하면 씬 전환 시 항상 검은 화면에서 시작
            if (fadeImage != null)
            {
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        // OnEnable은 매 씬마다 호출됨 (DontDestroyOnLoad 오브젝트도)
        // Scene 로드 이벤트 구독 (여기서 하면 씬 로드 전에 구독됨)
        SceneManager.sceneLoaded -= OnSceneLoaded;  // 중복 방지
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    void Start()
    {
        if (fadeImage == null)
        {
            Debug.LogError("[SceneFader] fadeImage가 설정되지 않았습니다!");
            return;
        }

        // ⚠️ 게임 시작 시 한 번만 FPS 측정 및 프레임 수 계산
        if (!framesCalculated)
        {
            StartCoroutine(CalculateFPSAndFrames());
        }

        // 첫 씬만 바로 투명으로 변경 (Awake에서 검은색으로 초기화됨)
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

        // Canvas를 즉시 활성화
        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
        }

        isFading = false;

        // ⚠️ 첫 씬은 fade in 없으므로 바로 완료 상태로 설정
        IsFadeInComplete = true;

        Debug.Log("[SceneFader] First scene - No fade, starting transparent");
    }

    // FPS 측정 및 프레임 수 계산 (게임 시작 시 한 번만 실행)
    IEnumerator CalculateFPSAndFrames()
    {
        // 몇 프레임 대기 (안정화)
        for (int i = 0; i < 10; i++)
        {
            yield return null;
        }

        // FPS 측정 (평균 30프레임)
        float totalDeltaTime = 0f;
        int sampleCount = 30;

        for (int i = 0; i < sampleCount; i++)
        {
            totalDeltaTime += Time.unscaledDeltaTime;
            yield return null;
        }

        float averageDeltaTime = totalDeltaTime / sampleCount;
        float measuredFPS = 1f / averageDeltaTime;

        // Duration을 프레임 수로 변환
        calculatedFadeOutFrames = Mathf.RoundToInt(fadeOutDuration * measuredFPS);
        calculatedFadeInFrames = Mathf.RoundToInt(fadeInDuration * measuredFPS);

        framesCalculated = true;

        Debug.LogWarning($"[SceneFader] FPS Calculated: {measuredFPS:F1} fps");
        Debug.LogWarning($"[SceneFader] Fade Out: {fadeOutDuration}s = {calculatedFadeOutFrames} frames");
        Debug.LogWarning($"[SceneFader] Fade In: {fadeInDuration}s = {calculatedFadeInFrames} frames");
        Debug.LogWarning($"[SceneFader] These frame counts will be used for ALL scene transitions");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SceneFader] OnSceneLoaded: {scene.name}, isFirstScene: {isFirstScene}");

        // 첫 번째 씬(GameStart)은 Start()에서 투명하게 설정하므로 fade in 안 함
        if (isFirstScene)
        {
            isFirstScene = false;
            Debug.Log("[SceneFader] First scene (GameStart) - Skip fade in, Start() will set transparent");
            return;
        }

        // ⚠️ 새 씬이 로드되면 fade in 미완료 상태로 리셋
        IsFadeInComplete = false;

        // ⚠️ 첫 씬 이후 모든 씬 전환에서는 Fade In 실행
        if (autoFadeInOnStart)
        {
            Debug.Log($"[SceneFader] Scene transition detected → Starting fade in for {scene.name}");
            StartCoroutine(OnSceneLoadedFadeIn());
        }
    }

    IEnumerator OnSceneLoadedFadeIn()
    {
        if (fadeImage == null)
        {
            Debug.LogError("[SceneFader] fadeImage is NULL in OnSceneLoadedFadeIn!");
            yield break;
        }

        if (canvas == null)
        {
            Debug.LogError("[SceneFader] canvas is NULL in OnSceneLoadedFadeIn!");
            yield break;
        }

        Debug.LogWarning($"[SceneFader] ===== OnSceneLoadedFadeIn START =====");

        // Canvas를 최상위로 강제 설정
        canvas.sortingOrder = 9999;
        canvas.gameObject.SetActive(true);

        // ⚠️ Time.timeScale을 0으로 설정 → 게임 로직 일시정지
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        Debug.LogWarning("[SceneFader] Time.timeScale set to 0 - Game logic paused");

        // 확실하게 검은 화면 상태로 설정 (Awake에서 이미 설정되어있지만 재확인)
        Color color = fadeImage.color;
        color.a = 1f;
        fadeImage.color = color;

        Debug.LogWarning($"[SceneFader] FadeImage alpha set to 1.0 (black screen)");

        // isFading 플래그 리셋
        isFading = false;

        // ⚠️ yield return null 없이 바로 Fade In 시작
        // 이렇게 하면 OnSceneLoaded가 호출되는 프레임부터 바로 fade 시작
        Debug.LogWarning("[SceneFader] Starting FadeIn immediately...");

        yield return StartCoroutine(FadeInImmediate(originalTimeScale));

        Debug.LogWarning("[SceneFader] ===== OnSceneLoadedFadeIn COMPLETE =====");
    }

    // 즉시 Fade In 수행 (OnSceneLoaded용) - 계산된 프레임 기반
    IEnumerator FadeInImmediate(float originalTimeScale)
    {
        if (fadeImage == null) yield break;

        // ⚠️ FPS 계산이 완료될 때까지 대기
        while (!framesCalculated)
        {
            yield return null;
        }

        int frames = calculatedFadeInFrames;
        Debug.LogWarning($"[SceneFader] ===== FadeIn STARTED ===== Total frames: {frames}");

        isFading = true;
        Color color = fadeImage.color;
        color.a = 1f;  // 확실하게 검은색부터 시작
        fadeImage.color = color;

        Debug.LogWarning($"[SceneFader] FadeIn initial alpha: {color.a}");

        // 계산된 프레임 수로 fade
        for (int frame = 0; frame <= frames; frame++)
        {
            float t = (float)frame / frames;
            color.a = Mathf.Lerp(1f, 0f, t);
            fadeImage.color = color;

            if (frame % 10 == 0)
            {
                Debug.LogWarning($"[SceneFader] FadeIn frame {frame}/{frames}: alpha={color.a:F3}");
            }

            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;
        isFading = false;

        // Fade In 완료 후 Time.timeScale 복원
        Time.timeScale = originalTimeScale;

        // ⚠️ Fade In 완료 상태로 설정 → 게임 로직 시작 허용
        IsFadeInComplete = true;

        Debug.LogWarning($"[SceneFader] ===== FadeIn COMPLETED ===== Final alpha: 0, Total frames: {frames}");
        Debug.LogWarning($"[SceneFader] Time.timeScale restored to {originalTimeScale}");
        Debug.LogWarning($"[SceneFader] IsFadeInComplete = true → Game logic can now start");
    }

    /// <summary>
    /// Fade Out 후 씬 전환 (외부에서 호출)
    /// </summary>
    public void FadeToScene(string sceneName)
    {
        // 이미 fade 중이면 중복 방지
        if (isFading)
        {
            Debug.LogWarning($"[SceneFader] Already fading! Ignoring FadeToScene({sceneName})");
            return;
        }

        Debug.Log($"[SceneFader] FadeToScene called: {sceneName}");
        StartCoroutine(FadeOutAndLoadScene(sceneName));
    }

    /// <summary>
    /// Fade Out 후 씬 전환 (커스텀 페이드 시간)
    /// </summary>
    public void FadeToScene(string sceneName, float customFadeDuration)
    {
        // 이미 fade 중이면 중복 방지
        if (isFading)
        {
            Debug.LogWarning($"[SceneFader] Already fading! Ignoring FadeToScene({sceneName}, {customFadeDuration})");
            return;
        }

        Debug.Log($"[SceneFader] FadeToScene (custom duration) called: {sceneName}");
        StartCoroutine(FadeOutAndLoadScene(sceneName, customFadeDuration));
    }

    /// <summary>
    /// 즉시 Fade Out (씬 전환 없음)
    /// </summary>
    public void FadeOut()
    {
        StartCoroutine(FadeOutCoroutine());
    }

    /// <summary>
    /// 즉시 Fade In (씬 전환 없음)
    /// </summary>
    public void FadeInManual()
    {
        StartCoroutine(FadeIn());
    }

    // Fade In 코루틴 (계산된 프레임 기반)
    IEnumerator FadeIn()
    {
        if (fadeImage == null)
        {
            Debug.LogError("[SceneFader] FadeIn: fadeImage is NULL!");
            yield break;
        }

        // FPS 계산 대기
        while (!framesCalculated)
        {
            yield return null;
        }

        int frames = calculatedFadeInFrames;
        Debug.LogWarning($"[SceneFader] ===== FadeIn STARTED ===== Total frames: {frames}");

        isFading = true;
        Color color = fadeImage.color;

        Debug.LogWarning($"[SceneFader] FadeIn initial alpha: {color.a}");

        for (int frame = 0; frame <= frames; frame++)
        {
            float t = (float)frame / frames;
            color.a = Mathf.Lerp(1f, 0f, t);
            fadeImage.color = color;

            if (frame % 10 == 0)
            {
                Debug.LogWarning($"[SceneFader] FadeIn frame {frame}/{frames}: alpha={color.a:F3}");
            }

            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;
        isFading = false;

        Debug.LogWarning($"[SceneFader] ===== FadeIn COMPLETED ===== Final alpha: {fadeImage.color.a}, Total frames: {frames}");
    }

    // Fade Out 코루틴 (씬 전환 없음) - 계산된 프레임 기반
    IEnumerator FadeOutCoroutine()
    {
        if (fadeImage == null) yield break;

        // FPS 계산 대기
        while (!framesCalculated)
        {
            yield return null;
        }

        int frames = calculatedFadeOutFrames;
        isFading = true;
        Color color = fadeImage.color;

        for (int frame = 0; frame <= frames; frame++)
        {
            float t = (float)frame / frames;
            color.a = Mathf.Lerp(0f, 1f, t);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;
        isFading = false;
    }

    // Fade Out 후 씬 로드 코루틴 - 계산된 프레임 기반
    IEnumerator FadeOutAndLoadScene(string sceneName, float customFadeDuration = -1f)
    {
        if (fadeImage == null)
        {
            Debug.LogError("[SceneFader] fadeImage가 없어서 바로 씬을 로드합니다.");
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        // ⚠️ FPS 계산이 완료될 때까지 대기
        while (!framesCalculated)
        {
            yield return null;
        }

        int frames = calculatedFadeOutFrames;

        isFading = true;
        Color color = fadeImage.color;

        Debug.Log($"[SceneFader] FadeOut started! Total frames: {frames}, Initial alpha: {color.a}");

        // Fade Out (계산된 프레임 기반)
        for (int frame = 0; frame <= frames; frame++)
        {
            float t = (float)frame / frames;
            color.a = Mathf.Lerp(0f, 1f, t);
            fadeImage.color = color;
            yield return null;
        }

        // 완전히 불투명하게 (검은 화면 유지)
        color.a = 1f;
        fadeImage.color = color;

        Debug.Log($"[SceneFader] FadeOut completed! Final alpha: {color.a}");

        // ⚠️ 씬 로드 전에 fade in 미완료 상태로 리셋
        IsFadeInComplete = false;
        Debug.LogWarning($"[SceneFader] IsFadeInComplete = false → Next scene will wait for fade in");

        // ⚠️ isFading을 유지한 채로 씬 로드
        // (OnSceneLoaded에서 isFading을 false로 리셋하고 FadeIn 시작)
        Debug.Log($"[SceneFader] Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);

        // ⚠️ 여기서 isFading을 false로 바꾸지 않음!
        // OnSceneLoaded에서 자동으로 FadeIn이 시작되고, 그때 isFading = false 처리됨
    }

    /// <summary>
    /// 페이드 색상 변경
    /// </summary>
    public void SetFadeColor(Color color)
    {
        fadeColor = color;
        if (fadeImage != null)
        {
            Color currentColor = fadeImage.color;
            fadeImage.color = new Color(color.r, color.g, color.b, currentColor.a);
        }
    }

    // ===== Static 헬퍼 메서드 =====

    /// <summary>
    /// Static 메서드: Fade 효과와 함께 씬 로드
    /// 기존 SceneManager.LoadScene을 대체
    /// </summary>
    public static void LoadScene(string sceneName)
    {
        if (Instance != null)
        {
            Instance.FadeToScene(sceneName);
        }
        else
        {
            Debug.LogWarning("[SceneFader] Instance가 없어서 바로 씬을 로드합니다.");
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// Static 메서드: Fade 효과와 함께 씬 로드 (커스텀 페이드 시간 - 호환성 유지용)
    /// </summary>
    public static void LoadScene(string sceneName, float fadeDuration)
    {
        // 프레임 기반으로 변경되었으므로 fadeDuration은 무시됨
        LoadScene(sceneName);
    }
}