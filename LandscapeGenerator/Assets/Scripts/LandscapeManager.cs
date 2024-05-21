using System;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.TextCore.Text;

public class LandscapeManager : MonoBehaviour{
	
	private const int TerrainChunkSize = 17;
	private const float ViewerMoveThresholdForChunkUpdate = (TerrainChunkSize - 1) * 0.5f;
	private static int _chunksVisibleInViewDst = 3;
	private static readonly Dictionary<int2, TerrainChunk> TerrainChunkDictionary = new Dictionary<int2, TerrainChunk>();
	private static readonly HashSet<TerrainChunk> TerrainChunksVisibleLastUpdate = new HashSet<TerrainChunk>();
	private static readonly List<TerrainChunk> SurroundTerrainChunks = new List<TerrainChunk>();
	private static float _worldTerrainChunkSize;
	private Transform _transform;
	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;
	[Range(-90, 90)]
	private int _lastLatitude;
	[Range(-90, 90)]
	private int _lastLongitude;
	
	public const float Scale = 1f;
	public const float ViewerRotateThresholdForChunkUpdate = 20f;
	public const float AngleThresholdForChunkCulling = 120f; 
	public const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;
	public static float MaxViewDst;
	public static event Action CompleteMeshGenerationEvent;
	
	public static LandscapeManager Instance;
	public MapGenerator mapGenerator;
	public LODInfo[] detailLevels;
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
		
		_transform = transform;
		_lastLatitude = initialLatitude + 90;
		_lastLongitude = initialLongitude + 90;
		
		_worldTerrainChunkSize = (TerrainChunkSize - 1) * Scale;
		
		MaxViewDst = 0;
		_chunksVisibleInViewDst = 0;
		foreach (var detailLevel in detailLevels)
		{
			_chunksVisibleInViewDst += detailLevel.visibleChunksThreshold;
			MaxViewDst += detailLevel.visibleChunksThreshold * (TerrainChunkSize - 1);
		}

		MaxViewDst *= Scale;
		
