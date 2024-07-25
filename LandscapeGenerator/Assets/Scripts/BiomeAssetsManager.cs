using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    public static GameObject SpawnAsset(GameObject biomeAsset, Vector3 position, Quaternion rotation)
    {
        return _biomeAssetsPool.Spawn(biomeAsset, position, rotation);
    }
    
    public static void DespawnAsset(GameObject biomeAsset)
    {
        _biomeAssetsPool.Despawn(biomeAsset);
    }

    public static void SpawnAssets(List<BiomeAsset> biomeAssets)
    {
        foreach (var biomeAsset in biomeAssets)
        {
            _biomeAssetsPool.Spawn(biomeAsset.instantiatedGameObjects);
        }
    }

    public static void DespawnAssets(List<BiomeAsset> biomeAssets)
    {
        foreach (var biomeAsset in biomeAssets)
        {
            _biomeAssetsPool.Despawn(biomeAsset.instantiatedGameObjects);
        }
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
