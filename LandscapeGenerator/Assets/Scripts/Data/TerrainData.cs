
using System;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
    public MeshParameters parameters;

    public float MinHeight => parameters.scale * parameters.heightScale * 1; // 1 es el valor base de la amplitud

    public float MaxHeight => parameters.scale * parameters.heightScale * 1 * Mathf.Pow(1.5f, 8); // 1 * Mathf.Pow(1.5f, 8) es el valor final de la amplitud si se tienen 8 octavas y la persistencia es de 1.5; 

    protected override void OnValidate()
    {
        Debug.Log("Cambio los valores del terreno");
        
        base.OnValidate();
    }
}
