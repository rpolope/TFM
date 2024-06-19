using System;
using System.Collections.Generic;
using System.Net;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class TerrainChunksManager{
	
	private const float ViewerMoveThresholdForChunkUpdate = (TerrainChunk.Resolution - 1) * 0.5f;
	private const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;
	private static int _chunksVisibleInViewDst = 0;
	private static readonly Dictionary<int2, TerrainChunk> TerrainChunkDictionary = new Dictionary<int2, TerrainChunk>();
	private static readonly HashSet<TerrainChunk> TerrainChunksVisibleLastUpdate = new HashSet<TerrainChunk>();
	private static readonly List<TerrainChunk> SurroundTerrainChunks = new List<TerrainChunk>();
	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;
	
	public static event Action CompleteMeshGenerationEvent;
	private static LODInfo[] _detailLevels;
	private static int _wrapCountX;
	private static int _wrapCountY;
	public void Initialize()
	{
		_detailLevels = new [] {
			new LODInfo(0, 2, false),
			new LODInfo(1, 3, true),
			new LODInfo(2, 4, false)
		};
		
		// _chunksVisibleInViewDst = 0;
		// foreach (var detailLevel in _detailLevels)
		// {
		// 	_chunksVisibleInViewDst += detailLevel.visibleChunksThreshold;
		// }
		
		UpdateVisibleChunks ();
		/**/
	}

	public void Update() {
		
		
		if (Viewer.PositionChanged()) {
			Viewer.UpdateOldPosition();
			UpdateVisibleChunks();
		}
		/* */
		
		if (Viewer.RotationChanged())
		{
			Viewer.UpdateOldRotation();
			UpdateCulledChunks();
		}
	}

	
	public static void CompleteMeshGeneration()
	{
		CompleteMeshGenerationEvent?.Invoke();
	}

	private void UpdateVisibleChunks() {
        foreach (var visibleChunk in TerrainChunksVisibleLastUpdate) {
            visibleChunk.SetVisible(false);
        }
        
        TerrainChunksVisibleLastUpdate.Clear();
        SurroundTerrainChunks.Clear();
            
        int currentChunkCoordX = Mathf.RoundToInt(Viewer.PositionV2.x / (TerrainChunk.WorldSize));
        int currentChunkCoordY = Mathf.RoundToInt(Viewer.PositionV2.y / (TerrainChunk.WorldSize));
        Viewer.ChunkCoord = new int2(currentChunkCoordX, currentChunkCoordY);

        UpdateWrapCount(currentChunkCoordX, currentChunkCoordY);

        for (int yOffset = -_chunksVisibleInViewDst; yOffset <= _chunksVisibleInViewDst; yOffset++) {
            for (int xOffset = -_chunksVisibleInViewDst; xOffset <= _chunksVisibleInViewDst; xOffset++) {
                var viewedChunkCoord = new int2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                var wrappedChunkCoord = GetWrappedChunkCoords(viewedChunkCoord);
                
                TerrainChunk chunk;
                if (TerrainChunkDictionary.TryGetValue(wrappedChunkCoord, out var value)) {
                    chunk = value;
                    chunk.SetChunkCoord(viewedChunkCoord);
                    chunk.Update();
                    SurroundTerrainChunks.Add(chunk);

                } else {
                    chunk = new TerrainChunk(wrappedChunkCoord);
                    chunk.SetChunkCoord(viewedChunkCoord);
                    chunk.Update();
                    TerrainChunkDictionary.Add(wrappedChunkCoord, chunk);
                    SurroundTerrainChunks.Add(chunk);
                }
                
                if (chunk.IsVisible())
                    TerrainChunksVisibleLastUpdate.Add(chunk);
            }
        }
    }

	private bool CoordsInMap(int2 wrappedChunkCoord)
	{
		return wrappedChunkCoord.x is < LandscapeManager.MapWidth and >= 0 &&
		       wrappedChunkCoord.y is < LandscapeManager.MapHeight and >= 0;
	}

	private void UpdateWrapCount(int currentChunkCoordX, int currentChunkCoordY) {
        if (currentChunkCoordX >= LandscapeManager.MapWidth * _wrapCountX + LandscapeManager.MapWidth) {
            _wrapCountX++;
        }
        if (currentChunkCoordX < -LandscapeManager.MapWidth * _wrapCountX) {
            _wrapCountX--;
        }
        if (currentChunkCoordY >= LandscapeManager.MapHeight * _wrapCountY + LandscapeManager.MapHeight) {
            _wrapCountY++;
        }
        if (currentChunkCoordY < -LandscapeManager.MapHeight * _wrapCountY) {
            _wrapCountY--;
        }
    }

    private int2 GetWrappedChunkCoords(int2 viewedChunkCoord) {
        int wrappedXCoord = viewedChunkCoord.x;
        int wrappedYCoord = viewedChunkCoord.y;

        if (viewedChunkCoord.x < 0) {
            wrappedXCoord = (viewedChunkCoord.x % LandscapeManager.MapWidth + LandscapeManager.MapWidth) % LandscapeManager.MapWidth;
        } else if (viewedChunkCoord.x >= LandscapeManager.MapWidth) {
            wrappedXCoord = viewedChunkCoord.x % LandscapeManager.MapWidth;
        }

        if (viewedChunkCoord.y < 0) {
            wrappedYCoord = (viewedChunkCoord.y % LandscapeManager.MapHeight + LandscapeManager.MapHeight) % LandscapeManager.MapHeight;
        } else if (viewedChunkCoord.y >= LandscapeManager.MapHeight) {
            wrappedYCoord = viewedChunkCoord.y % LandscapeManager.MapHeight;
        }
                
        return new int2(wrappedXCoord, wrappedYCoord);
    }

	void UpdateCulledChunks()
	{
		var viewerForward = Viewer.ForwardV2.normalized;
		foreach (var chunk in SurroundTerrainChunks)
		{
			CullChunkAndSetVisibility(chunk, chunk.IsCulled(viewerForward));
		}
	}
	
	private static void CullChunkAndSetVisibility(TerrainChunk chunk, bool isCulled, bool inDistance = true)
	{
		var visible = inDistance;
		if (LandscapeManager.Instance.culling == CullingMode.Layer)
		{
			chunk.GameObject.layer = isCulled? 
				LayerMask.NameToLayer("Culled") : 
				LayerMask.NameToLayer("Default");
		}
		else if(LandscapeManager.Instance.culling == CullingMode.Visibility)
		{
			visible = !isCulled && inDistance;
		}
		
		chunk.SetVisible(visible);
	}

	public class TerrainChunk
	{
		public const int Resolution = 241;
		public GameObject GameObject { get; }
		public Transform Transform { get; private set; }
		public Vector3 Position { get; private set; }
		public MapData MapData { get; private set; }
		public int2 Coord => _wrappedCoord;
		
		public static Material Material;
		private Vector3 _positionV3;
		private readonly LODMesh[] _lodMeshes;

		private int2 _coord;
		private readonly int2 _wrappedCoord;
		private LOD[] _lods;
		public static readonly float WorldSize = (Resolution - 1) * LandscapeManager.Scale;
		private int _lodIndex = -1;
		private Biome _biome;
		private readonly LODMesh _colliderMesh;
		private readonly MeshFilter _meshFilter;
		private readonly MeshCollider _meshCollider;
		

		public TerrainChunk(int2 coord)
		{
			CompleteMeshGenerationEvent += CompleteMeshGeneration;
			
			GameObject = new GameObject("TerrainChunk");
			_coord = coord;
			_wrappedCoord = coord;
			Position = new Vector3(_wrappedCoord.x, 0, _wrappedCoord.y) * (Resolution - 1);

			_biome = BiomesManager.GetBiome(_wrappedCoord);
			
			var meshRenderer = GameObject.AddComponent<MeshRenderer>();
			meshRenderer.material = Material;

			_meshFilter = GameObject.AddComponent<MeshFilter>();

			_meshCollider = GameObject.AddComponent<MeshCollider>();
			
			Transform = GameObject.transform;
			Transform.parent = LandscapeManager.Instance.Transform;
			
			_lodMeshes = new LODMesh[_detailLevels.Length];
			for (int i = 0; i < _detailLevels.Length; i++) {
				_lodMeshes[i] = new LODMesh(_detailLevels[i].lod, this);
				if (_detailLevels[i].useForCollider) {
					_colliderMesh = _lodMeshes[i];
				}
			}

			MapData = LandscapeManager.Maps[_coord.x, _coord.y];
		}

		public void Update()
		{
			var chunksFromViewer = Viewer.ChunkCoord - _coord;
			chunksFromViewer = new int2(Mathf.Abs(chunksFromViewer.x), Mathf.Abs(chunksFromViewer.y));
			bool inDistance = chunksFromViewer.x <= _chunksVisibleInViewDst && 
			                  chunksFromViewer.y <= _chunksVisibleInViewDst;
			
			if (inDistance)
			{
				int lodIndex = 0;

				for (int i = 0; i < _detailLevels.Length - 1; i++)
				{
					if(i>0) _detailLevels[i].visibleChunksThreshold += _detailLevels[i - 1].visibleChunksThreshold;
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

			CullChunkAndSetVisibility(this, IsCulled(Viewer.ForwardV2.normalized), inDistance);
			
		}

		public bool IsCulled(Vector2 viewerForward)
		{
			if (_coord.Equals(Viewer.ChunkCoord)) return false;

			Vector2 chunkCenter = new Vector2(_positionV3.x, _positionV3.z);
			chunkCenter += chunkCenter.normalized * (WorldSize / 2);
			Vector2 chunkDirection = (chunkCenter - Viewer.PositionV2).normalized;
			float dot = Vector2.Dot(viewerForward, chunkDirection);
			float chunkAngle = Mathf.Acos(dot) * Mathf.Rad2Deg;

			return dot < 0 && chunkAngle > Viewer.FOV;
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

		public bool IsVisible() {
			return GameObject.activeSelf;
		}

		public void SetChunkCoord(int2 viewedChunkCoord)
		{
			_coord = viewedChunkCoord;
			_positionV3 = new Vector3(viewedChunkCoord.x,0,viewedChunkCoord.y) * WorldSize;
			Transform.position = _positionV3;
		}

		public static void InitializeMaterial()
		{
			string materialPath = "Assets/Materials/LatitudeVisualizer.mat";
			Material = (Material)AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material));
			
			Material.SetColor("_Color 1", new Color(0, 0, 0, 1));
			Material.SetColor("_Color2", new Color(0.173f, 0.357f, 0.863f, 1));
			Material.SetColor("_Color3", new Color(0.592f, 0.941f, 1f, 1));
			Material.SetColor("_Color4", new Color(0.329f, 0.941f, 0.761f, 1));
			Material.SetColor("_Color5", new Color(0.208f, 0.839f, 0.125f, 1));
			Material.SetColor("_Color6", new Color(0.580f, 0.957f, 0.243f, 1));
			Material.SetColor("_Color7", new Color(0.957f, 0.831f, 0.243f, 1));
			Material.SetColor("_Color8", new Color(0.945f, 0.588f, 0.133f, 1));
			Material.SetColor("_Color9", Color.red);
			
			
			Material.SetFloat("_ChunkResolution", TerrainChunk.Resolution);
			Material.SetFloat("_MapWidth", LandscapeManager.MapWidth);
			Material.SetFloat("_MapHeight", LandscapeManager.MapHeight);
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
			_meshData = new MeshData(TerrainChunk.Resolution, _lod);
			var resolution = (TerrainChunk.Resolution - 1) / _meshData.LODScale + 1;
			var terrainParams = new TerrainParameters(LandscapeManager.Instance.noiseData.parameters,
				LandscapeManager.Instance.terrainData.parameters);
			_meshJobHandle = MeshGenerator.ScheduleMeshGenerationJob(terrainParams, resolution, _chunk.MapData, ref _meshData);
			RequestedMesh = true;
		}

		public void CompleteMeshGeneration()
		{
			_meshJobHandle.Complete(); 
			NormalizeUVToWorldScale(_chunk.Coord);
			SetMesh();
		}

		private void NormalizeUVToWorldScale(float2 coords)
		{
			var uvNormalizer = new Vector2(TerrainChunk.Resolution * coords.x / LandscapeManager.MapWidth,
				coords.y / LandscapeManager.MapHeight);
			for (int i = 0; i < _meshData.UVs.Length; i++)
			{
				var x = i % TerrainChunk.Resolution;
				var y = i / TerrainChunk.Resolution;
				_meshData.UVs[i] =
					new Vector2(
						(TerrainChunk.Resolution * coords.x + x) /
						(LandscapeManager.MapWidth * TerrainChunk.Resolution),
						(TerrainChunk.Resolution * coords.y + y) /
						(LandscapeManager.MapWidth * TerrainChunk.Resolution));
			}
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

		public LODInfo(int lod, int visibleChunksThreshold, bool useForCollider)
		{
			this.lod = lod;
			this.visibleChunksThreshold = visibleChunksThreshold;
			this.useForCollider = useForCollider;
		}
	}
}