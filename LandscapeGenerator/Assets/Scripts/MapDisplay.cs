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
        const int resolution = TerrainChunksManager.TerrainChunkResolution;
        
        switch (displayMode)
        {
            case DisplayMode.NoiseMap:
                renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                DrawTexture(TextureGenerator.TextureFromHeightMap(chunk.HeightMap, resolution), renderer);
                break;
            case DisplayMode.ColorMap:
                renderer.sharedMaterial = new Material(TerrainChunksManager.ChunksMaterial);
                if (meshFilter != null)
                    meshFilter.mesh.colors = MapGenerator.GenerateColorMap(resolution * resolution, chunk.Biome.Heat,
                        chunk.Biome.Moisture, chunk.HeightMap, chunk.Biome.ColorGradient);
                // Implement logic for ColorMap display mode
                break;
            case DisplayMode.Mesh:
                // Implement logic for Mesh display mode
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(displayMode), displayMode, null);
        }
    }
    

    public static void DrawTexture(Texture2D texture, Renderer textureRenderer)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
    }

    public static void DrawMesh(MeshData meshData, MeshFilter meshFilter)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
    }
}
