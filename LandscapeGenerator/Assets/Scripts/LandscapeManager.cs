using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class LandscapeManager : MonoBehaviour{
	
	public const float Scale = 1f;
	public const int MapHeight = 33;
	public const int MapWidth = 33;
	public static LandscapeManager Instance;
	public static MapData[,] Maps { get; private set; }
	public static float[] LatitudeHeats { get; private set; }
	public static float[] MoistureMap { get; set; }
	
	public Transform Transform { get; private set; }
	public int initialLatitude;
	public int initialLongitude;
	public TerrainParameters terrainParameters;
	public NoiseParameters moistureParameters;
	public BiomesParameters biomesParameters;
	public Viewer viewer;
	public CullingMode culling;

	private const float FixedMoisture = 0.1f;
	
	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;
	private TerrainChunksManager _chunksManager;

	private static float[] _fixedBorderHeightValues = new float[TerrainChunksManager.TerrainChunk.Resolution];
	
	 private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(Instance.gameObject);
            Instance = this;
        }
    }

    private void Start()
    {
        Transform = transform;
        var relativeInitialLatitude = Mathf.RoundToInt(((initialLatitude + 90f) / 180f) * MapHeight);
        var relativeInitialLongitude = Mathf.RoundToInt(((initialLongitude + 90f) / 180f) * MapWidth);
        Viewer.SetInitialPos(relativeInitialLongitude, relativeInitialLatitude);
        GenerateMoistureMap();
        InitializeLatitudeHeats();
        BiomesManager.Initialize();

        GenerateMap();

        _chunksManager = new TerrainChunksManager();
        _chunksManager.Initialize();
    }

    private void GenerateMap()
    {
        Maps = new MapData[MapHeight, MapWidth];
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                Maps[x, y] = MapGenerator.GenerateMapData(TerrainChunksManager.TerrainChunk.Resolution,
                    new float2(x, y) * (TerrainChunksManager.TerrainChunk.Resolution - 1), terrainParameters.noiseParameters, BiomesManager.GetBiome(new int2(x, y)));
            }
        }
        UnifyMapBorders();
    }

    private void UnifyMapBorders()
    {
        int resolution = TerrainChunksManager.TerrainChunk.Resolution;

        // North and South border unification
        for (int i = 0; i < MapWidth; i++)
        {
            var northMap = Maps[MapHeight - 1, i];
            var southMap = Maps[0, i];

            for (int j = 0; j < resolution; j++)
            {
                _fixedBorderHeightValues[j] = northMap.HeightMap[i];
                southMap.HeightMap[southMap.HeightMap.Length - resolution + j] = _fixedBorderHeightValues[j];
            }
        }

        // West and East border unification
        for (int i = 0; i < MapHeight; i++)
        {
            var westMap = Maps[i, 0];
            var eastMap = Maps[i, MapWidth - 1];

            for (int j = 0; j < resolution; j++)
            {
                _fixedBorderHeightValues[j] = westMap.HeightMap[j * resolution];
                eastMap.HeightMap[j * resolution + resolution - 1] = _fixedBorderHeightValues[j];
            }
        }
    }

	private void Update()
	{
		_chunksManager.Update();
	}

	private void LateUpdate()
	{
		TerrainChunksManager.CompleteMeshGeneration();
	}
	
	public void GenerateFixedMoistureMap()
	{
		MoistureMap = Enumerable.Repeat(FixedMoisture, MapHeight * MapWidth).ToArray();
	}

	public static void GenerateStaticMoistureMap(NoiseParameters moistureParameters)
	{
		MoistureMap = MapGenerator.GenerateNoiseMap(MapWidth * MapHeight, new int2(), moistureParameters);
	}

	private void GenerateMoistureMap()
	{
		MoistureMap = MapGenerator.GenerateNoiseMap(MapWidth * MapHeight, new int2(), moistureParameters);
	}

	public static float GetMoisture(int2 coordinates)
	{
		return MoistureMap[coordinates.x + coordinates.y * MapWidth];
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

	public static float GetHeat(int2 coordinates)
	{
		return LatitudeHeats[coordinates.y];
	}
}

public enum CullingMode
{
	Layer,
	Visibility
}