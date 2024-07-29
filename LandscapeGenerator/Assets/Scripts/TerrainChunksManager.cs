using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class TerrainChunksManager : MonoBehaviour{
	private static readonly float ViewerMoveThresholdForChunkUpdate = TerrainChunk.WorldSize * 0.5f;
	public static readonly float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;
	public static readonly float ViewerRotateThresholdForChunkUpdate = 5f;
	private static readonly Dictionary<int2, TerrainChunk> TerrainChunkDictionary = new Dictionary<int2, TerrainChunk>();
	private static readonly HashSet<TerrainChunk> TerrainChunksVisibleLastUpdate = new HashSet<TerrainChunk>();
	private static readonly List<TerrainChunk> SurroundTerrainChunks = new List<TerrainChunk>();
	private static readonly Queue<IEnumerator> MeshGenerationQueue = new Queue<IEnumerator>();
	
	private const int MaxConcurrentMeshCoroutines = 4;
	private static int ChunksVisibleInViewDist { get; set; } = 2;

	private static LODInfo[] _detailLevels;
	private static int _wrapCountX;
	private static int _wrapCountY;
	private static int _activeCoroutines = 0;

	public void Initialize()
	{
		_detailLevels = new [] {
			new LODInfo(0, 2, false),
			new LODInfo(1, 1, false),
			new LODInfo(2, 1, true)
		};
		
		ChunksVisibleInViewDist = 0;
		foreach (var detailLevel in _detailLevels)
		{
			ChunksVisibleInViewDist += detailLevel.visibleChunksThreshold;
		}

		UpdateVisibleChunks();
	}


	public void Update() {
		
		
		if (Viewer.PositionChanged()) {
			Viewer.UpdateOldPosition();
			UpdateVisibleChunks();
			UpdateVisibleObjects();
		}

		if (Viewer.RotationChanged())
		{
			Viewer.UpdateOldRotation();
			UpdateCulledChunks();
			UpdateCulledObjects();
		}
	}

	private void UpdateCulledObjects()
	{
		// foreach (var chunk in SurroundTerrainChunks.Where(chunk => chunk.LODIndex == 0))
		// {
		// 	chunk.ManageObjects();
		// }
	}

	private void UpdateVisibleObjects()
	{
		foreach (var chunk in TerrainChunksVisibleLastUpdate.Where(chunk => chunk.LODIndex == 0))
		{
			chunk.PlaceObjects();
		}
	}

	private void UpdateVisibleChunks() {
        foreach (var visibleChunk in TerrainChunksVisibleLastUpdate) {
            visibleChunk.SetVisible(false);
        }
        
        TerrainChunksVisibleLastUpdate.Clear();
        SurroundTerrainChunks.Clear();

        Viewer.UpdateChunkCoord();
        UpdateWrapCount(Viewer.ChunkCoord.x, Viewer.ChunkCoord.y);

        for (int yOffset = -ChunksVisibleInViewDist; yOffset <= ChunksVisibleInViewDist; yOffset++) {
            for (int xOffset = -ChunksVisibleInViewDist; xOffset <= ChunksVisibleInViewDist; xOffset++) {
                
	            var viewedChunkCoord = new int2(Viewer.ChunkCoord.x + xOffset,  Viewer.ChunkCoord.y + yOffset);
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
			UpdateVisibility(chunk, chunk.IsCulled(viewerForward));
		}
	}
	
	private static void UpdateVisibility(TerrainChunk chunk, bool isCulled, bool inDistance = true)
	{
		var visible = !isCulled && inDistance;
		
		
		chunk.GameObject.layer = !visible ? LayerMask.NameToLayer("Culled") : LayerMask.NameToLayer("Default");
		chunk._water.GameObject.layer = !visible ? LayerMask.NameToLayer("Culled") : LayerMask.NameToLayer("Water");

		chunk.UpdateObjectsVisibility(visible);
		chunk.SetColliderEnable(visible && chunk.LODIndex == 0);
	}
	
	public class TerrainChunk
	{
		public const int Resolution = 105;
		public GameObject GameObject { get; }
		public Transform Transform { get; private set; }
		public Vector3 Position { get; private set; }
		public Vector3 WorldPos => new Vector3(_coord.x, 0, _coord.y) * WorldSize;
		public MapData MapData { get; private set; }
		public int2 Coord => _wrappedCoord;
		public Biome Biome { get; }
		public int LODIndex { get; private set; } = -1;
		public Bounds Bounds;
		public static Material Material;

		private Vector3 _positionV3;
		private readonly LODMesh[] _lodMeshes;

		private int2 _coord;
		private readonly int2 _wrappedCoord;
		private LOD[] _lods;
		public const float WorldSize = (Resolution - 1) * LandscapeManager.Scale;
		private Biome _biome;
		private readonly LODMesh _colliderMesh;
		private readonly MeshFilter _meshFilter;
		private readonly MeshCollider _meshCollider;
		internal bool ObjectsPlaced = false;
		internal bool ObjectsVisible = false;
		internal Water _water;
		public List<GameObject> InstantiatedGameObjects;

		public TerrainChunk(int2 coord)
		{
			GameObject = new GameObject("TerrainChunk");
			_coord = coord;
			_wrappedCoord = coord;
			Position = new Vector3(_wrappedCoord.x, 0, _wrappedCoord.y) * (Resolution - 1);
			Bounds = new Bounds(WorldPos, new Vector3(WorldSize, 1, WorldSize));
			Biome = BiomesManager.GetBiome(_wrappedCoord);

			var meshRenderer = GameObject.AddComponent<MeshRenderer>();
			meshRenderer.material = Material;

			_meshFilter = GameObject.AddComponent<MeshFilter>();

			_meshCollider = GameObject.AddComponent<MeshCollider>();

			Transform = GameObject.transform;
			Transform.parent = LandscapeManager.Instance.Transform;

			_lodMeshes = new LODMesh[_detailLevels.Length];
			for (int i = 0; i < _detailLevels.Length; i++)
			{
				_lodMeshes[i] = new LODMesh(_detailLevels[i].lod, this);
				if (_detailLevels[i].useForCollider)
				{
					_colliderMesh = _lodMeshes[i];
				}
			}
			
			MapData = LandscapeManager.Maps[_coord.x, _coord.y];

			// _water = UnityEngine.Random.value >= Biome.GetWaterProbability();
			
			_water = new Water(
				Transform,
				WorldSize
			);

			InstantiatedGameObjects = new List<GameObject>();
		}

		private int2 CalculateDistanceFromViewer()
		{
			var chunksFromViewer = Viewer.ChunkCoord - _coord;
			return new int2(Mathf.Abs(chunksFromViewer.x), Mathf.Abs(chunksFromViewer.y));
		}

		private int GetLODFromDistance(int2 chunksFromViewer)
		{
			int lodIndex = 0;
			var lodThreshold = _detailLevels[0].visibleChunksThreshold;
			
			for (int i = 0; i < _detailLevels.Length; i++)
			{
				if (i > 0) lodThreshold += _detailLevels[i - 1].visibleChunksThreshold;
				
				if (chunksFromViewer.x < lodThreshold && 
				    chunksFromViewer.y < lodThreshold)
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
			bool inDistance = chunksFromViewer.x <= ChunksVisibleInViewDist && 
			                  chunksFromViewer.y <= ChunksVisibleInViewDist;
			
			if (inDistance)
			{
				int lodIndex = GetLODFromDistance(chunksFromViewer);
				
				if (lodIndex != LODIndex)
				{
					var lodMesh = _lodMeshes[lodIndex];
					LODIndex = lodIndex;

					if (lodMesh.HasMesh)
					{
						_meshFilter.mesh = lodMesh.Mesh;
						if (lodIndex == 0)
						{
							_meshCollider.sharedMesh = _colliderMesh.Mesh;
						}
					}
					else
					{
						lodMesh.RequestMesh(LandscapeManager.Instance.ChunksManager);
					}
				}
				
				if (LODIndex == 0 )
				{
					if (_colliderMesh.HasMesh)
					{
						_meshCollider.sharedMesh = _colliderMesh.Mesh;
					}
					else
					{
						_colliderMesh.RequestMesh(LandscapeManager.Instance.ChunksManager);
					}
				}
			}

			UpdateVisibility(this, IsCulled(Viewer.ForwardV2.normalized), inDistance);
		}

		public bool IsCulled(Vector2 viewerForward)
		{
			if (_coord.Equals(Viewer.ChunkCoord)) return false;

			Vector2 chunkCenter = new Vector2(_positionV3.x, _positionV3.z);
			chunkCenter += chunkCenter.normalized * (WorldSize * 0.5f);
			Vector2 chunkDirection = (chunkCenter - Viewer.PositionV2).normalized;
			float dot = Vector2.Dot(viewerForward, chunkDirection);
			float chunkAngle = Mathf.Acos(dot) * Mathf.Rad2Deg;

			return dot < 0 && chunkAngle > Viewer.ExtendedFOV;
		}

		public void SetVisible(bool visible)
		{
			switch (LandscapeManager.Instance.culling)
			{
				case CullingMode.Layer:
					GameObject.layer = visible ? LayerMask.NameToLayer("Default") :
												 LayerMask.NameToLayer("Culled");
					break;
				case CullingMode.Visibility:
				default:
					SetActive(visible);
					break;
			}

			SetColliderEnable(visible);
		}
		
		
		public void UpdateObjectsVisibility(bool visible)
		{
			if (visible) return;

			foreach (var gameObject in InstantiatedGameObjects)
			{
				BiomesAssetsManager.DespawnAsset(gameObject);
			}
		}

		public void SetActive(bool visible) {
			
			GameObject.SetActive (visible);
		}

		public void SetColliderEnable(bool enable)
		{
			_meshCollider.enabled = enable;
			if(!ObjectsPlaced )
				_water.BoxCollider.enabled = enable;
		}

		internal void CompleteMeshGeneration()
		{
			if (LODIndex < 0)
			{
				LODIndex = GetLODFromDistance(CalculateDistanceFromViewer());
			}
			var lodMesh = _lodMeshes[LODIndex];
			if (lodMesh.RequestedMesh)
			{
				lodMesh.CompleteMeshGeneration();
				_meshFilter.mesh = lodMesh.Mesh;

				if (LODIndex == 0 && _colliderMesh.RequestedMesh)
				{
					_colliderMesh.CompleteMeshGeneration();
					_meshCollider.sharedMesh = _colliderMesh.Mesh;
				}
			}
			
			PlaceObjects();
		}
		public void PlaceObjects()
		{
			if (LODIndex == 0 && !ObjectsPlaced)
				LandscapeManager.Instance.StartCoroutine(
					ObjectPlacer.PlaceObjectsCoroutine(this));
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

		
		public static void InitializeMaterial(TerrainData terrainData = default)
		{
			var baseTextures = Shader.PropertyToID("groundTextures");
			
			const string materialPath = "Assets/Materials/TmpMoistBasedBiomes.mat";

			
			const string mockTexturesPath = "Assets/Textures/MockTextures/";
			const string debugTexturesPath = "Assets/Textures/Debugging/";
			const string texturesPath = "Assets/Textures/Biomes/Ground/Size-512px/";
			
			Material = (Material)AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material));
			
			Material.EnableKeyword("_NORMALMAP");
			Material.SetTexture ("_BumpMap", (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Textures/Biomes/Normals/" + "Normal_Map.jpg", typeof(Texture2D)));

			var biomesData = BiomesManager.BiomesData;
			
			var biomeMinTemp = new float[biomesData.Length];
			var biomeMaxTemp = new float[biomesData.Length];
			var biomeMinMoist = new float[biomesData.Length];
			var biomeMaxMoist = new float[biomesData.Length];

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

			// TODO: Correct textures
			var groundTexturesDictionary = new Dictionary<BiomeType, Texture2D>()
			{
				{ BiomeType.Snow, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Ice_Ground_Large.jpg", typeof(Texture2D)) },
				{ BiomeType.Tundra, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Rocks 1.png", typeof(Texture2D)) },
				{ BiomeType.BorealForest, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Sandy grass.png", typeof(Texture2D)) },
				{ BiomeType.TemperateConiferousForest, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Grasslands_Ground_Small.jpg", typeof(Texture2D)) },
				{ BiomeType.TemperateSeasonalForest, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Sandy grass.png", typeof(Texture2D)) },
				{ BiomeType.TropicalSeasonalForestSavanna, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Grasslands_Ground_Large.jpg", typeof(Texture2D)) },
				{ BiomeType.TropicalRainforest, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Grass.png", typeof(Texture2D)) },
				{ BiomeType.WoodlandShrubland, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Desert_Ground_Small.jpg", typeof(Texture2D)) },
				{ BiomeType.TemperateGrasslandColdDesert, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Desert_Shore_Large.jpg", typeof(Texture2D)) },
				{ BiomeType.SubtropicalDesert, (Texture2D)AssetDatabase.LoadAssetAtPath(texturesPath + "Rocks 2.png", typeof(Texture2D)) }
			}; 
			
			var mockBiomesTextures = new Dictionary<BiomeType, Texture2D>()
			{
				{ BiomeType.Snow, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "snow_texture.png", typeof(Texture2D)) },
				{ BiomeType.Tundra, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "tundra_texture.png", typeof(Texture2D)) },
				{ BiomeType.BorealForest, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "tropical_forest_1.png", typeof(Texture2D)) },
				{ BiomeType.TemperateConiferousForest, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "forest_texture.png", typeof(Texture2D)) },
				{ BiomeType.TemperateSeasonalForest, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "tropical_forest_texture_2.png", typeof(Texture2D)) },
				{ BiomeType.TropicalSeasonalForestSavanna, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "desert_texture_1.png", typeof(Texture2D)) },
				{ BiomeType.TropicalRainforest, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "tropical_forest_texture_2.png", typeof(Texture2D)) },
				{ BiomeType.WoodlandShrubland, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "shrubland_texture.png", typeof(Texture2D)) },
				{ BiomeType.TemperateGrasslandColdDesert, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "desert_texture_1.png", typeof(Texture2D)) },
				{ BiomeType.SubtropicalDesert, (Texture2D)AssetDatabase.LoadAssetAtPath(mockTexturesPath + "desert_texture_2.png", typeof(Texture2D)) }
			};
			
			var debugBiomesTextures = new Dictionary<BiomeType, Texture2D>()
			{
				{ BiomeType.Snow, (Texture2D)AssetDatabase.LoadAssetAtPath(debugTexturesPath + "SNOW.png", typeof(Texture2D)) },
				{ BiomeType.Tundra, (Texture2D)AssetDatabase.LoadAssetAtPath(debugTexturesPath + "TUNDRA.png", typeof(Texture2D)) },
				{ BiomeType.BorealForest, (Texture2D)AssetDatabase.LoadAssetAtPath(debugTexturesPath + "BOREAL_FOREST.png", typeof(Texture2D)) },
				{ BiomeType.TemperateConiferousForest, (Texture2D)AssetDatabase.LoadAssetAtPath(debugTexturesPath + "CONIFEROUS_FOREST.png", typeof(Texture2D)) },
				{ BiomeType.TemperateSeasonalForest, (Texture2D)AssetDatabase.LoadAssetAtPath(debugTexturesPath + "SEASONAL_FOREST.png", typeof(Texture2D)) },
				{ BiomeType.TropicalSeasonalForestSavanna, (Texture2D)AssetDatabase.LoadAssetAtPath(debugTexturesPath + "SAVANNA.png", typeof(Texture2D)) },
				{ BiomeType.TropicalRainforest, (Texture2D)AssetDatabase.LoadAssetAtPath(debugTexturesPath + "TROPICAL_RAINFOREST.png", typeof(Texture2D)) },
				{ BiomeType.WoodlandShrubland, (Texture2D)AssetDatabase.LoadAssetAtPath(debugTexturesPath + "SHRUBLAND.png", typeof(Texture2D)) },
				{ BiomeType.TemperateGrasslandColdDesert, (Texture2D)AssetDatabase.LoadAssetAtPath(debugTexturesPath + "COLD_DESERT.png", typeof(Texture2D)) },
				{ BiomeType.SubtropicalDesert, (Texture2D)AssetDatabase.LoadAssetAtPath(debugTexturesPath + "SUBTROPICAL_DESERT.png", typeof(Texture2D)) }
			};
			
			Material.SetTexture (baseTextures, GenerateTextureArray (groundTexturesDictionary.Values.ToArray()));
			
			Material.SetFloat("_WaterLevel", terrainData is not null ? terrainData.parameters.waterLevel : Water.HeightLevel);
			Material.SetFloat("_MaxHeight", terrainData is not null ? terrainData.MaxHeight : LandscapeManager.Instance.terrainData.MaxHeight);
		}
		private static Texture2DArray GenerateTextureArray(Texture2D[] textures) {
			
			const int textureSize = 512;
			const TextureFormat textureFormat = TextureFormat.RGB565;

			var textureArray = new Texture2DArray (textureSize, textureSize, textures.Length, textureFormat, true);
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
		
		public void RequestMesh(TerrainChunksManager terrainChunksManager) {
			RequestedMesh = true;
			terrainChunksManager.StartCoroutine(RequestMeshCoroutine());	
		}

		private IEnumerator RequestMeshCoroutine() {

			MeshGenerationQueue.Enqueue(GenerateMeshDataCoroutine());
			yield return null;

			while (MeshGenerationQueue.Count > 0) {
				if (_activeCoroutines < MaxConcurrentMeshCoroutines) {
					var generateMeshDataCoroutine = MeshGenerationQueue.Dequeue();
					LandscapeManager.Instance.StartCoroutine(generateMeshDataCoroutine);
				}
				yield return null;
			}
		}
		
		private IEnumerator GenerateMeshDataCoroutine() {
			_activeCoroutines++;
			_meshData = new MeshData(TerrainChunk.Resolution, _lod);
			var resolution = (TerrainChunk.Resolution - 1) / _meshData.LODScale + 1;
			var terrainParams = new TerrainParameters(LandscapeManager.Instance.noiseData.parameters,
				LandscapeManager.Instance.terrainData.parameters);
			_meshJobHandle = MeshGenerator.ScheduleMeshGenerationJob(terrainParams, resolution, _chunk.Coord, _chunk.MapData, ref _meshData);

			while (!_meshJobHandle.IsCompleted) {
				yield return null;
			}

			_chunk.CompleteMeshGeneration();
			_activeCoroutines--;
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

		public LODInfo(int lod, int visibleChunksThreshold, bool useForCollider)
		{
			this.lod = lod;
			this.visibleChunksThreshold = visibleChunksThreshold;
			this.useForCollider = useForCollider;
		}
	}
}