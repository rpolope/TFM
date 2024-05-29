using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = System.Random;

public class TerrainManagerWithCorrutine : MonoBehaviour
{
    
    public static event Action CompleteMeshGenerationEvent;
    
    private static readonly Dictionary<Coordinates, TerrainChunk> TerrainChunkDictionary = new Dictionary<Coordinates, TerrainChunk>();
    private static readonly HashSet<TerrainChunk> TerrainChunksVisibleLastUpdate = new HashSet<TerrainChunk>();
    private static readonly List<TerrainChunk> SurroundTerrainChunks = new List<TerrainChunk>();

    private Transform _transform;
    public static LODInfo[] DetailLevels { get; set; }
    public static int ChunksVisibleInViewDist { get; set; }
    public float MaxViewDst { get; set; }
    public Material ChunkMaterial { get; set; }
    
    void Start()
    {
        _transform = transform;
        ChunkMaterial = new Material(Shader.Find("Custom/VertexColorWithLighting"));
        
        MaxViewDst = 0;
        ChunksVisibleInViewDist = 0;
        DetailLevels = new LODInfo[3] {
            new LODInfo(0, 3, false), 
            new LODInfo(1, 4, true), 
            new LODInfo(3, 5, false)
        };
        foreach (var detailLevel in DetailLevels)
        {
            ChunksVisibleInViewDist += detailLevel.visibleChunksThreshold;
            MaxViewDst += detailLevel.visibleChunksThreshold * (TerrainChunk.Resolution - 1);
        }

        MaxViewDst *= LandscapeManager.Scale;
        
        UpdateVisibleChunks ();
        
        //FIXME
        // StartCoroutine(GenerateChunksInBackground());
        /**/
    }
    
    //FIXME
    /*
    private IEnumerator GenerateChunksInBackground()
    {
        foreach (var chunk in TerrainChunkDictionary.Values)
        {
            if (!chunk.IsVisible && !chunk.HasMesh)
            {
                yield return StartCoroutine(GenerateChunkMesh(chunk));
            }
        }
    }
    
    private IEnumerator GenerateChunkMesh(TerrainChunk chunk)
    {
        NativeArray<float> heightMap = new NativeArray<float>(chunk.HeightMap, Allocator.TempJob);
        NativeArray<int> triangles = new NativeArray<int>(chunk.GetTriangles(), Allocator.TempJob);
        NativeArray<Vector3> vertices = new NativeArray<Vector3>(chunk.GetVertices(), Allocator.TempJob);

        var generateMeshJob = new MeshGenerator.GenerateMeshJob
        {
            Vertices = meshData.Vertices,
            UVs = meshData.UVs,
            Triangles = meshData.Triangles,
            Colors = meshData.Colors,
            MapData = mapData,
            Resolution = resolution,
            FacesCount = meshData.Triangles.Length / 6,
            Scale = LandscapeManager.Scale,
            LODScale = meshData.LODScale,
            MeshParameters = meshParameters
        };

        JobHandle jobHandle = meshJob.Schedule();
        yield return new WaitUntil(() => jobHandle.IsCompleted);

        jobHandle.Complete();

        chunk.SetMesh(vertices.ToArray(), triangles.ToArray());

        heightMap.Dispose();
        triangles.Dispose();
        vertices.Dispose();
    }
    */


    private void Update() {
        
        if (Viewer.PositionChanged()) {
            Viewer.UpdateOldPosition();
            UpdateVisibleChunks();
        }
    }

    private void LateUpdate()
    {
        CompleteMeshGenerationEvent?.Invoke();
    }


    private void UpdateVisibleChunks()
    {
        foreach (var visibleChunk in TerrainChunksVisibleLastUpdate)
        {
            visibleChunk.SetVisible(false);
        }
		
        TerrainChunksVisibleLastUpdate.Clear();
        SurroundTerrainChunks.Clear();
			
        int currentChunkCoordX = Mathf.RoundToInt (Viewer.PositionV2.x / (TerrainChunk.Resolution - 1));
        int currentChunkCoordY = Mathf.RoundToInt (Viewer.PositionV2.y / (TerrainChunk.Resolution - 1));
        Viewer.ChunkCoord = new Coordinates(currentChunkCoordX, currentChunkCoordY);
		
        for (int yOffset = -ChunksVisibleInViewDist; yOffset <= ChunksVisibleInViewDist; yOffset++) {
            for (int xOffset = -ChunksVisibleInViewDist; xOffset <= ChunksVisibleInViewDist; xOffset++) {
				
                var viewedChunkCoord = new Coordinates(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                TerrainChunk chunk;
                if (TerrainChunkDictionary.TryGetValue(viewedChunkCoord, out var value))
                {
                    chunk = value;
                    SurroundTerrainChunks.Add(chunk);
                    chunk.Update ();
                } else
                {
                    chunk = new TerrainChunk(viewedChunkCoord, DetailLevels, _transform);
                    TerrainChunkDictionary.Add(viewedChunkCoord, chunk);
                    SurroundTerrainChunks.Add(chunk);
                }
				
                if (chunk.IsVisible)
                    TerrainChunksVisibleLastUpdate.Add(chunk);
            }
        }
    }
}
