using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [BurstCompile]
    private struct GenerateMapJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] 
        public NativeArray<float> HeightMap;
        public int MapSize;
        public float2 Centre;
        public NoiseParameters Parameters;
        public void Execute(int threadIndex)
        {
            int x = threadIndex / MapSize;
            int y = threadIndex % MapSize;
            
            float2 pos = new float2(x, y) + Centre;

            float value = Noise.GetNoiseValue(pos, Parameters);

            // if (value > 0.85f)
            //     value = RidgeValue(pos, value);
            
            HeightMap[threadIndex] = value;
        }
        
        private float RidgeValue(float2 pos, float noiseValue)
        {
            float ridgedFactor = Parameters.ridgeness;
            
            float ridgedValue = (1 - ridgedFactor) * noiseValue;
            ridgedValue += ridgedFactor * Noise.GetFractalRidgeNoise(pos, Parameters);
            
            if (ridgedFactor > 0)
                ridgedValue = Mathf.Pow(ridgedValue, Parameters.ridgeRoughness);

            return ridgedValue;
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
            // ColorMap[threadIndex] = Color.Lerp(Color.black, Color.white, HeightMap[threadIndex]);
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
    }

    public static Color[] GenerateColorMap(int mapSize, Biome biome, float[] heightMap)
    {
        var colorMap = new NativeArray<Color>(mapSize, Allocator.TempJob);
        var heights = new NativeArray<float>(heightMap, Allocator.TempJob);
        var moistureColors = new NativeArray<Color>(biome.ColorGradient, Allocator.TempJob);
        
        var generateMapJob = new GenerateColorMapJob()
        {
            HeightMap = heights,
            ColorMap = colorMap,
            BiomeMoistureColorRange = moistureColors,
            ChunkMoisture = biome.Moisture,
            LatitudeHeat = biome.Heat,
        };
        generateMapJob.Schedule( mapSize, 3000).Complete();

        var colors = new Color[mapSize];
        colorMap.CopyTo(colors);
        colorMap.Dispose();
        heights.Dispose();
        moistureColors.Dispose();
        
        return colors;
    }

    public static float[] GenerateNoiseMap(int mapSize, float2 centre, NoiseParameters parameters)
    {
        var map = new NativeArray<float>(mapSize, Allocator.TempJob);
        
        var generateMapJob = new GenerateMapJob()
        {
            HeightMap = map,
            Centre = centre,
            MapSize = mapSize,
            Parameters = parameters
        };
        generateMapJob.Schedule( mapSize, 3000).Complete();

        var mapArray = new float[mapSize];
        map.CopyTo(mapArray);
        map.Dispose();
        
        return mapArray;
    }
    
    public static MapData GenerateMapData(int resolution, float2 centre, NoiseParameters heightMapParams, NoiseParameters moistureMapParams, DisplayMode displayMode)
    {
        float[] noiseMap = GenerateNoiseMap(resolution * resolution, centre, heightMapParams); 
        float[] moistureMap = GenerateNoiseMap(resolution * resolution, centre, moistureMapParams);
        // var colorMap = GenerateColorMap(noiseMap, 0.5f);
        var colorMap = GetColorRangeFromMoisture(LandscapeManager.FixedMoisture);
        var colorGradient = BiomesManager.ColorMap[(int)(LandscapeManager.FixedMoisture * 128)];
       
        return displayMode == DisplayMode.Mesh ? 
                new MapData (noiseMap, colorGradient) : 
                new MapData (noiseMap, colorMap);
    }

    private static Color[] GetColorRangeFromMoisture(float moisture)
    {
        return BiomesManager.ColorMap[(int)(LandscapeManager.FixedMoisture * 128)];
    }
    
    private static Color[] GenerateTestingColorMap(float[] heightMap, float moisture)
    {
        var mapSize = heightMap.Length;
        var colorMap = new Color[mapSize];
        for (var i = 0; i < mapSize; i++) {
            
            var height = heightMap[i];

            Color color = Color.Lerp(Color.red, Color.blue, moisture);
            colorMap[i] = color * height;
        }

        return colorMap;
    }
}

public readonly struct MapData {
    [NativeDisableParallelForRestriction]
    public readonly NativeArray<float> HeightMap;
    [NativeDisableParallelForRestriction]
    public readonly NativeArray<Color> ColorMap;

    public MapData (float[] heightMap, Color[] colorMap)
    {
        HeightMap = new NativeArray<float>(heightMap, Allocator.Persistent);
        ColorMap = new NativeArray<Color>(colorMap, Allocator.Persistent);
    }

    public void Dispose()
    {
        HeightMap.Dispose();
        ColorMap.Dispose();
    }
}

