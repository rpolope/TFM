
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
    public MeshParameters parameters;
    
    protected override void OnValidate()
    {
        Debug.Log("Cambio los valores del terreno");
        
        base.OnValidate();
    }
}
