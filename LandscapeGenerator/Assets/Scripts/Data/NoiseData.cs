using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu()]
public class NoiseData : ScriptableObject
{
    public NoiseParameters parameters;

    private void OnValidate()
    {
        Debug.Log("Cambio los valores");
    }
}
