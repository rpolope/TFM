using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BiomesAssetsManager
{

    private static Dictionary<ClimateType, List<BiomeAsset>> _biomesAssetsDict;
    
    public BiomesAssetsManager(List<BiomeAsset> biomesAssets)
    {
        _biomesAssetsDict = new Dictionary<ClimateType, List<BiomeAsset>>();

        foreach (var asset in biomesAssets)
        {
            foreach (var climate in asset.biomes)
            {
                if (!_biomesAssetsDict.ContainsKey(climate))
                {
                    _biomesAssetsDict[climate] = new List<BiomeAsset>();
                }
                _biomesAssetsDict[climate].Add(asset);
            }
        }
    }

    public static List<BiomeAsset> GetAssetsForBiome(ClimateType biome)
    {
        if (_biomesAssetsDict.TryGetValue(biome, out var assets))
        {
            return assets;
        }
        return null;
    }
    
    public static List<BiomeAsset> GetAssetsForType(List<BiomeAsset> biomeAssets, AssetType type)
    {
        return biomeAssets?.Where(b => b.type == type).ToList();
    }
}

public enum AssetType
{
    Organic,
    Inorganic
}

public enum AssetSize
{
    Large = 3,
    Medium = 2,
    Small = 1
}

[CreateAssetMenu()]
public class BiomeAsset: ScriptableObject
{
    public AssetType type;
    public AssetSize size;
    public ClimateType[] biomes;
    public List<GameObject> gameObjects;
    public float minHeight;
    public float maxHeight;
    [Min(0.1f)]
    public float radius;
    [Range(0, 1)]
    public float density;
}