using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Вешается на слайдер (или на его кнопку/область).
/// При наведении / выборе показывает текст в общем label-е.
/// </summary>
public class SliderHint : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler
{
    [Header("Текст подсказки для этого слайдера")]
    [TextArea(2, 4)]
    public string hintText;

    [Header("Куда выводить подсказку")]
    public TextMeshProUGUI targetLabel;

    void Show()
    {
        if (targetLabel != null)
            targetLabel.text = hintText;
    }

    void Clear()
    {
        if (targetLabel != null)
            targetLabel.text = "";
    }

    // мышь навели на элемент
    public void OnPointerEnter(PointerEventData eventData) => Show();

    // мышь ушла
    public void OnPointerExit(PointerEventData eventData) => Clear();

    // фокус с клавиатуры / геймпада
    public void OnSelect(BaseEventData eventData) => Show();
    public void OnDeselect(BaseEventData eventData) => Clear();
}
