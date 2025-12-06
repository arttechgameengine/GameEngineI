using UnityEngine;
using System.Collections.Generic;

public class NoteSpawner : MonoBehaviour
{
    public RectTransform notePrefab;
    public RectTransform spawnPoint;
    public RectTransform hitLine;  // HitLine 참조 추가
    public RectTransform notesParent;
    public float noteSpeed = 500f;

    [Header("Enemy Reference")]
    public Transform enemySprite;  // 적 스프라이트 (Canvas 안의 Enemy Image)

    [Header("Long Note Visual")]
    public RectTransform longNoteBarPrefab;  // 롱노트 시각적 막대 Prefab

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

        // 거리 계산
        float distance = spawnLocalX - hitLineLocalX;
        spawnLeadTime = distance / noteSpeed;

        Debug.Log($"[NoteSpawner] spawnLocalX: {spawnLocalX}, hitLineLocalX: {hitLineLocalX}, distance: {distance}, spawnLeadTime: {spawnLeadTime}");
    }

    public void LoadPattern(PatternData pattern)
    {
        notes = pattern.notes;
        nextIndex = 0;
        songStarted = false;
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

        while (nextIndex < notes.Count &&
               notes[nextIndex].time - spawnLeadTime <= songTime)
        {
            Spawn(notes[nextIndex], songTime);
            nextIndex++;
        }

        // 곡 종료 체크: 음악이 끝났고, 모든 노트가 스폰되었고, 화면에 노트가 없으면
        CheckSongEnd();
    }

    void CheckSongEnd()
    {
        // 음악이 재생 중이면 아직 끝나지 않음
        if (bgmSource != null && bgmSource.isPlaying) return;

        // 모든 노트가 스폰되지 않았으면 아직 끝나지 않음
        if (nextIndex < notes.Count) return;

        // 화면에 노트가 남아있으면 아직 끝나지 않음
        if (notesParent.childCount > 0) return;

        // 곡 종료!
        songEnded = true;
        Debug.Log("[NoteSpawner] Song ended! Going to result scene...");

        // 잠시 후 결과 화면으로 이동
        Invoke(nameof(GoToResult), endDelay);
    }

    void GoToResult()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.GoToResultScene();
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

        visual.SetType(arrowKey);

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
}
