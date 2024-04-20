using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LandscapeManager : MonoBehaviour
{
    private Material _terrainMaterial;
    
    public Material TerrainMaterial => _terrainMaterial;
    public TerrainChunksGenerator terrainChunksGenerator;
    public SettingsConfiguration settings;
    public int viewThreshold;
    public static LandscapeManager Instance;
     
    void Start()
    {
        _terrainMaterial = new Material(Shader.Find("Standard"));
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
