using UnityEngine;

[CreateAssetMenu()]
public class BiomeData : UpdatableData
{
    public int id;
    public float minTemperature, maxTemperature;
    public float minMoisture, maxMoisture;

    public void SetBiome(Material material)
    {
        material.SetInt($"_Biome_{id}_id", id);
        material.SetFloat($"_Biome_{id}_MaxTemp", maxTemperature);
        material.SetFloat($"_Biome_{id}_MaxMoist", maxMoisture);
        material.SetFloat($"_Biome_{id}_MinTemp", minTemperature);
        material.SetFloat($"_Biome_{id}_MinMoist", minMoisture);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
    }
}