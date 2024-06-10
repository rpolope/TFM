
using System;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
    public MeshParameters parameters;
    public float MinHeight => 0f; 
    public float MaxHeight => parameters.scale * parameters.heightScale; 

    protected override void OnValidate()
    {
        base.OnValidate();
    }
}
