Shader "Custom/BiomeBased"
{
    Properties
    {
        _MainTex ("Gradient Texture", 2D) = "white" {}
        _TempNoiseTex ("Temperature Noise Texture", 2D) = "white" {}
        _MoistureNoiseTex ("Moisture Noise Texture", 2D) = "white" {}
        
        _TundraTex ("Tundra Texture", 2D) = "white" {}
        _TaigaTex ("Taiga Texture", 2D) = "white" {}
        _ForestTex ("Forest Texture", 2D) = "white" {}
        _GrasslandTex ("Grassland Texture", 2D) = "white" {}
        _DesertTex ("Desert Texture", 2D) = "white" {}
        
        _NoiseScale ("Noise Scale", Float) = 1.0
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

        float _NoiseScale;

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
                else if (moisture < 0.66)
                    return tex2D(_TaigaTex, uv); // Taiga
                else
                    return tex2D(_ForestTex, uv); // Bosque boreal
            }
            else if (temp < 0.66) // Zonas templadas
            {
                if (moisture < 0.33)
                    return tex2D(_GrasslandTex, uv); // Pradera
                else if (moisture < 0.66)
                    return tex2D(_ForestTex, uv); // Bosque templado
                else
                    return tex2D(_ForestTex, uv); // Selva tropical
            }
            else // Zonas cálidas
            {
                if (moisture < 0.33)
                    return tex2D(_DesertTex, uv); // Desierto
                else if (moisture < 0.66)
                    return tex2D(_GrasslandTex, uv); // Sabana
                else
                    return tex2D(_ForestTex, uv); // Bosque seco
            }
        }



        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float temp = tex2D(_MainTex, IN.uv_MainTex).r;
            float distortion = tex2D(_TempNoiseTex, IN.uv_MainTex * _NoiseScale).r;
            temp = temp + (distortion - 0.5f) * 0.1f;
            temp = saturate(temp);

            float moisture = tex2D(_MoistureNoiseTex, IN.uv_MainTex * _NoiseScale).r;

            half4 color = sampleColorFromBiome(temp, moisture, IN.uv_MainTex);

            o.Albedo = color.rgb;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
