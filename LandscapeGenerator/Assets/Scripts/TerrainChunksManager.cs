using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class TerrainChunksManager : MonoBehaviour{
	
	private const float ViewerMoveThresholdForChunkUpdate = (TerrainChunk.Resolution - 1) * 0.25f;
	private const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;
	private static int _chunksVisibleInViewDst = 4;
	private static readonly Dictionary<int2, TerrainChunk> TerrainChunkDictionary = new Dictionary<int2, TerrainChunk>();
	private static readonly HashSet<TerrainChunk> TerrainChunksVisibleLastUpdate = new HashSet<TerrainChunk>();
	private static readonly List<TerrainChunk> SurroundTerrainChunks = new List<TerrainChunk>();
	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;
	
	public static int ChunksVisibleInViewDist => _chunksVisibleInViewDst;
	private static TerrainChunk.LODInfo[] _detailLevels;
	private static int _wrapCountX;
	private static int _wrapCountY;
	public void Initialize()
	{
		// LandscapeManager.Instance.textureData.ApplyToMaterial (TerrainChunk.Material);
		_detailLevels = new [] {
			new TerrainChunk.LODInfo(0, 2, false),
			new TerrainChunk.LODInfo(1, 3, true),
			new TerrainChunk.LODInfo(2, 4, false)
		};
		
		// _chunksVisibleInViewDst = 0;
		// foreach (var detailLevel in _detailLevels)
		// {
		// 	_chunksVisibleInViewDst += detailLevel.visibleChunksThreshold;
		// }
		
		UpdateVisibleChunks ();
		/**/
	}

	public void Update() {
		
		
		if (Viewer.PositionChanged()) {
			Viewer.UpdateOldPosition();
			UpdateVisibleChunks();
		}
		/* */
		
		if (Viewer.RotationChanged())
		{
			Viewer.UpdateOldRotation();
			UpdateCulledChunks();
		}
	}

	private void UpdateVisibleChunks() {
        foreach (var visibleChunk in TerrainChunksVisibleLastUpdate) {
            visibleChunk.SetVisible(false);
        }
        
        TerrainChunksVisibleLastUpdate.Clear();
        SurroundTerrainChunks.Clear();
        
        float offset = TerrainChunk.WorldSize / 2f;

        int currentChunkCoordX = (int)((Viewer.PositionV2.x + offset) / TerrainChunk.WorldSize);
        int currentChunkCoordY = (int)((Viewer.PositionV2.y + offset) / TerrainChunk.WorldSize);

        
        if (!new int2(currentChunkCoordX, currentChunkCoordY).Equals(Viewer.ChunkCoord))
			Debug.Log($"Chunk Actual: {Viewer.ChunkCoord}");
        
        Viewer.ChunkCoord = new int2(currentChunkCoordX, currentChunkCoordY);
        UpdateWrapCount(currentChunkCoordX, currentChunkCoordY);

        for (int yOffset = -_chunksVisibleInViewDst; yOffset <= _chunksVisibleInViewDst; yOffset++) {
            for (int xOffset = -_chunksVisibleInViewDst; xOffset <= _chunksVisibleInViewDst; xOffset++) {
                var viewedChunkCoord = new int2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                var wrappedChunkCoord = GetWrappedChunkCoords(viewedChunkCoord);
                
                
                TerrainChunk chunk;
                if (TerrainChunkDictionary.TryGetValue(wrappedChunkCoord, out var value)) {
                    chunk = value;
                    chunk.SetChunkCoord(viewedChunkCoord);
                    chunk.Update();
                    SurroundTerrainChunks.Add(chunk);

                } else {
                    chunk = new TerrainChunk(wrappedChunkCoord, _detailLevels);
                    chunk.SetChunkCoord(viewedChunkCoord);
                    chunk.Update();
                    TerrainChunkDictionary.Add(wrappedChunkCoord, chunk);
                    SurroundTerrainChunks.Add(chunk);
                }
                
                if (chunk.IsVisible())
                    TerrainChunksVisibleLastUpdate.Add(chunk);
            }
        }
	}

	private void UpdateWrapCount(int currentChunkCoordX, int currentChunkCoordY) {
        if (currentChunkCoordX >= LandscapeManager.MapWidth * _wrapCountX + LandscapeManager.MapWidth) {
            _wrapCountX++;
        }
        if (currentChunkCoordX < -LandscapeManager.MapWidth * _wrapCountX) {
            _wrapCountX--;
        }
        if (currentChunkCoordY >= LandscapeManager.MapHeight * _wrapCountY + LandscapeManager.MapHeight) {
            _wrapCountY++;
        }
        if (currentChunkCoordY < -LandscapeManager.MapHeight * _wrapCountY) {
            _wrapCountY--;
        }
    } 
    private int2 GetWrappedChunkCoords(int2 viewedChunkCoord) {
        int wrappedXCoord = viewedChunkCoord.x;
        int wrappedYCoord = viewedChunkCoord.y;

        if (viewedChunkCoord.x < 0) {
            wrappedXCoord = (viewedChunkCoord.x % LandscapeManager.MapWidth + LandscapeManager.MapWidth) % LandscapeManager.MapWidth;
        } else if (viewedChunkCoord.x >= LandscapeManager.MapWidth) {
            wrappedXCoord = viewedChunkCoord.x % LandscapeManager.MapWidth;
        }

        if (viewedChunkCoord.y < 0) {
            wrappedYCoord = (viewedChunkCoord.y % LandscapeManager.MapHeight + LandscapeManager.MapHeight) % LandscapeManager.MapHeight;
        } else if (viewedChunkCoord.y >= LandscapeManager.MapHeight) {
            wrappedYCoord = viewedChunkCoord.y % LandscapeManager.MapHeight;
        }
                
        return new int2(wrappedXCoord, wrappedYCoord);
    }

	void UpdateCulledChunks()
	{
		var viewerForward = Viewer.ForwardV2.normalized;
		foreach (var chunk in SurroundTerrainChunks)
		{
			CullChunkAndSetVisibility(chunk, chunk.IsCulled(viewerForward));
		}
	}
	
	internal static void CullChunkAndSetVisibility(TerrainChunk chunk, bool isCulled, bool inDistance = true)
	{
		var visible = inDistance;
		if (LandscapeManager.Instance.culling == CullingMode.Layer)
		{
			chunk.GameObject.layer = isCulled? 
				LayerMask.NameToLayer("Culled") : 
				LayerMask.NameToLayer("Default");
		}
		else if(LandscapeManager.Instance.culling == CullingMode.Visibility)
		{
			visible = !isCulled && inDistance;
		}
		
		chunk.SetVisible(visible);
	}
}