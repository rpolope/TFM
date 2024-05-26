using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public enum DisplayMode
{
    NoiseMap,
    ColorMap,
    Mesh
}

public static class MapDisplay
{
    public static DisplayMode DisplayMode = DisplayMode.NoiseMap;

    public static void DisplayChunk(DisplayMode displayMode, TerrainChunk chunk)
    {
        Renderer renderer = chunk.Renderer;
        MeshFilter meshFilter = chunk.MeshFilter;

        switch (displayMode)
        {
            case DisplayMode.NoiseMap:
                DrawTexture(TextureGenerator.TextureFromHeightMap(chunk.HeightMap, TerrainChunksManager.TerrainChunkResolution), renderer);
                break;
            case DisplayMode.ColorMap:
                // Implement logic for ColorMap display mode
                break;
            case DisplayMode.Mesh:
                // Implement logic for Mesh display mode
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(displayMode), displayMode, null);
        }
    }

    public static void DrawMapInEditor(DisplayMode drawMode, MapData maps, TerrainParameters terrainParameters, MapVisualizer visualizer)
    {
        GameObject canvas = visualizer.gameObject;
        MeshRenderer renderer = canvas.GetComponent<MeshRenderer>();
        MeshFilter meshFilter = canvas.GetComponent<MeshFilter>();
        
        int resolution = visualizer.meshParameters.resolution;
        var mockedMoistureValue = 0.4f;
        var mockedHeatValue = 0.66f;
        var colorGradient = BiomesManager.ColorMap[(int)(mockedMoistureValue*127)];
        
        switch (drawMode)
        {
            case DisplayMode.NoiseMap:
                DrawTexture(TextureGenerator.TextureFromHeightMap(maps.HeightMap.ToArray(), resolution), renderer);
                break;
            
            case DisplayMode.ColorMap:
                /* Para pintar el mapa */
                Color[] colorMap = GetColorRangeFromMockedMoistureAndMockedHeat(resolution, mockedHeatValue, colorGradient, maps.HeightMap.ToArray());
                // Color[] colorMap = GetColorMapFromGradient(visualizer.colorGradient, maps.HeightMap.ToArray());
                DrawTexture (TextureGenerator.TextureFromColorMap (colorMap, resolution), renderer);
                
                /* Para mostrar la textura de test del BiomesManager */
                // DrawTexture (BiomesManager.TextureTest, renderer);
                break;
            
            case DisplayMode.Mesh:
                MeshData meshData = new MeshData(resolution, 0);
                Color[] colors = GetColorRangeFromMockedMoistureAndMockedHeat(resolution, mockedHeatValue, colorGradient, maps.HeightMap.ToArray());
                meshData.Colors = new NativeArray<Color>(colors, Allocator.Persistent);
                
                float2 center = new float2(canvas.transform.position.x, canvas.transform.position.z);
                MeshGenerator.ScheduleMeshGenerationJob(terrainParameters, resolution, 1, center, maps, ref meshData).Complete();
                DrawMesh(meshData, meshFilter);
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(drawMode), drawMode, null);
        }
    }

    private static void DrawTexture(Texture2D texture, Renderer textureRenderer)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
    }

    private static void DrawMesh(MeshData meshData, MeshFilter meshFilter)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
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
    
    private static Color[] GetColorRangeFromMockedMoistureAndMockedHeat(int mapSize, float heat, Color[] colorGradient, float[] heightMap)
    {
        Color[] colorMap = new Color[mapSize * mapSize];

        for (int i = 0; i < colorMap.Length; i++)
        {
            float h = heat - heightMap[i] * 0.6f;
            Color color = colorGradient[(int)(h * 127 + 1 / 128f)];
            colorMap[i] = color;
        }

        return colorMap;
    }
}
