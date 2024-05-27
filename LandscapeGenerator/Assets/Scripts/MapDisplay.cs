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
    

    public static void DrawTexture(Texture2D texture, Renderer textureRenderer)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
    }

    public static void DrawMesh(MeshData meshData, MeshFilter meshFilter)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
    }
}
