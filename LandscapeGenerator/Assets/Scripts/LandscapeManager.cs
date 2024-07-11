using System;
using System.Linq;
using Jobs;
using TreeEditor;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using static TerrainChunksManager;

public class LandscapeManager : MonoBehaviour{
	
	public const float Scale = 2.5f;
	public const int MapHeight = 16;
	public const int MapWidth = 16;
	public static LandscapeManager Instance;
	public static MapData[,] Maps { get; private set; }
	public static Texture2D[,] MapTextures { get; private set; }
	private static float[] LatitudeHeats { get; set; }
	private static float[] MoistureMap { get; set; }
	
	public Transform Transform { get; private set; }

	public int initialLatitude;
	public int initialLongitude;
	public MapGenerator mapGenerator;
	public NoiseData noiseData;
	public TerrainData terrainData;
	public NoiseParameters moistureParameters;
	public Texture2D moistureTexture;
	public BiomesManager biomesManager;
	public CullingMode culling;
	
	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;
	internal TerrainChunksManager ChunksManager;

	private static float[] _fixedBorderHeightValues = new float[TerrainChunk.Resolution];
	
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
	    // Scale = terrainData.parameters.scale;
        Transform = transform;
        
        GenerateMoistureMapFromTexture();
        InitializeLatitudeHeats();
        biomesManager.Initialize();

        GenerateMap();
        
        var relativeInitialLatitude = (int)(((initialLatitude + 90f) / 180f) * MapHeight);
        var relativeInitialLongitude = (int)(((initialLongitude + 90f) / 180f) * MapWidth);
        Viewer.ChunkCoord = new int2(relativeInitialLongitude, relativeInitialLatitude);
        SetViewerInitPos(relativeInitialLongitude, relativeInitialLatitude);

        TerrainChunk.InitializeMaterial(terrainData);
        ChunksManager = gameObject.AddComponent<TerrainChunksManager>();
        ChunksManager.Initialize();
    }

    private void SetViewerInitPos(int relativeInitialLongitude, int relativeInitialLatitude)
    {
	    var initPos = new float2((relativeInitialLongitude) * TerrainChunk.WorldSize,
		    (relativeInitialLatitude) * TerrainChunk.WorldSize);
	    Viewer.SetInitialPos(initPos);
    }

    private void GenerateMap()
    {
        Maps = new MapData[MapHeight, MapWidth];
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                Maps[x, y] = mapGenerator.GenerateMapData(TerrainChunk.Resolution, noiseData.parameters, new float2(x, y) * (TerrainChunk.Resolution - 1));
            }
        }
        UnifyMapBorders();
        
        MapTextures = new Texture2D[MapHeight, MapWidth];
        for (int y = 0; y < MapHeight; y++)
        {
	        for (int x = 0; x < MapWidth; x++)
	        {
		        MapTextures[x, y] = TextureGenerator.TextureFromHeightMap(Maps[x, y].HeightMap.ToArray());
	        }
        }
    }

    private void UnifyMapBorders()
    {
        int resolution = TerrainChunk.Resolution;

        // North and South border unification
        for (int i = 0; i < MapWidth; i++)
        {
            var northMap = Maps[i, MapHeight - 1];
            var southMap = Maps[i, 0];

            for (int j = 0; j < resolution; j++)
            {
	            _fixedBorderHeightValues[j] = 0.5f * (northMap.HeightMap[northMap.HeightMap.Length - j - 1] + southMap.HeightMap[resolution - 1 - j]);
	            southMap.HeightMap[resolution - 1 - j] = _fixedBorderHeightValues[j];
	            northMap.HeightMap[northMap.HeightMap.Length - j - 1] = _fixedBorderHeightValues[j];
            }
        }

        // West and East border unification
        for (int i = 0; i < MapHeight; i++)
        {
            var westMap = Maps[0, i];
            var eastMap = Maps[MapWidth - 1, i];
        
            for (int j = 0; j < resolution; j++)
            {
	            _fixedBorderHeightValues[j] = 0.5f * (westMap.HeightMap[j * resolution] + eastMap.HeightMap[j * resolution + resolution - 1]);
	            eastMap.HeightMap[j * resolution + resolution - 1] = _fixedBorderHeightValues[j];
	            westMap.HeightMap[j * resolution] = _fixedBorderHeightValues[j];
            }
        }
    }

    private void DisposeMaps()
    {
	    foreach (var map in Maps)
	    {
		    map.Dispose();
	    }
    }

	private void Update()
	{
		ChunksManager.Update();
	}

	public static void GenerateStaticMoistureMap(NoiseParameters moistureParameters)
	{
		MoistureMap = MapGenerator.GenerateNoiseMap(MapWidth * MapHeight, new int2(), moistureParameters);
	}

	private void GenerateMoistureMapFromTexture()
	{
		if (moistureTexture == null)
		{
			Debug.LogError("Moisture texture is not assigned!");
			return;
		}

		int textureWidth = moistureTexture.width;
		int textureHeight = moistureTexture.height;

		MoistureMap = new float[MapWidth * MapHeight];

		NativeArray<Color> moistureTextureData = new NativeArray<Color>(moistureTexture.GetPixels(), Allocator.TempJob);
		NativeArray<float> moistureMap = new NativeArray<float>(MoistureMap, Allocator.TempJob);

		var moistureJob = new MoistureComputeJob()
		{
			MoistureTextureData = moistureTextureData,
			TextureWidth = textureWidth,
			TextureHeight = textureHeight,
			MapWidth = MapWidth,
			MapHeight = MapHeight,
			MoistureMap = moistureMap
		};

		JobHandle jobHandle = moistureJob.Schedule(MapWidth * MapHeight, 64);
		jobHandle.Complete();

		moistureMap.CopyTo(MoistureMap);

		moistureTextureData.Dispose();
		moistureMap.Dispose();
	}


	public static float GetMoisture(int2 coordinates)
	{
		return MoistureMap[coordinates.x + coordinates.y * MapWidth];
	}
	
	private static void InitializeLatitudeHeats()
	{
		LatitudeHeats = new float[MapHeight];
		const float ecuador = MapHeight * 0.5f;
        
		for (int latitude = 0; latitude < MapHeight; latitude++)
		{
			float distanceFromEcuador = Math.Abs(latitude - ecuador);
			float heat = 1 - distanceFromEcuador/ecuador;
            
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