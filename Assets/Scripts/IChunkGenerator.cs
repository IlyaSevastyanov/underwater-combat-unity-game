public interface IChunkGenerator
{
    // Сгенерировать содержимое чанка в его локальном пространстве.
    void GenerateChunk(int width, int height, int seed);
}
