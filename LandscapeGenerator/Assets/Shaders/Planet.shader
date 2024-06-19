Shader "Custom/Planet"
{
    Properties
    {
        _ElevationMinMax("Elevation Min Max", Vector) = (0, 0, 0, 0)
        _PlanetTexture("Planet Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Uniforms
            float4 _ElevationMinMax;
            sampler2D _PlanetTexture;
            float4 _PlanetTexture_TexelSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.texcoord.xy;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Calculate elevation factor
                float elevation = length(i.worldPos);
                float elevationFactor = (elevation - _ElevationMinMax.x) / (_ElevationMinMax.y - _ElevationMinMax.x);

                // Sample the planet texture
                float4 planetTex = tex2D(_PlanetTexture, i.uv);

                // Combine elevation factor with texture
                fixed4 col = planetTex * elevationFactor;

                return col;
            }
            ENDCG
        }
    }
}
