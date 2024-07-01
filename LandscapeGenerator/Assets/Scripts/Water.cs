
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class Water
{
    public readonly GameObject GameObject;
    public static Material Material;
    public Water(float heightLevel, Viewer viewer, float size)
    {
        GameObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var transform = GameObject.transform;
        transform.localPosition = Vector3.zero + Vector3.up * heightLevel;
        transform.localScale = new Vector3(size, 1, size);
        transform.parent = viewer.transform;
        Material = GameObject.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Custom/Water"));
        // const string materialPath = "Assets/Materials/Water.mat";
        // Material = (Material)AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material));
    }

    public static void UpdateVisibility(Texture2D heightMap)
    {
        Material.SetTexture("_HeightMap", heightMap);
    }
}
