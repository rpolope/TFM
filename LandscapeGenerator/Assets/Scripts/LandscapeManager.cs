using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class LandscapeManager : MonoBehaviour{
	
	public const float Scale = 1f;
	public const int MapHeight = 30;
	public const int MapWidth = 30;
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
		var relativeInitialLatitude = Mathf.RoundToInt(((initialLatitude + 90f) / 180f) * MapHeight);
		var relativeInitialLongitude = Mathf.RoundToInt(((initialLongitude + 90f) / 180f) * MapWidth);
		Viewer.SetInitialPos(relativeInitialLongitude, relativeInitialLatitude);
		GenerateMoistureMap();
		InitializeLatitudeHeats();
		Maps = new MapData[MapHeight,MapWidth];

		for (int y = 0; y < MapHeight; y++)
		{
			for (int x = 0; x < MapWidth; x++)
			{
				Maps[x, y] = MapGenerator.GenerateMapData(TerrainChunksManager.TerrainChunk.Resolution,
					new float2(x, y) * (TerrainChunksManager.TerrainChunk.Resolution - 1), terrainParameters.noiseParameters);
			}
		}
		
		BiomesManager.Initialize();
		_chunksManager = new TerrainChunksManager();
		_chunksManager.Initialize();
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
    
	public void GenerateMoistureMap()
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