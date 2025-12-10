using UnityEngine;

public class AutoDespawn : MonoBehaviour
{
    public float lifetime = 20f;
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
