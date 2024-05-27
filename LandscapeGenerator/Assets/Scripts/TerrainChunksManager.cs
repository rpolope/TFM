using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public static class TerrainChunksManager
{
    public const int TerrainChunkResolution = 11;
    public static TerrainChunk[,] TerrainChunks;
    public static Material ChunksMaterial { get; private set; }

    public static void Initialize()
    {
        ChunksMaterial = new Material(Shader.Find("Custom/VertexColorWithLighting"));
        TerrainChunks = new TerrainChunk[LandscapeManager.MapWidth, LandscapeManager.MapHeight];
    }

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