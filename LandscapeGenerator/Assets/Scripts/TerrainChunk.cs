using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class TerrainChunk
	{
		public const int Resolution = 241;
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
		private readonly LODInfo[] _detailLevels;

		private int2 _coord;
		private readonly int2 _wrappedCoord;
		private LOD[] _lods;
		public static readonly float WorldSize = (Resolution - 1) * LandscapeManager.Scale;
		private int _lodIndex = -1;
		private Biome _biome;
		private readonly LODMesh _colliderMesh;
		private readonly MeshFilter _meshFilter;
		private readonly MeshCollider _meshCollider;
		private bool _isBackup;
		private bool _objectsPlaced = false;
		private bool _objectsVisible = false;
		private static bool _water;

		public TerrainChunk(int2 coord, LODInfo[] detailLevels, bool isBackup)
		{
			GameObject = new GameObject("TerrainChunk");
			_coord = coord;
			_wrappedCoord = coord;
			Position = new Vector3(_wrappedCoord.x, 0, _wrappedCoord.y) * (Resolution - 1);

			// Biome = BiomesManager.GetBiome(_wrappedCoord);

			var meshRenderer = GameObject.AddComponent<MeshRenderer>();
			meshRenderer.material = Material;

			_meshFilter = GameObject.AddComponent<MeshFilter>();

			_meshCollider = GameObject.AddComponent<MeshCollider>();

			Transform = GameObject.transform;
			Transform.parent = LandscapeManager.Instance.Transform;
			_detailLevels = detailLevels;
			_lodMeshes = new LODMesh[detailLevels.Length];
			for (int i = 0; i < detailLevels.Length; i++)
			{
				_lodMeshes[i] = new LODMesh(detailLevels[i].lod, this);
				if (detailLevels[i].useForCollider)
				{
					_colliderMesh = _lodMeshes[i];
				}
			}

			MapData = LandscapeManager.Maps[_coord.x, _coord.y];

			// _water = UnityEngine.Random.value >= Biome.GetWaterProbability();
			
			Water.Instantiate(
				LandscapeManager.Instance.terrainData.parameters.waterLevel,
				Transform,
				WorldSize
			);

			_isBackup = isBackup;
		}

		private int2 CalculateDistanceFromViewer()
		{
			var chunksFromViewer = Viewer.ChunkCoord - _coord;
			return new int2(Mathf.Abs(chunksFromViewer.x), Mathf.Abs(chunksFromViewer.y));
		}

		private int GetLODFromDistance(int2 chunksFromViewer)
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

			return lodIndex;
		}

		public void Update()
		{
			var chunksFromViewer = CalculateDistanceFromViewer();
			bool inDistance = chunksFromViewer.x < TerrainChunksManager.ChunksVisibleInViewDist && 
			                  chunksFromViewer.y < TerrainChunksManager.ChunksVisibleInViewDist;
			
			if (inDistance)
			{
				int lodIndex = GetLODFromDistance(chunksFromViewer);
				
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
						lodMesh.RequestMeshData(LandscapeManager.Instance.ChunksManager);
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
						_colliderMesh.RequestMeshData(LandscapeManager.Instance.ChunksManager);
					}
				}
				else
				{
					_meshCollider.enabled = false;
				}
			}

			TerrainChunksManager.CullChunkAndSetVisibility(this, IsCulled(Viewer.ForwardV2.normalized), inDistance);
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
				if (_lodIndex < 0)
				{
					_lodIndex = GetLODFromDistance(CalculateDistanceFromViewer());
				}
				var lodMesh = _lodMeshes[_lodIndex];
				if (lodMesh.RequestedMesh)
				{
					lodMesh.CompleteMesh();
					_meshFilter.mesh = lodMesh.Mesh;

					if (_lodIndex == 0 && _colliderMesh.RequestedMesh)
					{
						_colliderMesh.CompleteMesh();
						_meshCollider.sharedMesh = _colliderMesh.Mesh;
					}
				}

				// ManageObjectsPlacement();
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

		
		public static void InitializeMaterial(TerrainData terrainData = default)
		{
			var baseTextures = Shader.PropertyToID("baseTextures");
			const string materialPath = "Assets/Materials/TempMoistBased.mat";

			
			const string mockTexturesPath = "Assets/Textures/MockTextures/";
			const string texturesPath = "Assets/Textures/";
			Material = (Material)AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material));
			
			Material.EnableKeyword("_NORMALMAP");
			Material.SetTexture ("_NormalMap", (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Normal_Map.jpg", typeof(Texture2D)));
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
				{ClimateType.DesertWarm, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Desert_Shore_Large.jpg", typeof(Texture2D))},
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

			Material.SetTexture (baseTextures, GenerateTextureArray (mockBiomesTextures.Values.ToArray()));
			Material.SetFloat("_WaterLevel", terrainData != null ? terrainData.parameters.waterLevel : LandscapeManager.Instance.terrainData.parameters.waterLevel);
			Material.SetFloat("_MaxHeight", terrainData != null ? terrainData.MaxHeight : LandscapeManager.Instance.terrainData.MaxHeight);
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
		
			public void RequestMeshData(TerrainChunksManager terrainChunksManager) {
				terrainChunksManager.StartCoroutine(RequestMeshDataCoroutine());	
			}

			private IEnumerator RequestMeshDataCoroutine() {
				_meshData = new MeshData(TerrainChunk.Resolution, _lod);
				var resolution = (TerrainChunk.Resolution - 1) / _meshData.LODScale + 1;
				var terrainParams = new TerrainParameters(LandscapeManager.Instance.noiseData.parameters,
					LandscapeManager.Instance.terrainData.parameters);
				_meshJobHandle = MeshGenerator.ScheduleMeshGenerationJob(terrainParams, resolution, _chunk.Coord, _chunk.MapData, ref _meshData, false);
				RequestedMesh = true;

				yield return new WaitUntil(() => _meshJobHandle.IsCompleted);
			
				_chunk.CompleteMeshGeneration();
			}

			public void CompleteMesh()
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

			public LODInfo(int lod, int visibleChunksThreshold, bool useForCollider)
			{
				this.lod = lod;
				this.visibleChunksThreshold = visibleChunksThreshold;
				this.useForCollider = useForCollider;
			}
		}
	}

	