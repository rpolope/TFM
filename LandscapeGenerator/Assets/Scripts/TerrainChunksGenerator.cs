using System.Collections.Generic;
using UnityEngine;

public class TerrainChunksGenerator : MonoBehaviour
{
    
    const float Scale = 5f;

    const float ViewerMoveThresholdForChunkUpdate = 25f;
    const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;

    public static float maxViewDst;

    public Transform viewer;
    public Material chunkMaterial;

    public static Vector2 viewerPosition;
    Vector2 _viewerPositionOld;
    // static MapGenerator _mapGenerator;
    int _chunkSize;
    int _chunksVisibleInViewDst;
    
    void Start()
    {
        maxViewDst = LandscapeManager.Instance.viewThreshold;
        _chunkSize = MapGenerator.MapChunkSize - 1;
        _chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / _chunkSize);

        UpdateVisibleChunks ();
    }

    private void UpdateVisibleChunks()
    {
        throw new System.NotImplementedException();
    }

    void Update()
    {
        
    }
}