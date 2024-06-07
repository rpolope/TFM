using UnityEngine;
using System.Collections;

[CreateAssetMenu()]
public class TextureData : UpdatableData {
    
    public Color[] baseColours;
    [Range(0,1)]
    public float[] baseStartHeights;
    
    private float _savedMinHeight;
    private float _savedMaxHeight;
    
    private static readonly int MinHeight = Shader.PropertyToID("minHeight");
    private static readonly int MaxHeight = Shader.PropertyToID("maxHeight");
    private static readonly int BaseColourCount = Shader.PropertyToID("baseColourCount");
    private static readonly int BaseColours = Shader.PropertyToID("baseColours");
    private static readonly int BaseStartHeights = Shader.PropertyToID("baseStartHeights");

    public void ApplyToMaterial(Material material)
    {
        UpdateMeshHeights(material, _savedMinHeight, _savedMaxHeight);
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        _savedMinHeight = minHeight;
        _savedMaxHeight = maxHeight;
        
        material.SetInt (BaseColourCount, baseColours.Length);
        material.SetColorArray (BaseColours, baseColours);
        material.SetFloatArray (BaseStartHeights, baseStartHeights);
        
        material.SetFloat(MinHeight, minHeight);
        material.SetFloat(MaxHeight, maxHeight);
    }
}