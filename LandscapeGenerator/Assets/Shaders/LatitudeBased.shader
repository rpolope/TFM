Shader "Custom/LatitudeBasedWithNoise"
{
    Properties
    {
        _MainTex ("Gradient Texture", 2D) = "white" {}
        _TempNoiseTex ("Temperature Noise Texture", 2D) = "white" {}
        _MoistureNoiseTex ("Moisture Noise Texture", 2D) = "white" {}
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
        float _NoiseScale;

        struct Input {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
        };

        half4 determineColor(float temp, float moisture)
        {
            half4 color;

            if (temp < 0.33) // Zonas frías
            {
                if (moisture < 0.33)
                    color = half4(0.5, 0.5, 1.0, 1.0); // Tundra
                else if (moisture < 0.66)
                    color = half4(0.4, 0.4, 0.8, 1.0); // Taiga
                else
                    color = half4(0.3, 0.3, 0.7, 1.0); // Bosque boreal
            }
            else if (temp < 0.66) // Zonas templadas
            {
                if (moisture < 0.33)
                    color = half4(0.2, 0.7, 0.2, 1.0); // Pradera
                else if (moisture < 0.66)
                    color = half4(0.1, 0.6, 0.1, 1.0); // Bosque templado
                else
                    color = half4(0.0, 0.5, 0.0, 1.0); // Selva tropical
            }
            else // Zonas cálidas
            {
                if (moisture < 0.33)
                    color = half4(1.0, 0.9, 0.5, 1.0); // Desierto
                else if (moisture < 0.66)
                    color = half4(0.8, 0.8, 0.2, 1.0); // Sabana
                else
                    color = half4(0.6, 0.6, 0.0, 1.0); // Bosque seco
            }

            return color;
        }


        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 lat = IN.uv_MainTex;

            float distortion = tex2D(_TempNoiseTex, IN.uv_MainTex * _NoiseScale).r;

            lat = lat + (distortion - 0.5f) * 0.1f;
            lat = saturate(lat);
            float moisture = tex2D(_MoistureNoiseTex, IN.uv_MainTex * _NoiseScale).r;

            half4 color = tex2D(_MainTex, determineColor(lat, moisture));
            o.Albedo = color.rgb;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
