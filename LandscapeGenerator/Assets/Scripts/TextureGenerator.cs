using System;
using UnityEngine;
using System.Collections;

public static class TextureGenerator {

    public static Texture2D TextureFromColorMap(Color[] colorMap, int size) {
        var texture = new Texture2D (size, size)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels (colorMap);
        texture.Apply ();
        return texture;
    }


    public static Texture2D TextureFromHeightMap(float[] heightMap, int size)
    {
        var colorMap = new Color[size * size];
        for (int i = 0; i < colorMap.Length; i++) {
            colorMap [i] = Color.Lerp (Color.black, Color.white, heightMap [i]);
        }

        return TextureFromColorMap (colorMap, size);
    }
}