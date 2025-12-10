using UnityEngine;
using UnityEngine.UI;

public class CausticsScroll : MonoBehaviour
{
    public RawImage img;
    public Vector2 scrollSpeed = new Vector2(0.02f, 0.01f);

    void Reset()
    {
        img = GetComponent<RawImage>();
    }

    void Update()
    {
        if (img != null && img.texture != null)
        {
            // просто смещаем uv-координаты
            Rect r = img.uvRect;
            r.x += scrollSpeed.x * Time.deltaTime;
            r.y += scrollSpeed.y * Time.deltaTime;
            img.uvRect = r;
        }
    }
}
