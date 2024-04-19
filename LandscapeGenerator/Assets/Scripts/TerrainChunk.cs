using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;

public class TerrainChunk
{
    private int _width;
    private int _height;
    private Mesh _mesh;
    private Vector2 _position;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    private Material _material;

    public TerrainChunk(Vector2 position, int width, int height, float[] heightMap, Material material)
    {
        _width = width;
        _height = height;
        _position = position;
        _material = material;
        _mesh = MeshGenerator.GenerateMesh(_width, _height, heightMap);
        _material.mainTexture = MapVisualizer.Instance.GetTextureFromMap(heightMap);
        
        Instantiate();
    }

    public void Instantiate()
    {
        GameObject terrainChunk = new GameObject("TerrainChunk");
        
        _meshFilter = terrainChunk.AddComponent<MeshFilter>();
        _meshRenderer = terrainChunk.AddComponent<MeshRenderer>();
        _meshCollider = terrainChunk.AddComponent<MeshCollider>();
        terrainChunk.transform.localScale = Vector3.one;
        terrainChunk.transform.position = new Vector3(_position.x, 0, _position.y);
        _meshFilter.sharedMesh= _mesh;
        _meshCollider.sharedMesh = _mesh;
        _meshRenderer.sharedMaterial = _material;
    }

}
