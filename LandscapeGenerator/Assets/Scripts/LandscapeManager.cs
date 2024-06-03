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
		BiomesManager.Initialize();

		GenerateMap();
		
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
	
	private void GenerateMap()
	{
		Maps = new MapData[MapHeight,MapWidth];
		for (int y = 0; y < MapHeight; y++)
		{
			for (int x = 0; x < MapWidth; x++)
			{
				Maps[x, y] = MapGenerator.GenerateMapData(TerrainChunksManager.TerrainChunk.Resolution,
					new float2(x, y) * (TerrainChunksManager.TerrainChunk.Resolution - 1), terrainParameters.noiseParameters, BiomesManager.GetBiome(new int2(x, y)));
			}
		}
		// UnifyMapBorders();
	}

	private void UnifyMapBorders()
	{
		int resolution = TerrainChunksManager.TerrainChunk.Resolution;

		// Unify internal borders
		for (int y = 0; y < MapHeight; y++)
		{
			for (int x = 0; x < MapWidth; x++)
			{
				MapData currentMap = Maps[x, y];

				// Unify the left border with the right border of the left neighbor
				if (x > 0)
				{
					MapData leftMap = Maps[x - 1, y];
					for (int i = 0; i < resolution; i++)
					{
						currentMap.HeightMap[i * resolution] = leftMap.HeightMap[i * resolution + (resolution - 1)];
					}
				}

				// Unify the top border with the bottom border of the top neighbor
				if (y > 0)
				{
					MapData topMap = Maps[x, y - 1];
					for (int i = 0; i < resolution; i++)
					{
						currentMap.HeightMap[i] = topMap.HeightMap[(resolution - 1) * resolution + i];
					}
				}
			}
		}

		// Unify the leftmost and rightmost borders
		for (int y = 0; y < MapHeight; y++)
		{
			MapData leftmostMap = Maps[0, y];
			MapData rightmostMap = Maps[MapWidth - 1, y];
			for (int i = 0; i < resolution; i++)
			{
				leftmostMap.HeightMap[i * resolution] = rightmostMap.HeightMap[i * resolution + (resolution - 1)];
				rightmostMap.HeightMap[i * resolution + (resolution - 1)] = leftmostMap.HeightMap[i * resolution];
			}
		}

		// Unify the topmost and bottommost borders
		for (int x = 0; x < MapWidth; x++)
		{
			MapData topmostMap = Maps[x, 0];
			MapData bottommostMap = Maps[x, MapHeight - 1];
			for (int i = 0; i < resolution; i++)
			{
				topmostMap.HeightMap[i] = bottommostMap.HeightMap[(resolution - 1) * resolution + i];
				bottommostMap.HeightMap[(resolution - 1) * resolution + i] = topmostMap.HeightMap[i];
			}
		}
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