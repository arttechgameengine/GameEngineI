using UnityEngine;
using System.Collections.Generic;

public class NoteSpawner : MonoBehaviour
{
    public RectTransform notePrefab;
    public RectTransform spawnPoint;
    public RectTransform hitLine;  // HitLine 참조 추가
    public RectTransform notesParent;
    public float noteSpeed = 500f;

    [Header("Visual Scale")]
    public RectTransform shakeablePanel;  // 전체 게임플레이 UI (스케일 조정용)

    [Header("Enemy Reference")]
    public Transform enemySprite;  // 적 스프라이트 (Canvas 안의 Enemy Image)

    [Header("Long Note Visual")]
    public RectTransform longNoteBarPrefab;  // 롱노트 시각적 막대 Prefab

    [Header("Rapid Note Visual")]
    public RectTransform rapidNoteUIPrefab;  // 연타 노트 UI Prefab (타이머, 카운터)

    [Header("Audio")]
    public AudioSource bgmSource;

    [Header("End Song Settings")]
    public float endDelay = 2f;  // 곡 끝난 후 결과 화면까지 대기 시간

    List<NoteData> notes = new List<NoteData>();
    int nextIndex = 0;
    public double songStartDspTime;
    private bool songStarted = false;
    private bool songEnded = false;

    // SpawnPoint에서 HitLine까지의 거리를 기반으로 계산된 leadTime
    private float spawnLeadTime;

    // 스폰 시 사용할 로컬 X 좌표 (NotesParent 기준)
    private float spawnLocalX;
    // HitLine의 로컬 X 좌표 (NotesParent 기준) - 판정용으로 공개
    public float hitLineLocalX { get; private set; }

    void Awake()
    {
        // NotesParent 기준 로컬 좌표로 변환
        spawnLocalX = notesParent.InverseTransformPoint(spawnPoint.position).x;
        hitLineLocalX = notesParent.InverseTransformPoint(hitLine.position).x;
    }

    public void LoadPattern(PatternData pattern)
    {
        notes = pattern.notes;
        nextIndex = 0;
        songStarted = false;

        // JSON의 노트 속도 적용
        if (pattern.noteSpeed > 0)
        {
            noteSpeed = pattern.noteSpeed;
            Debug.Log($"[NoteSpawner] Loaded noteSpeed from JSON: {noteSpeed}");
        }

        // 거리 계산 및 leadTime 재계산
        float distance = spawnLocalX - hitLineLocalX;
        spawnLeadTime = distance / noteSpeed;

        Debug.Log($"[NoteSpawner] spawnLocalX: {spawnLocalX}, hitLineLocalX: {hitLineLocalX}, distance: {distance}, spawnLeadTime: {spawnLeadTime}");
    }

    public void StartSong(AudioSource audio)
    {
        songStartDspTime = AudioSettings.dspTime;
        audio.Play();
        songStarted = true;
    }

    void Update()
    {
        // 일시정지 중에는 스폰하지 않음
        if (PauseManager.IsPaused) return;

        if (!songStarted || notes == null || notes.Count == 0) return;

        // 이미 곡이 끝났으면 처리 안 함
        if (songEnded) return;

        double songTime = AudioSettings.dspTime - songStartDspTime;

        while (nextIndex < notes.Count)
        {
            NoteData note = notes[nextIndex];

            // SPACE 노트는 등장 애니메이션 시간(0.5초)만큼 일찍 스폰
            float extraLeadTime = (note.arrow == "SPACE") ? 0.5f : 0f;

            if (note.time - spawnLeadTime - extraLeadTime <= songTime)
            {
                Spawn(note, songTime);
                nextIndex++;
            }
            else
            {
                break;
            }
        }

        // 곡 종료 체크: 음악이 끝났고, 모든 노트가 스폰되었고, 화면에 노트가 없으면
        CheckSongEnd();
    }

