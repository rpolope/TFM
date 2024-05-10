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
	public const float AngleThresholdForChunkVisibility = 90f; 
	
	public const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;
	public static float maxViewDst = (TerrainChunkSize - 1) * 1;
	public LODInfo[] detailLevels;

	private Transform _transform;
	private Vector2 _viewerPositionOld;
	private float _viewerRotationYOld;
	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;
	public TerrainChunk _currentChunk;
	[Range(-90, 90)]
	private int _lastLatitude;
	[Range(-90, 90)]
	private int _lastLongitude;
	int _chunksVisibleInViewDst;
	Dictionary<int2, TerrainChunk> _terrainChunkDictionary = new Dictionary<int2, TerrainChunk>();
	static HashSet<TerrainChunk> _terrainChunksVisibleLastUpdate = new HashSet<TerrainChunk>();
	static List<TerrainChunk> _surrounderTerrainChunks = new List<TerrainChunk>();

	public static LandscapeManager Instance;
	public int initialLatitude = 0;
	public int initialLongitude = 0;
	public TerrainParameters terrainParameters;
	public Material chunkMaterial;
	public Transform viewer;
	public Vector2 viewerPosition;
	public float viewerRotationY;
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
		
		
		_chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / (TerrainChunkSize - 1));
		_transform = transform;
		_lastLatitude = initialLatitude + 90;
		_lastLongitude = initialLongitude + 90;
		
		UpdateVisibleChunks ();
		/**/
	}

	void Update() {
		
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z);
		viewerRotationY = viewer.rotation.eulerAngles.y;
		
		if ((_viewerPositionOld - viewerPosition).sqrMagnitude > SqrViewerMoveThresholdForChunkUpdate) {
			_viewerPositionOld = viewerPosition;
			UpdateVisibleChunks();
		}
		/* */
		
		if (Math.Abs(viewerRotationY - _viewerRotationYOld) > ViewerRotateThresholdForChunkUpdate)
		{
			_viewerRotationYOld = viewerRotationY;
			UpdateCulledChunks();
		}
	}
		
	void UpdateVisibleChunks() {

		foreach (var visibleChunk in _terrainChunksVisibleLastUpdate)
		{
			visibleChunk.SetVisible(false);
		}
		
		_terrainChunksVisibleLastUpdate.Clear();
		_surrounderTerrainChunks.Clear();
			
		int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / TerrainChunkSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / TerrainChunkSize);
		
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
		Vector2 viewerForward = new Vector2(viewer.forward.x, viewer.forward.z).normalized;
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
			_bounds = _meshRenderer.bounds;

			_detailLevels = Instance.detailLevels;
			_lodMeshes = new LODMesh[_detailLevels.Length];
			for (int i = 0; i < _detailLevels.Length; i++) {
				_lodMeshes[i] = new LODMesh(_detailLevels[i].lod);
			}

			UpdateTerrainChunk();
		}
	

		public void UpdateTerrainChunk() {
			float viewerDstFromNearestEdge = (Instance.viewerPosition - new Vector2(_position.x, _position.y)).sqrMagnitude;
			bool inDistance = viewerDstFromNearestEdge <= (maxViewDst * maxViewDst);

			if (inDistance){
				
				int lodIndex = 0;
				
				for (int i = 0; i < _detailLevels.Length - 1; i++) {
					if (viewerDstFromNearestEdge > _detailLevels [i].visibleDstThreshold) {
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
			
			var forward = Instance.viewer.forward;
			bool visible = inDistance && !IsCulled(new (forward.x, forward.z));
			SetVisible(visible);
			if (visible)
				_terrainChunksVisibleLastUpdate.Add(this);
		}

		public bool IsCulled(Vector2 viewerForward)
		{
			if (this == Instance.CurrentChunk) return false;
        
			Vector2 chunkDirection = new Vector2(Position.x - Instance.viewerPosition.x, Position.z - Instance.viewerPosition.y).normalized;
			float dot = Vector2.Dot(viewerForward, chunkDirection);
			float chunkAngle = Mathf.Acos(dot) * Mathf.Rad2Deg;

			return dot < 0 && chunkAngle > 100f;
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
