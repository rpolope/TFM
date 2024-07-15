using UnityEditor;
using UnityEngine;

public class Water
{
    public readonly GameObject GameObject;
    public readonly MeshCollider MeshCollider;
    private static Material _material;

    public static readonly float HeightLevel;
    
    static Water()
    {
        HeightLevel = LandscapeManager.Instance.terrainData.parameters.waterLevel *
                      LandscapeManager.Instance.terrainData.parameters.heightScale;
    }
    public Water(Transform parent, float size)
    {
        const string materialPath = "Assets/Materials/Water.mat";
        const string debugMaterialPath = "Assets/Materials/DebugMaterial.mat";

        GameObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
        MeshCollider = GameObject.GetComponent<MeshCollider>();
        _material ??= (Material)AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material));
        
        GameObject.GetComponent<MeshRenderer>().sharedMaterial = _material;
        
        var transform = GameObject.transform;
        transform.localScale = new Vector3(size/10, 1, size/10);
        transform.parent = parent;
        transform.localPosition = Vector3.zero + Vector3.up * HeightLevel;
        
        Debug.Log($"Capa final del objeto: {GameObject.layer}");
    }
}
