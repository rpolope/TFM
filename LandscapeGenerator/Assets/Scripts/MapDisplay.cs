using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public enum DrawMode
{
    NoiseMap,
    Mesh
}
public static class MapDisplay
{
    private static readonly MapGenerator MapGenerator = Object.FindObjectOfType<MapGenerator>();
    public static Renderer TextureRender = MapGenerator?.GetComponent<MeshRenderer>();
    public static Renderer MeshRenderer = TextureRender;
    public static MeshFilter MeshFilter = MapGenerator?.GetComponent<MeshFilter>();

    private static void DrawTexture(Texture2D texture) {
        TextureRender.sharedMaterial.mainTexture = texture;
        TextureRender.transform.localScale = new Vector3 (texture.width, 1, texture.height);
    }

    private static void DrawMesh(MeshData meshData) {
        MeshFilter.mesh = meshData.CreateMesh ();
        MeshFilter.transform.localScale = Vector3.one * TerrainChunksManager.TerrainChunk.WorldSize;
    }
    
    public static void DrawMapInEditor(DrawMode drawMode, MapData mapData, TerrainParameters terrainParameters)
    {
        var mapSize = terrainParameters.meshParameters.resolution;

        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.HeightMap.ToArray()));
                break;
            case DrawMode.Mesh:
            {
                var meshData = new MeshData(mapSize, 0);
                MeshGenerator.ScheduleMeshGenerationJob(terrainParameters, mapSize, mapData,ref meshData).Complete();
                DrawMesh (meshData);
                break;
            }
        }
    }
}