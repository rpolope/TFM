using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class TerrainChunksManager{
	
	private const float ViewerMoveThresholdForChunkUpdate = (TerrainChunk.Resolution - 1) * 0.25f;
	private const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;
	private static int _chunksVisibleInViewDst = 4;
	private static readonly Dictionary<int2, TerrainChunk> TerrainChunkDictionary = new Dictionary<int2, TerrainChunk>();
	private static readonly HashSet<TerrainChunk> TerrainChunksVisibleLastUpdate = new HashSet<TerrainChunk>();
	private static readonly List<TerrainChunk> SurroundTerrainChunks = new List<TerrainChunk>();
	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;
	
	public static event Action CompleteMeshGenerationEvent;
	public static int ChunksVisibleInViewDist => _chunksVisibleInViewDst;
	private static LODInfo[] _detailLevels;
	private static int _wrapCountX;
	private static int _wrapCountY;
	public void Initialize()
	{
		// LandscapeManager.Instance.textureData.ApplyToMaterial (TerrainChunk.Material);
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
        
        float offset = TerrainChunk.WorldSize / 2f;

        int currentChunkCoordX = (int)((Viewer.PositionV2.x + offset) / TerrainChunk.WorldSize);
        int currentChunkCoordY = (int)((Viewer.PositionV2.y + offset) / TerrainChunk.WorldSize);

        
        if (!new int2(currentChunkCoordX, currentChunkCoordY).Equals(Viewer.ChunkCoord))
			Debug.Log($"Chunk Actual: {Viewer.ChunkCoord}");
        
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
		public const int Resolution = 233;
		public GameObject GameObject { get; }
		public Transform Transform { get; private set; }
		public Vector3 Position { get; private set; }
		public Vector3 WorldPos => new Vector3(_coord.x, 0, _coord.y) * WorldSize;
		public MapData MapData { get; private set; }
		public int2 Coord => _wrappedCoord;
		public Biome Biome { get; }

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
		private bool _objectsPlaced = false;
		private bool _objectsVisible = false;

		public TerrainChunk(int2 coord)
		{
			CompleteMeshGenerationEvent += CompleteMeshGeneration;
			
			GameObject = new GameObject("TerrainChunk");
			_coord = coord;
			_wrappedCoord = coord;
			Position = new Vector3(_wrappedCoord.x, 0, _wrappedCoord.y) * (Resolution - 1);

			Biome = BiomesManager.GetBiome(_wrappedCoord);
			
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
			bool inDistance = Viewer.ChunkCoord.Equals(_coord) || chunksFromViewer.x < _chunksVisibleInViewDst && 
			                  chunksFromViewer.y < _chunksVisibleInViewDst;
			
			if (inDistance)
			{
				int lodIndex = 0;

				for (int i = 0; i < _detailLevels.Length - 1; i++)
				{
					if (i > 0) _detailLevels[i].visibleChunksThreshold += _detailLevels[i - 1].visibleChunksThreshold;
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

				ManageObjectsPlacement();
			}
		}

		// Caso 1: Si los objetos están colocados y visibles en el chunk actual del viewer, no hacer nada
		// Caso 2: Si los objetos no están visibles y el viewer no está en el chunk, no hacer nada
		// Caso 3: Si el viewer está en el chunk y los objetos no están colocados, colocarlos y hacerlos visibles
		// Caso 4: Si el viewer está en el chunk, los objetos están colocados pero no visibles, hacerlos visibles
		// Caso 5: Si el viewer no está en el chunk pero los objetos están visibles, ocultarlos

		private void ManageObjectsPlacement()
		{
			if (_objectsPlaced && _coord.Equals(Viewer.ChunkCoord) && _objectsVisible) return;

			if (!_coord.Equals(Viewer.ChunkCoord) && !_objectsVisible) return;

			if (_coord.Equals(Viewer.ChunkCoord) && !_objectsPlaced)
			{
				ObjectPlacer.PlaceObjects(this, AssetType.Organic);
				ObjectPlacer.PlaceObjects(this, AssetType.Inorganic);
				_objectsPlaced = true;
				_objectsVisible = true;
				Transform.GetChild(0)?.gameObject.SetActive(true);
			}
			else if (_coord.Equals(Viewer.ChunkCoord) && _objectsPlaced && !_objectsVisible)
			{
				Transform.GetChild(0)?.gameObject.SetActive(true);
				_objectsVisible = true;
			}
			else if (!_coord.Equals(Viewer.ChunkCoord) && _objectsPlaced && _objectsVisible)
			{
				Transform.GetChild(0)?.gameObject.SetActive(false);
				_objectsVisible = false;
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

		private struct BiomeInfo
		{
			public int id;
			public float minTemp;
			public float maxTemp;
			public float minMoist;
			public float maxMoist;

			public BiomeInfo(int id, float minTemp, float maxTemp, float minMoist, float maxMoist)
			{
				this.id = id;
				this.minTemp = minTemp;
				this.maxTemp = maxTemp;
				this.minMoist = minMoist;
				this.maxMoist = maxMoist;
			}
		}

		
		public static void InitializeMaterial()
		{
			var baseTextures = Shader.PropertyToID("baseTextures");
			const string materialPath = "Assets/Materials/TempMoistBased.mat";

			
			const string mockTexturesPath = "Assets/Textures/MockTextures/";
			const string texturesPath = "Assets/Textures/";
			Material = (Material)AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material));
			// return;

			BiomeInfo[] biomesData = new BiomeInfo[]
			{
				new BiomeInfo(0, -30.0f, -10.0f, 0.1f, 0.5f),  // TUNDRA
				new BiomeInfo(1, 0.0f, 10.0f, 0.5f, 1.0f),     // FOREST
				new BiomeInfo(2, 10.0f, 30.0f, 0.33f, 1.0f),   // TROPICAL_FOREST
				new BiomeInfo(3, -30.0f, -10.0f, 0.0f, 0.1f),  // SCORCHED
				new BiomeInfo(4, -10.0f, 0.0f, 0.33f, 0.66f),  // SHRUBLAND
				new BiomeInfo(5, -30.0f, -10.0f, 0.5f, 1.0f),  // SNOW
				new BiomeInfo(6, -30.0f, -10.0f, 0.1f, 0.2f),  // BARE
				new BiomeInfo(7, -10.0f, 0.0f, 0.66f, 1.0f),   // TAIGA
				new BiomeInfo(8, 0.0f, 10.0f, 0.16f, 0.5f),    // GRASSLAND_COLD
				new BiomeInfo(9, 10.0f, 30.0f, 0.16f, 0.33f),  // GRASSLAND_HOT
				new BiomeInfo(10, -10.0f, 0.0f, 0.0f, 0.33f),  // DESERT_COLD
				new BiomeInfo(11, 0.0f, 10.0f, 0.0f, 0.16f),   // DESERT_WARM
				new BiomeInfo(12, 10.0f, 30.0f, 0.0f, 0.16f)   // DESERT_HOT
			};

			
			float[] biomeMinTemp = new float[biomesData.Length];
			float[] biomeMaxTemp = new float[biomesData.Length];
			float[] biomeMinMoist = new float[biomesData.Length];
			float[] biomeMaxMoist = new float[biomesData.Length];

			for (int i = 0; i < biomesData.Length; i++)
			{
				biomeMinTemp[i] = biomesData[i].minTemp;
				biomeMaxTemp[i] = biomesData[i].maxTemp;
				biomeMinMoist[i] = biomesData[i].minMoist;
				biomeMaxMoist[i] = biomesData[i].maxMoist;
			}

			Material.SetFloatArray("biomeMinTemp", biomeMinTemp);
			Material.SetFloatArray("biomeMaxTemp", biomeMaxTemp);
			Material.SetFloatArray("biomeMinMoist", biomeMinMoist);
			Material.SetFloatArray("biomeMaxMoist", biomeMaxMoist);

			var biomesTextures = new Dictionary<ClimateType, Texture2D>()
			{
				{ClimateType.Tundra, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Snow.png", typeof(Texture2D))},
				{ClimateType.Forest, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Rocks 2.png", typeof(Texture2D))},
				{ClimateType.TropicalForest, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Stony ground.png", typeof(Texture2D))},
				{ClimateType.Scorched, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Sandy grass.png", typeof(Texture2D))},
				{ClimateType.Shrubland, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Stony ground.png", typeof(Texture2D))},
				{ClimateType.Snow, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Snow.png", typeof(Texture2D))},
				{ClimateType.Bare, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Rocks 1.png", typeof(Texture2D))},
				{ClimateType.Taiga, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Rocks 2.png", typeof(Texture2D))},
				{ClimateType.Grassland, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Grass.png", typeof(Texture2D))},
				{ClimateType.GrasslandHot, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Grass.png", typeof(Texture2D))},
				{ClimateType.Desert, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Sandy grass.png", typeof(Texture2D))},
				{ClimateType.DesertWarm, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Sandy grass.png", typeof(Texture2D))},
				{ClimateType.DesertHot, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Sandy grass.png", typeof(Texture2D))}
			};
			
			var mockBiomesTextures = new Dictionary<ClimateType, Texture2D>()
			{
				{ClimateType.Tundra, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "tundra_texture.png", typeof(Texture2D))},
				{ClimateType.Forest, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "forest_texture.png", typeof(Texture2D))},
				{ClimateType.TropicalForest, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "tropical_forest_texture_2.png", typeof(Texture2D))},
				{ClimateType.Scorched, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "scorched_texture.png", typeof(Texture2D))},
				{ClimateType.Shrubland, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "shrubland_texture.png", typeof(Texture2D))},
				{ClimateType.Snow, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "snow_texture.png", typeof(Texture2D))},
				{ClimateType.Bare, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "bare_texture.png", typeof(Texture2D))},
				{ClimateType.Taiga, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "taiga_texture.png", typeof(Texture2D))},
				{ClimateType.Grassland, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "grassland_texture.png", typeof(Texture2D))},
				{ClimateType.GrasslandHot, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "tropical_forest_texture_2.png", typeof(Texture2D))},
				{ClimateType.Desert, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "desert_texture_1.png", typeof(Texture2D))},
				{ClimateType.DesertWarm, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "desert_texture_2.png", typeof(Texture2D))},
				{ClimateType.DesertHot, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "desert_texture_2.png", typeof(Texture2D))}
			};

			Material.SetTexture (baseTextures, GenerateTextureArray (biomesTextures.Values.ToArray()));
			Material.SetFloat("_WaterHeight", 1f);
			Material.SetFloat("_SnowHeight", 40f);
			Material.SetFloat("_MaxHeight", LandscapeManager.Instance.terrainData.MaxHeight);
		}
		private static Texture2DArray GenerateTextureArray(Texture2D[] textures) {
			
			const int textureSize = 512;
			const TextureFormat textureFormat = TextureFormat.RGB565;

			Texture2DArray textureArray = new Texture2DArray (textureSize, textureSize, textures.Length, textureFormat, true);
			for (int i = 0; i < textures.Length; i++) {
				textureArray.SetPixels (textures [i].GetPixels (), i);
			}
			textureArray.Apply ();
			return textureArray;
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