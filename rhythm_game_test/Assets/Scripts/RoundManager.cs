using UnityEngine; 

public class RoundManager : MonoBehaviour
{
    public NoteSpawner spawner;
    public AudioSource bgmSource;
    public TextAsset jsonPattern;

    void Start()
    {
        PatternData pattern = PatternLoader.Load(jsonPattern.text);

        spawner.LoadPattern(pattern);

        // 🔥 여기서 bgmSource를 넘겨야 함
        spawner.StartSong(bgmSource);
    }
}
