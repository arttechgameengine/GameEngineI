using UnityEngine; 
using UnityEngine.UI;

public class NoteVisual : MonoBehaviour
{
    public Image image;

    public Sprite leftSprite;
    public Sprite rightSprite;
    public Sprite upSprite;
    public Sprite downSprite;
    public Sprite spaceSprite;

    public void SetType(string type)
    {
        switch (type)
        {
            case "LEFT": image.sprite = leftSprite; break;
            case "RIGHT": image.sprite = rightSprite; break;
            case "UP": image.sprite = upSprite; break;
            case "DOWN": image.sprite = downSprite; break;
            case "SPACE": image.sprite = spaceSprite; break;
            default:
                Debug.LogWarning("Unknown note type: " + type);
                image.sprite = leftSprite; // fallback sprite
                break;
        }
    }
}
