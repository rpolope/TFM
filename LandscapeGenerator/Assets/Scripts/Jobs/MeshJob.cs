using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;

// [BurstCompile]
public struct MeshJob : IJobParallelFor
{
    int _size;
    [ReadOnly] 
    private NativeArray<float> _heightMap;
    private NativeArray<Vector3> _vertices;
    private NativeArray<Vector2> _uvs;
    [NativeDisableParallelForRestriction] 
    private NativeArray<int> _triangles;
    private MeshSettings _meshSettings;
    private readonly int _offsetX;
    private readonly int _offsetZ;
    
    public MeshJob(int size,
                   NativeArray<float> heightMap, 
                   NativeArray<Vector3> vertices, 
                   NativeArray<Vector2> uvs, 
                   NativeArray<int> triangles,
                   MeshSettings meshSettings)
    {
        _size = size;
        _heightMap = heightMap;
        _vertices = vertices;
        _uvs = uvs;
        _triangles = triangles;
        _meshSettings = meshSettings;
        _offsetX = (_size - 1) / 2;
        _offsetZ = (_size - 1) / 2;
    }
    
    public void Execute(int index)
    {
        int x = index % _size;
        int y = index / _size;
        float h = _meshSettings.meshHeightMultiplier * _heightMap[index];
        _vertices[index] = new Vector3(x - _offsetX,  h, y - _offsetZ);
        _uvs[index] = new Vector2(x/(float)_size, y/(float)_size);
                
        if ((y < _size - 2) && (x < _size - 1))
        {
            int triangleIndex = index * 6;
            _triangles[triangleIndex] = index;
            _triangles[triangleIndex + 1] = index + _size;
            _triangles[triangleIndex + 2] = index + _size + 1;
            _triangles[triangleIndex + 3] = index;
            _triangles[triangleIndex + 4] = index + _size + 1;
            _triangles[triangleIndex + 5] = index + 1;
            
        }
    }
}