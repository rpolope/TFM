using UnityEngine;

public class MapVisualizer : MonoBehaviour
{
    public void VisualizeMap()
    {
        int size = LandscapeManager.TerrainChunkSize;
        Texture2D texture = GetTextureFromMap(MapGenerator.GenerateMap(size, LandscapeManager.Scale), size);
        GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
    }


    public Texture2D GetTextureFromMap(float[] map, int textureSize)
    {
        Color[] colors = new Color[map.Length];
        Texture2D texture = new Texture2D(textureSize, textureSize);

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
}