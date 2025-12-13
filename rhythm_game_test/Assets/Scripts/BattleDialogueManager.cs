using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 마지막 dialogue와 rhythm test 사이의 특별 대화 씬 관리
/// 중앙 대각선, 양쪽 캐릭터 슬라이드 인, 대화창 시스템
///
/// 사용 방법:
/// 1. 기존 DialogueManager의 onDialogueEnd 이벤트에서 이 씬으로 전환
/// 2. 또는 별도 씬으로 만들어서 SceneManager.LoadScene("BattleDialogueScene") 호출
/// </summary>
public class BattleDialogueManager : MonoBehaviour
{
    public static BattleDialogueManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("배경 이미지")]
    public Image backgroundImage;

    [Tooltip("중앙 대각선 이미지")]
    public Image diagonalLine;

    [Tooltip("왼쪽 캐릭터 이미지 (주인공)")]
    public Image leftCharacterImage;

    [Tooltip("오른쪽 캐릭터 이미지 (상대방)")]
    public Image rightCharacterImage;

    [Tooltip("대화창 GameObject")]
    public GameObject dialogueBox;

    [Tooltip("대화창 스프라이트 (flip용)")]
    public Image dialogueBoxImage;

    [Tooltip("대화 텍스트 (TextMeshPro)")]
    public TextMeshProUGUI dialogueText;

    [Tooltip("화자 이름 텍스트 (TextMeshPro)")]
    public TextMeshProUGUI speakerNameText;

    [Tooltip("챕터 제목 텍스트 (TextMeshPro)")]
    public TextMeshProUGUI chapterTitleText;

    [Tooltip("다음 대화 표시 아이콘 (▼)")]
    public GameObject nextDialogueIcon;

    [Tooltip("효과 스프라이트를 표시할 부모 Transform")]
    public Transform effectSpritesContainer;

    [Header("Dialogue Data")]
    [Tooltip("전투 대화 데이터 (ScriptableObject)")]
    public BattleDialogueData battleDialogueData;

    [Header("Animation Settings")]
    [Tooltip("캐릭터 슬라이드 인 시간 (초)")]
    public float characterSlideInDuration = 0.8f;

    [Tooltip("왼쪽 캐릭터 슬라이드 인 시작 X 위치 (오프셋)")]
    public float leftCharacterStartOffsetX = -1500f;

    [Tooltip("오른쪽 캐릭터 슬라이드 인 시작 X 위치 (오프셋)")]
    public float rightCharacterStartOffsetX = 1500f;

    [Header("Visual Settings")]
    [Tooltip("화자 강조 효과 사용 여부")]
    public bool useSpeakerHighlight = true;

    [Tooltip("현재 화자의 캐릭터 색상")]
    public Color activeSpeakerColor = Color.white;

    [Tooltip("비화자의 캐릭터 색상")]
    public Color inactiveSpeakerColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Tooltip("대화창 flip 효과 사용 여부")]
    public bool useDialogueBoxFlip = true;

    [Header("Typing Effect")]
    [Tooltip("타이핑 효과 사용 여부")]
    public bool useTypingEffect = true;

    [Tooltip("대화 텍스트 타이핑 속도 (글자/초)")]
    public float textTypingSpeed = 30f;

    [Tooltip("대화 종료 후 대기 시간 (초)")]
    public float endDelay = 1f;

    private int currentDialogueIndex = 0;
    private bool isTyping = false;
    private bool dialogueComplete = false;
    private Coroutine typingCoroutine;

    // 캐릭터 원본 위치
    private Vector2 leftCharacterTargetPos;
    private Vector2 rightCharacterTargetPos;

    // 대화창 원본 위치 저장용 (flip용)
    private Vector2 originalDialogueBoxPos;
    private Vector2 originalSpeakerNamePos;
    private Vector2 originalDialogueTextPos;
    private bool positionsSaved = false;

    // 현재 활성화된 효과 스프라이트들
    private List<GameObject> activeEffectSprites = new List<GameObject>();

    void Awake()
    {
        Instance = this;

        // 대화창과 다음 아이콘 모두 숨김 (캐릭터 애니메이션 끝난 후 보임)
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }

        if (nextDialogueIcon != null)
        {
            nextDialogueIcon.SetActive(false);
        }

        // 캐릭터를 즉시 화면 밖으로 배치 (씬 로드 시 깜빡임 방지)
        HideCharactersOffscreen();
    }

    /// <summary>
    /// 캐릭터를 화면 밖으로 즉시 배치 (씬 로드 시 깜빡임 방지)
    /// </summary>
    void HideCharactersOffscreen()
    {
        if (leftCharacterImage != null)
        {
            // 목표 위치 저장
            leftCharacterTargetPos = leftCharacterImage.rectTransform.anchoredPosition;
            // 화면 밖으로 즉시 이동
            leftCharacterImage.rectTransform.anchoredPosition = new Vector2(
                leftCharacterTargetPos.x + leftCharacterStartOffsetX,
                leftCharacterTargetPos.y
            );
        }

        if (rightCharacterImage != null)
        {
            // 목표 위치 저장
            rightCharacterTargetPos = rightCharacterImage.rectTransform.anchoredPosition;
            // 화면 밖으로 즉시 이동
            rightCharacterImage.rectTransform.anchoredPosition = new Vector2(
                rightCharacterTargetPos.x + rightCharacterStartOffsetX,
                rightCharacterTargetPos.y
            );
        }

        Debug.Log("[BattleDialogueManager] Characters hidden offscreen");
    }

    void Start()
    {
        // 씬 시작 시 Fade In 대기 후 대화 시작
        StartCoroutine(WaitForFadeInAndStartDialogue());
    }

    IEnumerator WaitForFadeInAndStartDialogue()
    {
        // SceneFader의 fade in 완료 대기
        while (!SceneFader.IsFadeInComplete)
        {
            yield return null;
        }

        Debug.Log("[BattleDialogueManager] Fade in complete, starting dialogue...");
        StartDialogue();
    }

    void Update()
    {
        // Space, Enter, 마우스 클릭, 오른쪽 방향키, D 키로 대화 진행
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
            Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.RightArrow) ||
            Input.GetKeyDown(KeyCode.D))
        {
            HandleDialogueInput();
        }
    }

    /// <summary>
    /// 대화 시작
    /// </summary>
    public void StartDialogue()
    {
        if (battleDialogueData == null)
        {
            Debug.LogError("[BattleDialogueManager] BattleDialogueData is null!");
            GoToRhythmTest();
            return;
        }

        if (battleDialogueData.dialogues == null || battleDialogueData.dialogues.Length == 0)
        {
            Debug.LogWarning("[BattleDialogueManager] No dialogues in BattleDialogueData!");
            GoToRhythmTest();
            return;
        }

        currentDialogueIndex = 0;
        dialogueComplete = false;

        // 배경 이미지 설정
        if (backgroundImage != null && battleDialogueData.backgroundSprite != null)
        {
            backgroundImage.sprite = battleDialogueData.backgroundSprite;
        }

        // 챕터 제목 설정
        if (chapterTitleText != null && !string.IsNullOrEmpty(battleDialogueData.chapterTitle))
        {
            chapterTitleText.text = battleDialogueData.chapterTitle;
        }

        // 캐릭터 스프라이트 기본값 설정 (BattleDialogueData에서 가져오기)
        if (leftCharacterImage != null && battleDialogueData.playerSprite != null)
        {
            leftCharacterImage.sprite = battleDialogueData.playerSprite;
        }

        if (rightCharacterImage != null && battleDialogueData.enemySprite != null)
        {
            rightCharacterImage.sprite = battleDialogueData.enemySprite;
        }

        // 슬라이드 인 애니메이션 시작
        StartCoroutine(SlideInCharacters());
    }

    /// <summary>
    /// 양쪽 캐릭터 슬라이드 인 애니메이션
    /// </summary>
    IEnumerator SlideInCharacters()
    {
        if (leftCharacterImage == null || rightCharacterImage == null) yield break;

        // 시작 위치는 이미 HideCharactersOffscreen()에서 설정됨
        Vector2 leftStartPos = leftCharacterImage.rectTransform.anchoredPosition;
        Vector2 rightStartPos = rightCharacterImage.rectTransform.anchoredPosition;

        float elapsed = 0f;

        while (elapsed < characterSlideInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / characterSlideInDuration;

            // EaseOutCubic
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            leftCharacterImage.rectTransform.anchoredPosition = Vector2.Lerp(leftStartPos, leftCharacterTargetPos, easeT);
            rightCharacterImage.rectTransform.anchoredPosition = Vector2.Lerp(rightStartPos, rightCharacterTargetPos, easeT);

            yield return null;
        }

        leftCharacterImage.rectTransform.anchoredPosition = leftCharacterTargetPos;
        rightCharacterImage.rectTransform.anchoredPosition = rightCharacterTargetPos;

        Debug.Log("[BattleDialogueManager] Characters slide-in complete");

        // 첫 번째 대화 표시
        yield return new WaitForSeconds(0.3f);
        ShowDialogue(currentDialogueIndex);
    }

    /// <summary>
    /// 대화 표시 (타이핑 효과)
    /// </summary>
    void ShowDialogue(int index)
    {
        if (battleDialogueData == null || index >= battleDialogueData.dialogues.Length)
        {
            // 모든 대화 완료
            dialogueComplete = true;
            Debug.Log("[BattleDialogueManager] All dialogues complete");
            StartCoroutine(EndDialogueAndTransition());
            return;
        }

        // 첫 번째 대화일 때 대화창 보이기
        if (index == 0 && dialogueBox != null)
        {
            dialogueBox.SetActive(true);
        }

        BattleDialogueEntry dialogue = battleDialogueData.dialogues[index];

        // 화자 이름 표시
        if (speakerNameText != null)
        {
            speakerNameText.text = dialogue.speakerName;
        }

        // 화자 스프라이트 변경 (대화별 표정 등)
        if (dialogue.speakerSprite != null)
        {
            if (dialogue.isLeftSpeaker && leftCharacterImage != null)
            {
                leftCharacterImage.sprite = dialogue.speakerSprite;
            }
            else if (!dialogue.isLeftSpeaker && rightCharacterImage != null)
            {
                rightCharacterImage.sprite = dialogue.speakerSprite;
            }
        }

        // 이전 효과 스프라이트 제거
        ClearEffectSprites();

        // 새로운 효과 스프라이트 표시
        if (dialogue.effectSprites != null && dialogue.effectSprites.Length > 0)
        {
            ShowEffectSprites(dialogue.effectSprites);
        }

        // 현재 화자 강조 (비화자는 어둡게) - 옵션 확인
        if (useSpeakerHighlight)
        {
            UpdateSpeakerHighlight(dialogue.isLeftSpeaker);
        }

        // 대화창 flip 업데이트 - 옵션 확인
        if (useDialogueBoxFlip)
        {
            UpdateDialogueBoxFlip(dialogue.isLeftSpeaker);
        }

        // 다음 아이콘 숨김
        if (nextDialogueIcon != null)
        {
            nextDialogueIcon.SetActive(false);
        }

        // 타이핑 효과 시작 (옵션에 따라)
        if (useTypingEffect)
        {
            // 타이핑 효과 사용
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            typingCoroutine = StartCoroutine(TypeText(dialogue.dialogueText));
        }
        else
        {
            // 타이핑 효과 없이 바로 표시
            if (dialogueText != null)
            {
                dialogueText.text = dialogue.dialogueText;
            }
            isTyping = false;
            if (nextDialogueIcon != null)
            {
                nextDialogueIcon.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 타이핑 효과 코루틴
    /// </summary>
    IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        float delay = 1f / textTypingSpeed;

        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;

        // 타이핑 완료 후 다음 아이콘 표시
        if (nextDialogueIcon != null)
        {
            nextDialogueIcon.SetActive(true);
        }

        Debug.Log($"[BattleDialogueManager] Dialogue {currentDialogueIndex} typing complete");
    }

    /// <summary>
    /// 대화 입력 처리 (Space/Enter)
    /// </summary>
    void HandleDialogueInput()
    {
        if (dialogueComplete) return;

        // 타이핑 중이면 즉시 완료
        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            if (battleDialogueData != null && currentDialogueIndex < battleDialogueData.dialogues.Length)
            {
                BattleDialogueEntry dialogue = battleDialogueData.dialogues[currentDialogueIndex];
                dialogueText.text = dialogue.dialogueText;
            }
            isTyping = false;

            if (nextDialogueIcon != null)
            {
                nextDialogueIcon.SetActive(true);
            }

            return;
        }

        // 다음 대화로 진행
        currentDialogueIndex++;
        ShowDialogue(currentDialogueIndex);
    }

    /// <summary>
    /// 대화 종료 후 Rhythm Test Scene으로 전환
    /// </summary>
    IEnumerator EndDialogueAndTransition()
    {
        Debug.Log("[BattleDialogueManager] Ending dialogue, transitioning to rhythm test...");

        // 잠시 대기
        yield return new WaitForSeconds(endDelay);

        GoToRhythmTest();
    }

    void GoToRhythmTest()
    {
        string targetScene = battleDialogueData != null && !string.IsNullOrEmpty(battleDialogueData.rhythmTestSceneName)
            ? battleDialogueData.rhythmTestSceneName
            : "RhythmTestScene";

        Debug.Log($"[BattleDialogueManager] Loading {targetScene}...");

        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeToScene(targetScene);
        }
        else
        {
            SceneManager.LoadScene(targetScene);
        }
    }

    /// <summary>
    /// 효과 스프라이트 표시
    /// </summary>
    void ShowEffectSprites(EffectSpriteData[] effectSprites)
    {
        if (effectSpritesContainer == null)
        {
            Debug.LogWarning("[BattleDialogueManager] effectSpritesContainer is null!");
            return;
        }

        foreach (EffectSpriteData data in effectSprites)
        {
            if (data == null || data.effectSprite == null) continue;

            // 새로운 GameObject 생성
            GameObject effectObj = new GameObject($"Effect_{data.effectSprite.name}");
            effectObj.transform.SetParent(effectSpritesContainer, false);

            // Image 컴포넌트 추가
            Image img = effectObj.AddComponent<Image>();
            img.sprite = data.effectSprite;
            img.color = new Color(data.tintColor.r, data.tintColor.g, data.tintColor.b, data.alpha);

            // RectTransform 설정
            RectTransform rect = effectObj.GetComponent<RectTransform>();

            // Anchor 설정
            SetAnchorPreset(rect, data.anchorPreset);

            // 위치, 크기, 회전, 스케일 설정
            rect.anchoredPosition = data.position;
            rect.sizeDelta = data.size;
            rect.localEulerAngles = data.rotation;
            rect.localScale = data.scale;

            // Canvas 순서 설정 (sorting order는 Canvas에서만 적용되므로 Transform의 sibling index 사용)
            effectObj.transform.SetSiblingIndex(data.sortingOrder);

            activeEffectSprites.Add(effectObj);

            Debug.Log($"[BattleDialogueManager] Effect sprite displayed: {data.effectSprite.name} at {data.position}");
        }
    }

    /// <summary>
    /// 활성화된 효과 스프라이트 제거
    /// </summary>
    void ClearEffectSprites()
    {
        foreach (GameObject obj in activeEffectSprites)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        activeEffectSprites.Clear();
    }

    /// <summary>
    /// Anchor 프리셋 설정 (EffectSpriteData의 AnchorPreset enum에 따라)
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

    /// <summary>
    /// 현재 화자 강조 (비화자는 어둡게)
    /// </summary>
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

    /// <summary>
    /// 대화창 flip 및 위치 조정 (화자에 따라)
    /// </summary>
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
}

/// <summary>
/// 전투 대화 항목 (DialogueLine과 동일한 구조)
/// </summary>
[System.Serializable]
public class BattleDialogueEntry
{
    [Tooltip("화자 이름 (대화창에 표시)")]
    public string speakerName;

    [Tooltip("화자 캐릭터 스프라이트 (대화별로 다른 표정 등)")]
    public Sprite speakerSprite;

    [Tooltip("대화 내용")]
    [TextArea(2, 5)]
    public string dialogueText;

    [Tooltip("true: 왼쪽 화자, false: 오른쪽 화자")]
    public bool isLeftSpeaker = true;

    [Header("Effect Sprites")]
    [Tooltip("이 대사에 표시할 효과 스프라이트들")]
    public EffectSpriteData[] effectSprites;
}
