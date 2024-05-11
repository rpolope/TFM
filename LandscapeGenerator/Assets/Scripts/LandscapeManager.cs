using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class LandscapeManager : MonoBehaviour
{
	public const float Scale = 3f;
	public const int TerrainChunkSize = 17;
	public const float ViewerMoveThresholdForChunkUpdate = 25f;
	public const float ViewerRotateThresholdForChunkUpdate = 5f;
	public const float AngleThresholdForChunkCulling = 100f; 
	public const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;
	public static readonly float MaxViewDst = (TerrainChunkSize - 1) * 4;
	
	
	static Dictionary<int2, TerrainChunk> _terrainChunkDictionary = new Dictionary<int2, TerrainChunk>();
	static HashSet<TerrainChunk> _terrainChunksVisibleLastUpdate = new HashSet<TerrainChunk>();
	static List<TerrainChunk> _surrounderTerrainChunks = new List<TerrainChunk>();
	private Transform _transform;
	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;
	public TerrainChunk _currentChunk;
	[Range(-90, 90)]
	private int _lastLatitude;
	[Range(-90, 90)]
	private int _lastLongitude;
	int _chunksVisibleInViewDst;

	public static LandscapeManager Instance;
	public LODInfo[] detailLevels;
	public int initialLatitude = 0;
	public int initialLongitude = 0;
	public TerrainParameters terrainParameters;
	public Material chunkMaterial;
	public Viewer viewer;
	public TerrainChunk CurrentChunk => _currentChunk;

	
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

	void Start()
	{
		/*
		_meshFilter ??= GetComponent<MeshFilter>();
		_meshRenderer ??= GetComponent<MeshRenderer>();

		GenerateSimpleTerrainChunk(_meshFilter, _meshRenderer);
		/**/
		
		
		_chunksVisibleInViewDst = Mathf.RoundToInt(MaxViewDst / (TerrainChunkSize - 1));
		// _chunksVisibleInViewDst = 0;
		_transform = transform;
		_lastLatitude = initialLatitude + 90;
		_lastLongitude = initialLongitude + 90;
		
		UpdateVisibleChunks ();
		/**/
	}

	void Update() {
		
		
		if (viewer.PositionChanged()) {
			viewer.UpdateOldPosition();
			UpdateVisibleChunks();
		}
		/* */
		
		// if (viewer.RotationChanged())
		// {
		// 	viewer.UpdateOldRotation();
		// 	UpdateCulledChunks();
		// }
	}
		
	void UpdateVisibleChunks() {

		foreach (var visibleChunk in _terrainChunksVisibleLastUpdate)
		{
			visibleChunk.SetVisible(false);
		}
		
		_terrainChunksVisibleLastUpdate.Clear();
		_surrounderTerrainChunks.Clear();
			
		int currentChunkCoordX = Mathf.RoundToInt (viewer.PositionV2.x / TerrainChunkSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewer.PositionV2.y / TerrainChunkSize);
		
		for (int yOffset = -_chunksVisibleInViewDst; yOffset <= _chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -_chunksVisibleInViewDst; xOffset <= _chunksVisibleInViewDst; xOffset++) {
				
				int2 viewedChunkCoord = new int2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
				
				if (_terrainChunkDictionary.ContainsKey (viewedChunkCoord))
				{
					_surrounderTerrainChunks.Add(_terrainChunkDictionary[viewedChunkCoord]);
					_terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
				} else
				{
					TerrainChunk chunk = new TerrainChunk(viewedChunkCoord, TerrainChunkSize, _transform, chunkMaterial);
					_terrainChunkDictionary.Add(viewedChunkCoord, chunk);
					_surrounderTerrainChunks.Add(chunk);
				}
				_lastLatitude = (_lastLatitude + yOffset) % 180;
				_lastLongitude = (_lastLongitude + yOffset) % 180;
			}
		}
		_currentChunk = _terrainChunkDictionary[new (currentChunkCoordX, currentChunkCoordY)];

	}

	void UpdateCulledChunks()
	{
		Vector2 viewerForward = viewer.ForwardV2.normalized;
		foreach (TerrainChunk chunk in _surrounderTerrainChunks)
		{
			Vector3 pos = chunk.Position;
			bool isVisible = !chunk.IsCulled(viewerForward);
			chunk.SetVisible(isVisible);

			if (isVisible)
			{
				_terrainChunksVisibleLastUpdate.Add(chunk);
			}
		}
	}

	 
	public static void GenerateSimpleTerrainChunk(MeshFilter meshFilter, MeshRenderer meshRenderer)
	{
		Mesh mesh = MeshGenerator.GenerateTerrainMesh(Instance.terrainParameters, Instance.terrainParameters.meshParameters.resolution, Scale, new float2(), 0);
		meshFilter.sharedMesh = mesh;

		Texture2D heightTexture = GenerateHeightTexture(mesh);

		Material material = new Material(Shader.Find("Standard"));
		material.mainTexture = heightTexture;
		meshRenderer.sharedMaterial = material;
	}

	private static Texture2D GenerateHeightTexture(Mesh mesh)
	{
		Vector3[] vertices = mesh.vertices;
		Texture2D texture = new Texture2D(Instance.terrainParameters.meshParameters.resolution, Instance.terrainParameters.meshParameters.resolution, TextureFormat.RFloat, false);

		for (int i = 0; i < vertices.Length; i++)
		{
			float height = vertices[i].y;
			float normalizedHeight = Mathf.Lerp(0, Instance.terrainParameters.meshParameters.heightScale, height);

			texture.SetPixel(i, 0, new Color(normalizedHeight, normalizedHeight, normalizedHeight));
		}

		texture.Apply();

		return texture;
	}
	
	public class TerrainChunk {
		
		GameObject _meshObject;
		float2 _position;
		int2 _coord;
		Bounds _bounds;
		LOD[] _lods;
		LODMesh[] _lodMeshes;
		LODInfo[] _detailLevels;

		int _previousLODIndex = -1;

		MeshRenderer _meshRenderer;
		MeshFilter _meshFilter;
		MeshCollider _meshCollider;
		
		public Vector3 Position => new(_position.x, 0, _position.y);

		public TerrainChunk(int2 coord, int size, Transform parent, Material material)
		{
			_coord = coord;
			_position = (size - 1) * coord;
			Vector3 positionV3 = new Vector3(_position.x,0,_position.y);

			_meshObject = new GameObject("TerrainChunk");
			_meshRenderer = _meshObject.AddComponent<MeshRenderer>();
			_meshFilter = _meshObject.AddComponent<MeshFilter>();
			_meshCollider = _meshObject.AddComponent<MeshCollider>();
			_meshRenderer.material = material;
			
			_meshObject.transform.position = positionV3 * Scale;
			_meshObject.transform.parent = parent;
			_bounds = _bounds = new Bounds(positionV3,Vector2.one * size);

			_detailLevels = Instance.detailLevels;
			_lodMeshes = new LODMesh[_detailLevels.Length];
			for (int i = 0; i < _detailLevels.Length; i++) {
				_lodMeshes[i] = new LODMesh(_detailLevels[i].lod);
			}

			UpdateTerrainChunk();
		}
	

		public void UpdateTerrainChunk()
		{
			var viewerPosV3 = new Vector3(Instance.viewer.PositionV2.x, 0, Instance.viewer.PositionV2.y);
			float distFromViewer = _bounds.SqrDistance (viewerPosV3);
			bool inDistance = distFromViewer <= (MaxViewDst * MaxViewDst);

			if (inDistance){
				
				int lodIndex = 0;
				
				for (int i = 0; i < _detailLevels.Length - 1; i++) {
					if (distFromViewer > _detailLevels [i].visibleDstThreshold) {
						lodIndex = i + 1;
					} else {
						break;
					}
				}
				
				if (lodIndex != _previousLODIndex)
				{
					LODMesh lodMesh = _lodMeshes[lodIndex];
					_previousLODIndex = lodIndex;
					
					if (lodMesh.hasMesh)
					{
						_meshFilter.mesh = lodMesh.mesh;
						_meshCollider.sharedMesh = lodMesh.mesh;
					}
					else
					{
						// Se pide la mesh y se actualiza en el lateUpdate
						Mesh mesh = MeshGenerator.GenerateTerrainMesh(Instance.terrainParameters, TerrainChunkSize, Scale, _position, lodIndex);
						lodMesh.mesh = mesh;
						lodMesh.hasMesh = true;
						_meshFilter.mesh = lodMesh.mesh;
						_meshCollider.sharedMesh = lodMesh.mesh;
					}

					_meshCollider.enabled = lodIndex == 0;
				}
			}
			
			bool visible = inDistance && !IsCulled(Instance.viewer.ForwardV2.normalized);
			SetVisible(visible);
			if (visible)
				_terrainChunksVisibleLastUpdate.Add(this);
		}

		public bool IsCulled(Vector2 viewerForward)
		{
			if (this == Instance.CurrentChunk) return false;
        
			Vector2 chunkDirection = new Vector2(Position.x - Instance.viewer.PositionV2.x, Position.z - Instance.viewer.PositionV2.y).normalized;
			float dot = Vector2.Dot(viewerForward, chunkDirection);
			float chunkAngle = Mathf.Acos(dot) * Mathf.Rad2Deg;

			return dot < 0 && chunkAngle > AngleThresholdForChunkCulling;
		}

		public void SetVisible(bool visible) {
			_meshObject.SetActive (visible);
		}

		public bool IsVisible() {
			return _meshObject.activeSelf;
		}

	}
	
	class LODMesh {

		public Mesh mesh;
		public bool hasMesh;
		int _lod;

		public LODMesh(int lod) {
			_lod = lod;
			hasMesh = false;
		}
	}

	[Serializable]
	public struct LODInfo {
		public int lod;
		public float visibleDstThreshold;
	}

}
