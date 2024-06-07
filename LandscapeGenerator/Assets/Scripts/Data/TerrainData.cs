
using System;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
    public MeshParameters parameters;
    public float MinHeight => parameters.scale * parameters.heightScale * 0f; // 1 es el valor base de la amplitud

    public float MaxHeight => parameters.scale * parameters.heightScale * 8; // 8 son las octavas máximas y la persistencia máxima es 1,
                                                                             // por tanto si en cada octava se suma la amplitud por la persistencia máxima,
                                                                             // en el max num de octavas y max persistencia la altura final será 8 SUM[1,8](1*1); 

    protected override void OnValidate()
    {
        Debug.Log("Cambio los valores del terreno");
        
        base.OnValidate();
    }
}
