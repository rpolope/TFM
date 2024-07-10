using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TerrainChunksManager;

public static class ObjectPlacer
{
    private static readonly Dictionary<AssetSize, List<Vector3>> PlacedPositions = new Dictionary<AssetSize, List<Vector3>>()
    {
        { AssetSize.Large, new List<Vector3>() },
        { AssetSize.Medium, new List<Vector3>() },
        { AssetSize.Small, new List<Vector3>() }
    };
    
    public static void PlaceObjects(TerrainChunk chunk, AssetType assetType)
    {
        var assets = BiomesAssetsManager.GetAssetsForType(chunk.Biome.Assets, assetType);

        if (assets == null)
        {
            SetAssetsParent(chunk);
            Debug.LogWarning("No assets found for biome: " + chunk.Biome.ClimateType);
            return;
        }
        var assetsParent = SetAssetsParent(chunk);

        foreach (var key in PlacedPositions.Keys.ToList())
        {
            PlacedPositions[key].Clear();
        }

        var orderedAssets = assets.OrderByDescending(a => a.size).ToList();

        foreach (var asset in orderedAssets)
        {
            float radius = asset.radius;
            List<Vector3> centerPoints = GetCenterPointsForSize(asset.size, chunk.WorldPos);
            foreach (var centerPoint in centerPoints)
            {
                int assetsPlacedCount = 0;

                var sampleArea = asset.size.Equals(AssetSize.Large) ? 
                    new Vector2(TerrainChunk.WorldSize,TerrainChunk.WorldSize) :
                    new Vector2(radius * 2.5f, radius * 2.5f);
                var points = PoissonDiskSampler.GeneratePoints(radius, sampleArea);

                foreach (var point in points)
                {
                    if (assetsPlacedCount > asset.density * points.Count) break;

                    var offset = new Vector3(point.x - radius, 0, point.y - radius);
                    var worldPos = centerPoint + offset;
                    if (TryGetPositionAndRotation(worldPos, out var hitPosition, out var rotation, 100f))
                    {
                        if (!IsPositionValid(hitPosition, asset)) continue;

                        PlaceAsset(asset, hitPosition, assetsParent);
                        PlacedPositions[asset.size].Add(worldPos);
                        assetsPlacedCount++;
                    }
                }
            }
        }
    }

    private static Transform SetAssetsParent(TerrainChunk chunk)
    {
        var assetsParent = chunk.Transform.Find("Assets")?.transform;

        if (assetsParent == null)
        {
            assetsParent = new GameObject("Assets")
            {
                transform = { parent = chunk.Transform }
            }.transform;
        }

        return assetsParent;
    }

    private static List<Vector3> GetCenterPointsForSize(AssetSize size, Vector3 worldPos)
    {
        var offset = TerrainChunk.WorldSize * 0.5f;
        var defaultCenterPoint = worldPos + new Vector3(-offset, 0, -offset);
        
        return size switch
        {
            AssetSize.Large => new List<Vector3> {},
            AssetSize.Medium => PlacedPositions[AssetSize.Large].Count > 0 ? PlacedPositions[AssetSize.Large] : new List<Vector3> {defaultCenterPoint},
            AssetSize.Small => PlacedPositions[AssetSize.Medium].Count > 0 ? PlacedPositions[AssetSize.Medium] : (PlacedPositions[AssetSize.Large].Count > 0 ? PlacedPositions[AssetSize.Large] : new List<Vector3> {defaultCenterPoint}),
            _ => new List<Vector3>()
        };
    }

    private static bool TryGetPositionAndRotation(Vector3 position, out Vector3 hitPosition, out Quaternion rotation, float raycastHeight)
    {
        var ray = new Ray(position + Vector3.up * raycastHeight, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            hitPosition = hit.point;
            rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            return true;
        }
        hitPosition = Vector3.zero;
        rotation = Quaternion.identity;
        return false;
    }

    private static bool IsPositionValid(Vector3 worldPos, BiomeAsset asset)
    {
        var terrainParameters = LandscapeManager.Instance.terrainData.parameters;
        float height = worldPos.y;
        return height >= Water.HeightLevel + asset.minHeight  && 
               height <= asset.maxHeight * terrainParameters.heightScale;
    }

    private static void PlaceAsset(BiomeAsset asset, Vector3 position, Transform parent)
    {
        var randIndex = asset.gameObjects.Count > 1 ? Random.Range(0, asset.gameObjects.Count) : 0;
        
        var instance = BiomesManager.Instantiate(asset.gameObjects[randIndex], position);
        instance.transform.up = GetNormalAt(position);
        instance.transform.parent = parent;
        instance.transform.rotation *= Quaternion.Euler(0f, Random.Range(0f, 359f), 0f);
        instance.transform.localScale *= Random.Range(0.8f, 1.2f);
        instance.transform.position -= instance.transform.up * 0.1f;
    }
    
    private static Vector3 GetNormalAt(Vector3 position)
    {
        return Vector3.up;
    }
}
