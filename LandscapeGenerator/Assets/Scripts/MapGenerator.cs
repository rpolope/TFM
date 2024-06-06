using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.Serialization;

public class MapGenerator : MonoBehaviour
{
    public bool autoUpdate = true;
    public DrawMode drawMode;
    public NoiseData noiseData; 
    public TerrainData terrainData;
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

    public MapData GenerateMapData(int resolution, NoiseParameters parameters, float2 centre = default) {
        
        centre = centre.Equals(default) ? new float2(1f, 1f) : centre;
        
        var noiseMap = GenerateNoiseMap(resolution, centre, parameters);
        
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.MinHeight, terrainData.MaxHeight);
        
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
        return new TerrainParameters(noiseData.parameters, terrainData.parameters);
    }
    
    void OnValidate() {
        
        if (terrainData != null) {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }
        if (noiseData != null) {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null) {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }
    
    void OnValuesUpdated() {
        if (!Application.isPlaying) {
            MapDisplay.DrawMapInEditor(drawMode, GenerateMapData(terrainData.parameters.resolution, noiseData.parameters), GetTerrainParameters());
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

