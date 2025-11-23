using UnityEngine; 
using UnityEngine.UI;

public class CookingEffect : MonoBehaviour
{
    public static CookingEffect Instance { get; private set; }

    [Header("Cooking Animation Container")]
    public RectTransform cookingArea;  // 요리 애니메이션이 표시될 영역

    [Header("Ingredient Prefabs - 각 노트별 요리 재료")]
    public GameObject leftIngredientPrefab;   // LEFT 노트 재료
    public GameObject rightIngredientPrefab;  // RIGHT 노트 재료
    public GameObject upIngredientPrefab;     // UP 노트 재료
    public GameObject downIngredientPrefab;   // DOWN 노트 재료
    public GameObject spaceIngredientPrefab;  // SPACE 노트 재료

    [Header("Spawn Settings")]
    public float ingredientLifetime = 1.0f;  // 재료가 보이는 시간

    void Awake()
    {
        Instance = this;
    }

    public void PlayCookingEffect(string noteType)
    {
        GameObject prefab = GetPrefabForType(noteType);
        if (prefab == null || cookingArea == null) return;

        // 재료 생성
        GameObject ingredient = Instantiate(prefab, cookingArea);

        // Animator가 있으면 자동 재생됨
        // 일정 시간 후 파괴
        Destroy(ingredient, ingredientLifetime);
    }

    GameObject GetPrefabForType(string noteType)
    {
        switch (noteType)
        {
            case "LEFT": return leftIngredientPrefab;
            case "RIGHT": return rightIngredientPrefab;
            case "UP": return upIngredientPrefab;
            case "DOWN": return downIngredientPrefab;
            case "SPACE": return spaceIngredientPrefab;
            default: return null;
        }
    }
}
