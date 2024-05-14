using System;
using System.Collections.Generic;
using TMPro;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class LandscapeManager : MonoBehaviour
{
	public const float Scale = 2.5f;
	public const int TerrainChunkSize = 233;
	public static int ChunksVisibleInViewDst = 3;
	// public const float ViewerMoveThresholdForChunkUpdate = 25f;
	public const float ViewerMoveThresholdForChunkUpdate = (TerrainChunkSize - 1) * Scale;
	public const float ViewerRotateThresholdForChunkUpdate = 20f;
	public const float AngleThresholdForChunkCulling = 120f; 
	public const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;
	public static float MaxViewDst;
	public static float WorldTerrainChunkSize;
	
	public static event Action CompleteMeshGenerationEvent;

	
	static Dictionary<int2, TerrainChunk> _terrainChunkDictionary = new Dictionary<int2, TerrainChunk>();
	static readonly HashSet<TerrainChunk> TerrainChunksVisibleLastUpdate = new HashSet<TerrainChunk>();
	static readonly List<TerrainChunk> SurrounderTerrainChunks = new List<TerrainChunk>();
	private Transform _transform;
	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;
	[Range(-90, 90)]
	private int _lastLatitude;
	[Range(-90, 90)]
	private int _lastLongitude;

	public static LandscapeManager Instance;
	public LODInfo[] detailLevels;
	public int initialLatitude = 0;
	public int initialLongitude = 0;
	public TerrainParameters terrainParameters;
	public Material chunkMaterial;
	public Viewer viewer;
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
		
		// GenerateSimpleTerrainChunk();
		
		_transform = transform;
		_lastLatitude = initialLatitude + 90;
		_lastLongitude = initialLongitude + 90;
		
		WorldTerrainChunkSize = (TerrainChunkSize - 1) * Scale;
		MaxViewDst = detailLevels [^1].visibleDstThreshold;;
		ChunksVisibleInViewDst = Mathf.RoundToInt(MaxViewDst / (TerrainChunkSize - 1));
		// MaxViewDst = WorldTerrainChunkSize;
		// TerrainChunk.Current = new TerrainChunk();
		
		UpdateVisibleChunks ();
		/**/
	}

	void Update() {
		
		
		if (viewer.PositionChanged()) {
			viewer.UpdateOldPosition();
			UpdateVisibleChunks();
		}
		/* */
		
		if (viewer.RotationChanged())
		{
			viewer.UpdateOldRotation();
			UpdateCulledChunks();
		}
	}

	void LateUpdate()
	{
		CompleteMeshGenerationEvent?.Invoke();
	}

	void UpdateVisibleChunks() {

		foreach (var visibleChunk in TerrainChunksVisibleLastUpdate)
		{
			visibleChunk.SetVisible(false);
		}
		
		TerrainChunksVisibleLastUpdate.Clear();
		SurrounderTerrainChunks.Clear();
			
		int currentChunkCoordX = Mathf.RoundToInt (viewer.PositionV2.x / WorldTerrainChunkSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewer.PositionV2.y / WorldTerrainChunkSize);
		viewer.ChunkCoord = new int2(currentChunkCoordX, currentChunkCoordY);
		
		for (int yOffset = -ChunksVisibleInViewDst; yOffset <= ChunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -ChunksVisibleInViewDst; xOffset <= ChunksVisibleInViewDst; xOffset++) {
				
				var viewedChunkCoord = new int2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
				
				if (_terrainChunkDictionary.ContainsKey (viewedChunkCoord))
				{
					SurrounderTerrainChunks.Add(_terrainChunkDictionary[viewedChunkCoord]);
					_terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
				} else
				{
					var chunk = new TerrainChunk(viewedChunkCoord, TerrainChunkSize, _transform, chunkMaterial);
					_terrainChunkDictionary.Add(viewedChunkCoord, chunk);
					SurrounderTerrainChunks.Add(chunk);
				}
				_lastLatitude = (_lastLatitude + yOffset) % 180;
				_lastLongitude = (_lastLongitude + yOffset) % 180;
			}
		}
	}

	void UpdateCulledChunks()
	{
		Vector2 viewerForward = viewer.ForwardV2.normalized;
		foreach (TerrainChunk chunk in SurrounderTerrainChunks)
		{
			Vector3 pos = chunk.PositionV3;
			bool isVisible = !chunk.IsCulled(viewerForward);
			
			// chunk.SetVisible(isVisible);

			if (isVisible)
			{
				chunk.GameObject.layer = LayerMask.NameToLayer("Default");
				// TerrainChunksVisibleLastUpdate.Add(chunk);
			}
			else
			{
				chunk.GameObject.layer = LayerMask.NameToLayer("Culled");
			}
		}
	}
	
	
	
	

	//  
	// public static void GenerateSimpleTerrainChunk()
	// {
	// 	Instance._meshFilter ??= Instance.GetComponent<MeshFilter>();
	// 	Instance._meshRenderer ??= Instance.GetComponent<MeshRenderer>();
	//
	// 	MeshData meshData = new MeshData(Instance.terrainParameters.meshParameters.resolution, 0);
	// 	
	// 	Mesh mesh = MeshGenerator.RequestMesh(Instance.terrainParameters, Instance.terrainParameters.meshParameters.resolution, Scale, new float2(), 0, meshData);
	// 	Instance._meshFilter.sharedMesh = mesh;
	//
	// 	Texture2D heightTexture = GenerateHeightTexture(mesh);
	//
	// 	Material material = new Material(Shader.Find("Standard"));
	// 	material.mainTexture = heightTexture;
	// 	Instance._meshRenderer.sharedMaterial = material;
	// }

	// private static Texture2D GenerateHeightTexture(Mesh mesh)
	// {
	// 	Vector3[] vertices = mesh.vertices;
	// 	Texture2D texture = new Texture2D(Instance.terrainParameters.meshParameters.resolution, Instance.terrainParameters.meshParameters.resolution);
	//
	// 	for (int i = 0; i < vertices.Length; i++)
	// 	{
	// 		int y = i / Instance.terrainParameters.meshParameters.resolution;
	// 		int x = i % Instance.terrainParameters.meshParameters.resolution;
	// 		
	// 		float height = vertices[i].y;
	// 		float normalizedHeight = Mathf.Lerp(0, NoiseGenerator.MaxValue * Scale * Instance.terrainParameters.meshParameters.heightScale, height);
	//
	// 		texture.SetPixel(x, y, new Color(normalizedHeight, normalizedHeight, normalizedHeight));
	// 	}
	//
	// 	texture.Apply();
	// 	byte[] pngData = texture.EncodeToPNG();
	//
	// 	System.IO.File.WriteAllBytes("./Assets/Textures/savedTexture.png", pngData);
	//
	// 	return texture;
	// }

	private class TerrainChunk
	{
		private readonly GameObject _meshObject;
		private readonly float2 _position;
		private readonly Vector3 _positionV3;
		private readonly LODMesh[] _lodMeshes;
		private readonly LODInfo[] _detailLevels;
		private int2 _coord;
		private Bounds _bounds;
		private LOD[] _lods;

		private int _lodIndex = -1;
		
		private readonly LODMesh _colliderMesh;
		private readonly MeshFilter _meshFilter;
		private readonly MeshCollider _meshCollider;
		
		public Vector3 PositionV3 => new(_position.x, 0, _position.y);
		public float2 Position => _position;
		public GameObject GameObject => _meshObject;

		public TerrainChunk(int2 coord, int size, Transform parent, Material material)
		{
			CompleteMeshGenerationEvent += CompleteMeshGeneration;
			
			_coord = coord;
			_position = (size - 1) * coord;
			_positionV3 = new Vector3(_position.x,0,_position.y) * Scale;

			_meshObject = new GameObject("TerrainChunk");
			var meshRenderer = _meshObject.AddComponent<MeshRenderer>();
			_meshFilter = _meshObject.AddComponent<MeshFilter>();
			meshRenderer.material = material;
			_meshCollider = _meshObject.AddComponent<MeshCollider>();
			
			_meshObject.transform.position = _positionV3;
			_meshObject.transform.parent = parent;
			_bounds = new Bounds(_positionV3,Vector2.one * (size * Scale));

			_detailLevels = Instance.detailLevels;
			_lodMeshes = new LODMesh[_detailLevels.Length];
			for (int i = 0; i < _detailLevels.Length; i++) {
				_lodMeshes[i] = new LODMesh(_detailLevels[i].lod, this);
				if (_detailLevels[i].useForCollider) {
					_colliderMesh = _lodMeshes[i];
				}
			}
			
			UpdateTerrainChunk();
		}

		private void CompleteMeshGeneration()
		{
			if (IsVisible())
			{
				var lodMesh = _lodMeshes[_lodIndex];
				if (lodMesh.RequestedMesh)
				{
					lodMesh.CompleteMeshGeneration();
					_meshFilter.mesh = lodMesh.Mesh;

					if (_lodIndex == 0 && _colliderMesh.RequestedMesh)
					{
						_colliderMesh.CompleteMeshGeneration();
						_meshCollider.sharedMesh = _colliderMesh.Mesh;
					}
				}
			}	
			
		}
	

		public void UpdateTerrainChunk()
		{
			float distFromViewer = _bounds.SqrDistance(Instance.viewer.PositionV3);
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
				
				if (lodIndex != _lodIndex)
				{
					var lodMesh = _lodMeshes[lodIndex];
					_lodIndex = lodIndex;
					
					if (lodMesh.HasMesh)
					{
						_meshFilter.mesh = lodMesh.Mesh;
						if (lodIndex == 0)
						{
							_meshCollider.enabled = true;
							_meshCollider.sharedMesh = _colliderMesh.Mesh;
						}
					}
					else
					{
						lodMesh.RequestMesh();
						
						if (lodIndex == 0)
						{
							_meshCollider.enabled = true;
							if (_colliderMesh.HasMesh) {
								_meshCollider.sharedMesh = _colliderMesh.Mesh;
							} else{
								_colliderMesh.RequestMesh();
							}
						}else
						{
							_meshCollider.enabled = false;
						}
					}
				}
			}
			
			// bool visible = inDistance && !IsCulled(Instance.viewer.ForwardV2.normalized);
			bool visible = inDistance;
			SetVisible(visible);
			if (visible)
			{
				TerrainChunksVisibleLastUpdate.Add(this);
			}

			var culled = IsCulled(Instance.viewer.ForwardV2.normalized)
				? _meshObject.layer = LayerMask.NameToLayer("Culled")
				: _meshObject.layer = LayerMask.NameToLayer("Default");
		}

		public bool IsCulled(Vector2 viewerForward)
		{
			if (_coord.Equals(Instance.viewer.ChunkCoord)) return false;

			var chunkCenter = new Vector2(_positionV3.x, _positionV3.z);
			chunkCenter += chunkCenter.normalized * (WorldTerrainChunkSize / 2);
			Vector2 chunkDirection = (chunkCenter - Instance.viewer.PositionV2).normalized;
			float dot = Vector2.Dot(viewerForward, chunkDirection);
			float chunkAngle = Mathf.Acos(dot) * Mathf.Rad2Deg;

			return dot < 0 && chunkAngle > Instance.viewer.FOV;
		}

		public void SetVisible(bool visible) {
			_meshObject.SetActive (visible);

			// ChangeSubscriptionToMeshGeneration();
		}

		private void ChangeSubscriptionToMeshGeneration()
		{
			if(IsVisible())
			{
				CompleteMeshGenerationEvent += CompleteMeshGeneration;
			}
			else
			{
				CompleteMeshGenerationEvent -= CompleteMeshGeneration;
			}
		}


		private bool IsVisible() {
			return _meshObject.activeSelf;
		}

	}

	private class LODMesh {

		public Mesh Mesh;
		public bool HasMesh;
		public bool RequestedMesh;
		
		private readonly int _lod;
		private readonly TerrainChunk _chunk;
		private JobHandle _meshJobHandle;
		private MeshData _meshData;

		public LODMesh(int lod, TerrainChunk chunk) {
			_lod = lod;
			_chunk = chunk;
			HasMesh = false;
		}

		public void RequestMesh()
		{
			_meshData = new MeshData(TerrainChunkSize, _lod);
			var resolution = (TerrainChunkSize - 1) / _meshData.LODScale + 1;
			_meshJobHandle = MeshGenerator.ScheduleMeshGenerationJob(Instance.terrainParameters, resolution , Scale, _chunk.Position, _lod, ref _meshData);
			RequestedMesh = true;
		}

		public void CompleteMeshGeneration()
		{
			_meshJobHandle.Complete();
			SetMesh();
		}

		private void SetMesh()
		{
			Mesh = _meshData.CreateMesh();
			RequestedMesh = false;
			HasMesh = true;
		}
	}

	[Serializable]
	public struct LODInfo {
		public int lod;
		public float visibleDstThreshold;
		public bool useForCollider;
	}
}
