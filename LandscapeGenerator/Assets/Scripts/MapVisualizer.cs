using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class MapVisualizer : MonoBehaviour
{
    public bool autoUpdate = true;
    public DisplayMode displayMode;
    [Header("HeightMap Parameters")]
    public NoiseParameters heightMapParameters;
    [Header("MoistureMap Parameters")]
    public NoiseParameters moistureMapParameters;
    [Header("Mesh Variables")]
    public MeshParameters meshParameters;
    [Header("Biomes Params")]
    public BiomesParameters biomeParameters;

    public Gradient colorGradient;
    public Material mat;
    public static MapVisualizer Instance;
    public static int MapSize = 7;

    private Transform _transform;
    private float[] _moistureMap;
    
    [BurstCompile]
    private struct GenerateMapJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] 
        public NativeArray<float> HeightMap;
        public int Resolution;
        public float2 Centre;
        public NoiseParameters Parameters;

        public void Execute(int threadIndex)
        {
            int x = threadIndex % Resolution;
            int y = threadIndex / Resolution;
            
            float2 pos = new float2(x, y) + Centre;

            float value = Noise.GetNoiseValue(pos, Parameters);
            // float value = GenerateHeight(pos);
            
            HeightMap[threadIndex] = value;
        }
        
        private float GenerateHeight(float2 samplePos)
        {
            float ridgedFactor = Parameters.ridgeness;
            // float noiseValue = Noise.GetNoiseValue(samplePos, TerrainParameters.noiseParameters);
            
            float noiseValue = (1 - ridgedFactor) * Noise.GetNoiseValue(samplePos, Parameters);
            noiseValue += ridgedFactor * Noise.GetFractalRidgeNoise(samplePos, Parameters);
            
            if (ridgedFactor > 0)
                noiseValue = Mathf.Pow(noiseValue, Parameters.ridgeRoughness);

            return noiseValue;
        }
    }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(Instance);
        }
    }

    public MapData GenerateMapData(int resolution, float2 centre, NoiseParameters heightMapParams, NoiseParameters moistureMapParams)
    {
        float[] noiseMap = GenerateNoiseMap(resolution, centre, heightMapParams); 
        float[] moistureMap = GenerateNoiseMap(MapSize, centre, moistureMapParams);
        // var colorMap = GenerateColorMap(noiseMap, 0.5f);
        var colorMap = GetColorRangeFromMoisture(0.5f);
        
        return displayMode == DisplayMode.Mesh ? new MapData (noiseMap, GetColorGradient()) : new MapData (noiseMap, colorMap);
    }

    public static Color[] GetColorRangeFromMoisture(float moisture)
    {
        return BiomesManager.ColorMap[(int)(moisture * 128)];
    }

    private static Color[] GenerateColorMap(float[] heightMap, float moisture)
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

    private static float[] GenerateNoiseMap(int resolution, float2 centre, NoiseParameters parameters)
    {
        var map = new NativeArray<float>(resolution * resolution, Allocator.TempJob);
        
        var generateMapJob = new GenerateMapJob()
        {
            HeightMap = map,
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

    private Color[] GetColorGradient()
    {
        var gradientColorArray = new Color[100];

        for (int i = 0; i < 100; i++)
        {
            gradientColorArray[i] = colorGradient.Evaluate(i / 100f);
        }

        return gradientColorArray;   
    }
}

