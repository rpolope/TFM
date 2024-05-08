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
	public static float maxViewDst = Scale * (TerrainChunkSize - 1) * 5f;
	public LODInfo[] detailLevels;

	private Transform _transform;
	private Vector2 _viewerPositionOld;
	private float _viewerRotationYOld;
	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;

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
	void Start()
	{
		/*
		_meshFilter = GetComponent<MeshFilter>();
		_meshRenderer = GetComponent<MeshRenderer>();
		
		GenerateSimpleTerrainChunk(_meshFilter, _meshRenderer);
		/**/
		
		
		_chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / (TerrainChunkSize - 1));
		_transform = transform;
		UpdateVisibleChunks ();
		/**/
	}

	void Update() {
		
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z) / Scale;
		viewerRotationY = viewer.rotation.y;
		
		if ((_viewerPositionOld - viewerPosition).sqrMagnitude > SqrViewerMoveThresholdForChunkUpdate) {
			_viewerPositionOld = viewerPosition;
			UpdateVisibleChunks();
		}
		/* */
		
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
		Bounds _bounds;
		LOD[] _lods;
		LODMesh[] _lodMeshes;
		LODInfo[] _detailLevels;

		int _previousLODIndex = -1;

		MeshRenderer _meshRenderer;
		MeshFilter _meshFilter;
		MeshCollider _meshCollider;
		
		public Vector3 Position => new(_position.x, 0, _position.y);
		
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

			_detailLevels = Instance.detailLevels;
			_lodMeshes = new LODMesh[_detailLevels.Length];
			for (int i = 0; i < _detailLevels.Length; i++) {
				_lodMeshes[i] = new LODMesh(_detailLevels[i].lod, UpdateTerrainChunk);
			}

			UpdateTerrainChunk();
		}
	

		public void UpdateTerrainChunk() {
			float viewerDstFromNearestEdge = (Instance.viewerPosition - new Vector2(_position.x, _position.y)).sqrMagnitude;
			bool visible = (viewerDstFromNearestEdge <= (maxViewDst * maxViewDst) &&
			                CustomCameraCulling.IsObjectVisible(Position));

			if (visible){
				
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
					
					if (lodMesh.mesh != null)
					{
						_meshFilter.mesh = lodMesh.mesh;
						_meshCollider.sharedMesh = lodMesh.mesh;
					}
					else
					{
						Mesh mesh = MeshGenerator.GenerateTerrainMesh(Instance.terrainParameters, TerrainChunkSize, Scale, _position, lodIndex);
						lodMesh.mesh = mesh;
						_meshFilter.mesh = lodMesh.mesh;
						_meshCollider.sharedMesh = lodMesh.mesh;
						
					}
				}

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
	
	class LODMesh {

		public Mesh mesh;
		int _lod;
		Action _callback;

		public LODMesh(int lod, Action callback) {
			_lod = lod;
			mesh = null;
			_callback = callback;
		}
	}

	[Serializable]
	public struct LODInfo {
		public int lod;
		public float visibleDstThreshold;
	}

}
