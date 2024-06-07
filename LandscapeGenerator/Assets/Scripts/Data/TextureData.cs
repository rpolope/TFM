using UnityEngine;
using System.Collections;

[CreateAssetMenu()]
public class TextureData : UpdatableData {
    
    private const int TextureSize = 512;
    private const TextureFormat TextureFormat = UnityEngine.TextureFormat.RGB565;
    
    public Color[] baseColours;
    [Range(0,1)]
    public float[] baseStartHeights;
    [Range(0,1)]
    public float[] baseBlends;
    
    private float _savedMinHeight;
    private float _savedMaxHeight;
    
    private static readonly int MinHeight = Shader.PropertyToID("minHeight");
    private static readonly int MaxHeight = Shader.PropertyToID("maxHeight");
    private static readonly int BaseColourCount = Shader.PropertyToID("baseColourCount");
    private static readonly int BaseColours = Shader.PropertyToID("baseColours");
    private static readonly int BaseStartHeights = Shader.PropertyToID("baseStartHeights");
    private static readonly int BaseBlends = Shader.PropertyToID("baseBlends");

    public void ApplyToMaterial(Material material)
    {
        material.SetInt (BaseColourCount, baseColours.Length);
        material.SetColorArray (BaseColours, baseColours);
        material.SetFloatArray (BaseStartHeights, baseStartHeights);
        material.SetFloatArray (BaseBlends, baseBlends);
        
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