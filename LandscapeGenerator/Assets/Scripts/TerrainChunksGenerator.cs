using System.Collections.Generic;
using UnityEngine;

public class TerrainChunksGenerator : MonoBehaviour
{
    
    const float Scale = 5f;

    const float ViewerMoveThresholdForChunkUpdate = 25f;
    const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;

    public static float MaxViewDst;

    public Transform viewer;

    public static Vector2 ViewerPosition;
    Vector2 _viewerPositionOld;
    public IMapGenerator MapGenerator;
    // static MapGenerator _mapGenerator;
    int _chunkSize;
    int _chunksVisibleInViewDst;
    
    Dictionary<Vector2, TerrainChunk> _terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> _terrainChunksVisibleLastUpdate = new List<TerrainChunk>();
    
    void Start()
    {
        MaxViewDst = LandscapeManager.Instance.viewThreshold;
        MapGenerator = new NoiseGenerator();
        _chunkSize = LandscapeManager.Instance.TerrainChunkSize;
        _chunksVisibleInViewDst = Mathf.RoundToInt(MaxViewDst / _chunkSize);

        UpdateVisibleChunks ();
    }

    private void UpdateVisibleChunks()
    {
        for (int i = 0; i < _terrainChunksVisibleLastUpdate.Count; i++) {
            _terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        _terrainChunksVisibleLastUpdate.Clear ();
			
        int currentChunkCoordX = Mathf.RoundToInt (ViewerPosition.x / _chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt (ViewerPosition.y / _chunkSize);

        for (int yOffset = -_chunksVisibleInViewDst; yOffset <= _chunksVisibleInViewDst; yOffset++) {
            for (int xOffset = -_chunksVisibleInViewDst; xOffset <= _chunksVisibleInViewDst; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (_terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
                    _terrainChunkDictionary[viewedChunkCoord].Update();
                    if (_terrainChunkDictionary[viewedChunkCoord].IsVisible())
                    {
                        _terrainChunksVisibleLastUpdate.Add(_terrainChunkDictionary[viewedChunkCoord]);
                    }
                } else {
                    _terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk (viewedChunkCoord, _chunkSize, MapGenerator.GenerateMap(_chunkSize)));
                }
            }
        }
    }

    void Update() {
        ViewerPosition = new Vector2 (viewer.position.x, viewer.position.z) / Scale;

        if ((_viewerPositionOld - ViewerPosition).sqrMagnitude > SqrViewerMoveThresholdForChunkUpdate) {
            _viewerPositionOld = ViewerPosition;
            UpdateVisibleChunks();
        }
    }

    
    public class TerrainChunk
    {
        private Mesh _mesh;
        private Vector2 _position;
        private GameObject _terrainChunkObject;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        private Material _material;
        private static Transform _parent;
    
        public TerrainChunk(Vector2 position, int size, float[] heightMap)
        {
            _position = position * size;
            _material = new Material(Shader.Find("Standard"));
            _mesh = MeshGenerator.GenerateMesh(size, heightMap);
            _material.mainTexture = MapVisualizer.Instance.GetTextureFromMap(heightMap);
            _parent = GameObject.Find("TerrainChunksGenerator").transform;
            
            Instantiate();
        }
    
        public void Instantiate()
        {
            _terrainChunkObject = new GameObject("TerrainChunk");
            _terrainChunkObject.transform.parent = _parent;
            _meshFilter = _terrainChunkObject.AddComponent<MeshFilter>();
            _meshRenderer = _terrainChunkObject.AddComponent<MeshRenderer>();
            _meshCollider = _terrainChunkObject.AddComponent<MeshCollider>();
            _terrainChunkObject.transform.localScale = Scale * Vector3.one;
            _terrainChunkObject.transform.position = Scale * new Vector3(_position.x, 0, _position.y);
            _meshFilter.sharedMesh= _mesh;
            _meshCollider.sharedMesh = _mesh;
            _meshRenderer.sharedMaterial = _material;
        }
        
        public void Update() {
           
            float viewerDst = (TerrainChunksGenerator.ViewerPosition - _position).sqrMagnitude;
            bool visible = viewerDst <= (LandscapeManager.Instance.viewThreshold * LandscapeManager.Instance.viewThreshold);
    
            if (visible) {
                _terrainChunksVisibleLastUpdate.Add (this);
            }
    
            SetVisible (visible);
        }
        public void SetVisible(bool visible)
        {
            _terrainChunkObject.SetActive (visible);
        }
        
        public bool IsVisible()
        {
            return _terrainChunkObject.activeInHierarchy;
        }
    }
}


