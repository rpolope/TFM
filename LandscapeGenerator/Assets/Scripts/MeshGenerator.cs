using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;


public struct MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    private int _trianglesIndex;

    public MeshData(int width, int height)
    {
        _trianglesIndex = 0;
        vertices = new Vector3[(width * height)];
        triangles = new int[(width - 1)* (height - 1) * 6];
        uvs = new Vector2[(width * height)];
    }
    public void AddTriangle(int a, int b, int c)
    {
        triangles[_trianglesIndex] = a;
        triangles[_trianglesIndex + 1] = b;
        triangles[_trianglesIndex + 2] = c;

        _trianglesIndex += 3;
    }
}

public static class MeshGenerator
{
    public static Mesh GenerateMesh(int size, float[] heightMap)
    {
        return GenerateMeshWithJobs(size, heightMap);
        MeshData meshData = new MeshData(size, size);
        int offsetX = (size - 1) / 2;
        int offsetY = (size - 1) / 2;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = x + y * size;
                float h = LandscapeManager.Instance.settings.NoiseSettings.Amplitude * heightMap[index];
                meshData.vertices[index] = new Vector3(x - offsetX,  heightMap[index], y - offsetY);
                meshData.uvs[index] = new Vector2(x/(float)size, y/(float)size);
                
                if ((y < size - 1) && (x < size - 1))
                {
                    meshData.AddTriangle(index,index + size, index + size + 1);
                    meshData.AddTriangle(index, index + size + 1,index + 1);
                }
            }
        }
        return CreateMesh(meshData);
    }

    public static Mesh GenerateMeshWithJobs(int size, float[] noiseMap)
    {
        NativeArray<Vector3> vertices = new NativeArray<Vector3>(size * size, Allocator.TempJob);
        NativeArray<Vector2> uvs = new NativeArray<Vector2>(size * size, Allocator.TempJob);
        NativeArray<int> trianglesIndices = new NativeArray<int>((size) * (size) * 6, Allocator.TempJob);
        NativeArray<float> heightMap = new NativeArray<float>(noiseMap, Allocator.TempJob);
    
        MeshJob meshGenerationJob = new MeshJob(size, heightMap, vertices, uvs, trianglesIndices, LandscapeManager.Instance.settings.meshSettings);
    
        meshGenerationJob.Schedule(vertices.Length, 10000).Complete();
    
        MeshData meshData = new MeshData(size, size);

        meshData.vertices = vertices.ToArray();
        meshData.triangles = trianglesIndices.ToArray();
        meshData.uvs = uvs.ToArray();
    
        vertices.Dispose();
        uvs.Dispose();
        trianglesIndices.Dispose();
        heightMap.Dispose();
    
        return CreateMesh(meshData);
    }
    

    private static Mesh CreateMesh(MeshData meshData)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = meshData.vertices;
        mesh.triangles = meshData.triangles;
        mesh.uv = meshData.uvs;
        
        mesh.RecalculateNormals();
        return mesh;
    }
}

