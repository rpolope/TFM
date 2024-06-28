Shader "Custom/TempMoistBased"
{
    Properties
    {
        _MainTex ("Gradient Texture", 2D) = "white" {}
        _TempNoiseTex ("Temperature Noise Texture", 2D) = "white" {}
        _TemperatureNoiseScale ("Temperature Distorsion Scale", Float) = 1.0
        _MoistureNoiseTex ("Moisture Noise Texture", 2D) = "white" {}
        _MoistureNoiseScale ("Moisture Noise Scale", Float) = 1.0
        _TextureNoiseScale ("Texture Scale", Float) = 1.0
        _WaterHeight ("Water Height", Float) = 10
        _SnowHeight ("Snow Height", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        // Define biomes
        #define TUNDRA 0
        #define FOREST 1
        #define TROPICAL_FOREST 2
        #define SCORCHED 3
        #define SHRUBLAND 4
        #define SNOW 5
        #define BARE 6
        #define TAIGA 7
        #define GRASSLAND_COLD 8
        #define GRASSLAND_HOT 9
        #define DESERT_COLD 10
        #define DESERT_WARM 11
        #define DESERT_HOT 12

        static const int NUM_BIOMES = 13;

        float biomeMinTemp[NUM_BIOMES];
        float biomeMaxTemp[NUM_BIOMES];
        float biomeMinMoist[NUM_BIOMES];
        float biomeMaxMoist[NUM_BIOMES];

        UNITY_DECLARE_TEX2DARRAY(baseTextures);

        sampler2D _MainTex;
        sampler2D _TempNoiseTex;
        sampler2D _MoistureNoiseTex;

        float _TextureNoiseScale;
        float _TemperatureNoiseScale;
        float _MoistureNoiseScale;
        float _WaterHeight;
        float _SnowHeight;

        struct Input {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
        };

        float inverseLerp(float a, float b, float value) {
            return saturate((value-a)/(b-a));
        }

        float getTemperature(float latitude, float2 uv, float height) {
            float temp = lerp(-30, 30, latitude);
            float distortion = tex2D(_TempNoiseTex, uv * _TemperatureNoiseScale).r;
            float heightPerturb = height;
            temp = temp - heightPerturb + distortion * 0.01;
            return temp;
        }

        float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
            float3 scaledWorldPos = worldPos / scale;
            float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
            float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
            float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
            return xProjection + yProjection + zProjection;
        }

        float3 getInterpolatedValue(float value, float value1, float value2, float value3, float3 color1, float3 color2, float3 color3) {
            if (value <= value1) {
                return color1;
            }
            if (value <= value2) {
                return lerp(color1, color2, inverseLerp(value1, value2, value));
            }
            if (value <= value3) {
                return lerp(color2, color3, inverseLerp(value2, value3, value));
            }
            return color3;
        }

        float3 lerpMoistureColor(float3 dryColor, float3 medColor, float3 wetColor, float moisture, int dryBiomes[2], int medBiomes[2], int wetBiomes[2]) {
            float dryMinMoist = min(biomeMinMoist[dryBiomes[0]], biomeMinMoist[dryBiomes[1]]);
            float dryMaxMoist = max(biomeMaxMoist[dryBiomes[0]], biomeMaxMoist[dryBiomes[1]]);
            float medMinMoist = min(biomeMinMoist[medBiomes[0]], biomeMinMoist[medBiomes[1]]);
            float medMaxMoist = max(biomeMaxMoist[medBiomes[0]], biomeMaxMoist[medBiomes[1]]);
            float wetMinMoist = min(biomeMinMoist[wetBiomes[0]], biomeMinMoist[wetBiomes[1]]);
            float wetMaxMoist = max(biomeMaxMoist[wetBiomes[0]], biomeMaxMoist[wetBiomes[1]]);
            
            float lowMoist = min(dryMinMoist, min(medMinMoist, wetMinMoist));
            float highMoist = max(dryMinMoist, max(medMinMoist, wetMinMoist));
            
            return getInterpolatedValue(moisture, 0.3f, 0.5f, 0.6f, dryColor, medColor, wetColor);
        }

        float3 lerpTemperatureColor(float temperature, float moisture, Input IN) {
            float normalizedTempRange = inverseLerp(-30, 30, temperature);

            int tempIndex = floor(normalizedTempRange * 4.0);
            int nextTempIndex = min(tempIndex + 1, 4);
            float tempStrength = inverseLerp(tempIndex / 4.0, (tempIndex + 1) / 4.0, normalizedTempRange);

            const int biomes[15] = {
                SNOW, SNOW, SNOW,
                SCORCHED, BARE, TUNDRA,
                DESERT_COLD, SHRUBLAND, TAIGA,
                DESERT_WARM, GRASSLAND_COLD, FOREST,
                DESERT_HOT, GRASSLAND_HOT, TROPICAL_FOREST
            };

            int moistureLevelsPerTemp = 3;
            int index = tempIndex * moistureLevelsPerTemp;
            int nextIndex = nextTempIndex * moistureLevelsPerTemp;

            int dryBiomes[] = {biomes[index], biomes[nextIndex]};
            int medBiomes[] = {biomes[index + 1], biomes[nextIndex + 1]};
            int wetBiomes[] = {biomes[index + 2], biomes[nextIndex + 2]};

            float3 blendAxes = abs(IN.worldNormal);
            blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

            float3 dryColor = lerp(triplanar(IN.worldPos, _TextureNoiseScale, blendAxes, dryBiomes[0]), triplanar(IN.worldPos, _TextureNoiseScale, blendAxes, dryBiomes[1]), tempStrength);
            float3 medColor = lerp(triplanar(IN.worldPos, _TextureNoiseScale, blendAxes, medBiomes[0]), triplanar(IN.worldPos, _TextureNoiseScale, blendAxes, medBiomes[1]), tempStrength);
            float3 wetColor = lerp(triplanar(IN.worldPos, _TextureNoiseScale, blendAxes, wetBiomes[0]), triplanar(IN.worldPos, _TextureNoiseScale, blendAxes, wetBiomes[1]), tempStrength);

            return lerpMoistureColor(dryColor, medColor, wetColor, moisture, dryBiomes, medBiomes, wetBiomes);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex * _TextureNoiseScale;
            float height = IN.worldPos.y;
            float latitude = tex2D(_MainTex, uv).r;
            float temperature = getTemperature(latitude, uv, height);
            float moisture = tex2D(_MoistureNoiseTex, uv * _MoistureNoiseScale).r;

            float3 terrainColor = lerpTemperatureColor(temperature, moisture, IN);
            o.Albedo = terrainColor;
            o.Metallic = 0.0;
            o.Smoothness = 0.0;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
