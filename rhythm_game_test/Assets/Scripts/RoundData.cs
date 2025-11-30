using UnityEngine;

/// <summary>
/// 라운드 정보 데이터
/// </summary>
[CreateAssetMenu(fileName = "Round", menuName = "Tournament/Round Data")]
public class RoundData : ScriptableObject
{
    [Header("Round Info")]
    public int roundNumber; // 1~5
    public string roundName; // "Round 1", "Round 2", etc.
    public string enemyName; // 적 이름
    public Sprite enemyPortrait; // 적 초상화

    public enum Difficulty { Easy, Normal, Hard, VeryHard }

    [Header("Difficulty")]
    public Difficulty difficulty = Difficulty.Normal;

    [Header("Map Info")]
    public Vector2 mapPosition; // 토너먼트 지도에서의 위치

    [Header("Scene")]
    public string sceneName; // 이동할 씬 이름 (예: "Round1Scene", "Round2Scene")

    [Header("Gameplay (Optional - 씬에서 직접 설정 가능)")]
    public string songName; // 곡 이름
    public AudioClip songClip; // 곡 파일
    public TextAsset beatmapFile; // 비트맵 파일 (CSV)

    [Header("Story")]
    [TextArea(3, 10)]
    public string storyText; // 라운드 시작 전 스토리 텍스트
}
