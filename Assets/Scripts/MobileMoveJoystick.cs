using UnityEngine;
using UnityEngine.EventSystems;

public class MobileMoveJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI")]
    public RectTransform handle;   // внутренн€€ пипка

    [Header("Ќастройки")]
    public float maxRadius = 80f;  // радиус хода пипки
    public float deadZone = 0.1f; // мЄртва€ зона по центру

    private Vector2 input;         // -1..1 по X/Y
    private Vector2 startPos;

    public float Horizontal
    {
        get
        {
            float v = Mathf.Abs(input.x) < deadZone ? 0f : input.x;
            Debug.Log($"[Joystick] H={v:F2}");
            return v;
        }
    }

    public float Vertical
    {
        get
        {
            float v = Mathf.Abs(input.y) < deadZone ? 0f : input.y;
            Debug.Log($"[Joystick] V={v:F2}");
            return v;
        }
    }

    private void Awake()
    {
        Debug.Log("[Joystick] Awake");

        if (!handle)
        {
            if (transform.childCount > 0)
                handle = transform.GetChild(0) as RectTransform;
        }

        if (!handle)
        {
            Debug.LogWarning("[Joystick] Handle не назначен!");
        }
        else
        {
            startPos = handle.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        Debug.Log("[Joystick] OnEnable");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[Joystick] OnPointerDown");
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransform rt = (RectTransform)transform;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rt, eventData.position, eventData.pressEventCamera, out localPoint))
            return;

        // центр джойстика Ч там, где ручка была изначально
        Vector2 center = startPos;

        // смещение от центра
        Vector2 offset = localPoint - center;

        // ограничиваем радиус
        if (offset.magnitude > maxRadius)
            offset = offset.normalized * maxRadius;

        // двигаем ручку
        if (handle)
            handle.anchoredPosition = center + offset;

        // нормализованное значение -1..1
        input = offset / maxRadius;

        Debug.Log($"[Joystick] Drag input={input}");
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("[Joystick] OnPointerUp");

        input = Vector2.zero;

        if (handle)
            handle.anchoredPosition = startPos;
    }
}
