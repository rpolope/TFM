using UnityEngine;
using UnityEngine.Serialization;

public enum MapMode
{
    NoiseMap,
    ColorMap,
    Mesh
}
// [RequireComponent(typeof(MeshFilter), 
//                  typeof(MeshRenderer))]
public class MapVisualizer : MonoBehaviour
{
    public MapMode mapMode = MapMode.Mesh;
    public IMapGenerator MapGenerator;
    private int _size;
    public static MapVisualizer Instance { get; private set; }
    
    public void VisualizeMap()
    {
        if (MapGenerator is null)
        {
            MapGenerator = new NoiseGenerator();
        }
        
        if (mapMode.Equals(MapMode.NoiseMap))
        {
            Texture2D texture = GetTextureFromMap(MapGenerator.GenerateMap(_size));
            GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
        }
    }


    public Texture2D GetTextureFromMap(float[] map)
    {
        _size = LandscapeManager.Instance.TerrainChunkSize;
        Color[] colors = new Color[map.Length];
        Texture2D texture = new Texture2D(_size, _size);

        for (int i = 0; i < map.Length; i++)
        {
            colors[i] = Color.Lerp(Color.black, Color.white, map[i]);
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        // VisualizeMap();
    }
}