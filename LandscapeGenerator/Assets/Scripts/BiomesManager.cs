using System;
using Unity.Mathematics;
using UnityEngine;

public enum ClimateType
{
    Grassland,
    Forest,
    Ocean,
    TropicalForest,
    Beach,
    Desert,
    Scorched,
    Shrubland,
    Tundra,
    Snow,
    Bare,
    Taiga
}

public class BiomesManager : MonoBehaviour
{
    private static Biome[,] _biomes;
    
    private static Texture2D _colorMapTexture;
    public static Color[][] ColorMap { get; private set; }
    private static bool _isInitialized = false;

    public BiomeData[] biomeData;
    public static void Initialize()
    {
        if (_isInitialized && !Application.isPlaying) return;
        
        LoadColorMapTexture();
        if (Application.isPlaying)
            InitializeBiomes();
        _isInitialized = true;
    }

    private static void TestColorRangeColorSampling(Color[] colorGradient)
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.position += Vector3.up * 3;

        MeshRenderer meshRenderer = plane.GetComponent<MeshRenderer>();
        int resolution = 11;

        Color[] colors = new Color[resolution * resolution];
        
        for (int i = 0; i < colors.Length; i++)
        {
            var row = i / resolution;
            var color = row / resolution;
            var latitude = Mathf.Abs((float)row / (resolution - 1) - 0.5f);
            colors[i] = colorGradient[color * 127];
        }

        Texture2D texture = TextureGenerator.TextureFromColorMap(colors, resolution);
        meshRenderer.sharedMaterial.mainTexture = texture;
    }

    private static void LoadColorMapTexture()
    {
        // _colorMapTexture = Resources.Load<Texture2D>("Textures/biome-lookup-discrete");
        _colorMapTexture = Resources.Load<Texture2D>("Textures/biome-lookup-smooth");
        if (_colorMapTexture == null)
        {
            Debug.LogError("No se pudo cargar la textura biome-lookup-discrete.");
            return;
        }

        ColorMap = new Color[_colorMapTexture.height][];
        for (int y = 0; y < _colorMapTexture.height; y++)
        {
            ColorMap[y] = new Color[_colorMapTexture.width];
            for (int x = 0; x < _colorMapTexture.width; x++)
            {
                ColorMap[y][x] = _colorMapTexture.GetPixel(y, x);
            }
        }
    }

    private static void InitializeBiomes()
    {
        int mapWidth = LandscapeManager.MapWidth;
        int mapHeight = LandscapeManager.MapHeight;

        _biomes = new Biome[mapWidth, mapHeight];

        for (int i = 0; i < mapHeight; i++)
        {
            for (int j = 0; j < mapWidth; j++)
            {
                var coordinates = new int2(j, i);
                var moisture = LandscapeManager.GetMoisture(coordinates);
                var heat = LandscapeManager.GetHeat(coordinates);
                
                _biomes[j, i] = new Biome(heat, moisture);
            }
        }
    }

    private static void UpdateBiomes()
    {
        foreach (var biome in _biomes)
        {
            biome.Update();
        }
    }
    
    public static Biome GetBiome(int2 coordinates)
    {
        return _biomes[coordinates.x, coordinates.y];
    }
}

public class Biome
{
    public TerrainParameters TerrainParameters { get; private set; }
    public float Moisture { get; private set; }
    public float Heat { get; private set; }
    public Color[] ColorGradient { get; private set; }
    private ClimateType ClimateType { get; set; }

    public Biome(float heat, float moisture)
    {
        Moisture = moisture;
        Heat = heat;
        ClimateType = GetClimateType(moisture, heat);
        ColorGradient = BiomesManager.ColorMap[Mathf.RoundToInt(Moisture * 127)];
        // TerrainParameters = new TerrainParameters(new NoiseParameters(GetNoiseType()), new MeshParameters(0.1f));
        TerrainParameters = new TerrainParameters(new NoiseParameters(NoiseType.Perlin), new MeshParameters(0.1f));
    }

    public void Update() { }

    private NoiseType GetNoiseType()
    {
        return ClimateType switch
        {
            ClimateType.Grassland or ClimateType.Forest or ClimateType.Ocean => NoiseType.Perlin,
            ClimateType.TropicalForest => NoiseType.Simplex,
            ClimateType.Beach or ClimateType.Desert or ClimateType.Scorched or ClimateType.Shrubland => NoiseType.Voronoi,
            ClimateType.Tundra or ClimateType.Snow or ClimateType.Bare or ClimateType.Taiga => NoiseType.Ridged,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
    
    private static ClimateType GetClimateType(float moisture, float heat)
    {
        return heat switch
        {
            > 0.9f => ClimateType.Ocean,
            > 0.88f => ClimateType.Beach,
            > 0.8f => moisture switch
            {
                < 0.16f => ClimateType.Desert,
                < 0.50f => ClimateType.Grassland,
                < 0.83f => ClimateType.Forest,
                _ => ClimateType.Forest
            },
            > 0.6f => moisture switch
            {
                < 0.16f => ClimateType.Desert,
                < 0.33f => ClimateType.Grassland,
                _ => ClimateType.TropicalForest
            },
            > 0.3f => moisture switch
            {
                < 0.33f => ClimateType.Desert,
                < 0.66f => ClimateType.Shrubland,
                _ => ClimateType.Taiga
            },
            _ => moisture switch
            {
                < 0.1f => ClimateType.Scorched,
                < 0.2f => ClimateType.Bare,
                < 0.5f => ClimateType.Tundra,
                _ => ClimateType.Snow
            }
        };
    }

}
