using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class MapGenerator
{
    private const int MapSize = 241;

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
        // public int MapSize;
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
    
    private static MapData GenerateMapData(NoiseParameters parameters, Biome[] regions) {
        // float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(MapSize, parameters);
        float[] noiseMap = GenerateMap(MapSize, new float2(),parameters);

        Color[] colourMap = new Color[MapSize * MapSize];
        for (int y = 0; y < MapSize; y++) {
            for (int x = 0; x < MapSize; x++)
            {
                float currentHeight = noiseMap [x + y * MapSize];
                foreach (var region in regions)
                {
                    if (currentHeight > region.Elevation) continue;
                    colourMap [y * MapSize + x] = region.Color;
                    break;
                }
            }
        }

	
        return new MapData (noiseMap, colourMap);
    }

    public static float[] GenerateMap(int mapSize, float2 centre, NoiseParameters parameters)
    {
        NativeArray<float> map = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);
        
        var generateMapJob = new GenerateMapJob()
        {
            Map = map,
            // MapSize = mapSize,
            Centre = centre,
            Parameters = parameters
        };
        generateMapJob.Schedule( mapSize* mapSize, 3000).Complete();

        float[] mapArray = new float[mapSize * mapSize];
        map.CopyTo(mapArray);
        map.Dispose();
        
        return mapArray;
    }
    
    public static void DrawMapInEditor(MapDisplay display, DrawMode drawMode, TerrainParameters terrainParameters)
    {
        Climate[] regions = terrainParameters.biomesParameters.climates;
        Biome[] biomes = new Biome[regions.Length];

        for(int i = 0; i < biomes.Length; i++)
        {
            biomes[i] = BiomeManager.GetBiomeFromClimate(regions[i]);
        }
        
        var mapData = GenerateMapData(terrainParameters.noiseParameters, biomes);
        
        if (drawMode == DrawMode.NoiseMap) {
            display.DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.HeightMap));
        } else if (drawMode == DrawMode.ColourMap) {
            display.DrawTexture (TextureGenerator.TextureFromColourMap (mapData.ColourMap, MapSize));
        } else if (drawMode == DrawMode.Mesh)
        {
            var meshData = new MeshData(MapSize, 0);
            MeshGenerator.ScheduleMeshGenerationJob(terrainParameters, MapSize, 1, new float2(), 0,ref meshData).Complete();
            
            display.DrawMesh (meshData, TextureGenerator.TextureFromColourMap (mapData.ColourMap, MapSize));
        }
    }
}
public struct MapData {
    public readonly float[] HeightMap;
    public readonly Color[] ColourMap;

    public MapData (float[] heightMap, Color[] colourMap)
    {
        HeightMap = heightMap;
        ColourMap = colourMap;
    }
}
public enum DrawMode
{
    NoiseMap,
    ColourMap,
    Mesh
}
