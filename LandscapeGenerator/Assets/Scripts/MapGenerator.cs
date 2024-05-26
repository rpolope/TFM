using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

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
            int x = threadIndex % MapSize;
            int y = threadIndex / MapSize;
            
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
            var heightTempDecr = 1.0f - (height);
            var heat = Mathf.Clamp01(LatitudeHeat - heightTempDecr);
            ColorMap[threadIndex] = BiomeMoistureColorRange[(int)heat * 127];
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

    public static Color[] GenerateColorMap(int mapSize, float heat, float moisture, float[] heightMap, Color[] moistureColorRange)
    {
        var colorMap = new NativeArray<Color>(mapSize, Allocator.TempJob);
        var heights = new NativeArray<float>(heightMap, Allocator.TempJob);
        var moistureColors = new NativeArray<Color>(moistureColorRange, Allocator.TempJob);
        
        var generateMapJob = new GenerateColorMapJob()
        {
            HeightMap = heights,
            ColorMap = colorMap,
            BiomeMoistureColorRange = moistureColors,
            ChunkMoisture = moisture,
            LatitudeHeat = heat,
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
}

public struct MapData {
    [NativeDisableParallelForRestriction]
    public NativeArray<float> HeightMap;
    [NativeDisableParallelForRestriction]
    public NativeArray<Color> ColorMap;

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

