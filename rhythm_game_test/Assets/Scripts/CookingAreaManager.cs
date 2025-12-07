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
    [Tooltip("Cook 애니메이션 state 이름")]
    public string cookAnimationName = "Cook";

    [Tooltip("Idle 애니메이션 state 이름")]
    public string idleAnimationName = "Idle";

    [Tooltip("애니메이션 재생 중 재호출 무시 시간 (초)")]
    public float animationCooldown = 0.1f;

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
    /// 해당 방향키에 대응하는 요리 애니메이션 재생
    /// 쿨다운 체크 + 코루틴으로 안전하게 재생
    /// </summary>
    public void PlayCookingAnimation(string noteType)
    {
        // 쿨다운 체크 (너무 빠른 연속 호출 방지)
        if (lastPlayTime.ContainsKey(noteType))
        {
            float timeSinceLastPlay = Time.time - lastPlayTime[noteType];
            if (timeSinceLastPlay < animationCooldown)
            {
                Debug.Log($"[CookingAreaManager] {noteType} 쿨다운 중 ({timeSinceLastPlay:F3}s < {animationCooldown}s) - 스킵");
                return;
            }
        }

        Animator targetAnimator = GetAnimatorForType(noteType);

        if (targetAnimator != null)
        {
            // 마지막 재생 시간 업데이트
            lastPlayTime[noteType] = Time.time;

            // 코루틴으로 안전하게 재생
            StartCoroutine(PlayCookAnimationCoroutine(targetAnimator, noteType));
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
    private IEnumerator PlayCookAnimationCoroutine(Animator animator, string noteType)
    {
        // 현재 상태 확인
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        // Cook 애니메이션 직접 재생 (트리거 없이)
        animator.Play(cookAnimationName, 0, 0f);

        Debug.Log($"[CookingAreaManager] {noteType} Cook 애니메이션 직접 재생 시작!");

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
}
