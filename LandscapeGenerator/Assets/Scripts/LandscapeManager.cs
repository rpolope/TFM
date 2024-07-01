using System;
using System.Linq;
using TreeEditor;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using static TerrainChunksManager;

public class LandscapeManager : MonoBehaviour{
	
	public static float Scale = 1f;
	public const int MapHeight = 16;
	public const int MapWidth = 16;
	public static LandscapeManager Instance;
	public static MapData[,] Maps { get; private set; }
	private static float[] LatitudeHeats { get; set; }
	private static float[] MoistureMap { get; set; }
	
	public Transform Transform { get; private set; }

	public int initialLatitude;
	public int initialLongitude;
	public MapGenerator mapGenerator;
	public NoiseData noiseData;
	public TerrainData terrainData;
	public TextureData textureData;
	public NoiseParameters moistureParameters;
	public BiomesManager biomesManager;
	public Viewer viewer;
	public CullingMode culling;

	private const float FixedMoisture = 0.1f;
	
	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;
	private TerrainChunksManager _chunksManager;

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
	    Scale = terrainData.parameters.scale;
        Transform = transform;
        
        GenerateMoistureMap();
        InitializeLatitudeHeats();
        biomesManager.Initialize();

        GenerateMap();
        
        var relativeInitialLatitude = (int)(((initialLatitude + 90f) / 180f) * MapHeight);
        var relativeInitialLongitude = (int)(((initialLongitude + 90f) / 180f) * MapWidth);
        Viewer.ChunkCoord = new int2(relativeInitialLongitude, relativeInitialLatitude);
        SetViewerInitPos(relativeInitialLongitude, relativeInitialLatitude);
        
        InstantiateWaterPlane();

        TerrainChunk.InitializeMaterial();
        _chunksManager = new TerrainChunksManager();
        _chunksManager.Initialize();
    }

    private void InstantiateWaterPlane()
    {
	    var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
	    var transform = water.transform;
	    transform.localPosition = new Vector3(Viewer.PositionV2.x, terrainData.parameters.waterLevel * terrainData.parameters.heightScale * Scale, Viewer.PositionV2.y);
	    var waterSize = ChunksVisibleInViewDist * TerrainChunk.WorldSize;
	    transform.localScale = new Vector3(waterSize, 1, waterSize);
	    transform.parent = viewer.transform;
	    water.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Custom/Water"));
    }

    private void SetViewerInitPos(int relativeInitialLongitude, int relativeInitialLatitude)
    {
	    // var initChunk = TerrainChunksManager.GetChunk(new int2(relativeInitialLongitude, relativeInitialLatitude));
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
        // UnifyMapBorders();
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
		_chunksManager.Update();
	}

	private void LateUpdate()
	{
		CompleteMeshGeneration();
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