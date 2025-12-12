using UnityEngine;

/// <summary>
/// 효과 스프라이트의 위치, 크기, 스프라이트 정보를 담은 ScriptableObject
/// 각 효과마다 개별 설정 가능
/// </summary>
[CreateAssetMenu(fileName = "NewEffectSprite", menuName = "Dialogue/EffectSpriteData")]
public class EffectSpriteData : ScriptableObject
{
    [Header("Sprite")]
    public Sprite effectSprite;          // 효과 스프라이트

    [Header("Transform Settings")]
    public Vector2 position = Vector2.zero;     // 앵커 위치 (Canvas 기준)
    public Vector2 size = new Vector2(100, 100); // 크기
    public Vector3 rotation = Vector3.zero;      // 회전 (Z축)
    public Vector3 scale = Vector3.one;          // 스케일

    [Header("Anchor Settings")]
    [Tooltip("앵커 프리셋: 0=Top Left, 1=Top Center, 2=Top Right, 3=Middle Left, 4=Middle Center, 5=Middle Right, 6=Bottom Left, 7=Bottom Center, 8=Bottom Right")]
    public AnchorPreset anchorPreset = AnchorPreset.MiddleCenter;

    [Header("Visual Settings")]
    public Color tintColor = Color.white;        // 색상 tint
    [Range(0f, 1f)]
    public float alpha = 1f;                     // 투명도

    [Header("Layer Settings")]
    public int sortingOrder = 0;                 // 효과 레이어 순서 (높을수록 앞)
}

/// <summary>
/// 앵커 프리셋 enum
/// </summary>
public enum AnchorPreset
{
    TopLeft = 0,
    TopCenter = 1,
    TopRight = 2,
    MiddleLeft = 3,
    MiddleCenter = 4,
    MiddleRight = 5,
    BottomLeft = 6,
    BottomCenter = 7,
    BottomRight = 8
}
