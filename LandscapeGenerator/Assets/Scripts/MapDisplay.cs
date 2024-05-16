using System;
using UnityEngine;
using System.Collections;
using Unity.Mathematics;
using UnityEditor;

public enum DrawMode
{
    NoiseMap,
    ColorMap,
    Mesh
}
public static class MapDisplay{
    
    public static Renderer textureRender;
    public static MeshFilter meshFilter;
    public static MeshRenderer meshRenderer;
    
    public static void DrawTexture(Texture2D texture) {
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3 (texture.width, 1, texture.height);
    }

    public static void DrawMesh(MeshData meshData, Texture2D texture) {
        meshFilter.sharedMesh = meshData.CreateMesh ();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
    
    public static void DrawMapInEditor(DrawMode drawMode, MapData mapData, TerrainParameters terrainParameters)
    {
        var mapSize = terrainParameters.meshParameters.resolution;
        
        if (drawMode == DrawMode.NoiseMap) {
            DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.HeightMap));
        } else if (drawMode == DrawMode.ColorMap) {
            DrawTexture (TextureGenerator.TextureFromColorMap (mapData.ColourMap, mapSize));
        } else if (drawMode == DrawMode.Mesh)
        {
            var meshData = new MeshData(mapSize, 0);
            MeshGenerator.ScheduleMeshGenerationJob(terrainParameters, mapSize, 1, new float2(), 0,ref meshData).Complete();
            
            DrawMesh (meshData, TextureGenerator.TextureFromColorMap (mapData.ColourMap, mapSize));
        }
    }
}