using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Serialization;

public class LandscapeManager : MonoBehaviour
{
    
    private static float[] MoistureMap { get; set; }
    private static float[] LatitudeHeats { get; set; }
    private const int InitialCoordOffset = MapWidth / 2;
    public static LandscapeManager Instance;
    
    public const float Scale = 1f;
    public const int MapWidth = 32;
    public const int MapHeight = 32;
    public const float FixedMoisture = 0.5f;
    public Transform Transform { get; private set; }

    public DisplayMode displayMode;
    public int initialLatitude, initialLongitude;
    [Header("World Moisture Map")]
    public NoiseParameters moistureParameters;
    public MeshFilter meshFilter;
    public CullingMode cullingMode;
    [Range(-90, 90)]
    private int _lastLatitude;
    [Range(-90, 90)]
    private int _lastLongitude;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(Instance);
        }
    }

    private void Start()
    {
        Transform = transform;
        MapDisplay.DisplayMode = displayMode;
        _lastLatitude = initialLatitude + InitialCoordOffset;
        _lastLongitude = initialLongitude + InitialCoordOffset;
        
        GenerateMoistureMap();
        InitializeLatitudeHeats();
        
        Viewer.InitialCoords = new Vector2Int(_lastLongitude, _lastLatitude);
        Viewer.Initialize();
        BiomesManager.Initialize(false);
        TerrainChunksManager.Initialize();
        BatchesManager.Initialize();
        
        BatchesManager.DisplayBatches();
    }

    private void Update()
    {
        if (Viewer.PositionChanged())
        {
            Viewer.UpdateOldPosition();
            BatchesManager.UpdateBatches();
            // BatchesManager.DisplayBatches();
        }
    }

    public void GenerateFixedMoistureMap()
    {
        MoistureMap = Enumerable.Repeat(FixedMoisture, MapHeight * MapWidth).ToArray();
    }
    
    public static void GenerateStaticMoistureMap(NoiseParameters moistureParameters)
    {
        MoistureMap = MapGenerator.GenerateNoiseMap(MapWidth * MapHeight, new float2(), moistureParameters);
    }
    
    public void GenerateMoistureMap()
    {
        MoistureMap = MapGenerator.GenerateNoiseMap(MapWidth * MapHeight, new float2(), moistureParameters);
    }

    public static float GetMoisture(Coordinates coordinates)
    {
        return MoistureMap[coordinates.Longitude + coordinates.Latitude * MapWidth];
    }

    private static void InitializeLatitudeHeats()
    {
        LatitudeHeats = new float[MapHeight];
        const float ecuador = (MapHeight - 1) * 0.5f;
        
        for (int latitude = 0; latitude < MapHeight; latitude++)
        {
            float distanceFromEcuador = Math.Abs(latitude - ecuador);
            float heat = distanceFromEcuador / ecuador;
            
            LatitudeHeats[latitude] = heat;
        }
    }
    
    public static float GetHeat(Coordinates coordinates)
    {
        return LatitudeHeats[coordinates.Latitude];
    }

    // private static void DisplayMap()
    // {
    //     foreach (var chunk in TerrainChunksManager.TerrainChunks)
    //     {
    //         if (chunk == null) continue;
    //         MapDisplay.DisplayChunk(Instance.displayMode, chunk);
    //     }
    // }
}
[Serializable]
public struct Coordinates
{
    public int Longitude { get; set; }
    public int Latitude { get; set; }

    public Coordinates(int x, int y)
    {
        Longitude = x;
        Latitude = y;
    }

    public int2 AsInt2() => new (Longitude, Latitude);

    public override bool Equals(object obj)
    {
        if (obj is not Coordinates coord) return false;
        return coord.Longitude == Longitude && coord.Latitude == Latitude;
    }

    public override int GetHashCode() => (Longitude, Latitude).GetHashCode();
}

