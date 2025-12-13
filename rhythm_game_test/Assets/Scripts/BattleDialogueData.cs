using UnityEngine;

/// <summary>
/// 전투 대화 데이터 (ScriptableObject)
/// DialogueData처럼 Project에 Asset으로 저장
/// </summary>
[CreateAssetMenu(fileName = "NewBattleDialogue", menuName = "Dialogue/BattleDialogueData")]
public class BattleDialogueData : ScriptableObject
{
    [Header("Scene Info")]
    [Tooltip("챕터 제목 (예: '제 4장')")]
    public string chapterTitle;

    [Tooltip("배경 이미지")]
    public Sprite backgroundSprite;

    [Header("Character Sprites")]
    [Tooltip("주인공 스프라이트 (왼쪽 캐릭터 기본)")]
    public Sprite playerSprite;

    [Tooltip("상대방 스프라이트 (오른쪽 캐릭터 기본)")]
    public Sprite enemySprite;

    [Header("Dialogue Lines")]
    [Tooltip("전투 대화 리스트")]
    public BattleDialogueEntry[] dialogues;

    [Header("Scene Transition")]
    [Tooltip("대화 종료 후 전환할 씬 이름 (Rhythm Test Scene)")]
    public string rhythmTestSceneName = "RhythmTestScene";
}
