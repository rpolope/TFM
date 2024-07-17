using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class BiomeAsset: ScriptableObject
{
    public AssetType type;
    public AssetSize size;
    public ClimateType[] biomes;
    public List<GameObject> gameObjects;
    public float minHeight;
    public float maxHeight;
    [Min(0.1f)]
    public float radius;
    [Range(0, 1)]
    public float density;
    [Range(0, 1)]
    public float normalOrientation;
}