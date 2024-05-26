using System;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Serialization;

public class LandscapeManager : MonoBehaviour
{
    public static LandscapeManager Instance;
    private static float[] MoistureMap { get; set; }
    private static float[] LatitudeHeats { get; set; }
    
    public const float Scale = 1f;
    public const int MapWidth = 7;
    public const int MapHeight = 7;

    public DisplayMode displayMode;
    public int initialLatitude, initialLongitude;
    [Header("World Moisture Map")]
    public NoiseParameters moistureParameters;
    public MeshFilter meshFilter;

    [Range(-90, 90)]
    private int _lastLatitude;
    [Range(-90, 90)]
    private int _lastLongitude;
    public Transform Transform { get; private set; }

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
        
        MapDisplay.DisplayMode = displayMode;
        GenerateFixedMoistureMap();
        InitializeLatitudeHeats();
        Transform = transform;
        BiomesManager.Initialize();
        TerrainChunksManager.Initialize();
        BatchesManager.Initialize();
        _lastLatitude = initialLatitude + 3;
        _lastLongitude = initialLongitude + 3;
        Viewer.InitialCoords = new Vector2Int(_lastLongitude, _lastLatitude);
    }

    private void Start()
    {
        
        DisplayMap();
    }

    public void GenerateFixedMoistureMap()
    {
        MoistureMap = Enumerable.Repeat(0.4f, MapHeight * MapWidth).ToArray();
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
        for (int latitude = 0; latitude < MapHeight; latitude++)
        {
            float distanceFromEcuador = Math.Abs(latitude - MapHeight/2);
            float normalizedDistance = distanceFromEcuador / 3.5f;
            float heat = 1.0f - normalizedDistance;
            
            LatitudeHeats[latitude] = heat;
        }
    }
    
    public static float GetHeat(Coordinates coordinates)
    {
        return LatitudeHeats[coordinates.Latitude];
    }

    private static void DisplayMap()
    {
        foreach (var chunk in TerrainChunksManager.TerrainChunks)
        {
            if (chunk == null) continue;
            MapDisplay.DisplayChunk(Instance.displayMode, chunk);
        }
    }
}

public struct Coordinates
{
    public readonly int Latitude;
    public readonly int Longitude;

    public Coordinates(int longitude, int latitude)
    {
        Longitude = longitude;
        Latitude = latitude;
    }
}
