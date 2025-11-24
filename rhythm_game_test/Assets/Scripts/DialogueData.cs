using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;           // 화자 이름 (대화창에 표시)
    public Sprite speakerSprite;         // 화자 캐릭터 스프라이트
    [TextArea(2, 5)]
    public string dialogue;              // 대사 내용
    public bool isLeftSpeaker = true;    // true: 왼쪽, false: 오른쪽
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/DialogueData")]
public class DialogueData : ScriptableObject
{
    [Header("Scene Info")]
    public string chapterTitle;          // 예: "제 4장"
    public Sprite backgroundSprite;      // 배경 이미지

    [Header("Characters")]
    public Sprite leftCharacterDefault;  // 왼쪽 캐릭터 기본 스프라이트
    public Sprite rightCharacterDefault; // 오른쪽 캐릭터 기본 스프라이트

    [Header("Dialogue Lines")]
    public DialogueLine[] lines;         // 대사 목록
}
