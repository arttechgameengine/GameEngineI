using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// TrackLine 밑에 4개의 요리 스프라이트가 항상 배치되어 Idle 재생
/// 방향키 입력 시 해당 스프라이트의 Cook 애니메이션 트리거
/// Scene에 이미 배치된 스프라이트를 직접 참조하여 사용
/// </summary>
public class CookingAreaManager : MonoBehaviour
{
    public static CookingAreaManager Instance { get; private set; }

    [Header("Cooking Sprites (Scene에 이미 배치된 스프라이트)")]
    [Tooltip("LEFT 키에 대응하는 요리 스프라이트 (Animator 포함)")]
    public GameObject leftCookingSprite;

    [Tooltip("RIGHT 키에 대응하는 요리 스프라이트 (Animator 포함)")]
    public GameObject rightCookingSprite;

    [Tooltip("UP 키에 대응하는 요리 스프라이트 (Animator 포함)")]
    public GameObject upCookingSprite;

    [Tooltip("DOWN 키에 대응하는 요리 스프라이트 (Animator 포함)")]
    public GameObject downCookingSprite;

    [Header("Animation Settings")]
    [Tooltip("Idle 애니메이션 state 이름")]
    public string idleAnimationName = "Idle";

    [Tooltip("애니메이션 재생 중 재호출 무시 시간 (초)")]
    public float animationCooldown = 0.1f;

    [Header("LEFT Direction Animations")]
    [Tooltip("단일 노트 성공 애니메이션")]
    public string leftSingleAnimation = "Left_Single";
    [Tooltip("연타 각 키입력 애니메이션")]
    public string leftRapidHitAnimation = "Left_Rapid_Hit";
    [Tooltip("연타 모두 성공 애니메이션")]
    public string leftRapidSuccessAnimation = "Left_Rapid_Success";
    [Tooltip("롱노트 시작 성공 애니메이션")]
    public string leftLongStartAnimation = "Left_Long_Start";
    [Tooltip("롱노트 홀드 애니메이션")]
    public string leftLongHoldAnimation = "Left_Long_Hold";
    [Tooltip("롱노트 성공 애니메이션")]
    public string leftLongSuccessAnimation = "Left_Long_Success";

    [Header("RIGHT Direction Animations")]
    [Tooltip("단일 노트 성공 애니메이션")]
    public string rightSingleAnimation = "Right_Single";
    [Tooltip("연타 각 키입력 애니메이션")]
    public string rightRapidHitAnimation = "Right_Rapid_Hit";
    [Tooltip("연타 모두 성공 애니메이션")]
    public string rightRapidSuccessAnimation = "Right_Rapid_Success";
    [Tooltip("롱노트 시작 성공 애니메이션")]
    public string rightLongStartAnimation = "Right_Long_Start";
    [Tooltip("롱노트 홀드 애니메이션")]
    public string rightLongHoldAnimation = "Right_Long_Hold";
    [Tooltip("롱노트 성공 애니메이션")]
    public string rightLongSuccessAnimation = "Right_Long_Success";

    [Header("UP Direction Animations")]
    [Tooltip("단일 노트 성공 애니메이션")]
    public string upSingleAnimation = "Up_Single";
    [Tooltip("연타 각 키입력 애니메이션")]
    public string upRapidHitAnimation = "Up_Rapid_Hit";
    [Tooltip("연타 모두 성공 애니메이션")]
    public string upRapidSuccessAnimation = "Up_Rapid_Success";
    [Tooltip("롱노트 시작 성공 애니메이션")]
    public string upLongStartAnimation = "Up_Long_Start";
    [Tooltip("롱노트 홀드 애니메이션")]
    public string upLongHoldAnimation = "Up_Long_Hold";
    [Tooltip("롱노트 성공 애니메이션")]
    public string upLongSuccessAnimation = "Up_Long_Success";

    [Header("DOWN Direction Animations")]
    [Tooltip("단일 노트 성공 애니메이션")]
    public string downSingleAnimation = "Down_Single";
    [Tooltip("연타 각 키입력 애니메이션")]
    public string downRapidHitAnimation = "Down_Rapid_Hit";
    [Tooltip("연타 모두 성공 애니메이션")]
    public string downRapidSuccessAnimation = "Down_Rapid_Success";
    [Tooltip("롱노트 시작 성공 애니메이션")]
    public string downLongStartAnimation = "Down_Long_Start";
    [Tooltip("롱노트 홀드 애니메이션")]
    public string downLongHoldAnimation = "Down_Long_Hold";
    [Tooltip("롱노트 성공 애니메이션")]
    public string downLongSuccessAnimation = "Down_Long_Success";

    // Animator 참조
    private Animator leftAnimator;
    private Animator rightAnimator;
    private Animator upAnimator;
    private Animator downAnimator;

