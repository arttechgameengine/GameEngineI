using UnityEngine; 

public class NoteMovement : MonoBehaviour
{
    public float speed;
    public float noteTime;
    public string noteType;
    public bool isJudged = false;  // 판정 완료 여부

    RectTransform rect;

    public void Init(float s, float t, string type)
    {
        speed = s;
        noteTime = t;
        noteType = type;
        isJudged = false;
    }

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        rect.anchoredPosition += Vector2.left * speed * Time.deltaTime;

        if (rect.anchoredPosition.x < -3000f)
            Destroy(gameObject);
    }
}