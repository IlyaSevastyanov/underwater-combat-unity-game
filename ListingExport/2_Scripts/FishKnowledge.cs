using UnityEngine;
using System.Collections.Generic;

public class FishKnowledge : MonoBehaviour
{
    public static FishKnowledge Instance;

    // какие рыбы уже были "разделаны"
    private HashSet<string> knownFish = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // если надо переживать сцену Ц раскомментируй:
        // DontDestroyOnLoad(gameObject);
    }

    public bool IsKnown(string fishName)
    {
        return knownFish.Contains(fishName);
    }

    public void MarkKnown(string fishName)
    {
        if (!string.IsNullOrEmpty(fishName))
            knownFish.Add(fishName);
    }
}
