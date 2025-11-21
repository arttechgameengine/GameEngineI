using UnityEngine;

public class NoteMovement : MonoBehaviour
{
    public float speed;
    public float noteTime;
    public string noteType;

    RectTransform rect;
    private Vector2 startPosition;  // 🔥 추가

    public void Init(float s, float t, string type)
    {
        speed = s;
        noteTime = t;
        noteType = type;
        startPosition = rect.anchoredPosition;  // 🔥 시작 위치 저장
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