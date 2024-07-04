Shader "Custom/NormalMap"
{
    Properties
    {
        _MainTex ("Gradient Texture", 2D) = "white" {}
        _TempNoiseTex ("Temperature Noise Texture", 2D) = "white" {}
        _TemperatureNoiseScale ("Temperature Distorsion Scale", Float) = 1.0
        _MoistureNoiseTex ("Moisture Noise Texture", 2D) = "white" {}
        _MoistureNoiseScale ("Moisture Noise Scale", Float) = 1.0
        _TextureNoiseScale ("Texture Scale", Float) = 1.0
        _WaterLevel ("Water Level", Float) = 10
        _NormalMap ("Normal Map", 2D) = "bump" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        
        sampler2D _MainTex;
        sampler2D _NormalMap; 
        float _TextureNoiseScale;

        struct Input {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA 
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 normalFromMap = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex * _TextureNoiseScale));
            o.Normal = normalFromMap;

            float3 debugColor = float3(1, 1, 1); 
            o.Albedo = debugColor;
            o.Metallic = 0.0;
            o.Smoothness = 0.0;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
