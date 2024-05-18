using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class MapGenerator : MonoBehaviour
{
    public bool autoUpdate = true;
    public DrawMode drawMode;
    [Header("HeightMap Parameters")]
    public NoiseParameters heightMapParameters;
    [Header("MoistureMap Parameters")]
    public NoiseParameters moistureMapParameters;
    [Header("Mesh Variables")]
    public MeshParameters meshParameters;
    [Header("Biomes Params")]
    public BiomesParameters biomeParameters;
    public GameObject[] canvasObjects;
    
    public static MapGenerator Instance;
    public static int MapSize = 1024;

    private static Climate[] _regions = new[]
    {
        Climate.OCEAN,
        Climate.BEACH,
        Climate.TEMPERATE_DESERT,
        Climate.SHRUBLAND,
        Climate.GRASSLAND,
        Climate.TEMPERATE_RAIN_FOREST,
        Climate.SCORCHED,
        Climate.SNOW
    };

    private Transform _transform;
    private float[] _moistureMap;
    
    [BurstCompile]
    private struct GenerateMapJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] 
        public NativeArray<float> Map;
        public int MapSize;
        public float2 Centre;
        public NoiseParameters Parameters;

        public void Execute(int threadIndex)
        {
            int x = threadIndex % MapSize;
            int y = threadIndex / MapSize;
            
            float2 pos = new float2(x, y) + Centre;

            // float value = NoiseGenerator.GetNoiseValue(pos, Parameters);
            float value = GenerateHeight(pos);
            
            Map[threadIndex] = value;
        }
        
        private float GenerateHeight(float2 samplePos)
        {
            float ridgedFactor = Parameters.ridgeness;
            // float noiseValue = NoiseGenerator.GetNoiseValue(samplePos, TerrainParameters.noiseParameters);
            
            float noiseValue = (1 - ridgedFactor) * NoiseGenerator.GetNoiseValue(samplePos, Parameters);
            noiseValue += ridgedFactor * NoiseGenerator.GetFractalRidgeNoise(samplePos, Parameters);
            
            if (ridgedFactor > 0)
                noiseValue = Mathf.Pow(noiseValue, Parameters.ridgeRoughness);

            return noiseValue;
        }
    }


    private void Awake()
    {
        if (Instance != null)
        {
            Instance = this;
        }
        else
        {
            Destroy(Instance);
        }
    }

    public static MapData GenerateMapData(int resolution, float2 centre, NoiseParameters heightMapParams, NoiseParameters moistureMapParams)
    {
        BiomeManager.Initialize();
        // resolution = 1024;
        float[] noiseMap = GenerateNoiseMap(MapSize, centre, heightMapParams);
        float[] moistureMap = GenerateNoiseMap(MapSize, centre, moistureMapParams);
        
        Color[] colorMap = GenerateColorMap(noiseMap, moistureMap);
	
        return new MapData (noiseMap, colorMap);
    }

    private static Color[] GenerateColorMap(float[] heightMap, float[] moistureMap)
    {
        var mapSize = heightMap.Length;
        Color[] colorMap = new Color[mapSize];
        for (int i = 0; i < mapSize; i++) {
            
            float height = heightMap [i];
            float moisture = moistureMap [i];

            Color color = Color.Lerp(Color.red, Color.blue, moisture);
            color *= height;

            color = Color.Lerp(Color.black, Color.white, height);
            colorMap[i] = color;
            // Biome biome = new Biome(height, moisture);
            // colorMap[i] = biome.Color;

            // colorMap[i] = BiomeManager.GetColorFromBiome(height, moisture);
        }

        return colorMap;
    }

    private static float[] GenerateNoiseMap(int mapSize, float2 centre, NoiseParameters parameters)
    {
        NativeArray<float> map = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);
        
        var generateMapJob = new GenerateMapJob()
        {
            Map = map,
            Centre = centre,
            MapSize = mapSize,
            Parameters = parameters
        };
        generateMapJob.Schedule( mapSize* mapSize, 3000).Complete();

        var mapArray = new float[mapSize* mapSize];
        map.CopyTo(mapArray);
        map.Dispose();
        
        return mapArray;
    }

    public void GenerateWorldNoiseMap()
    {
        const int batchesNumber = 18;
        const int terrainChunksBatches = 10;
        float worldTerrainChunkSize = (meshParameters.resolution - 1) * LandscapeManager.Scale;
        float distanceBetweenBatches = terrainChunksBatches * worldTerrainChunkSize;

        var mapData = new MapData();
        int resolution = meshParameters.resolution * terrainChunksBatches;
        _transform = transform;
        canvasObjects = InstantiateCanvasObjects(worldTerrainChunkSize, batchesNumber);
        
        for (int i = 0; i < batchesNumber; i++)
        {
            for (int j = 0; j < batchesNumber; j++)
            {
                var centre = new float2(canvasObjects[i * batchesNumber + j].transform.position.x,
                    canvasObjects[i * batchesNumber + j].transform.position.z);
                mapData.HeightMap =
                    new NativeArray<float>(GenerateNoiseMap(resolution, centre, heightMapParameters),Allocator.Temp);
                MapDisplay.DrawMapInEditor(DrawMode.NoiseMap, mapData, new TerrainParameters(heightMapParameters, new MeshParameters(0.1f)), canvasObjects[i * batchesNumber + j]);
            }
        }
    }

    private GameObject[] InstantiateCanvasObjects(float distanceBetween, int instancesPerLine)
    {
        var canvas = new GameObject[instancesPerLine * instancesPerLine];
        float2 pos = new float2();
    
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);

        for (int y = 0; y < instancesPerLine; y++)
        {
            for (int x = 0; x < instancesPerLine; x++)
            {
                pos.x = x * distanceBetween;
                pos.y = y * distanceBetween;
            
                var canvasObj = Instantiate(go, new Vector3(pos.x, 0, pos.y), Quaternion.identity);
                canvasObj.name = $"Plane{x},{y}";
                canvasObj.transform.localScale = _transform.localScale;
                canvas[y * instancesPerLine + x] = canvasObj;
            }
        }

        DestroyImmediate(go);
        return canvas;
    }

}
public struct MapData {
    public NativeArray<float> HeightMap;
    public NativeArray<Color> ColorMap;

    public MapData (float[] heightMap, Color[] colorMap)
    {
        HeightMap = new NativeArray<float>(heightMap, Allocator.Persistent);
        ColorMap = new NativeArray<Color>(colorMap, Allocator.Persistent);;
    }

    public void Dispose()
    {
        HeightMap.Dispose();
        ColorMap.Dispose();
    }
}

