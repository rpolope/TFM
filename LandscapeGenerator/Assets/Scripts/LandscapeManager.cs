using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
public class LandscapeManager : MonoBehaviour
{
	public const float Scale = 3f;
	public const int TerrainChunkSize = 65;
	public const float ViewerMoveThresholdForChunkUpdate = 25f;
	public const float ViewerRotateThresholdForChunkUpdate = 5f * Mathf.PI / 180;
	
	public const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;
	public static float maxViewDst = (TerrainChunkSize - 1) * 5f;

	private MeshFilter _meshFilter; 
	private MeshRenderer _meshRenderer;
	private Transform _transform;
	private Vector2 _viewerPositionOld;
	private float _viewerRotationYOld;

	public static LandscapeManager Instance;
	public TerrainParameters terrainParameters;
	public Transform viewer;
	public Vector2 viewerPosition;
	public float viewerRotationY;
	public Material chunkMaterial;

	int _chunksVisibleInViewDst;
	Dictionary<Vector2, TerrainChunk> _terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	static HashSet<TerrainChunk> _terrainChunksVisibleLastUpdate = new HashSet<TerrainChunk>();
	static List<TerrainChunk> _surrounderTerrainChunks = new List<TerrainChunk>();
	
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
	void Start() {

		_chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / (TerrainChunkSize - 1));
		_transform = transform;
		UpdateVisibleChunks ();
	}

	void Update() {
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z) / Scale;
		viewerRotationY = viewer.rotation.y;
		
		if ((_viewerPositionOld - viewerPosition).sqrMagnitude > SqrViewerMoveThresholdForChunkUpdate) {
			_viewerPositionOld = viewerPosition;
			UpdateVisibleChunks();
		}

		// if (Math.Abs(viewerRotationY - _viewerRotationYOld) > ViewerRotateThresholdForChunkUpdate)
		// {
		// 	_viewerRotationYOld = viewerRotationY;
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
			
		int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / TerrainChunkSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / TerrainChunkSize);

		for (int yOffset = -_chunksVisibleInViewDst; yOffset <= _chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -_chunksVisibleInViewDst; xOffset <= _chunksVisibleInViewDst; xOffset++) {
				
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				if (_terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {
					_surrounderTerrainChunks.Add(_terrainChunkDictionary [viewedChunkCoord]);
					_terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
				} else
				{
					TerrainChunk chunk = new TerrainChunk(viewedChunkCoord, TerrainChunkSize, _transform, chunkMaterial);
					_terrainChunkDictionary.Add(viewedChunkCoord, chunk);
					_surrounderTerrainChunks.Add(chunk);
				}
			}
		}
	}

	void UpdateCulledChunks()
	{
		CustomCameraCulling.UpdateVisibleChunks(_surrounderTerrainChunks, _terrainChunksVisibleLastUpdate);
	}
	 
	void GenerateSimpleTerrainChunk()
	{
		_meshFilter = GetComponent<MeshFilter>();
		_meshRenderer = GetComponent<MeshRenderer>();

		Mesh mesh = MeshGenerator.GenerateTerrainMesh(terrainParameters, terrainParameters.meshParameters.resolution, Scale, new float2());

		_meshFilter.sharedMesh = mesh;
		_meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
		// MapGenerator.GenerateMap(TerrainSize, Scale);
	}

	public class TerrainChunk {

		GameObject _meshObject;
		float2 _position;
		Bounds _bounds;
		
		MeshRenderer _meshRenderer;
		MeshFilter _meshFilter;
		MeshCollider _meshCollider;
		
		public Vector3 Position => new Vector3(_position.x, 0, _position.y);
		
		public TerrainChunk(Vector2 coord, int size, Transform parent, Material material)
		{
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

			Mesh mesh = MeshGenerator.GenerateTerrainMesh(Instance.terrainParameters, size, Scale, _position);
			_meshFilter.mesh = mesh;
			_meshCollider.sharedMesh = mesh;

			UpdateTerrainChunk();
		}
	

		public void UpdateTerrainChunk() {
			float viewerDstFromNearestEdge = (Instance.viewerPosition - new Vector2(_position.x, _position.y)).sqrMagnitude;
			bool visible = (viewerDstFromNearestEdge <= (maxViewDst * maxViewDst) &&
			                CustomCameraCulling.IsObjectVisible(Position));

			if (visible){
				_terrainChunksVisibleLastUpdate.Add(this);
			}
			
			SetVisible(visible);
		}

		public void SetVisible(bool visible) {
			_meshObject.SetActive (visible);
		}

		public bool IsVisible() {
			return _meshObject.activeSelf;
		}

	}
}
