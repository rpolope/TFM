using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class TerrainChunksManager
{
    public static event Action CompleteMeshGenerationEvent;

    private static readonly Dictionary<Coordinates, TerrainChunk> _terrainChunkDictionary = new Dictionary<Coordinates, TerrainChunk>();
    private static readonly HashSet<TerrainChunk> _terrainChunksVisibleLastUpdate = new HashSet<TerrainChunk>();
    private static readonly List<TerrainChunk> _surroundTerrainChunks = new List<TerrainChunk>();
    private static float MaxViewDst { get; set; }

    internal static LODInfo[] DetailLevels;

    public static Material ChunksMaterial { get; private set; }
    public static int ChunksVisibleInViewDist { get; private set; }
    public TerrainChunk GetChunkFromCoordinates(Coordinates coordinates) => _terrainChunkDictionary[coordinates];
    public bool HasActiveChunks => _terrainChunksVisibleLastUpdate.Count > 0;

    public static void Initialize()
    {
        ChunksMaterial = new Material(Shader.Find("Custom/VertexColorWithLighting"));
        
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

        InitializeTerrainChunks();
    }

    public static void CompleteChunksMeshGeneration()
    {
        CompleteMeshGenerationEvent?.Invoke();
    }
    
    public static void InitializeTerrainChunks()
    {
        for (int y = 0; y < LandscapeManager.MapHeight; y++)
        {
            for (int x = 0; x < LandscapeManager.MapWidth; x++)
            {
                var chunkCoords = new Coordinates(x, y);
                var chunk = new TerrainChunk(chunkCoords);
                chunk.GameObject.transform.parent = LandscapeManager.Instance.Transform;
                _terrainChunkDictionary.Add(chunkCoords, chunk);
            }
        }
    }

    private static void UpdateVisibleChunks()
    {
        foreach (var visibleChunk in _terrainChunksVisibleLastUpdate)
        {
            visibleChunk.SetVisible(false);
        }
		
        _terrainChunksVisibleLastUpdate.Clear();
        _surroundTerrainChunks.Clear();
			
        int currentChunkCoordX = Mathf.RoundToInt (Viewer.PositionV2.x / (TerrainChunk.Resolution - 1));
        int currentChunkCoordY = Mathf.RoundToInt (Viewer.PositionV2.y / (TerrainChunk.Resolution - 1));
        Viewer.ChunkCoord = new Coordinates(currentChunkCoordX, currentChunkCoordY);
		
        for (int yOffset = -ChunksVisibleInViewDist; yOffset <= ChunksVisibleInViewDist; yOffset++) {
            for (int xOffset = -ChunksVisibleInViewDist; xOffset <= ChunksVisibleInViewDist; xOffset++) {
				
                var viewedChunkCoord = new Coordinates(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (!IsWithinMapBounds(viewedChunkCoord)) continue;

                if (!_terrainChunkDictionary.TryGetValue(viewedChunkCoord, out var terrainChunk)) continue;
                
                _surroundTerrainChunks.Add(terrainChunk);
                terrainChunk.Update();
                if (terrainChunk.IsVisible)
                    _terrainChunksVisibleLastUpdate.Add(terrainChunk);
                
                // FIXME: Descomentar cuando se implemente la funcionalidad de ir añdiendo los chunks del principio 
                // al final cuando se salga de los límites del mapa para crear la sensación de mapa infinito.
                // else
                // {
                //     chunk = new TerrainChunk(viewedChunkCoord, TerrainChunkResolution);
                //     _terrainChunkDictionary.Add(viewedChunkCoord, chunk);
                //     _surroundTerrainChunks.Add(chunk);
                // }
                //
                // if (chunk.IsVisible)
                //     _terrainChunksVisibleLastUpdate.Add(chunk);
            }
        }
    }
    
    public static void UpdateCulledChunks()
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

    public static void Update()
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
    public const int Resolution = 129;
    public const float WorldSize = (Resolution - 1) * LandscapeManager.Scale;
    
    public Coordinates Coordinates { get; private set; }
    public Biome Biome { get; private set; }
    public float[] HeightMap { get; private set; }
    public Color[] ColorMap { get; set; }
    public MapData MapData { get; private set;}
    public bool IsIsland { get; private set; }
    public GameObject GameObject { get; private set; }
    public MeshRenderer Renderer { get; private set; }
    public MeshFilter MeshFilter { get; private set; }
    
    private readonly MeshCollider _meshCollider;
    private readonly LODInfo[] _detailLevels;
    private readonly LODMesh _colliderMesh;
    private readonly LODMesh[] _lodMeshes;
    private int _lodIndex;

    public float2 Position { get; private set; }
    private Dictionary<Cardinal, TerrainChunk> Neighbors { get; } = new(4);
    private Dictionary<Cardinal, Border> Borders { get; } = new(4);
    public Material Material { get; set; }
    public bool IsVisible => GameObject.activeSelf;

    public TerrainChunk(Coordinates coordinates)
    {
        
        TerrainChunksManager.CompleteMeshGenerationEvent += CompleteMeshGeneration;
        
        Coordinates = coordinates;
        IsIsland = Random.value > 0.9f;
        Biome = BiomesManager.GetBiome(coordinates);
        var coords = new float2(Coordinates.Longitude, Coordinates.Longitude);
        Position = new float2(Coordinates.Longitude, Coordinates.Latitude) * (Resolution - 1);

        HeightMap = MapGenerator.GenerateNoiseMap(Resolution * Resolution, coords, Biome.TerrainParameters.noiseParameters);
        ColorMap = MapGenerator.GenerateColorMap(Resolution * Resolution, Biome, HeightMap);
        MapData = new MapData(HeightMap, ColorMap);
        
        foreach (Cardinal cardinal in Enum.GetValues(typeof(Cardinal)))
        {
            Borders[cardinal] = new Border(cardinal, this);
        }

        GameObject = new GameObject($"Chunk({coordinates.Longitude},{coordinates.Latitude})");
        GameObject.transform.position = new Vector3(Position.x, 0, Position.y) * LandscapeManager.Scale;
        
        MeshFilter = GameObject.AddComponent<MeshFilter>();
        // MeshFilter.mesh = LandscapeManager.Instance.meshFilter.mesh;
        Renderer = GameObject.AddComponent<MeshRenderer>();
        Renderer.sharedMaterial = TerrainChunksManager.ChunksMaterial;
        
        _meshCollider = GameObject.AddComponent<MeshCollider>();
        
        _detailLevels = TerrainChunksManager.DetailLevels;
        _lodMeshes = new LODMesh[_detailLevels.Length];
        for (int i = 0; i < _detailLevels.Length; i++) {
            _lodMeshes[i] = new LODMesh(_detailLevels[i].lod, this);
            if (_detailLevels[i].useForCollider) {
                _colliderMesh = _lodMeshes[i];
            }
        }
        
        GameObject.transform.localScale = Vector3.one * LandscapeManager.Scale;
        GameObject.SetActive(false);
    }

    private void CompleteMeshGeneration()
    {
        if (!IsVisible) return;
        
        var lodMesh = _lodMeshes[_lodIndex];
        if (lodMesh.RequestedMesh)
        {
            lodMesh.CompleteMeshGeneration();
            MeshFilter.mesh = lodMesh.Mesh;

            if (_lodIndex == 0 && _colliderMesh.RequestedMesh)
            {
                _colliderMesh.CompleteMeshGeneration();
                _meshCollider.sharedMesh = _colliderMesh.Mesh;
            }
        }
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
        chunkCenter += chunkCenter.normalized * (TerrainChunk.WorldSize * 0.5f);
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
			
        if (inDistance)
        {
            int lodIndex = 0;
        
            for (int i = 0; i < _detailLevels.Length - 1; i++)
            {
                if (chunksFromViewer.x < TerrainChunksManager.DetailLevels[i].visibleChunksThreshold && 
                    chunksFromViewer.y < TerrainChunksManager.DetailLevels[i].visibleChunksThreshold )
                {
                    break;
                }
					   
                lodIndex = i + 1;
					   
            }
            if (lodIndex != _lodIndex)
            {
                var lodMesh = _lodMeshes[lodIndex];
                _lodIndex = lodIndex;
        
                if (lodMesh.HasMesh)
                {
                    MeshFilter.mesh = lodMesh.Mesh;
                    if (lodIndex == 0)
                    {
                        _meshCollider.enabled = true;
                        _meshCollider.sharedMesh = _colliderMesh.Mesh;
                    }
                }
                else
                {
                    lodMesh.RequestMesh();
                }
            }
				    
            if (_lodIndex == 0 && Coordinates.Equals(Viewer.ChunkCoord))
            {
                _meshCollider.enabled = true;
                if (_colliderMesh.HasMesh)
                {
                    _meshCollider.sharedMesh = _colliderMesh.Mesh;
                }
                else
                {
                    _colliderMesh.RequestMesh();
                }
            }
            else
            {
                _meshCollider.enabled = false;
            }
        }

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
        Values = new float[TerrainChunk.Resolution];
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
        const int length = TerrainChunk.Resolution;
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
            var adjustedValue = Mathf.Lerp(thisValue, neighborValue, 0.5f);
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

    public LODInfo(int lod, int visibleChunksThreshold, bool useForCollider)
    {
        this.lod = lod;
        this.visibleChunksThreshold = visibleChunksThreshold;
        this.useForCollider = useForCollider;
    }
}

internal class LODMesh {

    public Mesh Mesh;
    public bool HasMesh;
    public bool RequestedMesh;
		
    private readonly int _lod;
    private readonly TerrainChunk _chunk;
    internal JobHandle MeshJobHandle;
    private MeshData _meshData;

    public LODMesh(int lod, TerrainChunk chunk) {
        _lod = lod;
        _chunk = chunk;
        HasMesh = false;
    }

    public void RequestMesh()
    {
        _meshData = new MeshData(TerrainChunk.Resolution, _lod);
        var resolution = (TerrainChunk.Resolution - 1) / _meshData.LODScale + 1;
        MeshJobHandle = MeshGenerator.ScheduleMeshGenerationJob(_chunk.Biome.TerrainParameters.meshParameters, resolution, _chunk.MapData, ref _meshData);
        RequestedMesh = true;
    }

    public void CompleteMeshGeneration()
    {
        MeshJobHandle.Complete();
        SetMesh();
    }

    private void SetMesh()
    {
        Mesh = _meshData.CreateMesh();
        RequestedMesh = false;
        HasMesh = true;
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