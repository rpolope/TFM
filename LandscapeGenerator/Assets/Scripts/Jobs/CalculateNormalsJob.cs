using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Jobs
{
    [BurstCompile]
    public struct CalculateNormalsJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] 
        public NativeArray<Vector3> Normals;
        [ReadOnly]
        public NativeArray<int> Triangles;
        [ReadOnly] 
        public NativeArray<Vector3> Vertices;
        public void Execute(int threadIndex)
        {
            int index = threadIndex * 3;
            int index0 = Triangles[index];
            int index1 = Triangles[index + 1];
            int index2 = Triangles[index + 2];

            Vector3 v0 = Vertices[index1] - Vertices[index0];
            Vector3 v1 = Vertices[index2] - Vertices[index0];
            Vector3 normal = Vector3.Cross(v0, v1).normalized;

            Normals[index0] += normal;
            Normals[index1] += normal;
            Normals[index2] += normal;
        }
    }
}