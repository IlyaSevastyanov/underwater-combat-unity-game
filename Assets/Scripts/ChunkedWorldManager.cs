using System.Collections.Generic;
using UnityEngine;

public class ChunkedWorldManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject chunkPrefab; // На нём висит компонент, реализующий IChunkGenerator

    [Header("Chunk Settings")]
    public int chunkSize = 32;   // клеток по X/Y
    public int viewRadius = 1;   // 1 => 3x3 чанка вокруг игрока
    public int baseSeed = 123456;

    // Ключ = координаты чанка, Значение = инстанс GO
    private readonly Dictionary<Vector2Int, GameObject> loaded = new();

    void Update()
    {
        if (!player || !chunkPrefab) return;

        Vector2Int center = WorldToChunk(player.position);

        // Загрузить окрестность
        for (int cx = center.x - viewRadius; cx <= center.x + viewRadius; cx++)
            for (int cy = center.y - viewRadius; cy <= center.y + viewRadius; cy++)
                EnsureChunk(new Vector2Int(cx, cy));

        // Выгрузить далёкие
        var toRemove = new List<Vector2Int>();
        foreach (var kv in loaded)
        {
            var c = kv.Key;
            if (Mathf.Abs(c.x - center.x) > viewRadius + 1 ||
                Mathf.Abs(c.y - center.y) > viewRadius + 1)
                toRemove.Add(c);
        }
        foreach (var c in toRemove)
        {
            Destroy(loaded[c]);
            loaded.Remove(c);
        }
    }

    Vector2Int WorldToChunk(Vector3 pos)
    {
        int cx = Mathf.FloorToInt(pos.x / chunkSize);
        int cy = Mathf.FloorToInt(pos.y / chunkSize);
        return new Vector2Int(cx, cy);
    }

    void EnsureChunk(Vector2Int cpos)
    {
        if (loaded.ContainsKey(cpos)) return;

        Vector3 worldPos = new(cpos.x * chunkSize, cpos.y * chunkSize, 0);
        var go = Instantiate(chunkPrefab, worldPos, Quaternion.identity, transform);

        // Уникальный seed для каждого чанка — детерминированный
        int seed = HashSeed(baseSeed, cpos.x, cpos.y);

        var gen = go.GetComponent<IChunkGenerator>();
        if (gen != null)
        {
            gen.GenerateChunk(chunkSize, chunkSize, seed);
        }

        loaded[cpos] = go;
        go.name = $"Chunk_{cpos.x}_{cpos.y}";
    }

    // Простая детерминированная хеш-функция
    int HashSeed(int baseSeed, int x, int y)
    {
        unchecked
        {
            int h = baseSeed;
            h = h * 73856093 ^ x;
            h = h * 19349663 ^ y;
            return h;
        }
    }
}
