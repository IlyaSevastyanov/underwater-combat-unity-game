using UnityEngine;

public class MenuMusic : MonoBehaviour
{
    private AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        if (!src)
        {
            Debug.LogWarning("[MenuMusic] AudioSource не найден на объекте MenuAudio.");
        }
    }

    // Вызвать, когда начинаем игру.
    public void StopAndDestroy()
    {
        if (src)
        {
            src.Stop();
        }

        // убиваем объект, чтобы музыка не тащилась дальше между сценами
        Destroy(gameObject);
    }
}
