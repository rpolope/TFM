using Unity.Mathematics;
using UnityEngine;

public static class BatchesManager
{
    private static int _currentBatchX;
    private static int _currentBatchY;
    private static Batch[,] _batches;
    private const int BatchesNum = 1;
    private const int ChunksPerBatchSide = 7;
    private static Batch _currentBatch;

    public static void Initialize()
    {
        _batches = new Batch[BatchesNum, BatchesNum];
        InitializeBatches();
        UpdateCurrentBatch();
    }

    private static void UpdateCurrentBatch()
    {
        _currentBatchX = Viewer.ChunkCoord.Longitude / ChunksPerBatchSide;
        _currentBatchY = Viewer.ChunkCoord.Latitude / ChunksPerBatchSide;

        _currentBatch = _batches[_currentBatchX, _currentBatchY];
        _currentBatch.SetActive(true);
    }

    private static void InitializeBatches()
    {
        for (int y = 0; y < BatchesNum; y++)
        {
            for (int x = 0; x < BatchesNum; x++)
            {
                _batches[x, y] = new Batch(new int2(x * ChunksPerBatchSide, y * ChunksPerBatchSide));
            }
        }
    }

    static void Update()
    {
        // Add logic to change batches based on player position or other criteria
    }

    public class Batch
    {
        private TerrainChunk[,] _chunks;
        private GameObject _gameObject;

        public Batch(int2 coords)
        {
            _gameObject = new GameObject($"Batch({coords.x},{coords.y})") 
            {
                transform =
                {
                    position = new Vector3(coords.x, coords.y), 
                    parent = LandscapeManager.Instance.transform
                }
            };
            _gameObject.SetActive(true);
            
            LoadBatch(coords);
        }

        private void LoadBatch(int2 batchCoords)
        {
            TerrainChunksManager.InitializeTerrainChunks(ChunksPerBatchSide, batchCoords, out _chunks, _gameObject.transform);
        }

        public void SetActive(bool active)
        {
            _gameObject.SetActive(active);
        }
    }
}
