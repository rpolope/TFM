using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
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

    private const int YieldFrequency = 5;

    public static IEnumerator PlaceObjectsCoroutine(TerrainChunk chunk)
    {

        var assets = BiomesAssetsManager.GetAssetsForBiome(chunk.Biome.BiomeType);
        if (assets == null)
        {
            Debug.LogWarning("No assets found for biome: " + chunk.Biome.BiomeType);
            yield break;
        }

        foreach (var key in PlacedPositions.Keys.ToList())
        {
            PlacedPositions[key].Clear();
        }

        var orderedAssets = assets.OrderByDescending(a => a.size).ToList();

        foreach (var asset in orderedAssets)
        {
            float radius = asset.radius;
            var considerLargerAssets = asset.type.Equals(AssetType.Organic);
            List<Vector3> centerPoints = GetCenterPointsForSize(asset.size, chunk.WorldPos, considerLargerAssets);

            foreach (var centerPoint in centerPoints)
            {
                int assetsPlacedCount = 0;
                var sampleArea = asset.size.Equals(AssetSize.Large) ? 
                    new Vector2(TerrainChunk.WorldSize, TerrainChunk.WorldSize) :
                    new Vector2(radius * 2.5f, radius * 2.5f);

                var points = PoissonDiskSampler.GeneratePoints(radius, sampleArea);

                foreach (var point in points)
                {
                    if (assetsPlacedCount > asset.density * points.Count) break;

                    var offset = new Vector3(point.x - radius, 0, point.y - radius);
                    var worldPos = centerPoint + offset;
                    
                    if (!chunk.Bounds.Contains(worldPos)) continue;

                    if (TryGetPositionAndRotation(worldPos, out var hitPosition, out var rotation, out var layer, 500f))
                    {
                        if (!IsPositionValid(hitPosition, layer, asset)) continue;

                        var instance = PlaceAsset(asset, hitPosition, rotation);
                        chunk.InstantiatedGameObjects.Add(instance);

                        if (asset.type is AssetType.Organic)
                        {
                            PlacedPositions[asset.size].Add(worldPos);
                        }
                        
                        assetsPlacedCount++;

                        if (assetsPlacedCount % YieldFrequency == 0)
                        {
                            yield return null;
                        }
                    }
                }
            }

            chunk.SetObjectPlaced();
        }
    }

    private static List<Vector3> GetCenterPointsForSize(AssetSize size, Vector3 worldPos, bool considerLargerAssets)
    {
        var offset = TerrainChunk.WorldSize * 0.5f;
        var defaultCenterPoint = worldPos + new Vector3(-offset, 0, -offset);

        if (!considerLargerAssets) return new List<Vector3> { defaultCenterPoint };
        
        return size switch
        {
            AssetSize.Large => new List<Vector3> { defaultCenterPoint },
            AssetSize.Medium => PlacedPositions[AssetSize.Large].Count > 0 ? PlacedPositions[AssetSize.Large].ToList() : GetCenterPointsForSize(AssetSize.Large, worldPos, true),
            AssetSize.Small => PlacedPositions[AssetSize.Medium].Count > 0 ? PlacedPositions[AssetSize.Medium].ToList() : GetCenterPointsForSize(AssetSize.Medium, worldPos, true),
            _ => new List<Vector3>()
        };
    }

    private static bool TryGetPositionAndRotation(Vector3 position, out Vector3 hitPosition, out Quaternion rotation, out int layer, float raycastHeight)
    {
        var ray = new Ray(position + Vector3.up * raycastHeight, Vector3.down);
        if (Physics.Raycast(ray, out var hit))
        {
            hitPosition = hit.point;
            rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            layer = hit.transform.gameObject.layer;

            return true;
        }
        hitPosition = Vector3.zero;
        rotation = Quaternion.identity;
        layer = default;

        return false;
    }

    private static bool IsPositionValid(Vector3 worldPos, int layer, BiomeAsset asset)
    {
        var heightScale = LandscapeManager.Instance.terrainData.parameters.heightScale;
        var height = worldPos.y;

        return layer != Water.WaterLayer &&
               height >= asset.minHeight * heightScale &&
               height <= asset.maxHeight * heightScale;
    }

    private static GameObject PlaceAsset(BiomeAsset asset, Vector3 position, Quaternion rotation)
    {
        var randIndex = asset.gameObjects.Count > 1 ? Random.Range(0, asset.gameObjects.Count) : 0;
        var pivotRotation = Quaternion.Euler(0f, Random.Range(0f, 359f), 0f);
        var upVector = GeUpVector(rotation, asset.normalOrientation);
        var instance = BiomesAssetsManager.SpawnAsset(asset.gameObjects[randIndex], position - upVector * 0.1f, pivotRotation);
        instance.transform.up = upVector;
        instance.transform.localScale *= Random.Range(0.8f, 1.2f);
        return instance;
    }

    private static Vector3 GeUpVector(Quaternion rotation, float normalOrientation)
    {
        return Vector3.Slerp(Vector3.up, rotation * Vector3.up, normalOrientation).normalized;
    }
}
