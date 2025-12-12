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
    public Image dialogueBoxImage;       // 대화창 스프라이트 (flip용)
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;

    // 원본 위치 저장용
    private Vector2 originalDialogueBoxPos;
    private Vector2 originalSpeakerNamePos;
    private Vector2 originalDialogueTextPos;
    private bool positionsSaved = false;

    [Header("Chapter Title")]
    public TextMeshProUGUI chapterTitleText;

    [Header("Visual Settings")]
    public Color activeSpeakerColor = Color.white;
    public Color inactiveSpeakerColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Effect Sprites")]
    public Transform effectSpritesContainer;  // 효과 스프라이트를 담을 부모 오브젝트
    private GameObject[] currentEffectObjects;  // 현재 표시 중인 효과 오브젝트들

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
        {
            speakerNameText.text = line.speakerName;
            speakerNameText.overflowMode = TextOverflowModes.Truncate;
        }

        // 대사 표시
        if (dialogueText != null)
        {
            dialogueText.text = line.dialogue;
            dialogueText.overflowMode = TextOverflowModes.Truncate;
            dialogueText.enableWordWrapping = true;
        }

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

        // 대화창 flip 업데이트
        UpdateDialogueBoxFlip(line.isLeftSpeaker);

        // 효과 스프라이트 업데이트
        UpdateEffectSprites(line.effectSprites);
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

    void UpdateDialogueBoxFlip(bool isLeftSpeaker)
    {
        // 처음 호출 시 원본 위치 저장
        if (!positionsSaved)
        {
            if (dialogueBoxImage != null)
            {
                RectTransform rt = dialogueBoxImage.GetComponent<RectTransform>();
                if (rt != null) originalDialogueBoxPos = rt.anchoredPosition;
            }
            if (speakerNameText != null)
            {
                RectTransform rt = speakerNameText.GetComponent<RectTransform>();
                if (rt != null) originalSpeakerNamePos = rt.anchoredPosition;
            }
            if (dialogueText != null)
            {
                RectTransform rt = dialogueText.GetComponent<RectTransform>();
                if (rt != null) originalDialogueTextPos = rt.anchoredPosition;
            }
            positionsSaved = true;
        }

        float xOffset = isLeftSpeaker ? -30f : 0f;

        // 대화창 이미지 flip 및 위치 조정
        if (dialogueBoxImage != null)
        {
            // 오른쪽 화자면 정상, 왼쪽 화자면 flip
            dialogueBoxImage.transform.localScale = new Vector3(
                isLeftSpeaker ? -1f : 1f,
                1f,
                1f
            );

            // 원본 위치 기준으로 상대 이동
            RectTransform rectTransform = dialogueBoxImage.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(
                    originalDialogueBoxPos.x + xOffset,
                    originalDialogueBoxPos.y
                );
            }
        }

        // 텍스트 위치 조정
        if (speakerNameText != null)
        {
            RectTransform rectTransform = speakerNameText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(
                    originalSpeakerNamePos.x + xOffset,
                    originalSpeakerNamePos.y
                );
            }
        }

        if (dialogueText != null)
        {
            RectTransform rectTransform = dialogueText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(
                    originalDialogueTextPos.x + xOffset,
                    originalDialogueTextPos.y
                );
            }
        }
    }

    /// <summary>
    /// 효과 스프라이트 업데이트 (이전 효과는 제거하고 새 효과 생성)
    /// </summary>
    void UpdateEffectSprites(EffectSpriteData[] effectSprites)
    {
        // 이전 효과 스프라이트 제거
        ClearEffectSprites();

        // 효과 스프라이트가 없으면 종료
        if (effectSprites == null || effectSprites.Length == 0)
            return;

        // 컨테이너 확인
        if (effectSpritesContainer == null)
        {
            Debug.LogWarning("[DialogueUI] effectSpritesContainer가 설정되지 않았습니다! Inspector에서 설정하세요.");
            return;
        }

        // 새 효과 스프라이트 생성
        currentEffectObjects = new GameObject[effectSprites.Length];

        for (int i = 0; i < effectSprites.Length; i++)
        {
            EffectSpriteData data = effectSprites[i];
            if (data == null || data.effectSprite == null)
                continue;

            // 새 GameObject 생성
            GameObject effectObj = new GameObject($"EffectSprite_{i}");
            effectObj.transform.SetParent(effectSpritesContainer, false);

            // Image 컴포넌트 추가
            Image effectImage = effectObj.AddComponent<Image>();
            effectImage.sprite = data.effectSprite;
            effectImage.color = new Color(data.tintColor.r, data.tintColor.g, data.tintColor.b, data.alpha);

            // RectTransform 설정
            RectTransform rectTransform = effectObj.GetComponent<RectTransform>();

            // 앵커 설정
            SetAnchorPreset(rectTransform, data.anchorPreset);

            // 위치, 크기, 회전, 스케일 설정
            rectTransform.anchoredPosition = data.position;
            rectTransform.sizeDelta = data.size;
            rectTransform.localRotation = Quaternion.Euler(data.rotation);
            rectTransform.localScale = data.scale;

            // Sorting order 설정 (Canvas가 있는 경우)
            Canvas canvas = effectObj.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = data.sortingOrder;

            currentEffectObjects[i] = effectObj;

            Debug.Log($"[DialogueUI] 효과 스프라이트 생성: {data.effectSprite.name} at {data.position}");
        }
    }

    /// <summary>
    /// 현재 표시 중인 모든 효과 스프라이트 제거
    /// </summary>
    void ClearEffectSprites()
    {
        if (currentEffectObjects == null)
            return;

        foreach (GameObject obj in currentEffectObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        currentEffectObjects = null;
    }

    /// <summary>
    /// RectTransform의 앵커 프리셋 설정
    /// </summary>
    void SetAnchorPreset(RectTransform rect, AnchorPreset preset)
    {
        switch (preset)
        {
            case AnchorPreset.TopLeft:
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                break;
            case AnchorPreset.TopCenter:
                rect.anchorMin = new Vector2(0.5f, 1);
                rect.anchorMax = new Vector2(0.5f, 1);
                rect.pivot = new Vector2(0.5f, 1);
                break;
            case AnchorPreset.TopRight:
                rect.anchorMin = new Vector2(1, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(1, 1);
                break;
            case AnchorPreset.MiddleLeft:
                rect.anchorMin = new Vector2(0, 0.5f);
                rect.anchorMax = new Vector2(0, 0.5f);
                rect.pivot = new Vector2(0, 0.5f);
                break;
            case AnchorPreset.MiddleCenter:
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                break;
            case AnchorPreset.MiddleRight:
                rect.anchorMin = new Vector2(1, 0.5f);
                rect.anchorMax = new Vector2(1, 0.5f);
                rect.pivot = new Vector2(1, 0.5f);
                break;
            case AnchorPreset.BottomLeft:
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(0, 0);
                rect.pivot = new Vector2(0, 0);
                break;
            case AnchorPreset.BottomCenter:
                rect.anchorMin = new Vector2(0.5f, 0);
                rect.anchorMax = new Vector2(0.5f, 0);
                rect.pivot = new Vector2(0.5f, 0);
                break;
            case AnchorPreset.BottomRight:
                rect.anchorMin = new Vector2(1, 0);
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(1, 0);
                break;
        }
    }

    void OnDialogueEnd()
    {
        // 효과 스프라이트 정리
        ClearEffectSprites();

        // 대화 종료 시 다음 씬으로 이동
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneFader.LoadScene(nextSceneName);
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
