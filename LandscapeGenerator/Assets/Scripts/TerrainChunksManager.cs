using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using Unity.VisualScripting.FullSerializer.Internal.Converters;
using UnityEngine.PlayerLoop;
using Random = UnityEngine.Random;

public class TerrainChunksManager
{
    public const int TerrainChunkResolution = 11;
    public const float WorldTerrainChunkResolution = (TerrainChunkResolution - 1) * LandscapeManager.Scale;

    private static LODInfo[] _detailLevels;
    private readonly Dictionary<Coordinates, TerrainChunk> _terrainChunkDictionary = new Dictionary<Coordinates, TerrainChunk>();
    private readonly HashSet<TerrainChunk> _terrainChunksVisibleLastUpdate = new HashSet<TerrainChunk>();
    private readonly List<TerrainChunk> _surroundTerrainChunks = new List<TerrainChunk>();

    public static Material ChunksMaterial { get; private set; }
    public static int ChunksVisibleInViewDist { get; private set; }
    public TerrainChunk GetChunkFromCoordinates(Coordinates coordinates) => _terrainChunkDictionary[coordinates];
    public bool HasActiveChunks => _terrainChunksVisibleLastUpdate.Count > 0;

    public static void Initialize()
    {
        ChunksMaterial = new Material(Shader.Find("Custom/VertexColorWithLighting"));
        
        MaxViewDst = 0;
        ChunksVisibleInViewDist = 0;
        _detailLevels = new LODInfo[3] {
            new LODInfo(0, 3), 
            new LODInfo(1, 4), 
            new LODInfo(3, 5)
        };
        foreach (var detailLevel in _detailLevels)
        {
            ChunksVisibleInViewDist += detailLevel.visibleChunksThreshold;
            MaxViewDst += detailLevel.visibleChunksThreshold * (TerrainChunkResolution - 1);
        }

        MaxViewDst *= LandscapeManager.Scale;
    }

    private static float MaxViewDst { get; set; }

