using System;
using Unity.Collections;
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

    public static void DrawMapInEditor(DisplayMode drawMode, MapData maps, TerrainParameters terrainParameters, MapVisualizer visualizer)
    {
        GameObject canvas = visualizer.gameObject;
        MeshRenderer renderer = canvas.GetComponent<MeshRenderer>();
        MeshFilter meshFilter = canvas.GetComponent<MeshFilter>();

        int resolution = visualizer.meshParameters.resolution;
        var mockedMoistureValue = LandscapeManager.FixedMoisture;
        var mockedHeatValue = 0.66f;
        var colorGradient = BiomesManager.ColorMap[(int)(mockedMoistureValue*127)];
        
        switch (drawMode)
        {
            case DisplayMode.NoiseMap:
                MapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(maps.HeightMap.ToArray(), resolution), renderer);
                break;
            
            case DisplayMode.ColorMap:
                /* Para pintar el mapa */
                // Color[] colorMap = GetColorMapFromGradient(visualizer.colorGradient, maps.HeightMap.ToArray());

                Color[] colorMap = GetColorMapFromColorRange(resolution, colorGradient, maps.HeightMap.ToArray(), mockedHeatValue);
                MapDisplay.DrawTexture (TextureGenerator.TextureFromColorMap (colorMap, resolution), renderer);
                
                /* Para mostrar la textura de test del BiomesManager */
                // DrawTexture (BiomesManager.TextureTest, renderer);
                break;
            
            case DisplayMode.Mesh:
                MeshData meshData = new MeshData(resolution, 0);
                Color[] colors = GetColorMapFromColorRange(resolution, colorGradient, maps.HeightMap.ToArray(), mockedHeatValue);
                meshData.Colors = new NativeArray<Color>(colors, Allocator.Persistent);
                
                MeshGenerator.ScheduleMeshGenerationJob(terrainParameters.meshParameters, resolution, maps, ref meshData).Complete();
                MapDisplay.DrawMesh(meshData, meshFilter);
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(drawMode), drawMode, null);
        }
    }
    
    
    private static Color[] GetColorMapFromColorRange(int mapSize, Color[] colorRange, float[] heightMap, float heat)
    {
        var colorMap = new Color[mapSize * mapSize];
        for (int i = 0; i < colorMap.Length; i++)
        {
            var row = i / mapSize;
            var latitudeRelative = row / (mapSize - 1f);

            var rangeIndex = Mathf.FloorToInt(latitudeRelative * 127);
            colorMap[i] = colorRange[rangeIndex];
        }

        return colorMap;
    }

    public static Color[] GetColorRangeFromMoisture(float moisture)
    {
        return BiomesManager.ColorMap[(int)(moisture * 128)];
    }

    public static Texture2D GetTextureFromMoistureColorGradient(int resolution)
    {
        var colorMap = new Color[resolution * resolution];
        return TextureGenerator.TextureFromColorMap(colorMap, resolution);
    }
    private static Color[] GetColorMapFromGradient(Gradient gradient, float[] heightMap)
    {
        Color[] colorMap = new Color[heightMap.Length];
        for (int i = 0; i < colorMap.Length; i++)
        {
            colorMap[i] = gradient.Evaluate(heightMap[i]);
        }
        return colorMap;
    }

    private static Color[] GetColorRangeFromMoisture(int mapSize, TerrainChunk chunk)
    {
        Color[] colorMap = new Color[mapSize * mapSize];
        Biome biome = chunk.Biome;
        Color[] colorGradient = biome.ColorGradient;
        

        for (int i = 0; i < colorMap.Length; i++)
        {
            float heat = biome.Heat - chunk.HeightMap[i] * 0.6f;
            Color color = colorGradient[(int)(heat * 127 + 1 / 128f)];
            colorMap[i] = color;
        }

        return colorMap;
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

