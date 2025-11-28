using UnityEngine;
using UnityEngine.UI;

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
    [Tooltip("Cook 애니메이션 트리거 이름")]
    public string cookTriggerName = "cook";

    // Animator 참조
    private Animator leftAnimator;
    private Animator rightAnimator;
    private Animator upAnimator;
    private Animator downAnimator;

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
    /// 해당 방향키에 대응하는 요리 애니메이션 트리거
    /// Idle → Cook 애니메이션 재생
    /// </summary>
    public void PlayCookingAnimation(string noteType)
    {
        Debug.Log($"[CookingAreaManager] PlayCookingAnimation 호출됨! noteType: {noteType}");

        Animator targetAnimator = GetAnimatorForType(noteType);

        if (targetAnimator != null)
        {
            // 애니메이터 상태 확인
            AnimatorControllerParameter[] parameters = targetAnimator.parameters;
            bool hasCookTrigger = false;
            foreach (var param in parameters)
            {
                if (param.name == cookTriggerName && param.type == AnimatorControllerParameterType.Trigger)
                {
                    hasCookTrigger = true;
                    break;
                }
            }

            if (hasCookTrigger)
            {
                targetAnimator.SetTrigger(cookTriggerName);
                Debug.Log($"[CookingAreaManager] {noteType} Cook 애니메이션 트리거 성공! (현재 상태: {targetAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle")})");
            }
            else
            {
                Debug.LogError($"[CookingAreaManager] Animator에 '{cookTriggerName}' 트리거 파라미터가 없습니다! 현재 파라미터: {string.Join(", ", System.Array.ConvertAll(parameters, p => p.name))}");
            }
        }
        else
        {
            Debug.LogWarning($"[CookingAreaManager] {noteType}에 대응하는 Animator를 찾을 수 없습니다!");
        }
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
