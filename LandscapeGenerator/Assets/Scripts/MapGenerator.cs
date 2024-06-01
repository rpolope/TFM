using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public bool autoUpdate = true;
    public DrawMode drawMode;
    public TerrainParameters mapParameters;
    [Header("Biomes Params")]
    public BiomesParameters biomeParameters;

    public static MapGenerator Instance;
    
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

    [BurstCompile]
    private struct GenerateColorMapJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] 
        public NativeArray<float> HeightMap;
        [NativeDisableParallelForRestriction] 
        public NativeArray<Color> ColorMap;
        [NativeDisableParallelForRestriction] 
        public NativeArray<Color> BiomeMoistureColorRange;
        public float LatitudeHeat;
        public float ChunkMoisture;

        public Color BiomeColor;
        
        public void Execute(int threadIndex)
        {
            var height = HeightMap[threadIndex];
            var heightTempDecr = height * 0.6f;
            var heat = Mathf.Clamp01(LatitudeHeat - heightTempDecr);
            ColorMap[threadIndex] = BiomeMoistureColorRange[Mathf.RoundToInt(LatitudeHeat * 127)] * height;
        }
        
        private void GenerateMoistureBasedColor(int threadIndex)
        {
            var color = Color.Lerp(Color.red, Color.blue, ChunkMoisture);
            ColorMap[threadIndex] = color * HeightMap[threadIndex];
        }
        
        private void GenerateLatitudeHeatBasedColor(int threadIndex)
        {
            ColorMap[threadIndex] = Color.Lerp(Color.blue, Color.red, LatitudeHeat);
        }

        private void GenerateHeightBasedColor(int threadIndex)
        {
            ColorMap[threadIndex] = Color.Lerp(Color.black, Color.white, HeightMap[threadIndex]);
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

    public static MapData GenerateMapData(int resolution, float2 centre, NoiseParameters parameters, Biome biome) {

        float[] noiseMap = GenerateNoiseMap(resolution, centre, parameters);
        
        Color[] colorMap = GenerateColorMap(noiseMap, biome);
	
        return new MapData (noiseMap, colorMap);
    }

    private static Color[] GenerateColorMap(float[] noiseMap, Biome biome)
    {
        NativeArray<float> heightMap = new NativeArray<float>(noiseMap, Allocator.Persistent);
        NativeArray<Color> colorMap = new NativeArray<Color>(noiseMap.Length, Allocator.Persistent);
        NativeArray<Color> colorRange = new NativeArray<Color>(biome.ColorGradient, Allocator.Persistent);
        

        GenerateColorMapJob colorMapJob = new GenerateColorMapJob()
        {
            HeightMap = heightMap,
            ColorMap = colorMap,
            BiomeMoistureColorRange = colorRange,
            LatitudeHeat = biome.Heat,
            ChunkMoisture = biome.Moisture,
        };
        colorMapJob.Schedule(colorMap.Length, 64).Complete();

        var colorMapArray = colorMap.ToArray();
        heightMap.Dispose();
        colorMap.Dispose();
        colorRange.Dispose();
        
        return colorMapArray;
    }


    public static float[] GenerateNoiseMap(int mapSize, float2 centre, NoiseParameters parameters)
    {
        NativeArray<float> map = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);
        
        var generateMapJob = new GenerateMapJob
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

