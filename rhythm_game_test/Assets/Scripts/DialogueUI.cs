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

    // 캐릭터 슬라이드 인 애니메이션용
    private Vector2 leftCharacterTargetPos;
    private Vector2 rightCharacterTargetPos;
    private bool isSlideInComplete = false;

    [Header("Chapter Title")]
    public TextMeshProUGUI chapterTitleText;

    [Header("Character Animation")]
    [Tooltip("캐릭터 슬라이드 인 애니메이션 사용 여부")]
    public bool useCharacterSlideIn = false;

    [Tooltip("캐릭터 슬라이드 인 시간 (초)")]
    public float characterSlideInDuration = 0.8f;

    [Tooltip("왼쪽 캐릭터 슬라이드 인 시작 X 위치 (오프셋)")]
    public float leftCharacterStartOffsetX = -1500f;

    [Tooltip("오른쪽 캐릭터 슬라이드 인 시작 X 위치 (오프셋)")]
    public float rightCharacterStartOffsetX = 1500f;

    [Header("Visual Settings")]
    public Color activeSpeakerColor = Color.white;
    public Color inactiveSpeakerColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Typing Effect")]
    [Tooltip("타이핑 효과 사용 여부")]
    public bool useTypingEffect = false;
    [Tooltip("타이핑 속도 (글자/초)")]
    public float typingSpeed = 30f;

    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private string currentDialogueText = ""; // 현재 대사 전체 텍스트 저장

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

        // 슬라이드 인 애니메이션 사용 시 대화창 미리 숨김
        if (useCharacterSlideIn && dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }

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

    void Update()
    {
        // 타이핑 효과가 켜져 있고, 타이핑 중일 때만 스킵 가능
        if (useTypingEffect && isTyping)
        {
            // Space, Enter, 마우스 클릭, 오른쪽 방향키, D 키로 타이핑 스킵
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.RightArrow) ||
                Input.GetKeyDown(KeyCode.D))
            {
                SkipTyping();
            }
        }
    }

    /// <summary>
    /// 타이핑 효과 즉시 완료
    /// </summary>
    void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // 저장된 현재 대사 전체를 표시
        if (dialogueText != null && !string.IsNullOrEmpty(currentDialogueText))
        {
            dialogueText.text = currentDialogueText;
        }

        isTyping = false;
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

        // 슬라이드 인 애니메이션 사용 여부에 따라 처리
        if (useCharacterSlideIn)
        {
            // 캐릭터를 화면 밖으로 즉시 배치
            HideCharactersOffscreen();
            // 슬라이드 인 애니메이션 시작
            StartCoroutine(SlideInCharacters());
        }
        else
        {
            // 슬라이드 인 없이 바로 대화창 표시
            if (dialogueBox != null)
                dialogueBox.SetActive(true);
            isSlideInComplete = true;
        }
    }

    void OnLineChanged(DialogueLine line)
    {
        // 화자 이름 표시
        if (speakerNameText != null)
        {
            speakerNameText.text = line.speakerName;
            speakerNameText.overflowMode = TextOverflowModes.Truncate;
        }

        // 대사 표시 (타이핑 효과 적용 여부에 따라)
        if (dialogueText != null)
        {
            dialogueText.overflowMode = TextOverflowModes.Truncate;
            dialogueText.enableWordWrapping = true;

            // 현재 대사 저장 (타이핑 스킵에 사용)
            currentDialogueText = line.dialogue;

            if (useTypingEffect)
            {
                // 타이핑 효과 사용
                if (typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                }
                typingCoroutine = StartCoroutine(TypeText(line.dialogue));
            }
            else
            {
                // 타이핑 효과 없이 바로 표시
                dialogueText.text = line.dialogue;
            }
        }

        // 캐릭터 스프라이트 업데이트 (대사별로 다른 표정 등)
        if (line.isLeftSpeaker && leftCharacterImage != null)
        {
            // speakerSprite가 있으면 사용, 없으면 기본 스프라이트로 복원
            if (line.speakerSprite != null)
            {
                leftCharacterImage.sprite = line.speakerSprite;
            }
            else if (dialogueManager != null && dialogueManager.currentDialogue != null && dialogueManager.currentDialogue.leftCharacterDefault != null)
            {
                leftCharacterImage.sprite = dialogueManager.currentDialogue.leftCharacterDefault;
            }
        }
        else if (!line.isLeftSpeaker && rightCharacterImage != null)
        {
            // speakerSprite가 있으면 사용, 없으면 기본 스프라이트로 복원
            if (line.speakerSprite != null)
            {
                rightCharacterImage.sprite = line.speakerSprite;
            }
            else if (dialogueManager != null && dialogueManager.currentDialogue != null && dialogueManager.currentDialogue.rightCharacterDefault != null)
            {
                rightCharacterImage.sprite = dialogueManager.currentDialogue.rightCharacterDefault;
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

        // 컨테이너 RectTransform을 Canvas 전체 크기로 설정 (Empty GameObject는 크기가 0이라 자식이 안 보임)
        RectTransform containerRect = effectSpritesContainer.GetComponent<RectTransform>();
        if (containerRect != null)
        {
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
        }

        // 새 효과 스프라이트 생성
        currentEffectObjects = new GameObject[effectSprites.Length];
        Debug.Log($"[DialogueUI] 효과 스프라이트 {effectSprites.Length}개 생성 시작");

        for (int i = 0; i < effectSprites.Length; i++)
        {
            EffectSpriteData data = effectSprites[i];
            if (data == null || data.effectSprite == null)
                continue;

            // 새 GameObject 생성
            GameObject effectObj = new GameObject($"EffectSprite_{i}", typeof(RectTransform));
            effectObj.layer = 5; // UI Layer
            effectObj.transform.SetParent(effectSpritesContainer, false);

            // CanvasRenderer 명시적 추가 (Image 전에)
            effectObj.AddComponent<CanvasRenderer>();

            // Image 컴포넌트 추가
            Image effectImage = effectObj.AddComponent<Image>();
            effectImage.sprite = data.effectSprite;
            effectImage.color = new Color(data.tintColor.r, data.tintColor.g, data.tintColor.b, data.alpha);
            effectImage.raycastTarget = false; // UI 입력 차단 방지

            // RectTransform 설정
            RectTransform rectTransform = effectObj.GetComponent<RectTransform>();

            // 앵커 설정
            SetAnchorPreset(rectTransform, data.anchorPreset);

            // 위치, 크기, 회전, 스케일 설정
            rectTransform.anchoredPosition = data.position;

            // 비율 유지하면서 크기 설정
            Vector2 spriteSize = new Vector2(data.effectSprite.rect.width, data.effectSprite.rect.height);

            if (data.size == Vector2.zero)
            {
                // size가 (0, 0)이면 원본 크기 사용
                rectTransform.sizeDelta = spriteSize;
            }
            else if (data.size.x > 0 && data.size.y == 0)
            {
                // width만 지정: 비율 유지하면서 height 자동 계산
                float aspectRatio = spriteSize.y / spriteSize.x;
                rectTransform.sizeDelta = new Vector2(data.size.x, data.size.x * aspectRatio);
            }
            else if (data.size.x == 0 && data.size.y > 0)
            {
                // height만 지정: 비율 유지하면서 width 자동 계산
                float aspectRatio = spriteSize.x / spriteSize.y;
                rectTransform.sizeDelta = new Vector2(data.size.y * aspectRatio, data.size.y);
            }
            else
            {
                // 둘 다 지정되면 그대로 사용 (비율 무시)
                rectTransform.sizeDelta = data.size;
            }

            rectTransform.localRotation = Quaternion.Euler(data.rotation);
            rectTransform.localScale = data.scale;

            // 계층 구조 내 렌더링 순서 설정 (Canvas는 부모에만 있어야 함)
            // sortingOrder를 hierarchy 순서로 반영하려면 SetAsLastSibling() 사용
            effectObj.transform.SetSiblingIndex(effectSpritesContainer.childCount - 1 + data.sortingOrder);

            currentEffectObjects[i] = effectObj;

            // 디버깅: 실제 월드 좌표와 부모 정보 출력
            Vector3 worldPos = rectTransform.position;
            Debug.Log($"[DialogueUI] === Effect {i}: {data.effectSprite.name} ===");
            Debug.Log($"  - Size (sizeDelta): {rectTransform.sizeDelta} (설정값: {data.size})");
            Debug.Log($"  - Scale (localScale): {rectTransform.localScale} (설정값: {data.scale})");
            Debug.Log($"  - 결과 크기: {rectTransform.sizeDelta.x * rectTransform.localScale.x} x {rectTransform.sizeDelta.y * rectTransform.localScale.y}");
            Debug.Log($"  - Position: {rectTransform.anchoredPosition}");
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

    /// <summary>
    /// 타이핑 효과 코루틴
    /// </summary>
    System.Collections.IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(1f / typingSpeed);
        }

        isTyping = false;
    }

    void OnDialogueEnd()
    {
        // 효과 스프라이트 정리
        ClearEffectSprites();

        // 대화 종료 시 다음 씬으로 이동
        // DialogueData에 nextSceneName이 지정되어 있으면 그걸 사용, 없으면 기본값 사용
        string targetScene = nextSceneName; // 기본값

        if (dialogueManager != null && dialogueManager.currentDialogue != null)
        {
            if (!string.IsNullOrEmpty(dialogueManager.currentDialogue.nextSceneName))
            {
                targetScene = dialogueManager.currentDialogue.nextSceneName;
            }
        }

        if (!string.IsNullOrEmpty(targetScene))
        {
            Debug.Log($"[DialogueUI] Transitioning to scene: {targetScene}");

            // BattleDialogueScene으로 가는 경우 Fade 없이 바로 전환
            if (targetScene.Contains("BattleDialogue"))
            {
                SceneManager.LoadScene(targetScene);
            }
            else
            {
                // 다른 씬으로는 Fade 사용
                SceneFader.LoadScene(targetScene);
            }
        }
        else
        {
            Debug.LogWarning("[DialogueUI] No next scene specified!");
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

    /// <summary>
    /// 캐릭터를 화면 밖으로 즉시 배치 (슬라이드 인 애니메이션용)
    /// </summary>
    void HideCharactersOffscreen()
    {
        if (leftCharacterImage != null)
        {
            // 목표 위치 저장
            RectTransform leftRect = leftCharacterImage.GetComponent<RectTransform>();
            if (leftRect != null)
            {
                leftCharacterTargetPos = leftRect.anchoredPosition;
                // 화면 밖으로 즉시 이동
                leftRect.anchoredPosition = new Vector2(
                    leftCharacterTargetPos.x + leftCharacterStartOffsetX,
                    leftCharacterTargetPos.y
                );
            }
        }

        if (rightCharacterImage != null)
        {
            // 목표 위치 저장
            RectTransform rightRect = rightCharacterImage.GetComponent<RectTransform>();
            if (rightRect != null)
            {
                rightCharacterTargetPos = rightRect.anchoredPosition;
                // 화면 밖으로 즉시 이동
                rightRect.anchoredPosition = new Vector2(
                    rightCharacterTargetPos.x + rightCharacterStartOffsetX,
                    rightCharacterTargetPos.y
                );
            }
        }

        Debug.Log("[DialogueUI] Characters hidden offscreen for slide-in animation");
    }

    /// <summary>
    /// 양쪽 캐릭터 슬라이드 인 애니메이션
    /// </summary>
    System.Collections.IEnumerator SlideInCharacters()
    {
        RectTransform leftRect = leftCharacterImage != null ? leftCharacterImage.GetComponent<RectTransform>() : null;
        RectTransform rightRect = rightCharacterImage != null ? rightCharacterImage.GetComponent<RectTransform>() : null;

        if (leftRect == null || rightRect == null)
        {
            // 애니메이션 없이 대화창 표시
            if (dialogueBox != null)
                dialogueBox.SetActive(true);
            isSlideInComplete = true;
            yield break;
        }

        // 시작 위치는 이미 HideCharactersOffscreen()에서 설정됨
        Vector2 leftStartPos = leftRect.anchoredPosition;
        Vector2 rightStartPos = rightRect.anchoredPosition;

        float elapsed = 0f;

        while (elapsed < characterSlideInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / characterSlideInDuration;

            // EaseOutCubic
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            if (leftRect != null)
            {
                leftRect.anchoredPosition = Vector2.Lerp(leftStartPos, leftCharacterTargetPos, easeT);
            }

            if (rightRect != null)
            {
                rightRect.anchoredPosition = Vector2.Lerp(rightStartPos, rightCharacterTargetPos, easeT);
            }

            yield return null;
        }

        if (leftRect != null)
            leftRect.anchoredPosition = leftCharacterTargetPos;

        if (rightRect != null)
            rightRect.anchoredPosition = rightCharacterTargetPos;

        Debug.Log("[DialogueUI] Characters slide-in complete");

        // 슬라이드 인 완료 후 대화창 표시
        if (dialogueBox != null)
            dialogueBox.SetActive(true);

        isSlideInComplete = true;
    }
}
