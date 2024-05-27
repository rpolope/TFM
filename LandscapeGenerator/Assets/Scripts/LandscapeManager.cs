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
    
    public static LandscapeManager Instance;
    
    public const float Scale = 1f;
    public const int MapWidth = 7;
    public const int MapHeight = 7;
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
        MapDisplay.DisplayMode = displayMode;
        GenerateMoistureMap();
        InitializeLatitudeHeats();
        Transform = transform;
        BiomesManager.Initialize(false);
        BatchesManager.Initialize();
        TerrainChunksManager.Initialize();
        _lastLatitude = initialLatitude + 3;
        _lastLongitude = initialLongitude + 3;
        Viewer.InitialCoords = new Vector2Int(_lastLongitude, _lastLatitude);
        Viewer.Initialize();
        
        TerrainChunksManager.DisplayChunks();
    }

    private void Update()
    {
        if (Viewer.PositionChanged()) {
            Viewer.UpdateOldPosition();
            TerrainChunksManager.UpdateVisibleChunks();
        }
        /* */
		
        if (Viewer.RotationChanged())
        {
            Viewer.UpdateOldRotation();
            TerrainChunksManager.UpdateCulledChunks();
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
