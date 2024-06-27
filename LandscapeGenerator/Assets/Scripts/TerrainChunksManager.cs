using System;
using System.Collections.Generic;
using System.Linq;
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
			var baseTextures = Shader.PropertyToID("baseTextures");
			const string materialPath = "Assets/Materials/TempMoistMix.mat";
			const string texturesPath = "Assets/Textures/";
			const string mockTexturesPath = "Assets/Textures/MockTextures/";
			Material = (Material)AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material));
			
			// var biomesTextures = new Dictionary<ClimateType, Texture2D>()
			// {
			// 	{ClimateType.Ocean, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Water.png", typeof(Texture2D))},
			// 	{ClimateType.Beach, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Sandy grass.png", typeof(Texture2D))},
			// 	{ClimateType.TropicalForest, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Stony ground.png", typeof(Texture2D))},
			// 	{ClimateType.Forest, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Rocks 2.png", typeof(Texture2D))},
			// 	{ClimateType.Grassland, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Grass.png", typeof(Texture2D))},
			// 	{ClimateType.Shrubland, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Stony ground.png", typeof(Texture2D))},
			// 	{ClimateType.Taiga, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Rocks 2.png", typeof(Texture2D))},
			// 	{ClimateType.Tundra, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Snow.png", typeof(Texture2D))},
			// 	{ClimateType.Desert, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Sandy grass.png", typeof(Texture2D))},
			// 	{ClimateType.Bare, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Rocks 1.png", typeof(Texture2D))},
			// 	{ClimateType.Scorched, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Sandy grass.png", typeof(Texture2D))},
			// 	{ClimateType.Snow, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Snow.png", typeof(Texture2D))}
			// };
			
			var mockBiomesTextures = new Dictionary<ClimateType, Texture2D>()
			{
				{ClimateType.Ocean, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "ocean_texture.png", typeof(Texture2D))},
				{ClimateType.Beach, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "beach_texture.png", typeof(Texture2D))},
				{ClimateType.TropicalForest, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "tropical_forest_texture_2.png", typeof(Texture2D))},
				{ClimateType.Forest, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "forest_texture.png", typeof(Texture2D))},
				{ClimateType.Grassland, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "grassland_texture.png", typeof(Texture2D))},
				{ClimateType.Shrubland, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "shrubland_texture.png", typeof(Texture2D))},
				{ClimateType.Taiga, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "taiga_texture.png", typeof(Texture2D))},
				{ClimateType.Tundra, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "tundra_texture.png", typeof(Texture2D))},
				{ClimateType.Desert, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "desert_texture_2.png", typeof(Texture2D))},
				{ClimateType.Bare, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "bare_texture.png", typeof(Texture2D))},
				{ClimateType.Scorched, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "scorched_texture.png", typeof(Texture2D))},
				{ClimateType.Snow, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "snow_texture.png", typeof(Texture2D))}
			};


			Material.SetTexture (baseTextures, GenerateTextureArray (mockBiomesTextures.Values.ToArray()));
			Material.SetFloat("_WaterHeight", 1f);
			Material.SetFloat("_SnowHeight", 40f);
			Material.SetFloat("_MaxHeight", LandscapeManager.Instance.terrainData.MaxHeight);
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

		
		public static void InitializeMaterial(BiomeData[] biomes)
		{
			var baseTextures = Shader.PropertyToID("baseTextures");
			const string materialPath = "Assets/Materials/TempMoistMix.mat";
			Material = (Material)AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material));
			
			
			BiomeInfo[] biomesData = new BiomeInfo[]
			{
				new BiomeInfo(0, -30.0f, -10.0f, 0.2f, 0.5f),  // TUNDRA
				new BiomeInfo(1, 0.0f, 10.0f, 0.5f, 1.0f),     // FOREST
				new BiomeInfo(2, 10.0f, 30.0f, 0.33f, 1.0f),   // TROPICAL_FOREST
				new BiomeInfo(3, -30.0f, -10.0f, 0.0f, 0.1f),  // SCORCHED
				new BiomeInfo(4, -10.0f, 0.0f, 0.33f, 0.66f),  // SHRUBLAND
				new BiomeInfo(5, -30.0f, -10.0f, 0.5f, 1.0f),  // SNOW
				new BiomeInfo(6, -30.0f, -10.0f, 0.1f, 0.2f),  // BARE
				new BiomeInfo(7, -10.0f, 0.0f, 0.66f, 1.0f),   // TAIGA
				new BiomeInfo(8, 0.0f, 10.0f, 0.16f, 0.5f),    // GRASSLAND
				new BiomeInfo(9, 10.0f, 30.0f, 0.16f, 0.33f),  // GRASSLAND 
				new BiomeInfo(10, -10.0f, 0.0f, 0.0f, 0.33f),  // DESERT
				new BiomeInfo(11, 0.0f, 10.0f, 0.0f, 0.16f),   // DESERT
				new BiomeInfo(12, 10.0f, 30.0f, 0.0f, 0.16f)   // DESERT
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

			
			// for (int i = 0; i < biomes.Length; i++)
			// {
			// 	biomes[i].SetBiome(Material);
			// }

			Material.SetFloat("_WaterHeight", 1f);
			Material.SetFloat("_SnowHeight", 40f);
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