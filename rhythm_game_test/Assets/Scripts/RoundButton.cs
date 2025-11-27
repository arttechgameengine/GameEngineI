using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 토너먼트 맵의 라운드 버튼
/// </summary>
public class RoundButton : MonoBehaviour
{
    [Header("UI References")]
    public Button button;
    public Image enemyImage; // 적 이미지
    public GameObject lockIcon; // 잠금 아이콘
    public GameObject clearedIcon; // 클리어 표시

    private int roundIndex;
    private RoundData roundData;

    public Action onClicked;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(OnClick);
    }

    /// <summary>
    /// 라운드 버튼 설정
    /// </summary>
    public void Setup(int index, RoundData data)
    {
        roundIndex = index;
        roundData = data;

        // 적 이미지 설정
        if (enemyImage != null && data.enemyPortrait != null)
        {
            enemyImage.sprite = data.enemyPortrait;
        }

        // 클리어 여부 표시
        bool isCleared = GameModeManager.Instance.roundCleared[index];
        if (clearedIcon != null)
        {
            clearedIcon.SetActive(isCleared);
        }
    }

    /// <summary>
    /// 잠금 상태 설정
    /// </summary>
    public void SetLocked(bool locked)
    {
        if (lockIcon != null)
        {
            lockIcon.SetActive(locked);
        }

        button.interactable = !locked;

        // 잠긴 라운드는 어둡게 표시
        if (enemyImage != null)
        {
            Color c = enemyImage.color;
            c.a = locked ? 0.3f : 1f;
            enemyImage.color = c;
        }
    }

    /// <summary>
    /// 버튼 클릭 시
    /// </summary>
    void OnClick()
    {
        onClicked?.Invoke();
    }
}
