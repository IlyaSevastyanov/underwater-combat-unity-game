using UnityEngine;

public class FishData : MonoBehaviour
{
    [Header("Identity")]
    public string fishName = "Неизвестная рыба";
    [TextArea(2, 5)]
    public string description = "Описание этой рыбы...";
    public ParticleSystem butcherFXPrefab;
    [Header("Type")]
    public bool isHostile = false; // опасная? да/нет
    public bool istoxic = false; // Ядовитая? да/нет

    [Header("Scoring / effects")]
    public int scoreValue = 10;      // сколько очков даёт (мирная рыба)
    public int scorePenalty = 20;    // сколько очков отнимает (опасная рыба)
    public float healthPenalty = 20; // сколько хп снимает опасная рыба
    public float healthHeal = 12;    // сколько хп даёт хорошая рыба (0 = не лечит)

    [HideInInspector]
    public bool caught = false; // чтобы не триггерилось 2 раза
}