    public void InitializeTerrainChunks(int size, int2 coords , Transform batch)
    {
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var chunkCoords = new Coordinates(coords.x + x, coords.y + y);
                var chunk = new TerrainChunk(chunkCoords, TerrainChunkResolution);
                chunk.GameObject.transform.parent = batch;
                _terrainChunkDictionary.Add(chunkCoords, chunk);
            }
        }
    }

    private void UpdateVisibleChunks()
    {
        foreach (var visibleChunk in _terrainChunksVisibleLastUpdate)
        {
            visibleChunk.SetVisible(false);
        }
		
        _terrainChunksVisibleLastUpdate.Clear();
        _surroundTerrainChunks.Clear();
			
        int currentChunkCoordX = Mathf.RoundToInt (Viewer.PositionV2.x / (TerrainChunkResolution - 1));
        int currentChunkCoordY = Mathf.RoundToInt (Viewer.PositionV2.y / (TerrainChunkResolution - 1));
        Viewer.ChunkCoord = new Coordinates(currentChunkCoordX, currentChunkCoordY);
		
        for (int yOffset = -ChunksVisibleInViewDist; yOffset <= ChunksVisibleInViewDist; yOffset++) {
            for (int xOffset = -ChunksVisibleInViewDist; xOffset <= ChunksVisibleInViewDist; xOffset++) {
				
                var viewedChunkCoord = new Coordinates(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (!IsWithinMapBounds(viewedChunkCoord)) continue;
                
                TerrainChunk chunk;
                if (_terrainChunkDictionary.TryGetValue(viewedChunkCoord, out var value))
                {
                    chunk = value;
                    _surroundTerrainChunks.Add(chunk);
                    chunk.Update();
                } else
                {
                    chunk = new TerrainChunk(viewedChunkCoord, TerrainChunkResolution);
                    _terrainChunkDictionary.Add(viewedChunkCoord, chunk);
                    _surroundTerrainChunks.Add(chunk);
                }
				
                if (chunk.IsVisible)
                    _terrainChunksVisibleLastUpdate.Add(chunk);
            }
        }
    }
    
    public void UpdateCulledChunks()
    {
        var viewerForward = Viewer.ForwardV2.normalized;
        foreach (var chunk in _surroundTerrainChunks)
        {
            CullChunkAndSetVisibility(chunk, chunk.IsCulled(viewerForward));
        }
    }

    public void DisplayChunks()
    {
        foreach (var chunk in _terrainChunkDictionary.Values.Where(chunk => chunk != null))
        {
            MapDisplay.DisplayChunk(LandscapeManager.Instance.displayMode, chunk);
        }
    }

    public void Update()
    {
        UpdateVisibleChunks();
        /* */
		
        if (Viewer.RotationChanged())
        {
            Viewer.UpdateOldRotation();
            UpdateCulledChunks();
        }
    }
    
    internal static void CullChunkAndSetVisibility(TerrainChunk chunk, bool isCulled, bool inDistance = true)
    {
        var visible = inDistance;
        if (chunk.Coordinates.Equals(new Coordinates(29,14))) 
            Debug.Log("Invisible en coords: "+chunk.Coordinates);
        switch (LandscapeManager.Instance.cullingMode)
        {
            case CullingMode.Layer:
                chunk.GameObject.layer = isCulled? 
                    LayerMask.NameToLayer("Culled") : 
                    LayerMask.NameToLayer("Default");
                break;
            case CullingMode.Visibility:
                visible = !isCulled && inDistance;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
		
        chunk.SetVisible(visible);
    }
    private static bool IsWithinMapBounds(Coordinates coord)
    {
        return coord.Latitude is >= 0 and < LandscapeManager.MapHeight 
               &&
               coord.Longitude is >= 0 and < LandscapeManager.MapWidth;
    }

}

public class TerrainChunk
{
    public Coordinates Coordinates { get; private set; }
    public Biome Biome { get; private set; }
    public float[] HeightMap { get; private set; }
    public bool IsIsland { get; private set; }
    public GameObject GameObject { get; private set; }
    public MeshRenderer Renderer { get; private set; }
    public MeshFilter MeshFilter { get; private set; }
    
    public float2 Position { get; private set; }
    private Dictionary<Cardinal, TerrainChunk> Neighbors { get; } = new(4);
    private Dictionary<Cardinal, Border> Borders { get; } = new(4);
    public Material Material { get; set; }
    public bool IsVisible => GameObject.activeSelf;

    public TerrainChunk(Coordinates coordinates, int resolution)
    {
        Coordinates = coordinates;
        IsIsland = Random.value > 0.9f;
        Biome = BiomesManager.GetBiome(coordinates);
        var coords = new float2(Coordinates.Longitude, Coordinates.Longitude);
        Position = new float2(Coordinates.Longitude, Coordinates.Latitude) * (resolution - 1);

        HeightMap = MapGenerator.GenerateNoiseMap(resolution * resolution, coords, Biome.TerrainParameters.noiseParameters);

        foreach (Cardinal cardinal in Enum.GetValues(typeof(Cardinal)))
        {
            Borders[cardinal] = new Border(cardinal, this);
        }

        GameObject = new GameObject($"Chunk({coordinates.Longitude},{coordinates.Latitude})");
        GameObject.transform.position = new Vector3(Position.x, 0, Position.y) * LandscapeManager.Scale;
        
        MeshFilter = GameObject.AddComponent<MeshFilter>();
        MeshFilter.mesh = LandscapeManager.Instance.meshFilter.mesh;
        // ModifyVertices(MeshFilter);
        
        Renderer = GameObject.AddComponent<MeshRenderer>();
        
        GameObject.transform.localScale = Vector3.one * LandscapeManager.Scale;
        GameObject.SetActive(false);
    }

    private void ModifyVertices(MeshFilter meshFilter, float heightMultiplier = 1f)
    {
        float[] heightMap = HeightMap;
        var originalMesh = LandscapeManager.Instance.meshFilter.mesh;

        Vector3[] originalVertices = originalMesh.vertices;

        if (originalVertices.Length != heightMap.Length)
        {
            Debug.LogError("El tamaño del mapa de alturas no coincide con el número de vértices.");
            return;
        }

        Vector3[] modifiedVertices = new Vector3[originalVertices.Length];
        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 vertex = originalVertices[i];
            float height = heightMap[i] * heightMultiplier; 
            modifiedVertices[i] = vertex + Vector3.up * height;
        }
        
        Mesh clonedMesh = new Mesh();
        clonedMesh.vertices = modifiedVertices;
        clonedMesh.triangles = originalMesh.triangles;
        clonedMesh.uv = originalMesh.uv;
        clonedMesh.normals = originalMesh.normals;
        clonedMesh.RecalculateBounds();
        clonedMesh.RecalculateNormals();
        
        meshFilter.mesh = clonedMesh;
    }

    public void AssignNeighbor(Cardinal direction, TerrainChunk neighbor)
    {
        if (neighbor != null)
        {
            Neighbors[direction] = neighbor;
        }
    }

    public void AdjustBorders()
    {
        foreach (var border in Borders.Values.Where(border => !border.Adjusted))
        {
            foreach (var direction in Neighbors.Keys)
            {
                var neighbor = Neighbors[direction];
                var neighborBorder = neighbor.Borders[direction.GetOpposite()];
                border.Adjust(neighborBorder);
            }
        }
    }

    public bool IsCulled(Vector2 viewerForward)
    {
        if (Coordinates.Equals(Viewer.ChunkCoord)) return false;

        var chunkCenter = new Vector2(Position.x, Position.y);
        chunkCenter += chunkCenter.normalized * (TerrainChunksManager.WorldTerrainChunkResolution * 0.5f);
        Vector2 chunkDirection = (chunkCenter - Viewer.PositionV2).normalized;
        float dot = Vector2.Dot(viewerForward, chunkDirection);
        float chunkAngle = Mathf.Acos(dot) * Mathf.Rad2Deg;

        return dot < 0 && chunkAngle > Viewer.FOV;
    }
    public void SetVisible(bool visible) {
        GameObject.SetActive (visible);
    }

    public void Update()
    {
        var chunksFromViewer = Viewer.ChunkCoord.AsInt2() - Coordinates.AsInt2();
        chunksFromViewer = new int2(Mathf.Abs(chunksFromViewer.x), Mathf.Abs(chunksFromViewer.y));
        var inDistance = chunksFromViewer.x < TerrainChunksManager.ChunksVisibleInViewDist && 
                         chunksFromViewer.y < TerrainChunksManager.ChunksVisibleInViewDist;
			
        // if (inDistance)
        // {
        //     int lodIndex = 0;
        //
        //     for (int i = 0; i < TerrainChunksManager.DetailLevels.Length - 1; i++)
        //     {
        //         if (chunksFromViewer.x < TerrainChunksManager.DetailLevels[i].visibleChunksThreshold && 
        //             chunksFromViewer.y < TerrainChunksManager.DetailLevels[i].visibleChunksThreshold )
        //         {
        //             break;
        //         }
					   //
        //         lodIndex = i + 1;
					   //
        //     }
        //     if (lodIndex != _lodIndex)
        //     {
        //         var lodMesh = _lodMeshes[lodIndex];
        //         _lodIndex = lodIndex;
        //
        //         if (lodMesh.HasMesh)
        //         {
        //             _meshFilter.mesh = lodMesh.Mesh;
        //             if (lodIndex == 0)
        //             {
        //                 _meshCollider.enabled = true;
        //                 _meshCollider.sharedMesh = _colliderMesh.Mesh;
        //             }
        //         }
        //         else
        //         {
        //             lodMesh.RequestMesh();
        //         }
        //     }
				    //
        //     if (_lodIndex == 0 && _coord.Equals(Viewer.ChunkCoord))
        //     {
        //         _meshCollider.enabled = true;
        //         if (_colliderMesh.HasMesh)
        //         {
        //             _meshCollider.sharedMesh = _colliderMesh.Mesh;
        //         }
        //         else
        //         {
        //             _colliderMesh.RequestMesh();
        //         }
        //     }
        //     else
        //     {
        //         _meshCollider.enabled = false;
        //     }
        // }

        TerrainChunksManager.CullChunkAndSetVisibility(this, IsCulled(Viewer.ForwardV2.normalized), inDistance);

    }
}

