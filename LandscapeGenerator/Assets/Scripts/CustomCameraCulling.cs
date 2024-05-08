using System.Collections.Generic;
using UnityEngine;

public static class CustomCameraCulling
{
    private static readonly Camera TargetCamera = Camera.main;

    public static void UpdateVisibleChunks(List<LandscapeManager.TerrainChunk> chunksToUpdate, 
        HashSet<LandscapeManager.TerrainChunk> lastVisibleChunks)
    {
        foreach (var chunk in chunksToUpdate)
        {
            if (IsObjectVisible(chunk.Position))
            {
                chunk.SetVisible(true);
                lastVisibleChunks.Add(chunk);
            }
            else
            {
                chunk.SetVisible(false);
                lastVisibleChunks.Remove(chunk);
            }
        }
    }
    
    public static bool IsObjectVisible(Vector3 center)
    {
        // Plane[] cameraPlanes = GeometryUtility.CalculateFrustumPlanes(TargetCamera);
        //
        // for (int i = 0; i < cameraPlanes.Length; i++)
        // {
        //     if (cameraPlanes[i].GetDistanceToPoint(center) < 0)
        //     {
        //         return false;
        //     }
        // }
        //
        return true;
    }
}