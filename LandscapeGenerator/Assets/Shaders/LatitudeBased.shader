Shader "Custom/BiomeBased"
{
    Properties
    {
        _MainTex ("Gradient Texture", 2D) = "white" {}
        _TempNoiseTex ("Temperature Noise Texture", 2D) = "white" {}
        _TemperatureNoiseScale ("Temperature Distorsion Scale", Float) = 1.0

        _MoistureNoiseTex ("Moisture Noise Texture", 2D) = "white" {}
        _MoistureNoiseScale ("Moisture Noise Scale", Float) = 1.0

        _TundraTex ("Tundra Texture", 2D) = "white" {}
        _TaigaTex ("Taiga Texture", 2D) = "white" {}
        _ForestTex ("Forest Texture", 2D) = "white" {}
        _GrasslandTex ("Grassland Texture", 2D) = "white" {}
        _DesertTex ("Desert Texture", 2D) = "white" {}
        
        _TextureNoiseScale ("Texture Scale", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _TempNoiseTex;
        sampler2D _MoistureNoiseTex;

        sampler2D _TundraTex;
        sampler2D _TaigaTex;
        sampler2D _ForestTex;
        sampler2D _GrasslandTex;
        sampler2D _DesertTex;

        float _TextureNoiseScale;
        float _TemperatureNoiseScale;
        float _MoistureNoiseScale;

        struct Input {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
        };

        half4 sampleColorFromBiome(float temp, float moisture, float2 uv)
        {
            if (temp < 0.33) // Zonas frías
            {
                if (moisture < 0.33)
                    return tex2D(_TundraTex, uv); // Tundra
                if (moisture < 0.66)
                    return tex2D(_TaigaTex, uv); // Taiga
                return tex2D(_ForestTex, uv);// Bosque boreal
            }
            if (temp < 0.66) // Zonas templadas
            {
                if (moisture < 0.33)
                    return tex2D(_GrasslandTex, uv); // Pradera
                if (moisture < 0.66)
                    return tex2D(_ForestTex, uv); // Bosque templado
                return tex2D(_ForestTex, uv); // Selva tropical
            }
            // Zonas cálidas
            if (moisture < 0.33)
                return tex2D(_DesertTex, uv); // Desierto
            if (moisture < 0.66)
                return tex2D(_GrasslandTex, uv); // Sabana
            return tex2D(_ForestTex, uv);  // Bosque seco
        }


        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float temp = tex2D(_MainTex, IN.uv_MainTex).r;
            float distortion = tex2D(_TempNoiseTex, IN.uv_MainTex * _TemperatureNoiseScale).r;
            temp = temp + (distortion - 0.5f) * 0.1f;
            temp = saturate(temp);

            float moisture = tex2D(_MoistureNoiseTex, IN.uv_MainTex * _MoistureNoiseScale).r;

            half4 color = sampleColorFromBiome(temp, moisture, IN.worldPos.xz * _TextureNoiseScale);

            o.Albedo = color.rgb;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
