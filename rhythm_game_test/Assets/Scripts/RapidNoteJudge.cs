using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 연타 노트 판정 시스템
/// 정해진 시간 안에 N회 연타해야 성공
/// </summary>
public class RapidNoteJudge : MonoBehaviour
{
    [Header("Rapid Note Settings")]
    public int requiredHitCount = 5;      // 필요한 연타 횟수
    public float timeLimit = 1.0f;        // 제한 시간 (초)
    public string assignedArrow;          // 입력해야 하는 키 ("UP", "DOWN", etc.)

    [Header("State")]
    public int currentHitCount = 0;       // 현재 연타 횟수
    public bool isActive = false;         // 판정 활성화 여부
    public bool isCompleted = false;      // 성공 여부
    public bool isFailed = false;         // 실패 여부

    private float activationTime;         // 활성화된 시간
    private float elapsedTime = 0f;       // 경과 시간
    private NoteMovement noteMovement;

    [HideInInspector]
    public RapidNoteVisual rapidVisual;   // NoteSpawner에서 설정

    void Awake()
    {
        noteMovement = GetComponent<NoteMovement>();
    }

    /// <summary>
    /// 연타 노트 초기화
    /// </summary>
    public void Initialize(int hitCount, float duration, string arrow)
    {
        requiredHitCount = hitCount;
        timeLimit = duration;
        assignedArrow = arrow;
        currentHitCount = 0;
        isActive = false;
        isCompleted = false;
        isFailed = false;
        elapsedTime = 0f;

        Debug.Log($"[RapidNoteJudge] Initialized - Arrow: {arrow}, Required: {hitCount}, Time: {duration}s");
    }

    /// <summary>
    /// 연타 판정 시작 (HitLine 도달 시 호출)
    /// </summary>
    public void Activate()
    {
        if (isActive) return;

        isActive = true;
        activationTime = Time.time;
        currentHitCount = 0;
        elapsedTime = 0f;

        Debug.Log($"[RapidNoteJudge] Activated! Hit {requiredHitCount} times in {timeLimit}s");

        // HitLine에 노트 고정 (이동 중지)
        NoteMovement noteMovement = GetComponent<NoteMovement>();
        if (noteMovement != null)
        {
            noteMovement.enabled = false;  // 이동 멈춤
            Debug.Log($"[RapidNoteJudge] Note movement stopped - fixed at HitLine");
        }

        // 비주얼 업데이트
        if (rapidVisual != null)
        {
            rapidVisual.OnActivated();
        }
    }

    void Update()
    {
        // 일시정지 중에는 업데이트 안 함
        if (PauseManager.IsPaused) return;

        if (!isActive || isCompleted || isFailed) return;

        // 경과 시간 업데이트
        elapsedTime += Time.deltaTime;

        // 제한 시간 초과 → 실패
        if (elapsedTime >= timeLimit)
        {
            Fail();
            return;
        }

        // 비주얼 업데이트 (타이머)
        if (rapidVisual != null)
        {
            rapidVisual.UpdateProgress(currentHitCount, requiredHitCount, elapsedTime, timeLimit);
        }
    }

    /// <summary>
    /// 키 입력 처리 (PlayerJudge에서 호출)
    /// </summary>
    public bool OnKeyPressed(string pressedArrow)
    {
        if (!isActive || isCompleted || isFailed) return false;

        // 올바른 키인지 확인
        if (pressedArrow != assignedArrow) return false;

        // 연타 카운트 증가
        currentHitCount++;
        Debug.Log($"[RapidNoteJudge] Hit {currentHitCount}/{requiredHitCount}");

        // 비주얼 피드백
        if (rapidVisual != null)
        {
            rapidVisual.OnHit(currentHitCount, requiredHitCount);
        }

        // 목표 달성 → 성공
        if (currentHitCount >= requiredHitCount)
        {
            Complete();
        }

        return true;
    }

    /// <summary>
    /// 연타 성공
    /// </summary>
    void Complete()
    {
        if (isCompleted) return;

        isCompleted = true;
        isActive = false;

        Debug.Log($"[RapidNoteJudge] SUCCESS! Completed in {elapsedTime:F2}s");

        // 점수 계산 (빠를수록 높은 점수)
        string judgement = CalculateJudgement();

        // ScoreManager에 점수 추가
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddJudge(judgement);
        }

        // 비주얼 성공 연출
        if (rapidVisual != null)
        {
            rapidVisual.OnSuccess(judgement);
        }

        // 요리 애니메이션 트리거
        if (CookingAreaManager.Instance != null)
        {
            CookingAreaManager.Instance.PlayCookingAnimation(assignedArrow);
        }

        // 노트 파괴
        Invoke(nameof(DestroyNote), 0.3f);
    }

    /// <summary>
    /// 연타 실패
    /// </summary>
    void Fail()
    {
        if (isFailed) return;

        isFailed = true;
        isActive = false;

        Debug.Log($"[RapidNoteJudge] FAILED! Only {currentHitCount}/{requiredHitCount} hits");

        // Miss 처리
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddJudge("MISS");
        }

        // 비주얼 실패 연출
        if (rapidVisual != null)
        {
            rapidVisual.OnFail();
        }

        // 노트 파괴
        Invoke(nameof(DestroyNote), 0.3f);
    }

    /// <summary>
    /// 판정 계산 (완료 속도에 따라) - 성공 시에만 호출됨
    /// </summary>
    string CalculateJudgement()
    {
        // 시간 비율 계산 (빠를수록 좋음)
        float timeRatio = elapsedTime / timeLimit;

        if (timeRatio <= 0.5f)
            return "PERFECT";   // 절반 이내 완료 (매우 빠름)
        else if (timeRatio <= 0.75f)
            return "GREAT";     // 75% 이내 완료 (빠름)
        else
            return "GOOD";      // 90% 이내 완료 (보통)
    }

    void DestroyNote()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// HitLine을 벗어남 → 실패 처리
    /// </summary>
    public void OnMissed()
    {
        if (isCompleted || isFailed) return;

        Debug.Log($"[RapidNoteJudge] Missed! (Not activated)");

        // Miss 처리
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddJudge("MISS");
        }

        isFailed = true;
        Destroy(gameObject);
    }
}
