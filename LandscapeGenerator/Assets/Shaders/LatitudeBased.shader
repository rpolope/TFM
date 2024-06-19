Shader "Custom/LatitudeBasedWithNoise"
{
    Properties
    {
        _MainTex ("Gradient Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
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
        sampler2D _NoiseTex;
        float _NoiseScale;

        struct Input {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Usar la coordenada uv.y para determinar el color de la textura
            float2 uv = IN.uv_MainTex;

            // Obtener valor de ruido para suavizar la transición
            float noiseValue = tex2D(_NoiseTex, IN.uv_MainTex * _NoiseScale).r;

            // Ajustar la latitud con el valor de ruido
            uv = uv + (noiseValue - 0.5f) * 0.1f;

            // Limitar latitud ajustada a [0, 1]
            uv = saturate(uv);

            // Obtener color de la textura de gradiente
            half4 color = tex2D(_MainTex, uv);
            o.Albedo = color.rgb;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
