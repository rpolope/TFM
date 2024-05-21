Shader "Custom/VertexColorWithLightingShader"
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
        #pragma surface surf Lambert vertex:vert

        #include "UnityCG.cginc"

        struct appdata_t
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float4 color : COLOR;
        };

        struct Input
        {
            float4 color : COLOR;
        };

        void vert (inout appdata_t v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.color = v.color;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            o.Albedo = IN.color.rgb;
            o.Alpha = IN.color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
