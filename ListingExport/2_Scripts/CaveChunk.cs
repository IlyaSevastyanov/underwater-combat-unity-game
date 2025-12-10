using UnityEngine;

public class CaveChunk : MonoBehaviour, IChunkGenerator
{
    [Header("Tiles")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;

    [Header("Cave Params")]
    [Range(0, 100)] public int fillPercent = 48;  // стартовая зашумлённость
    public int smoothIterations = 5;              // сглаживания клеточного автомата

    [Header("Encounters")]
    [Range(0, 1)] public float encounterChance = 0.04f; // шанс спавна встречи на клетке пола
    public GameObject encounterPrefab;

    int[,] map; // 1 = wall, 0 = empty

    public void GenerateChunk(int width, int height, int seed)
    {
        // Чистим старое
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        var rng = new System.Random(seed);

        // 1) Заполняем шумом
        map = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                bool border = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                map[x, y] = (border || rng.Next(100) < fillPercent) ? 1 : 0;
            }

        // 2) Сглаживаем
        for (int i = 0; i < smoothIterations; i++)
        {
            int[,] nm = (int[,])map.Clone();
            for (int x = 1; x < width - 1; x++)
                for (int y = 1; y < height - 1; y++)
                {
                    int walls = 0;
                    for (int ix = x - 1; ix <= x + 1; ix++)
                        for (int iy = y - 1; iy <= y + 1; iy++)
                            if (!(ix == x && iy == y)) walls += map[ix, iy];

                    if (walls > 4) nm[x, y] = 1;
                    else if (walls < 4) nm[x, y] = 0;
                }
            map = nm;
        }

        // 3) Строим стены/пол
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var local = new Vector3(x, y, 0);
                var world = transform.position + local;

                if (map[x, y] == 0) // пустота => пол + шанс встречи
                {
                    var f = Instantiate(floorPrefab, world, Quaternion.identity, transform);

                    // Спавн встречи (триггера)
                    if (encounterPrefab && Random01(rng) < encounterChance)
                    {
                        Instantiate(encounterPrefab, world, Quaternion.identity, transform);
                    }
                }
                else // стена
                {
                    Instantiate(wallPrefab, world, Quaternion.identity, transform);
                }
            }
    }

    float Random01(System.Random rng) => (float)rng.NextDouble();
}
