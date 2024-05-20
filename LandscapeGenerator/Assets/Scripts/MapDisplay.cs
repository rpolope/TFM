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
    
    public static void DrawMapInEditor(DrawMode drawMode, MapData mapData, TerrainParameters terrainParameters, GameObject canvas)
    {
        // var mapSize = terrainParameters.meshParameters.resolution;
        TextureRender = MeshRenderer = canvas.GetComponent<MeshRenderer>();
        MeshFilter = canvas.GetComponent<MeshFilter>();
        var mapSize = MapGenerator.MapSize;
        
        if (drawMode == DrawMode.NoiseMap) {
            DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.HeightMap.ToArray(), mapSize));
        } else if (drawMode == DrawMode.ColorMap) {
            DrawTexture (TextureGenerator.TextureFromColorMap (mapData.ColorMap.ToArray(), mapSize));
        } else if (drawMode == DrawMode.Mesh) {
            var meshData = new MeshData(mapSize, 0);
            var center = canvas.transform.position;
            MeshGenerator.ScheduleMeshGenerationJob(terrainParameters, mapSize, 1, new float2(center.x, center.z), ref meshData).Complete();
            DrawMesh (meshData, TextureGenerator.TextureFromColorMap (mapData.ColorMap.ToArray(), mapSize));
        }
    }
}