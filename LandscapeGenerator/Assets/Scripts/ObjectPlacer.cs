using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using static TerrainChunksManager;

public static class ObjectPlacer
{
    private static readonly Dictionary<AssetSize, List<Vector3>> PlacedPositions = new Dictionary<AssetSize, List<Vector3>>();

    public static void PlaceObjects(TerrainChunk chunk, AssetType assetType)
    {
        var assets = BiomesAssetsManager.GetAssetsForType( chunk.Biome.Assets, assetType);

        if (assets == null)
        {
            Debug.LogWarning("No assets found for biome: " + chunk.Biome.ClimateType);
            return;
        }
        var assetsParent = SetAssetsParent(chunk);

        
        PlacedPositions[AssetSize.Large] = new List<Vector3>();
        PlacedPositions[AssetSize.Medium] = new List<Vector3>();
        PlacedPositions[AssetSize.Small] = new List<Vector3>();

        var orderedAssets = assets.OrderByDescending(a => a.size).ToList();

        foreach (var asset in orderedAssets)
        {
            float radius = asset.radius;
            List<Vector3> centerPoints = GetCenterPointsForSize(asset.size, chunk.WorldPos);
            foreach (var centerPoint in centerPoints)
            {
                int assetsPlacedCount = 0;

                var sampleArea = asset.size.Equals(AssetSize.Large)? 
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
                    
                        PlaceAsset(asset, hitPosition, chunk.Transform);
                        PlacedPositions[asset.size].Add(worldPos);
                        assetsPlacedCount++;    
                    }
                }
            }
        }
    }

    private static Transform SetAssetsParent(TerrainChunk chunk)
    {
        Transform assetsParent;

        if (!chunk.Transform.Find("Assets"))
        {
            assetsParent = new GameObject("Assets")
            {
                transform =
                {
                    parent = chunk.Transform
                },
            }.transform;
        }
        else
        {
            assetsParent = chunk.Transform.Find("Assets").transform;
        }

        return assetsParent;
    }

    private static List<Vector3> GetCenterPointsForSize(AssetSize size, Vector3 worldPos)
    {
        var offset = TerrainChunk.WorldSize * 0.5f;
        switch (size)
        {
            case AssetSize.Large:
                return new List<Vector3> { worldPos + new Vector3(-offset, 0, -offset) };

            case AssetSize.Medium:
                return PlacedPositions[AssetSize.Large].Count > 0 
                    ? PlacedPositions[AssetSize.Large] 
                    : new List<Vector3> { worldPos + new Vector3(-offset, 0, -offset) };

            case AssetSize.Small:
                if (PlacedPositions[AssetSize.Medium].Count > 0)
                    return PlacedPositions[AssetSize.Medium];
                if (PlacedPositions[AssetSize.Large].Count > 0)
                    return PlacedPositions[AssetSize.Large];
                return new List<Vector3> { worldPos + new Vector3(-offset, 0, -offset) };

            default:
                return new List<Vector3>();
        }
    }
    
    private static bool TryGetPositionAndRotation(Vector3 position, out Vector3 hitPosition, out Quaternion rotation, float raycastHeight)
    {
        Ray ray = new Ray(position + Vector3.up * raycastHeight, Vector3.down);
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
        return worldPos.y >= asset.minHeight * terrainParameters.heightScale * terrainParameters.scale && 
               worldPos.y <= asset.maxHeight * terrainParameters.heightScale * terrainParameters.scale;
    }

    private static void PlaceAsset(BiomeAsset asset, Vector3 position, Transform parent)
    {
        var rnd = new System.Random();
        var randIndex = asset.gameObjects.Count > 1 ? rnd.Next(asset.gameObjects.Count) : 0;
        
        var instance = BiomesManager.Instantiate(asset.gameObjects[randIndex], position);
        instance.transform.up = GetNormalAt(position);
        instance.transform.parent = parent;
        instance.transform.up = Quaternion.Euler(0f, Random.Range(0f, 359f), 0f) * Vector3.up;
        instance.transform.localScale *= Random.Range(0.8f, 1.2f);
        instance.transform.position -= instance.transform.up * 0.1f;
    }
    

    private static Vector3 GetNormalAt(Vector3 position)
    {
        return Vector3.up;
    }
}