public class Border
{
    private float[] Values { get; set; }
    private Cardinal Cardinal { get; set; }
    private readonly TerrainChunk _chunk;
    public bool Adjusted { get; private set; }

    public Border(Cardinal cardinal, TerrainChunk chunk)
    {
        Cardinal = cardinal;
        _chunk = chunk;
        Values = new float[TerrainChunksManager.TerrainChunkResolution];
        InitializeValues();
    }

    private void InitializeValues()
    {
        for (int i = 0; i < Values.Length; i++)
        {
            Values[i] = GetHeightValueForCardinal(i);
        }
    }

    private float GetHeightValueForCardinal(int index)
    {
        const int length = TerrainChunksManager.TerrainChunkResolution;
        return Cardinal switch
        {
            Cardinal.West => _chunk.HeightMap[0 + index * length],
            Cardinal.East => _chunk.HeightMap[length - 1 + index * length],
            Cardinal.North => _chunk.HeightMap[index],
            Cardinal.South => _chunk.HeightMap[index + (length - 1) * length],
            _ => 0.0f
        };
    }

    public void Adjust(Border neighborBorder)
    {
        for (int i = 0; i < Values.Length; i++)
        {
            var thisValue = Values[i];
            var neighborValue = neighborBorder.Values[i];
            var adjustedValue = Mathf.Lerp(thisValue, neighborValue, 0.5f); // Interpolación lineal
            neighborBorder.Values[i] = adjustedValue;
            Values[i] = adjustedValue;
        }
        Adjusted = true;
        neighborBorder.Adjusted = true;
    }

}


[Serializable]
public struct LODInfo {
    public int lod;
    public int visibleChunksThreshold;
    public bool useForCollider;

    public LODInfo(int lod, int visibleChunksThreshold)
    {
        this.lod = lod;
        this.visibleChunksThreshold = visibleChunksThreshold;
        useForCollider = false;
    }
}

public enum CullingMode
{
    Layer,
    Visibility
}

public enum Cardinal
{
    East,
    West,
    North,
    South
}

public static class CardinalExtensions
{
    public static Cardinal GetOpposite(this Cardinal cardinal)
    {
        return cardinal switch
        {
            Cardinal.East => Cardinal.West,
            Cardinal.West => Cardinal.East,
            Cardinal.North => Cardinal.South,
            Cardinal.South => Cardinal.North,
            _ => throw new ArgumentOutOfRangeException(nameof(cardinal), cardinal, null)
        };
    }
}