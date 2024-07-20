using Unity.Mathematics;
using UnityEngine;

public enum DrawMode
{
    NoiseMap,
    Mesh
}
public static class MapDisplay
{
    private static readonly MapGenerator MapGenerator = Object.FindObjectOfType<MapGenerator>();
    public static Renderer TextureRender = MapGenerator != null ? MapGenerator.GetComponent<MeshRenderer>() : null;
    public static Renderer MeshRenderer = TextureRender;
    public static MeshFilter MeshFilter = MapGenerator != null ? MapGenerator.GetComponent<MeshFilter>() : null;

    private static void DrawTexture(Texture2D texture) {
        TextureRender.sharedMaterial.mainTexture = texture;
        TextureRender.transform.localScale = new Vector3 (texture.width, 1, texture.height);
    }

    private static void DrawMesh(MeshData meshData) {
        MeshFilter.mesh = meshData.CreateMesh ();
    }
    
    public static void DrawMapInEditor(DrawMode drawMode, MapData mapData, TerrainParameters terrainParameters)
    {
        var resolution = terrainParameters.meshParameters.resolution;

        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.HeightMap.ToArray()));
                break;
            case DrawMode.Mesh:
            {
                var meshData = new MeshData(resolution, 0);
                MeshGenerator.ScheduleMeshGenerationJob(terrainParameters, resolution, new int2(), mapData,ref meshData, false).Complete();
                DrawMesh (meshData);
                break;
            }
        }
    }
}