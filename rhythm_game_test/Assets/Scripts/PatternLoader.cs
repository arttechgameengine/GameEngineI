using UnityEngine; 
using System.IO;

public static class PatternLoader
{
    public static PatternData LoadPattern(string fileName)
    {
        string path = Path.Combine(Application.dataPath, fileName);
        if (!File.Exists(path))
        {
            Debug.LogError("JSON ���� ���� ����: " + path);
            return new PatternData();
        }

        string jsonText = File.ReadAllText(path);
        return Load(jsonText);
    }

    public static PatternData Load(string jsonText)
    {
        return JsonUtility.FromJson<PatternData>(jsonText);
    }
}
