
using System;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
    public MeshParameters parameters;

    public float MinHeight => parameters.heightScale * 0; // 0 es el valor base de la amplitud

    public float MaxHeight => parameters.heightScale * 8; // 8 son las octavas máximas y la persistencia máxima es 1 por tanto si en cada octava se suma la amplitud por la persistencia máxima, en el max num de octavas y max persistencia dará 8; 

    protected override void OnValidate()
    {
        Debug.Log("Cambio los valores del terreno");
        
        base.OnValidate();
    }
}
