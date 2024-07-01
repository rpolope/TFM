Shader "Custom/Water"
{
    Properties
    {
        _GradientTex ("Gradient Texture", 2D) = "white" {}
        _WaterTex ("Water Texture", 2D) = "white" {}
        _TextureNoiseScale ("Texture Scale", Float) = 0.0001
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _WaterTex;
        sampler2D _GradientTex;
        sampler2D _HeightMap;

        struct Input
        {
            float3 worldPos;
            float2 uv_MainTex;
        };

        float _TextureNoiseScale;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 color = tex2D (_WaterTex, IN.uv_MainTex * _TextureNoiseScale);
            fixed4 latitude = tex2D (_GradientTex, IN.uv_MainTex);
            fixed4 groundHeight = tex2D(_HeightMap, IN.uv_MainTex);
            o.Albedo = color.rgb;
            o.Alpha = 1;
            // o.Alpha = latitude > 0.8 && groundHeight < IN.worldPos.y ? 1: 0; 
            
        }
        ENDCG
    }
    FallBack "Diffuse"
}
