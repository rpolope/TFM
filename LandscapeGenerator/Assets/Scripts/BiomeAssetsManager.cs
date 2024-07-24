using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;

public class BiomesAssetsManager
{
    private static Dictionary<BiomeType, List<BiomeAsset>> _biomesAssetsDict;
    private static Dictionary<GameObject, int> _biomesAssetsCount;
    private static PoolManager _biomeAssetsPool = Object.FindObjectOfType<PoolManager>();

    public BiomesAssetsManager(List<BiomeAsset> biomesAssets)
    {
        _biomesAssetsDict = new Dictionary<BiomeType, List<BiomeAsset>>();
        _biomesAssetsCount = new Dictionary<GameObject, int>();

        foreach (var asset in biomesAssets)
        {
            foreach (var climate in asset.biomes)
            {
                if (!_biomesAssetsDict.ContainsKey(climate))
                {
                    _biomesAssetsDict[climate] = new List<BiomeAsset>();
                }
                _biomesAssetsDict[climate].Add(asset);

                foreach (var gameObject in asset.gameObjects)
                {
                    if (!_biomesAssetsCount.ContainsKey(gameObject))
                    {
                        _biomesAssetsCount[gameObject] = 1;
                    }
                    else
                    {
                        _biomesAssetsCount[gameObject]++;
                    }
                    
                    _biomeAssetsPool.Load(gameObject, 50);
                }
            }
        }
    }

    public static List<BiomeAsset> GetAssetsForBiome(BiomeType biome)
    {
        return _biomesAssetsDict.TryGetValue(biome, out var assets) ? assets : null;
    }

    public static List<BiomeAsset> GetAssetsForType(List<BiomeAsset> biomeAssets, AssetType type)
    {
        return biomeAssets?.Where(b => b.type == type).ToList();
    }

    public GameObject GetAsset(GameObject biomeAsset, Vector3 position, Quaternion rotation)
    {
        return _biomeAssetsPool.Spawn(biomeAsset, position, rotation);
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
