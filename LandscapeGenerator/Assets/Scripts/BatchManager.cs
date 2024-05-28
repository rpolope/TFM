using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

public static class BatchesManager
{
    private static Batch[,] _batches;
    private const int BatchesNum = 1;
    private const int ChunksPerBatchSide = 32;
    private static readonly Dictionary<int2, Batch> ActiveBatches = new Dictionary<int2, Batch>();
    private static Batch _currentBatch;

    public static void Initialize()
    {
        _batches = new Batch[BatchesNum, BatchesNum];
        InitializeBatches();
        SetCurrentBatch();
        UpdateBatches();
    }

    private static void SetCurrentBatch()
    {
        int currentBatchXCoord = Mathf.Clamp((Viewer.ChunkCoord.Latitude / LandscapeManager.MapHeight) / ChunksPerBatchSide, 0, LandscapeManager.MapHeight);
        int currentBatchYCoord = Mathf.Clamp((Viewer.ChunkCoord.Longitude / LandscapeManager.MapWidth) / ChunksPerBatchSide, 0, LandscapeManager.MapWidth);

        if (_currentBatch is not null && _currentBatch.Equals(_batches[currentBatchXCoord, currentBatchYCoord])) 
           return;
        
        var newBatchCoord = new int2(currentBatchXCoord, currentBatchYCoord);
        _currentBatch = _batches[currentBatchXCoord, currentBatchYCoord];
        _currentBatch.SetActive(true);
        _currentBatch.LoadBatch();
        ActiveBatches[newBatchCoord] = _currentBatch;
    }
    
    public static void DisplayBatches()
    {
        foreach (var activeBatch in ActiveBatches.Values)
        {
            activeBatch.DisplayChunks();
        } 
    }

    private static void DeactivateOldBatches(int2 newBatchCoord)
    {
        List<int2> batchesToRemove = new List<int2>();

        foreach (var (batchCoord, batch) in ActiveBatches)
        {
            if (math.abs(batchCoord.x - newBatchCoord.x) > 1 || math.abs(batchCoord.y - newBatchCoord.y) > 1)
            {
                batch.SetActive(false);
                batchesToRemove.Add(batchCoord);
            }
        }

        // Remove the deactivated batches from the active batches dictionary
        foreach (var batchCoord in batchesToRemove)
        {
            ActiveBatches.Remove(batchCoord);
        }
    }


    private static void InitializeBatches()
    {
        for (int y = 0; y < BatchesNum; y++)
        {
            for (int x = 0; x < BatchesNum; x++)
            {
                _batches[x, y] = new Batch(new int2(x * ChunksPerBatchSide,
                                                          y * ChunksPerBatchSide));
            }
        }
    }

    public static void UpdateBatches()
    {
        foreach (var batch in ActiveBatches.Values)
        {
            batch.UpdateChunks();
        }
    }

    private class Batch
    {
        private readonly TerrainChunksManager _chunksManager;
        private readonly int2 _coords;
        private readonly GameObject _gameObject;
        public bool IsActive => _gameObject.activeSelf;

        public Batch(int2 coords)
        {
            _coords = coords;
            _gameObject = new GameObject($"Batch({coords.x},{coords.y})") 
            {
                transform =
                {
                    position = new Vector3(coords.x, coords.y) * TerrainChunksManager.WorldTerrainChunkResolution, 
                    parent = LandscapeManager.Instance.transform
                }
            };
            _chunksManager = new TerrainChunksManager();
            _gameObject.SetActive(false);
        }

        internal void LoadBatch()
        {
            _chunksManager.InitializeTerrainChunks(ChunksPerBatchSide, _coords, _gameObject.transform);
        }

        public void SetActive(bool active)
        {
            _gameObject.SetActive(active);
        }

        public void UpdateChunks()
        {
            SetCurrentBatch();
            foreach (var batch in ActiveBatches.Values)
            {
                batch._chunksManager.Update();
                batch._gameObject.SetActive(batch._chunksManager.HasActiveChunks);
            }
        }
        
        public override bool Equals(object obj)
        {
            if (obj is not Batch batch) return false;
            return batch._coords.x == _coords.x && batch._coords.y == _coords.y;
        }
        
        public override int GetHashCode() => (_coords.x, _coords.y).GetHashCode();

        public void DisplayChunks()
        {
            _chunksManager.DisplayChunks();
        }
    }
}
