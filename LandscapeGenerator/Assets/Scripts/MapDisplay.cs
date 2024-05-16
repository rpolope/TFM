using UnityEngine;
using Unity.Mathematics;

public enum DrawMode
{
    NoiseMap,
    ColorMap,
    Mesh
}
public static class MapDisplay{
    
    public static Renderer TextureRender;
    public static MeshFilter MeshFilter;
    public static MeshRenderer MeshRenderer;

    private static void DrawTexture(Texture2D texture) {
        TextureRender.sharedMaterial.mainTexture = texture;
        TextureRender.transform.localScale = new Vector3 (texture.width, 1, texture.height);
    }

    private static void DrawMesh(MeshData meshData, Texture2D texture) {
        MeshFilter.sharedMesh = meshData.CreateMesh ();
        MeshRenderer.sharedMaterial.mainTexture = texture;
    }
    
    public static void DrawMapInEditor(DrawMode drawMode, MapData mapData, TerrainParameters terrainParameters)
    {
        var mapSize = terrainParameters.meshParameters.resolution;
        
        if (drawMode == DrawMode.NoiseMap) {
            DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.HeightMap));
        } else if (drawMode == DrawMode.ColorMap) {
            DrawTexture (TextureGenerator.TextureFromColorMap (mapData.ColorMap, mapSize));
        } else if (drawMode == DrawMode.Mesh) {
            var meshData = new MeshData(mapSize, 0);
            MeshGenerator.ScheduleMeshGenerationJob(terrainParameters, mapSize, 1, new float2(), 0,ref meshData).Complete();
            DrawMesh (meshData, TextureGenerator.TextureFromColorMap (mapData.ColorMap, mapSize));
        }
    }
}