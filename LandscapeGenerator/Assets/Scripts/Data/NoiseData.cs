using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu()]
public class NoiseData : UpdatableData
{
    public NoiseParameters parameters;

    protected override void OnValidate()
    {
        base.OnValidate();
    }
}
