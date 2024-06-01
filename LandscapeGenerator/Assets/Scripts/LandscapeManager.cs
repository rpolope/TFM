using System;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class LandscapeManager : MonoBehaviour{
	
	private Transform _transform;
	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;
	[Range(-90, 90)]
	private int _lastLatitude;
	[Range(-90, 90)]
	private int _lastLongitude;

	private TerrainChunksManager _chunksManager;
	public const float Scale = 1f;
	
	public static LandscapeManager Instance;
	public int initialLatitude = 0;
	public int initialLongitude = 0;
	public TerrainParameters terrainParameters;
	public BiomesParameters biomesParameters;
	public Material chunkMaterial;
	public Viewer viewer;
	public CullingMode culling;

	private void Awake()
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

	private void Start()
	{
		BiomeManager.Initialize();
		_chunksManager = new TerrainChunksManager(viewer, chunkMaterial);
		_chunksManager.Initialize();
		_transform = transform;
		_lastLatitude = initialLatitude + 90;
		_lastLongitude = initialLongitude + 90;
		
		
	}

	private void Update()
	{
		_chunksManager.Update();
	}

	private void LateUpdate()
	{
		TerrainChunksManager.CompleteMeshGeneration();
	}
}

public enum CullingMode
{
	Layer,
	Visibility
}