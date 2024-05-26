Shader "Custom/VertexColorWithLighting"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        #include "UnityCG.cginc"

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
        };

        sampler2D _MainTex;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Metallic = 0.0;  // No metal
            o.Smoothness = 0.0;  // No reflectivity
        }
        ENDCG
    }
    FallBack "Diffuse"
}
