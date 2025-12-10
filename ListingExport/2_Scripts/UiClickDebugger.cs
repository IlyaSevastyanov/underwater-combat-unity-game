using UnityEngine;
using UnityEngine.EventSystems;

public class UiClickDebugger : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[UiClickDebugger] Click on " + gameObject.name +
                  " button, event=" + eventData.button);
    }
}