    // 마지막 애니메이션 재생 시간 (쿨다운 방지)
    private Dictionary<string, float> lastPlayTime = new Dictionary<string, float>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Scene에 배치된 스프라이트들의 Animator 가져오기
        if (leftCookingSprite != null)
        {
            leftAnimator = leftCookingSprite.GetComponent<Animator>();
            Debug.Log($"[CookingAreaManager] LEFT 스프라이트 ({leftCookingSprite.name}), Animator: {(leftAnimator != null ? "발견" : "NULL!")}");
        }

        if (rightCookingSprite != null)
        {
            rightAnimator = rightCookingSprite.GetComponent<Animator>();
            Debug.Log($"[CookingAreaManager] RIGHT 스프라이트 ({rightCookingSprite.name}), Animator: {(rightAnimator != null ? "발견" : "NULL!")}");
        }

        if (upCookingSprite != null)
        {
            upAnimator = upCookingSprite.GetComponent<Animator>();
            Debug.Log($"[CookingAreaManager] UP 스프라이트 ({upCookingSprite.name}), Animator: {(upAnimator != null ? "발견" : "NULL!")}");
        }

        if (downCookingSprite != null)
        {
            downAnimator = downCookingSprite.GetComponent<Animator>();
            Debug.Log($"[CookingAreaManager] DOWN 스프라이트 ({downCookingSprite.name}), Animator: {(downAnimator != null ? "발견" : "NULL!")}");
        }

