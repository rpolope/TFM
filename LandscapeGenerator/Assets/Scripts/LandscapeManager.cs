using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LandscapeManager : MonoBehaviour
{
    private Material _terrainMaterial;
    public Material TerrainMaterial => _terrainMaterial;
    public SettingsConfiguration settings;
    public static LandscapeManager Instance;
    
    void Start()
    {
        _terrainMaterial = new Material(Shader.Find("Standard"));
        MapVisualizer.Instance.VisualizeMap();
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(Instance);
        }
    }
}
