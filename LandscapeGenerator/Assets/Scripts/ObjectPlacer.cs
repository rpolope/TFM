using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;

public static class ObjectPlacer
{
    private static readonly Dictionary<AssetSize, List<Vector3>> PlacedPositions = new Dictionary<AssetSize, List<Vector3>>();

    public static void PlaceObjects(TerrainChunksManager.TerrainChunk chunk, AssetType assetType)
    {
        // List<BiomeAsset> assets = null;
        
        var assets = chunk.Biome.Assets;

        if (assets == null)
        {
            Debug.LogWarning("No assets found for biome: " + chunk.Biome.ClimateType);
            return;
        }
        
        PlacedPositions[AssetSize.Large] = new List<Vector3>();
        PlacedPositions[AssetSize.Medium] = new List<Vector3>();
        PlacedPositions[AssetSize.Small] = new List<Vector3>();

        var orderedAssets = assets.OrderByDescending(a => a.size).ToList();

        foreach (var asset in orderedAssets)
        {
            float radius = (int)asset.size;
            List<Vector3> centerPoints = GetCenterPointsForSize(asset.size);

            foreach (var centerPoint in centerPoints)
            {
                var points = PoissonDiskSampler.GeneratePoints(radius, new Vector2(radius * 2, radius * 2));

                foreach (var point in points)
                {
                    // var offset = new Vector3(point.x - radius, 0, point.y - radius);
                    // var worldPos = centerPoint + offset;
                    // worldPos.y = TerrainHeightAt(worldPos, chunk.GetMesh(), chunk.Position);
                    //
                    // if (!IsPositionValid(worldPos, asset)) continue;
                    //
                    // PlaceAsset(asset, worldPos, chunk.Transform);
                    // PlacedPositions[asset.size].Add(worldPos);
                }
            }
        }
    }

    private static List<Vector3> GetCenterPointsForSize(AssetSize size)
    {
        return size switch
        {
            AssetSize.Large => new List<Vector3> { Vector3.zero },
            AssetSize.Medium => PlacedPositions[AssetSize.Large],
            AssetSize.Small => PlacedPositions[AssetSize.Medium],
            _ => new List<Vector3>()
        };
    }

    private static bool IsPositionValid(Vector3 worldPos, BiomeAsset asset)
    {
        return worldPos.y >= asset.minHeight && worldPos.y <= asset.maxHeight;
    }

    private static void PlaceAsset(BiomeAsset asset, Vector3 position, Transform parent)
    {
        // var instance = BiomesManager.Instantiate(asset.gameObject, position);
        // instance.transform.up = GetNormalAt(position);
        // instance.transform.parent = parent;
        // instance.transform.position += instance.transform.up * (instance.GetComponent<Renderer>().bounds.size.y / 2f);
    }

    private static float TerrainHeightAt(Vector3 worldPos, Mesh terrainMesh, Vector2 position)
    {
        var localPos = worldPos - new Vector3(position.x, 0, position.y);
        
        if (terrainMesh == null) return 0f;
        
        Vector3[] vertices = terrainMesh.vertices;
        Vector2 terrainSize = new Vector2(terrainMesh.bounds.size.x, terrainMesh.bounds.size.z);

        float meshWidth = terrainSize.x;
        float meshHeight = terrainSize.y;
        float percentX = localPos.x / meshWidth;
        float percentZ = localPos.z / meshHeight;

        int numCellsX = (int)terrainMesh.bounds.size.x - 1;
        int numCellsZ = (int)terrainMesh.bounds.size.z - 1;
        int cellX = Mathf.Clamp((int)(percentX * numCellsX), 0, numCellsX - 1);
        int cellZ = Mathf.Clamp((int)(percentZ * numCellsZ), 0, numCellsZ - 1);

        int vertexIndex = (cellZ * (numCellsX + 1)) + cellX;
        Vector3 v00 = vertices[vertexIndex];
        Vector3 v10 = vertices[vertexIndex + 1];
        Vector3 v01 = vertices[vertexIndex + numCellsX + 1];
        Vector3 v11 = vertices[vertexIndex + numCellsX + 2];

        float localPercentX = (localPos.x - v00.x) / (v10.x - v00.x);
        float localPercentZ = (localPos.z - v00.z) / (v01.z - v00.z);

        float height00 = v00.y;
        float height10 = v10.y;
        float height01 = v01.y;
        float height11 = v11.y;

        var height = Mathf.Lerp(
            Mathf.Lerp(height00, height10, localPercentX),
            Mathf.Lerp(height01, height11, localPercentX),
            localPercentZ
        );

        return height;

    }

    private static Vector3 GetNormalAt(Vector3 position)
    {
        return Vector3.up;
    }
}
