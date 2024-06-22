Shader "Custom/BiomeBased"
{
    Properties
    {
        _MainTex ("Gradient Texture", 2D) = "white" {}
        _TempNoiseTex ("Temperature Noise Texture", 2D) = "white" {}
        _TemperatureNoiseScale ("Temperature Distorsion Scale", Float) = 1.0
        _MoistureNoiseTex ("Moisture Noise Texture", 2D) = "white" {}
        _MoistureNoiseScale ("Moisture Noise Scale", Float) = 1.0
        
        _TextureNoiseScale ("Texture Scale", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        #define TUNDRA 0
        #define GRASSLAND 1
        #define FOREST 2
        #define OCEAN 3
        #define TROPICAL_FOREST 4
        #define DESERT 5
        #define BEACH 6
        #define SCORCHED 7
        #define SHRUBLAND 8
        #define SNOW 9
        #define BARE 10
        #define TAIGA 11

        sampler2D _MainTex;
        sampler2D _MoistureNoiseTex;


        UNITY_DECLARE_TEX2DARRAY(baseTextures);


        float _TextureNoiseScale;
        float _TemperatureNoiseScale;
        float _MoistureNoiseScale;

        struct Input {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
        };

        float inverseLerp(float a, float b, float value) {
            return saturate((value-a)/(b-a));
        }
 
        float CalculateTemperature(float latitude, float height)
        {
            float baseTemperature = inverseLerp(30, -30, latitude); // De 30°C en el ecuador a -30°C en los polos
            float altitudeEffect = height * .5;
            return baseTemperature - altitudeEffect;
        }

        int GetClimateType(float moisture, float temperature)
        {
            if (temperature < -10.0)
            {
                if (moisture < 0.1)
                {
                    return SCORCHED;
                }
                if (moisture < 0.2)
                {
                    return BARE;
                }
                if (moisture < 0.5)
                {
                    return TUNDRA;
                }
                return SNOW;
            }
            if (temperature < 0.0)
            {
                if (moisture < 0.33)
                {
                    return DESERT;
                }
                if (moisture < 0.66)
                {
                    return SHRUBLAND;
                }
                return TAIGA;
            }
            if (temperature < 10.0)
            {
                if (moisture < 0.16)
                {
                    return DESERT;
                }
                if (moisture < 0.50)
                {
                    return GRASSLAND;
                }
                return FOREST;
            }
            if (moisture < 0.16)
            {
                return DESERT;
            }
            if (moisture < 0.33)
            {
                return GRASSLAND;
            }
            return TROPICAL_FOREST;
        }

        int DetermineBiome(float moisture, float height, float latitude)
        {
            const float temperature = CalculateTemperature(latitude, height);
            return GetClimateType(moisture, temperature);
        }

        float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
			float3 scaledWorldPos = worldPos / scale;
			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
			return xProjection + yProjection + zProjection;
		}
        
        float3 sampleBiomeTexture(int biome, float3 worldPos, float scale, float3 blendAxes) {
            return triplanar(worldPos, scale, blendAxes, biome);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float temp = tex2D(_MainTex, IN.uv_MainTex).r;
            float distortion = tex2D(_MoistureNoiseTex, IN.uv_MainTex * _TemperatureNoiseScale).r;
            temp = temp + (distortion - 0.5f) * 0.1f;
            temp = saturate(temp);

            float moisture = tex2D(_MoistureNoiseTex, IN.uv_MainTex * _MoistureNoiseScale).r;

            int biome = DetermineBiome(moisture, IN.worldPos.y, temp);

            float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
            float3 color = sampleBiomeTexture(biome, IN.worldPos, _TextureNoiseScale, blendAxes);

            o.Albedo = color;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
