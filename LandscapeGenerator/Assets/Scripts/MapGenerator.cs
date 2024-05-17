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

            float value = NoiseGenerator.GetNoiseValue(pos, Parameters);

            Map[threadIndex] = value;
        }
    }
    
    public MapData GenerateMapData() {

        BiomeManager.Initialize();
        float[] noiseMap = GenerateMap(mapParameters.meshParameters.resolution, new float2(), mapParameters.noiseParameters);
        
        Color[] colorMap = GenerateColorMap(noiseMap);
	
        return new MapData (noiseMap, colorMap);
    }

    private static Color[] GenerateColorMap(float[] noiseMap)
    {
        var mapSize = noiseMap.Length;
        Color[] colorMap = new Color[mapSize];
        for (int i = 0; i < mapSize; i++) {
            
            float currentHeight = noiseMap [i];
            
            foreach (var biome in BiomeManager.Biomes)
            {
                if (currentHeight > biome.Elevation) continue;
                colorMap [i] = BiomeManager.GetColorFromBiome(biome);
                break;
            }
        }

        return colorMap;
    }

    public static float[] GenerateMap(int mapSize, float2 centre, NoiseParameters parameters)
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

        float[] mapArray = new float[mapSize * mapSize];
        map.CopyTo(mapArray);
        map.Dispose();
        
        return mapArray;
    }
}
public struct MapData {
    public float[] HeightMap;
    public Color[] ColorMap;

    public MapData (float[] heightMap, Color[] colorMap)
    {
        HeightMap = heightMap;
        ColorMap = colorMap;
    }
}

