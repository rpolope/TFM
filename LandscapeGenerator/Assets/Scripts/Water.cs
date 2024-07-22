using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Water
{
    private readonly GameObject _gameObject;
    public readonly BoxCollider BoxCollider;
    public static readonly float HeightLevel;
    public static int WaterLayer;

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
        
        WaterLayer = LayerMask.NameToLayer("Water");
    }

    public Water(Transform parent, float size)
    {
        const string materialPath = "Assets/Materials/Water.mat";

        _gameObject = GameObject.CreatePrimitive(PrimitiveType.Plane);


        if (_fromEditor)
        {
            Object.DestroyImmediate(_gameObject.GetComponent<MeshCollider>());

        }
        else
        {
            Object.Destroy(_gameObject.GetComponent<MeshCollider>());   
        }
        // BoxCollider = GameObject.AddComponent<BoxCollider>();

        _material ??= (Material)AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material));
        
        _gameObject.GetComponent<MeshRenderer>().sharedMaterial = _material;
        _gameObject.layer = WaterLayer;
        
        var transform = _gameObject.transform;
        transform.localScale = new Vector3(size/10, 1, size/10);
        transform.parent = parent;
        transform.localPosition = Vector3.zero + Vector3.up * HeightLevel;
    }
}