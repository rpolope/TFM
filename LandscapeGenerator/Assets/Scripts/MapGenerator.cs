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
    public NoiseData noise; 
    public TerrainData terrain;
    public TextureData textureData;
    public Material terrainMaterial;

    [BurstCompile]
    private struct GenerateMapJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] 
        public NativeArray<float> Map;
        public int Resolution;
        public float2 Centre;
        public NoiseParameters Parameters;

        public void Execute(int threadIndex)
        {
            int x = threadIndex % Resolution;
            int y = threadIndex / Resolution;
            
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

    public static MapData GenerateMapData(int resolution, NoiseParameters parameters, Biome biome, float2 centre = new float2()) {

        BiomesManager.Initialize();
        
        float[] noiseMap = GenerateNoiseMap(resolution, centre, parameters);
        
        return new MapData (noiseMap);
    }

    public static float[] GenerateNoiseMap(int resolution, float2 centre, NoiseParameters parameters)
    {
        NativeArray<float> map = new NativeArray<float>(resolution * resolution, Allocator.TempJob);
        
        var generateMapJob = new GenerateMapJob
        {
            Map = map,
            Centre = centre,
            Resolution = resolution,
            Parameters = parameters
        };
        generateMapJob.Schedule( resolution* resolution, 3000).Complete();

        var mapArray = new float[resolution* resolution];
        map.CopyTo(mapArray);
        map.Dispose();
        
        return mapArray;
    }

    public TerrainParameters GetTerrainParameters()
    {
        return new TerrainParameters(noise.parameters, terrain.parameters);
    }
    
    void OnValidate() {

        BiomesManager.Initialize();
        
        if (terrain != null) {
            terrain.OnValuesUpdated -= OnValuesUpdated;
            terrain.OnValuesUpdated += OnValuesUpdated;
        }
        if (noise != null) {
            noise.OnValuesUpdated -= OnValuesUpdated;
            noise.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null) {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }
    
    void OnValuesUpdated() {
        if (!Application.isPlaying) {
            MapDisplay.DrawMapInEditor(drawMode, GenerateMapData(terrain.parameters.resolution, noise.parameters,new Biome(0f, 0f)), GetTerrainParameters());
        }
    }
    
    void OnTextureValuesUpdated() {
        textureData.ApplyToMaterial (terrainMaterial);
    }
}
public struct MapData {
    public NativeArray<float> HeightMap;

    public MapData (float[] heightMap)
    {
        HeightMap = new NativeArray<float>(heightMap, Allocator.Persistent);
    }

    public void Dispose()
    {
        HeightMap.Dispose();
    }
}

