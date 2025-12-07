using System.Collections.Generic;

[System.Serializable]
public class PatternData
{
    public string songName;
    public float bpm;
    public float offset;
    public int numberOfLanes;
    public float noteSpeed = 500f;  // 노트 이동 속도 (기본값 500)
    public List<NoteData> notes;
}

[System.Serializable]
public class NoteData
{
    public float time;
    public int lane;
    public string type;          // "tap", "hold", or "rapid"
    public string arrow;         // "UP", "DOWN", "LEFT", "RIGHT", "SPACE"
    public float duration;       // hold note duration (only for hold type)

    // Rapid note settings (only for rapid type)
    public int rapidCount = 0;        // 연타 필요 횟수 (예: 5회)
    public float rapidDuration = 0f;  // 연타 제한 시간 (예: 1.0초)

    // 런타임에서 롱노트 처리용 (JSON에는 없음, 로드 후 자동 생성)
    [System.NonSerialized] public string noteSubType = "NORMAL";    // "NORMAL", "LONG_START", "LONG_HOLD", "LONG_END", "RAPID"
    [System.NonSerialized] public int longNoteGroupId = -1;
    [System.NonSerialized] public float longNoteDuration = 0f;
}
