using System;
using UnityEngine;
using UnityEngine.UIElements;

public class LandscapeManager : MonoBehaviour{
	
	public const float Scale = 1f;
	public const int MapHeight = 30;
	public const int MapWidth = 30;
	public static LandscapeManager Instance;

	public Transform Transform { get; private set; }
	public int initialLatitude;
	public int initialLongitude;
	public TerrainParameters terrainParameters;
	public BiomesParameters biomesParameters;
	public Material chunkMaterial;
	public Viewer viewer;
	public CullingMode culling;


	private static MapData[] _maps;
	
	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;
	private TerrainChunksManager _chunksManager;
	private int _initialLatitude;
	private int _initialLongitude;
	
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
		Transform = transform;

		_initialLatitude = Mathf.RoundToInt(((initialLatitude + 90f) / 180f) * MapHeight);
		_initialLongitude = Mathf.RoundToInt(((initialLongitude + 90f) / 180f) * MapWidth);
		viewer.Position = new Vector3(_initialLongitude, viewer.Position.y, _initialLatitude);
		
		_maps = new MapData[MapHeight * MapWidth];
		
		BiomeManager.Initialize();
		_chunksManager = new TerrainChunksManager(viewer, chunkMaterial);
		_chunksManager.Initialize();
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