    void CheckSongEnd()
    {
        // 이미 종료 처리 중이면 리턴
        if (songEnded) return;

        // 음악이 재생 중이면 아직 끝나지 않음
        if (bgmSource != null && bgmSource.isPlaying)
        {
            return;
        }

        // 모든 노트가 스폰되지 않았으면 아직 끝나지 않음
        if (nextIndex < notes.Count)
        {
            Debug.Log($"[NoteSpawner] Song not ended - {notes.Count - nextIndex} notes remaining to spawn");
            return;
        }

        // 화면에 노트가 남아있으면 아직 끝나지 않음
        if (notesParent.childCount > 0)
        {
            Debug.Log($"[NoteSpawner] Song not ended - {notesParent.childCount} notes still on screen");
            return;
        }

        // 곡 종료!
        songEnded = true;
        Debug.Log($"[NoteSpawner] ===== SONG ENDED! ===== Music stopped: {!bgmSource.isPlaying}, All notes spawned: {nextIndex}/{notes.Count}, Notes on screen: {notesParent.childCount}");

        // 잠시 후 결과 화면으로 이동
        Debug.Log($"[NoteSpawner] Invoking GoToResult in {endDelay} seconds...");
        Invoke(nameof(GoToResult), endDelay);
    }

    void GoToResult()
    {
        Debug.Log("[NoteSpawner] GoToResult() called!");

        if (ScoreManager.Instance != null)
        {
            Debug.Log($"[NoteSpawner] ScoreManager found. Stats - Score: {ScoreManager.Instance.currentScore}, Combo: {ScoreManager.Instance.maxCombo}");
            Debug.Log("[NoteSpawner] Calling GoToResultScene()...");

            ScoreManager.Instance.PrintStats();
            ScoreManager.Instance.GoToResultScene();

            Debug.Log("[NoteSpawner] GoToResultScene() finished!");
        }
        else
        {
            Debug.LogError("[NoteSpawner] ===== ERROR: ScoreManager.Instance is NULL! =====");
            Debug.LogError("[NoteSpawner] Cannot go to result scene! Check if ScoreManager exists in the scene!");

            // ScoreManager 찾기 시도
            ScoreManager sm = FindObjectOfType<ScoreManager>();
            if (sm != null)
            {
                Debug.LogError($"[NoteSpawner] Found ScoreManager in scene, but Instance is null! Object: {sm.gameObject.name}");
            }
            else
            {
                Debug.LogError("[NoteSpawner] No ScoreManager found in the entire scene!");
            }
        }
    }

    void Spawn(NoteData data, double currentSongTime)
    {
        RectTransform n = Instantiate(notePrefab, notesParent);

        // arrow 필드를 사용 (방향키: UP, DOWN, LEFT, RIGHT, SPACE)
        string arrowKey = data.arrow;
        bool isSpaceNote = (arrowKey == "SPACE");

        // 노트의 판정 시간은 JSON 원본 그대로 사용 (준비 시간은 songStartDspTime에 이미 반영됨)
        float actualHitTime = data.time;

        // NotesParent 기준 로컬 좌표로 스폰 위치 설정
        n.localPosition = new Vector3(spawnLocalX, 0, 0);

        NoteMovement mv = n.GetComponent<NoteMovement>();
        NoteVisual visual = n.GetComponent<NoteVisual>();
        NoteEffect effect = n.GetComponent<NoteEffect>();

        // 노트 초기화 (arrowKey를 noteType으로, noteSubType과 longNoteGroupId 전달)
        mv.Init(noteSpeed, actualHitTime, arrowKey, data.noteSubType, data.longNoteGroupId);

        // 연타 노트가 아닌 경우에만 부모 노트에 방향키 sprite 설정
        if (data.noteSubType != "RAPID")
        {
            visual.SetType(arrowKey);
        }

        // 연타 노트 처리
        if (data.noteSubType == "RAPID")
        {
            // RapidNoteJudge 컴포넌트 추가
            RapidNoteJudge rapidJudge = n.gameObject.AddComponent<RapidNoteJudge>();
            rapidJudge.Initialize(data.rapidCount, data.rapidDuration, arrowKey);

            // Rapid UI 프리팹 생성 (Long Note Bar처럼)
            GameObject rapidUI = CreateRapidNoteUI(n, data.rapidCount, visual);
            if (rapidUI != null)
            {
                // RapidNoteVisual 컴포넌트 찾기
                RapidNoteVisual rapidVisual = rapidUI.GetComponent<RapidNoteVisual>();
                if (rapidVisual != null)
                {
                    rapidJudge.rapidVisual = rapidVisual;
                    rapidVisual.SetRapidInfo(data.rapidCount);

                    // 연타 노트는 rapidBackground에 방향키 sprite 설정 (clipping 방지)
                    rapidVisual.SetArrowSprite(visual, arrowKey);
                }
            }

            Debug.Log($"[Spawn] Rapid Note - Arrow: {arrowKey}, Count: {data.rapidCount}, Duration: {data.rapidDuration}s");
        }

        // 롱노트 시작 노트면 시각적 막대 생성
        if (data.noteSubType == "LONG_START" && data.longNoteDuration > 0f)
        {
            GameObject longBar = CreateLongNoteBar(n, data.longNoteDuration, arrowKey);
            mv.longNoteVisualBar = longBar;
        }

        // SPACE 노트면 Enemy Sprite 참조 할당
        if (isSpaceNote && effect != null && enemySprite != null)
        {
            effect.enemySprite = enemySprite;
        }

        Debug.Log($"[Spawn] arrow: {arrowKey}, type: {data.type}, subType: {data.noteSubType}, groupId: {data.longNoteGroupId}, time: {actualHitTime:F2}");
    }

