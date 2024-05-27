using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public static class TerrainChunksManager
{
    public const int TerrainChunkResolution = 11;
    public static TerrainChunk[,] TerrainChunks;
    public static float WorldTerrainChunkResolution = (TerrainChunkResolution - 1) * LandscapeManager.Scale;
    private static int _chunksVisibleInViewDst;

    internal static LODInfo[] DetailLevels;
    private static readonly Dictionary<Coordinates, TerrainChunk> TerrainChunkDictionary = new Dictionary<Coordinates, TerrainChunk>();
    private static readonly HashSet<TerrainChunk> TerrainChunksVisibleLastUpdate = new HashSet<TerrainChunk>();
    private static readonly List<TerrainChunk> SurroundTerrainChunks = new List<TerrainChunk>();

    public static Material ChunksMaterial { get; private set; }

    public static void Initialize()
    {
        ChunksMaterial = new Material(Shader.Find("Custom/VertexColorWithLighting"));
        TerrainChunks = new TerrainChunk[LandscapeManager.MapWidth, LandscapeManager.MapHeight];
        
        MaxViewDst = 0;
        _chunksVisibleInViewDst = 0;
        DetailLevels = new LODInfo[3] {
            new LODInfo(0, 3), 
            new LODInfo(1, 4), 
            new LODInfo(3, 5)
        };
        foreach (var detailLevel in DetailLevels)
        {
            _chunksVisibleInViewDst += detailLevel.visibleChunksThreshold;
            MaxViewDst += detailLevel.visibleChunksThreshold * (TerrainChunkResolution - 1);
        }

        MaxViewDst *= LandscapeManager.Scale;
		
        UpdateVisibleChunks ();
    }

    public static float MaxViewDst { get; set; }

    public static void InitializeTerrainChunks(int size, int2 coords, out TerrainChunk[,] chunks, Transform batch)
    {
        chunks = new TerrainChunk[size, size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var chunkCoords = new Coordinates(coords.x + x, coords.y + y);
                var chunk = new TerrainChunk(chunkCoords, TerrainChunkResolution);
                chunk.GameObject.transform.parent = batch;
                chunks[x, y] = chunk;
                TerrainChunks[coords.x + x, coords.y + y] = chunk;
            }
        }
    }

    public static void UpdateVisibleChunks()
    {
        foreach (var visibleChunk in TerrainChunksVisibleLastUpdate)
        {
            visibleChunk.SetVisible(false);
        }
		
        TerrainChunksVisibleLastUpdate.Clear();
        SurroundTerrainChunks.Clear();
			
        int currentChunkCoordX = Mathf.RoundToInt (Viewer.PositionV2.x / (TerrainChunkResolution - 1));
        int currentChunkCoordY = Mathf.RoundToInt (Viewer.PositionV2.y / (TerrainChunkResolution - 1));
        Viewer.ChunkCoord = new Coordinates(currentChunkCoordX, currentChunkCoordY);
		
        for (int yOffset = -_chunksVisibleInViewDst; yOffset <= _chunksVisibleInViewDst; yOffset++) {
            for (int xOffset = -_chunksVisibleInViewDst; xOffset <= _chunksVisibleInViewDst; xOffset++) {
				
                var viewedChunkCoord = new Coordinates(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (!IsWithinMapBounds(viewedChunkCoord)) continue;
                
                TerrainChunk chunk;
                if (TerrainChunkDictionary.TryGetValue(viewedChunkCoord, out var value))
                {
                    chunk = value;
                    SurroundTerrainChunks.Add(chunk);
                    chunk.Update();
                } else
                {
                    chunk = new TerrainChunk(viewedChunkCoord, TerrainChunkResolution);
                    TerrainChunkDictionary.Add(viewedChunkCoord, chunk);
                    SurroundTerrainChunks.Add(chunk);
                }
				
                if (chunk.IsVisible())
                    TerrainChunksVisibleLastUpdate.Add(chunk);
            }
        }
    }

    private static bool IsWithinMapBounds(Coordinates coord)
    {
        return coord.Latitude is >= 0 and < LandscapeManager.MapHeight 
                                                    &&
               coord.Longitude is >= 0 and < LandscapeManager.MapWidth;
    }

    public static void UpdateCulledChunks()
    {
        var viewerForward = Viewer.ForwardV2.normalized;
        foreach (TerrainChunk chunk in SurroundTerrainChunks)
        {
            CullChunkAndSetVisibility(chunk, chunk.IsCulled(viewerForward));
        }
    }
	
    private static void CullChunkAndSetVisibility(TerrainChunk chunk, bool isCulled, bool inDistance = true)
    {
        var visible = inDistance;
        if (LandscapeManager.Instance.cullingMode == CullingMode.Layer)
        {
            chunk.GameObject.layer = isCulled? 
                LayerMask.NameToLayer("Culled") : 
                LayerMask.NameToLayer("Default");
        }
        else if(LandscapeManager.Instance.cullingMode == CullingMode.Visibility)
        {
            visible = !isCulled && inDistance;
        }
		
        chunk.SetVisible(visible);
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
        GameObject.SetActive(true);
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
		
    public bool IsVisible() {
        return GameObject.activeSelf;
    }
    public void SetVisible(bool visible) {
        GameObject.SetActive (visible);
    }

    public void Update()
    {
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