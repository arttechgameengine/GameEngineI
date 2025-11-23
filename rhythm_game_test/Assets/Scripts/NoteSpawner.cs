using UnityEngine; 
using System.Collections.Generic;

public class NoteSpawner : MonoBehaviour
{
    public RectTransform notePrefab;
    public RectTransform spawnPoint;
    public RectTransform hitLine;  // HitLine 참조 추가
    public RectTransform notesParent;
    public float noteSpeed = 500f;

    List<NoteData> notes = new List<NoteData>();
    int nextIndex = 0;
    public double songStartDspTime;
    private bool songStarted = false;

    // SpawnPoint에서 HitLine까지의 거리를 기반으로 계산된 leadTime
    private float spawnLeadTime;

    void Awake()
    {
        // SpawnPoint와 HitLine 사이의 거리를 noteSpeed로 나눠서 leadTime 계산
        float distance = spawnPoint.position.x - hitLine.position.x;
        spawnLeadTime = distance / noteSpeed;
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
        if (!songStarted || notes == null || notes.Count == 0) return;

        double songTime = AudioSettings.dspTime - songStartDspTime;

        while (nextIndex < notes.Count &&
               notes[nextIndex].time - spawnLeadTime <= songTime)
        {
            Spawn(notes[nextIndex]);
            nextIndex++;
        }
    }

    void Spawn(NoteData data)
    {
        RectTransform n = Instantiate(notePrefab, spawnPoint.position, Quaternion.identity, notesParent);
        NoteMovement mv = n.GetComponent<NoteMovement>();
        NoteVisual visual = n.GetComponent<NoteVisual>();

        mv.Init(noteSpeed, data.time, data.type);
        visual.SetType(data.type);
    }
}