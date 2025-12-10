using UnityEngine;
using UnityEngine.EventSystems;

public class MobileVerticalThruster : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI")]
    public RectTransform handle;

    [Header("Настройки")]
    public float maxDistance = 80f;   // сколько пикселей ручка может уходить вверх/вниз
    public float deadZone = 0.15f; // мёртвая зона по центру

    float rawValue;       // -1..1 до учёта deadZone
    Vector2 startPos;

    // То, что будем читать снаружи:
    // > 0 — вверх, < 0 — вниз, 0 — нейтраль
    public float Value
    {
        get
        {
            if (Mathf.Abs(rawValue) < deadZone)
                return 0f;
            return rawValue;
        }
    }

    void Awake()
    {
        if (handle == null)
            handle = transform.GetChild(0) as RectTransform;

        startPos = handle.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransform rt = (RectTransform)transform;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rt, eventData.position, eventData.pressEventCamera, out localPoint))
            return;

        // Нас интересует только Y
        float y = Mathf.Clamp(localPoint.y, -maxDistance, maxDistance);

        handle.anchoredPosition = new Vector2(startPos.x, startPos.y + y);
        rawValue = y / maxDistance; // -1..1
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Фиксатор по центру: при отпускании ручка всегда возвращается в 0
        rawValue = 0f;
        handle.anchoredPosition = startPos;
    }
}
