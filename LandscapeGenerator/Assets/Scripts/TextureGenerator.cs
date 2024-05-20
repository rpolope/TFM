using System;
using UnityEngine;
using System.Collections;

public static class TextureGenerator {

    public static Texture2D TextureFromColorMap(Color[] colorMap, int size) {
        Texture2D texture = new Texture2D (MapGenerator.MapSize, MapGenerator.MapSize);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels (colorMap);
        texture.Apply ();
        return texture;
    }


    public static Texture2D TextureFromHeightMap(float[] heightMap, int size)
    {
        Color[] colorMap = new Color[size];
        for (int i = 0; i < size; i++) {
                colorMap [i] = Color.Lerp (Color.black, Color.white, heightMap [i]);
        }

        return TextureFromColorMap (colorMap, size);
    }
}