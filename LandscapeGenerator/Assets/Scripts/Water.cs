using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Water
{
    public readonly GameObject GameObject;
    public readonly BoxCollider BoxCollider;
    public static readonly float HeightLevel;
    public static LayerMask WaterLayerMask;

    private static Material _material;
    private static bool _fromEditor;
    
    static Water()
    {

        if (LandscapeManager.Instance != null)
        {
            HeightLevel = LandscapeManager.Instance.terrainData.parameters.waterLevel *
                          LandscapeManager.Instance.terrainData.parameters.heightScale;
            _fromEditor = false;
        }
        else
        {
            HeightLevel = MapGenerator.Instance.terrainData.parameters.waterLevel *
                          MapGenerator.Instance.terrainData.parameters.waterLevel;
            _fromEditor = true;
        }
        
        WaterLayerMask = LayerMask.NameToLayer("Water");
    }

    public Water(Transform parent, float size)
    {
        const string materialPath = "Assets/Materials/Water.mat";

        GameObject = GameObject.CreatePrimitive(PrimitiveType.Plane);


        if (_fromEditor)
        {
            Object.DestroyImmediate(GameObject.GetComponent<MeshCollider>());

        }
        else
        {
            Object.Destroy(GameObject.GetComponent<MeshCollider>());   
        }
        // BoxCollider = GameObject.AddComponent<BoxCollider>();

        _material ??= (Material)AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material));
        
        GameObject.GetComponent<MeshRenderer>().sharedMaterial = _material;
        GameObject.layer = WaterLayerMask;
        
        var transform = GameObject.transform;
        transform.localScale = new Vector3(size/10, 1, size/10);
        transform.parent = parent;
        transform.localPosition = Vector3.zero + Vector3.up * HeightLevel;
    }
}