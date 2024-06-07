using UnityEngine;
using System.Collections;

[CreateAssetMenu()]
public class TextureData : UpdatableData {
    
    private float _savedMinHeight;
    private float _savedMaxHeight;
    private static readonly int MinHeight = Shader.PropertyToID("minHeight");
    private static readonly int MaxHeight = Shader.PropertyToID("maxHeight");

    public void ApplyToMaterial(Material material)
    {
        UpdateMeshHeights(material, _savedMinHeight, _savedMaxHeight);
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        _savedMinHeight = minHeight;
        _savedMaxHeight = maxHeight;
        
        material.SetFloat(MinHeight, minHeight);
        material.SetFloat(MaxHeight, maxHeight);
    }
}