    /// <summary>
    /// 롱노트 시각적 막대 생성 (spawnPoint에서 시작하여 왼쪽으로 늘어나는 막대)
    /// </summary>
    GameObject CreateLongNoteBar(RectTransform startNote, float duration, string noteType)
    {
        if (longNoteBarPrefab == null)
        {
            Debug.LogWarning("[NoteSpawner] longNoteBarPrefab이 설정되지 않았습니다!");
            return null;
        }

        // 롱노트 막대 생성
        RectTransform bar = Instantiate(longNoteBarPrefab, notesParent);

        // 막대 길이 계산: duration * noteSpeed
        float barLength = duration * noteSpeed;

        // 막대 크기 설정 (가로 길이)
        bar.sizeDelta = new Vector2(barLength, startNote.sizeDelta.y);

        // 막대의 pivot을 왼쪽으로 설정 (왼쪽 끝이 spawnPoint에 고정, 오른쪽으로 늘어남)
        bar.pivot = new Vector2(0, 0.5f);

        // spawnLocalX 위치에 막대 배치 (pivot이 왼쪽이므로 막대 왼쪽 끝이 spawnPoint에 위치)
        bar.localPosition = new Vector3(spawnLocalX, 0, 0);

        // 막대 색상 설정 (흰색 반투명)
        UnityEngine.UI.Image barImage = bar.GetComponent<UnityEngine.UI.Image>();
        if (barImage != null)
        {
            barImage.sprite = null;  // sprite 사용 안 함 (rectangle로 표시)
            barImage.color = new Color(1f, 1f, 1f, 0.5f);  // 흰색 반투명

            Debug.Log($"[CreateLongNoteBar] Created bar - length: {barLength}, size: {bar.sizeDelta}, pos: {bar.localPosition}");
        }
        else
        {
            Debug.LogWarning($"[CreateLongNoteBar] barImage is null!");
        }

        // START 노트보다 뒤에 표시 (인덱스를 낮게 = 먼저 그려짐 = 뒤에 보임)
        bar.SetSiblingIndex(0);

        return bar.gameObject;
    }

    /// <summary>
    /// 연타 노트 UI 생성 (타이머, 카운터)
    /// </summary>
    GameObject CreateRapidNoteUI(RectTransform parentNote, int requiredCount, NoteVisual visual)
    {
        if (rapidNoteUIPrefab == null)
        {
            Debug.LogWarning("[NoteSpawner] rapidNoteUIPrefab이 설정되지 않았습니다!");
            return null;
        }

        // 부모 노트의 RectMask2D 제거 (clipping 방지)
        UnityEngine.UI.RectMask2D rectMask = parentNote.GetComponent<UnityEngine.UI.RectMask2D>();
        if (rectMask != null)
        {
            Destroy(rectMask);
            Debug.Log($"[CreateRapidNoteUI] Removed RectMask2D from parent note");
        }

        // 부모 노트의 Image는 비활성화 (자식 UI가 보이도록)
        UnityEngine.UI.Image parentImage = parentNote.GetComponent<UnityEngine.UI.Image>();
        if (parentImage != null)
        {
            parentImage.enabled = false;  // Image 컴포넌트 비활성화 (alpha 상속 문제 방지)
            Debug.Log($"[CreateRapidNoteUI] Disabled parent image");
        }

        // Rapid UI 프리팹 생성 (노트의 자식으로)
        RectTransform rapidUI = Instantiate(rapidNoteUIPrefab, parentNote);

        // 로컬 위치 초기화 (노트 중앙)
        rapidUI.localPosition = Vector3.zero;
        rapidUI.localScale = Vector3.one;

        Debug.Log($"[CreateRapidNoteUI] Created Rapid UI - Required Count: {requiredCount}");

        return rapidUI.gameObject;
    }
}
