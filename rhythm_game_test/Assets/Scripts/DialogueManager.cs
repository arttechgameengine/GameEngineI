using UnityEngine;
using UnityEngine.Events;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Current Dialogue")]
    public DialogueData currentDialogue;

    [Header("Events")]
    public UnityEvent onDialogueStart;
    public UnityEvent onDialogueEnd;
    public UnityEvent<DialogueLine> onLineChanged;

    private int currentLineIndex = 0;
    private bool isDialogueActive = false;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!isDialogueActive) return;

        // 클릭이나 스페이스바로 다음 대사
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            NextLine();
        }
    }

    // 대화 시작
    public void StartDialogue(DialogueData dialogue)
    {
        if (dialogue == null || dialogue.lines.Length == 0)
        {
            Debug.LogWarning("DialogueManager: No dialogue data or empty lines!");
            return;
        }

        currentDialogue = dialogue;
        currentLineIndex = 0;
        isDialogueActive = true;

        onDialogueStart?.Invoke();
        ShowCurrentLine();
    }

    // 현재 대화 시작 (Inspector에서 할당된 currentDialogue 사용)
    public void StartCurrentDialogue()
    {
        StartDialogue(currentDialogue);
    }

    // 다음 대사로 진행
    public void NextLine()
    {
        if (!isDialogueActive) return;

        currentLineIndex++;

        if (currentLineIndex >= currentDialogue.lines.Length)
        {
            EndDialogue();
        }
        else
        {
            ShowCurrentLine();
        }
    }

    // 현재 대사 표시
    void ShowCurrentLine()
    {
        if (currentDialogue == null || currentLineIndex >= currentDialogue.lines.Length)
            return;

        DialogueLine line = currentDialogue.lines[currentLineIndex];
        onLineChanged?.Invoke(line);
    }

    // 대화 종료
    public void EndDialogue()
    {
        isDialogueActive = false;
        onDialogueEnd?.Invoke();
    }

    // 대화 스킵
    public void SkipDialogue()
    {
        if (isDialogueActive)
        {
            EndDialogue();
        }
    }

    // 현재 대화 진행 상태
    public bool IsDialogueActive => isDialogueActive;
    public int CurrentLineIndex => currentLineIndex;
    public int TotalLines => currentDialogue != null ? currentDialogue.lines.Length : 0;
}
