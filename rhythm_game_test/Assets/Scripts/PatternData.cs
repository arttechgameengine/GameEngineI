using System.Collections.Generic;

[System.Serializable]
public class PatternData
{
    public List<NoteData> notes;
}

[System.Serializable]
public class NoteData
{
    public float time;
    public string type;
}