		UpdateVisibleChunks ();
		/**/
	}

	private void Update() {
		
		
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

	private void LateUpdate()
	{
		CompleteMeshGenerationEvent?.Invoke();
	}

	private void UpdateVisibleChunks() {

		foreach (var visibleChunk in TerrainChunksVisibleLastUpdate)
		{
			visibleChunk.SetVisible(false);
		}
		
		TerrainChunksVisibleLastUpdate.Clear();
		SurroundTerrainChunks.Clear();
			
		int currentChunkCoordX = Mathf.RoundToInt (viewer.PositionV2.x / (TerrainChunkSize - 1));
		int currentChunkCoordY = Mathf.RoundToInt (viewer.PositionV2.y / (TerrainChunkSize - 1));
		Viewer.ChunkCoord = new int2(currentChunkCoordX, currentChunkCoordY);
		
		for (int yOffset = -_chunksVisibleInViewDst; yOffset <= _chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -_chunksVisibleInViewDst; xOffset <= _chunksVisibleInViewDst; xOffset++) {
				
				var viewedChunkCoord = new int2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
				TerrainChunk chunk;
				if (TerrainChunkDictionary.TryGetValue(viewedChunkCoord, out var value))
				{
					chunk = value;
					SurroundTerrainChunks.Add(chunk);
					chunk.Update ();
				} else
				{
					chunk = new TerrainChunk(viewedChunkCoord, TerrainChunkSize, _transform, chunkMaterial);
					TerrainChunkDictionary.Add(viewedChunkCoord, chunk);
					SurroundTerrainChunks.Add(chunk);
				}
				
				if (chunk.IsVisible())
					TerrainChunksVisibleLastUpdate.Add(chunk);
				
				_lastLatitude = (_lastLatitude + yOffset) % 180;
				_lastLongitude = (_lastLongitude + yOffset) % 180;
			}
		}
	}

	void UpdateCulledChunks()
	{
		Vector2 viewerForward = viewer.ForwardV2.normalized;
		foreach (TerrainChunk chunk in SurroundTerrainChunks)
		{
			CullChunkAndSetVisibility(chunk, chunk.IsCulled(viewerForward));
		}
	}
	
	private static void CullChunkAndSetVisibility(TerrainChunk chunk, bool isCulled, bool inDistance = true)
	{
		var visible = inDistance;
		if (Instance.culling == CullingMode.Layer)
		{
			chunk.GameObject.layer = isCulled? 
				LayerMask.NameToLayer("Culled") : 
				LayerMask.NameToLayer("Default");
		}
		else if(Instance.culling == CullingMode.Visibility)
		{
			visible = !isCulled && inDistance;
		}
		
		chunk.SetVisible(visible);
	}

	private class TerrainChunk
	{
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
		private TerrainChunk[,] _neighbors;
		
		public float2 Position => _position;
		public GameObject GameObject { get; }
		public bool NeighborsFilled => (_neighbors.Length == 4);

		public TerrainChunk(int2 coord, int size, Transform parent, Material material)
		{
			CompleteMeshGenerationEvent += CompleteMeshGeneration;
			GameObject = new GameObject("TerrainChunk");

			_coord = coord;
			_position = (size - 1) * coord;
			_positionV3 = new Vector3(_position.x,0,_position.y) * Scale;
			_neighbors = new TerrainChunk[2, 2];
			
			var meshRenderer = GameObject.AddComponent<MeshRenderer>();
			_meshFilter = GameObject.AddComponent<MeshFilter>();
			meshRenderer.material = material;
			_meshCollider = GameObject.AddComponent<MeshCollider>();
			
			GameObject.transform.position = _positionV3;
			GameObject.transform.parent = parent;
			_bounds = new Bounds(_positionV3,Vector2.one * (size * Scale));

			_detailLevels = Instance.detailLevels;
			_lodMeshes = new LODMesh[_detailLevels.Length];
			for (int i = 0; i < _detailLevels.Length; i++) {
				_lodMeshes[i] = new LODMesh(_detailLevels[i].lod, this);
				if (_detailLevels[i].useForCollider) {
					_colliderMesh = _lodMeshes[i];
				}
			}
			
			Update();
		}

		public void Update()
		{
			var chunksFromViewer = Viewer.ChunkCoord - _coord;
			chunksFromViewer = new int2(Mathf.Abs(chunksFromViewer.x), Mathf.Abs(chunksFromViewer.y));
			bool inDistance = chunksFromViewer.x < _chunksVisibleInViewDst && 
			                  chunksFromViewer.y < _chunksVisibleInViewDst;
			
			if (inDistance)
			{
				int lodIndex = 0;

				for (int i = 0; i < _detailLevels.Length - 1; i++)
				{
					if (chunksFromViewer.x < _detailLevels[i].visibleChunksThreshold && 
					    chunksFromViewer.y < _detailLevels[i].visibleChunksThreshold )
					{
						break;
					}
					
					lodIndex = i + 1;
					
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
					}
				}
				
				if (_lodIndex == 0 && _coord.Equals(Viewer.ChunkCoord))
				{
					_meshCollider.enabled = true;
					if (_colliderMesh.HasMesh)
					{
						_meshCollider.sharedMesh = _colliderMesh.Mesh;
					}
					else
					{
						_colliderMesh.RequestMesh();
					}
				}
				else
				{
					_meshCollider.enabled = false;
				}
			}

			CullChunkAndSetVisibility(this, IsCulled(Instance.viewer.ForwardV2.normalized), inDistance);
			
		}

		public void SetNeighbor(TerrainChunk neighbor)
		{
			int x = (neighbor._coord - _coord).x % 180;
			int y = (neighbor._coord - _coord).y % 180;

			_neighbors[x, y] = neighbor;
		}
		public bool IsCulled(Vector2 viewerForward)
		{
			if (_coord.Equals(Viewer.ChunkCoord)) return false;

			var chunkCenter = new Vector2(_positionV3.x, _positionV3.z);
			chunkCenter += chunkCenter.normalized * (_worldTerrainChunkSize / 2);
			Vector2 chunkDirection = (chunkCenter - Instance.viewer.PositionV2).normalized;
			float dot = Vector2.Dot(viewerForward, chunkDirection);
			float chunkAngle = Mathf.Acos(dot) * Mathf.Rad2Deg;

			return dot < 0 && chunkAngle > Instance.viewer.FOV;
		}
		
		public bool IsVisible() {
			return GameObject.activeSelf;
		}
		public void SetVisible(bool visible) {
			GameObject.SetActive (visible);
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
			_meshJobHandle = MeshGenerator.ScheduleMeshGenerationJob(Instance.terrainParameters, resolution , Scale, _chunk.Position, Instance.mapGenerator.GetColorGradient(), ref _meshData);
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
		public int visibleChunksThreshold;
		public bool useForCollider;
	}

	public enum CullingMode
	{
		Layer,
		Visibility
	}
}