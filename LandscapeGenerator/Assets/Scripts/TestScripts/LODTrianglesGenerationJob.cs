using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct LODTrianglesGenerationJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction]
    [ReadOnly] public NativeArray<Vector3> Vertices;
    [NativeDisableParallelForRestriction]
    [WriteOnly] public NativeArray<int> Triangles;
    public int Resolution;
    public int LOD;

    public void Execute(int index)
    {
        int lodStep = 1 << LOD; // equivalente a 2^lod
        int lodResolution = Resolution / lodStep;

        int x = index % (lodResolution - 1);
        int z = index / (lodResolution - 1);
        
        int vertIndex = (z * lodStep * Resolution) + (x * lodStep);

        if (x < lodResolution - 1 && z < lodResolution - 1)
        {
            int triIndex = index * 6;

            // Primer triángulo
            Triangles[triIndex + 0] = vertIndex;
            Triangles[triIndex + 1] = vertIndex + lodStep * Resolution;
            Triangles[triIndex + 2] = vertIndex + lodStep * Resolution + lodStep;

            // Segundo triángulo
            Triangles[triIndex + 3] = vertIndex;
            Triangles[triIndex + 4] = vertIndex + lodStep * Resolution + lodStep;
            Triangles[triIndex + 5] = vertIndex + lodStep;
        }
    }
}