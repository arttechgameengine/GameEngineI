using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [Header("Background")]
    public Image backgroundImage;

    [Header("Character Sprites")]
    public Image leftCharacterImage;     // 왼쪽 캐릭터 (주인공)
    public Image rightCharacterImage;    // 오른쪽 캐릭터 (상대방)

    [Header("Dialogue Box")]
    public GameObject dialogueBox;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;

    [Header("Chapter Title")]
    public TextMeshProUGUI chapterTitleText;

    [Header("Visual Settings")]
    public Color activeSpeakerColor = Color.white;
    public Color inactiveSpeakerColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Scene Transition")]
    public string nextSceneName = "RhythmTest";  // 대화 후 이동할 씬

    [Header("References")]
    public DialogueManager dialogueManager;

    void Start()
    {
        if (dialogueManager == null)
            dialogueManager = DialogueManager.Instance;

        // 이벤트 연결
        if (dialogueManager != null)
        {
            dialogueManager.onDialogueStart.AddListener(OnDialogueStart);
            dialogueManager.onDialogueEnd.AddListener(OnDialogueEnd);
            dialogueManager.onLineChanged.AddListener(OnLineChanged);

            // 대화 자동 시작
            dialogueManager.StartCurrentDialogue();
        }
    }

    void OnDialogueStart()
    {
        // 배경 설정
        if (backgroundImage != null && dialogueManager.currentDialogue.backgroundSprite != null)
        {
            backgroundImage.sprite = dialogueManager.currentDialogue.backgroundSprite;
        }

        // 챕터 제목 설정
        if (chapterTitleText != null)
        {
            chapterTitleText.text = dialogueManager.currentDialogue.chapterTitle;
        }

        // 기본 캐릭터 스프라이트 설정
        if (leftCharacterImage != null && dialogueManager.currentDialogue.leftCharacterDefault != null)
        {
            leftCharacterImage.sprite = dialogueManager.currentDialogue.leftCharacterDefault;
            leftCharacterImage.gameObject.SetActive(true);
        }

        if (rightCharacterImage != null && dialogueManager.currentDialogue.rightCharacterDefault != null)
        {
            rightCharacterImage.sprite = dialogueManager.currentDialogue.rightCharacterDefault;
            rightCharacterImage.gameObject.SetActive(true);
        }

        // 대화창 표시
        if (dialogueBox != null)
            dialogueBox.SetActive(true);
    }

    void OnLineChanged(DialogueLine line)
    {
        // 화자 이름 표시
        if (speakerNameText != null)
            speakerNameText.text = line.speakerName;

        // 대사 표시
        if (dialogueText != null)
            dialogueText.text = line.dialogue;

        // 캐릭터 스프라이트 업데이트 (대사별로 다른 표정 등)
        if (line.speakerSprite != null)
        {
            if (line.isLeftSpeaker && leftCharacterImage != null)
            {
                leftCharacterImage.sprite = line.speakerSprite;
            }
            else if (!line.isLeftSpeaker && rightCharacterImage != null)
            {
                rightCharacterImage.sprite = line.speakerSprite;
            }
        }

        // 현재 화자 강조 (비화자는 어둡게)
        UpdateSpeakerHighlight(line.isLeftSpeaker);
    }

    void UpdateSpeakerHighlight(bool isLeftSpeaker)
    {
        if (leftCharacterImage != null)
        {
            leftCharacterImage.color = isLeftSpeaker ? activeSpeakerColor : inactiveSpeakerColor;
        }

        if (rightCharacterImage != null)
        {
            rightCharacterImage.color = isLeftSpeaker ? inactiveSpeakerColor : activeSpeakerColor;
        }
    }

    void OnDialogueEnd()
    {
        // 대화 종료 시 다음 씬으로 이동
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    void OnDestroy()
    {
        // 이벤트 해제
        if (dialogueManager != null)
        {
            dialogueManager.onDialogueStart.RemoveListener(OnDialogueStart);
            dialogueManager.onDialogueEnd.RemoveListener(OnDialogueEnd);
            dialogueManager.onLineChanged.RemoveListener(OnLineChanged);
        }
    }
}
