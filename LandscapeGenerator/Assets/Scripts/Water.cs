
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class Water
{
    public static GameObject GameObject;
    public static Material Material;

    public static float HeightLevel;
    static Water()
    {
        HeightLevel = LandscapeManager.Instance.terrainData.parameters.waterLevel *
                      LandscapeManager.Instance.terrainData.parameters.heightScale;
    }
    public static void Instantiate(float waterLevel, Transform parent, float size)
    {
        GameObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var transform = GameObject.transform;
        transform.localScale = new Vector3(size/10, 1, size/10);
        transform.parent = parent;
        transform.localPosition = Vector3.zero + Vector3.up * HeightLevel;
        const string materialPath = "Assets/Materials/Water.mat";
        GameObject.GetComponent<MeshRenderer>().sharedMaterial = Material = (Material)AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material));
        GameObject.isStatic = true;
    }

    public static void UpdateVisibility(Texture2D heightMap)
    {
        Material.SetTexture("_HeightMap", heightMap);
    }
}
