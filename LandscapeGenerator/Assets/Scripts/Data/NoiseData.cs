using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu()]
public class NoiseData : UpdatableData
{
    public NoiseParameters parameters;

    protected override void OnValidate()
    {
        Debug.Log("Cambio los valores de generación de ruido");
        
        base.OnValidate();
    }
}
