using System.Collections;
using UnityEngine;
using TMPro;

public class CrewPoisonMessage : MonoBehaviour
{
    [Header("UI refs")]
    public TMP_Text messageText;        // сам текст (TextMeshPro)

    [Header("Text")]
    [TextArea]
    public string poisonedText = "„лены экипажа отравлены!";

    [Header("Timing")]
    public float showTime = 3f;          // сколько времени висит сообщение

    void Awake()
    {
        // если не задано в инспекторе Ц берЄм с этого же объекта
        if (messageText == null)
            messageText = GetComponent<TMP_Text>();

        // текст изначально выключен, но объект активен
        if (messageText != null)
            messageText.enabled = false;
    }

    public void Show()
    {
        if (messageText == null) return;

        StopAllCoroutines();
        StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        messageText.text = poisonedText;
        messageText.enabled = true;   // показываем текст

        yield return new WaitForSeconds(showTime);

        messageText.enabled = false;  // скрываем текст, но объект остаЄтс€ активным
    }
}
