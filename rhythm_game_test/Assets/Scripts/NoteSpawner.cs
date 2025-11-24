using UnityEngine;
using System.Collections.Generic;

public class NoteSpawner : MonoBehaviour
{
    public RectTransform notePrefab;
    public RectTransform spawnPoint;
    public RectTransform hitLine;  // HitLine 참조 추가
    public RectTransform notesParent;
    public float noteSpeed = 500f;

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
        audio.PlayScheduled(songStartDspTime);
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

        // 노트가 실제로 hitLine에 도착해야 하는 시간 계산
        // 원래 noteTime이 spawnLeadTime보다 작으면 (음수 스폰 시간)
        // 현재 시간 + spawnLeadTime이 실제 도착 시간이 됨
        float actualHitTime;
        float idealSpawnTime = data.time - spawnLeadTime;

        if (idealSpawnTime < 0)
        {
            // 늦게 스폰된 경우: 현재 시간 + leadTime이 도착 시간
            actualHitTime = (float)currentSongTime + spawnLeadTime;
        }
        else
        {
            // 정상 스폰: 원래 noteTime 사용
            actualHitTime = data.time;
        }

        // NotesParent 기준 로컬 좌표로 스폰 위치 설정
        n.localPosition = new Vector3(spawnLocalX, 0, 0);

        NoteMovement mv = n.GetComponent<NoteMovement>();
        NoteVisual visual = n.GetComponent<NoteVisual>();

        mv.Init(noteSpeed, actualHitTime, data.type);
        visual.SetType(data.type);

        Debug.Log($"[Spawn] original: {data.time:F2}, actual: {actualHitTime:F2}, songTime: {currentSongTime:F2}");
    }
}