        Debug.Log("[CookingAreaManager] 4개의 요리 스프라이트 참조 완료!");
    }

    /// <summary>
    /// 단일 노트 성공 시 애니메이션 재생
    /// </summary>
    public void PlaySingleNoteAnimation(string noteType)
    {
        string animationName = GetAnimationName(noteType, "SINGLE");
        PlayAnimation(noteType, animationName, "Single");
    }

    /// <summary>
    /// 연타 노트 각 키입력 시 애니메이션 재생
    /// </summary>
    public void PlayRapidHitAnimation(string noteType)
    {
        string animationName = GetAnimationName(noteType, "RAPID_HIT");
        PlayAnimation(noteType, animationName, "RapidHit");
    }

    /// <summary>
    /// 연타 노트 모두 성공 시 애니메이션 재생
    /// </summary>
    public void PlayRapidSuccessAnimation(string noteType)
    {
        string animationName = GetAnimationName(noteType, "RAPID_SUCCESS");
        PlayAnimation(noteType, animationName, "RapidSuccess");
    }

    /// <summary>
    /// 롱노트 시작 성공 시 애니메이션 재생
    /// </summary>
    public void PlayLongStartAnimation(string noteType)
    {
        string animationName = GetAnimationName(noteType, "LONG_START");
        PlayAnimation(noteType, animationName, "LongStart");
    }

    /// <summary>
    /// 롱노트 홀드 중 애니메이션 재생
    /// </summary>
    public void PlayLongHoldAnimation(string noteType)
    {
        string animationName = GetAnimationName(noteType, "LONG_HOLD");
        PlayAnimation(noteType, animationName, "LongHold");
    }

    /// <summary>
    /// 롱노트 성공 시 애니메이션 재생
    /// </summary>
    public void PlayLongSuccessAnimation(string noteType)
    {
        string animationName = GetAnimationName(noteType, "LONG_SUCCESS");
        PlayAnimation(noteType, animationName, "LongSuccess");
    }

    /// <summary>
    /// 롱노트 실패 시 Idle로 복귀
    /// </summary>
    public void PlayIdleAnimation(string noteType)
    {
        Animator targetAnimator = GetAnimatorForType(noteType);
        if (targetAnimator != null)
        {
            targetAnimator.Play(idleAnimationName, 0, 0f);
            Debug.Log($"[CookingAreaManager] {noteType} Idle 애니메이션 재생 (롱노트 실패)");
        }
    }

    /// <summary>
    /// 방향과 애니메이션 타입에 따라 애니메이션 이름 반환
    /// </summary>
    private string GetAnimationName(string direction, string animType)
    {
        switch (direction)
        {
            case "LEFT":
                switch (animType)
                {
                    case "SINGLE": return leftSingleAnimation;
                    case "RAPID_HIT": return leftRapidHitAnimation;
                    case "RAPID_SUCCESS": return leftRapidSuccessAnimation;
                    case "LONG_START": return leftLongStartAnimation;
                    case "LONG_HOLD": return leftLongHoldAnimation;
                    case "LONG_SUCCESS": return leftLongSuccessAnimation;
                }
                break;

            case "RIGHT":
                switch (animType)
                {
                    case "SINGLE": return rightSingleAnimation;
                    case "RAPID_HIT": return rightRapidHitAnimation;
                    case "RAPID_SUCCESS": return rightRapidSuccessAnimation;
                    case "LONG_START": return rightLongStartAnimation;
                    case "LONG_HOLD": return rightLongHoldAnimation;
                    case "LONG_SUCCESS": return rightLongSuccessAnimation;
                }
                break;

            case "UP":
                switch (animType)
                {
                    case "SINGLE": return upSingleAnimation;
                    case "RAPID_HIT": return upRapidHitAnimation;
                    case "RAPID_SUCCESS": return upRapidSuccessAnimation;
                    case "LONG_START": return upLongStartAnimation;
                    case "LONG_HOLD": return upLongHoldAnimation;
                    case "LONG_SUCCESS": return upLongSuccessAnimation;
                }
                break;

            case "DOWN":
                switch (animType)
                {
                    case "SINGLE": return downSingleAnimation;
                    case "RAPID_HIT": return downRapidHitAnimation;
                    case "RAPID_SUCCESS": return downRapidSuccessAnimation;
                    case "LONG_START": return downLongStartAnimation;
                    case "LONG_HOLD": return downLongHoldAnimation;
                    case "LONG_SUCCESS": return downLongSuccessAnimation;
                }
                break;
        }

        Debug.LogWarning($"[CookingAreaManager] Unknown direction/animType: {direction}/{animType}");
        return idleAnimationName;
    }

    /// <summary>
    /// 애니메이션 재생 (쿨다운 체크 포함)
    /// </summary>
    private void PlayAnimation(string noteType, string animationName, string animTypeLabel)
    {
        // Rapid_Hit은 쿨다운 없이 매번 재생 (연타는 빠르게 눌러야 함)
        bool isRapidHit = (animTypeLabel == "RapidHit");

        if (!isRapidHit)
        {
            // 쿨다운 체크 (RapidHit 제외)
            string cooldownKey = $"{noteType}_{animTypeLabel}";
            if (lastPlayTime.ContainsKey(cooldownKey))
            {
                float timeSinceLastPlay = Time.time - lastPlayTime[cooldownKey];
                if (timeSinceLastPlay < animationCooldown)
                {
                    Debug.Log($"[CookingAreaManager] {cooldownKey} 쿨다운 중 ({timeSinceLastPlay:F3}s < {animationCooldown}s) - 스킵");
                    return;
                }
            }

            // 마지막 재생 시간 업데이트 (RapidHit 제외)
            lastPlayTime[cooldownKey] = Time.time;
        }

        Animator targetAnimator = GetAnimatorForType(noteType);

        if (targetAnimator != null)
        {
            // 코루틴으로 안전하게 재생
            StartCoroutine(PlayAnimationCoroutine(targetAnimator, animationName, noteType, animTypeLabel));
        }
        else
        {
            Debug.LogWarning($"[CookingAreaManager] {noteType}에 대응하는 Animator를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 애니메이션을 안전하게 재생하는 코루틴
    /// 트리거 대신 직접 Play() 사용
    /// </summary>
    private IEnumerator PlayAnimationCoroutine(Animator animator, string animationName, string noteType, string animTypeLabel)
    {
        // 애니메이션 직접 재생 (트리거 없이)
        animator.Play(animationName, 0, 0f);

        Debug.Log($"[CookingAreaManager] {noteType} {animTypeLabel} 애니메이션 재생: {animationName}");

        yield return null; // 1프레임 대기
    }

    Animator GetAnimatorForType(string noteType)
    {
        switch (noteType)
        {
            case "LEFT": return leftAnimator;
            case "RIGHT": return rightAnimator;
            case "UP": return upAnimator;
            case "DOWN": return downAnimator;
            default: return null;
        }
    }

    /// <summary>
    /// 모든 ingredient의 Idle 애니메이션 멈추기 (Dish Reveal Panel 전환 시 호출)
    /// </summary>
    public void StopAllIdleAnimations()
    {
        if (leftAnimator != null)
        {
            leftAnimator.enabled = false;
            Debug.Log("[CookingAreaManager] LEFT Animator stopped");
        }

        if (rightAnimator != null)
        {
            rightAnimator.enabled = false;
            Debug.Log("[CookingAreaManager] RIGHT Animator stopped");
        }

        if (upAnimator != null)
        {
            upAnimator.enabled = false;
            Debug.Log("[CookingAreaManager] UP Animator stopped");
        }

        if (downAnimator != null)
        {
            downAnimator.enabled = false;
            Debug.Log("[CookingAreaManager] DOWN Animator stopped");
        }

        Debug.Log("[CookingAreaManager] All ingredient idle animations stopped!");
    }

    /// <summary>
    /// 모든 ingredient의 Idle 애니메이션 재개하기 (필요한 경우)
    /// </summary>
    public void ResumeAllIdleAnimations()
    {
        if (leftAnimator != null)
        {
            leftAnimator.enabled = true;
            Debug.Log("[CookingAreaManager] LEFT Animator resumed");
        }

        if (rightAnimator != null)
        {
            rightAnimator.enabled = true;
            Debug.Log("[CookingAreaManager] RIGHT Animator resumed");
        }

        if (upAnimator != null)
        {
            upAnimator.enabled = true;
            Debug.Log("[CookingAreaManager] UP Animator resumed");
        }

        if (downAnimator != null)
        {
            downAnimator.enabled = true;
            Debug.Log("[CookingAreaManager] DOWN Animator resumed");
        }

        Debug.Log("[CookingAreaManager] All ingredient idle animations resumed!");
    }
}
