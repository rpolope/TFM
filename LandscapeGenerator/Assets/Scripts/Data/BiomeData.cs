using UnityEngine;

[CreateAssetMenu()]
public class BiomeData : UpdatableData
{
    public int id;
    public Layer[] layers;
    public float minTemperature, maxTemperature;
    public float minMoisture, maxMoisture;

    public void SetBiome(Material material)
    {
        for (int i = 0; i < layers.Length; i++)
        {
            material.SetTexture($"_Biome_{id}_Layer_{i}_Tex", layers[i].texture);
            material.SetColor($"_Biome_{id}_Layer_{i}_Tint", layers[i].tint);
            material.SetFloat($"_Biome_{id}_Layer_{i}_TintStrength", layers[i].tintStrength);
            material.SetFloat($"_Biome_{id}_Layer_{i}_StartHeight", layers[i].startHeight);
            material.SetFloat($"_Biome_{id}_Layer_{i}_BlendStrength", layers[i].blendStrength);
            material.SetFloat($"_Biome_{id}_Layer_{i}_TextureScale", layers[i].textureScale);
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();
    }
}

[System.Serializable]
public class Layer
{
    public Texture2D texture;
    public Color tint;
    [Range(0, 1)] public float tintStrength;
    [Range(0, 1)] public float startHeight;
    [Range(0, 1)] public float blendStrength;
    public float textureScale;
}