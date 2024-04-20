using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LandscapeManager : MonoBehaviour
{
    private readonly int _terrainChunkSize;

    public int TerrainChunkSize
    {
        get => _terrainChunkSize;
    }

    public TerrainChunksGenerator terrainChunksGenerator;
    public SettingsConfiguration settings;
    public int viewThreshold;
    public static LandscapeManager Instance;

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
