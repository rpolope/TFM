using UnityEngine;
using System.Collections;

[CreateAssetMenu()]
public class TextureData : UpdatableData {

    public void ApplyToMaterial(Material material) {
        //
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }
}