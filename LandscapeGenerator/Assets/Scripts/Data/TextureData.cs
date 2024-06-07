using UnityEngine;
using System.Collections;
using System.Linq;

[CreateAssetMenu()]
public class TextureData : UpdatableData {
    
    private const int TextureSize = 512;
    private const TextureFormat TextureFormat = UnityEngine.TextureFormat.RGB565;

    public Layer[] layers;

    float _savedMinHeight;
    float _savedMaxHeight;
    private static readonly int LayerCount = Shader.PropertyToID("layerCount");
    private static readonly int BaseColors = Shader.PropertyToID("baseColors");
    private static readonly int BaseBlends = Shader.PropertyToID("baseBlends");
    private static readonly int BaseStartHeights = Shader.PropertyToID("baseStartHeights");
    private static readonly int BaseColorStrength = Shader.PropertyToID("baseColorStrength");
    private static readonly int BaseTextureScales = Shader.PropertyToID("baseTextureScales");
    private static readonly int BaseTextures = Shader.PropertyToID("baseTextures");
    private static readonly int MinHeight = Shader.PropertyToID("minHeight");
    private static readonly int MaxHeight = Shader.PropertyToID("maxHeight");

    public void ApplyToMaterial(Material material) {
		
        material.SetInt (LayerCount, layers.Length);
        material.SetColorArray (BaseColors, layers.Select(x => x.tint).ToArray());
        material.SetFloatArray (BaseBlends, layers.Select(x => x.blendStrength).ToArray());
        material.SetFloatArray (BaseStartHeights, layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray (BaseColorStrength, layers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray (BaseTextureScales, layers.Select(x => x.textureScale).ToArray());
        material.SetTexture (BaseTextures, GenerateTextureArray (layers.Select (x => x.texture).ToArray ()));

        UpdateMeshHeights (material, _savedMinHeight, _savedMaxHeight);
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight) {
        _savedMinHeight = minHeight;
        _savedMaxHeight = maxHeight;

        material.SetFloat (MinHeight, minHeight);
        material.SetFloat (MaxHeight, maxHeight);
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures) {
        Texture2DArray textureArray = new Texture2DArray (TextureSize, TextureSize, textures.Length, TextureFormat, true);
        for (int i = 0; i < textures.Length; i++) {
            textureArray.SetPixels (textures [i].GetPixels (), i);
        }
        textureArray.Apply ();
        return textureArray;
    }

    [System.Serializable]
    public class Layer {
        public Texture2D texture;
        public Color tint;
        [Range(0,1)]
        public float tintStrength;
        [Range(0,1)]
        public float startHeight;
        [Range(0,1)]
        public float blendStrength;
        public float textureScale;
    }
		
	 
}