Shader "Custom/TempMoistMix"
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

        static const float3 BIOME_COLORS[] =
        {
            float3(0.69, 0.77, 0.54), // TUNDRA
            float3(0.4, 0.7, 0.1),    // FOREST
            float3(0.1, 0.3, 0.2),    // TROPICAL_FOREST
            float3(1.0, 0.84, 0.0),   // SCORCHED
            float3(0.75, 0.59, 0.15), // SHRUBLAND
            float3(0.8, 0.8, 0.9),    // SNOW
            float3(0.4, 0.4, 0.4),    // BARE
            float3(0.1, 0.1, 0.1),    // TAIGA
            float3(0.35, 0.88, 0.26), // GRASSLAND_COLD
            float3(0.35, 0.88, 0.26), // GRASSLAND_HOT
            float3(0.86, 0.79, 0.27), // DESERT_COLD
            float3(0.1, 0.53, 0.025), // DESERT_WARM
            float3(0.1, 0.3, 0.2)     // DESERT_HOT
        };

        static const float3 BIOME_GRAY_SCALE_COLORS[] =
        {
            float3(0.0, 0.0, 0.0), // TUNDRA
            float3(0.66, 0.66, 0.66),    // FOREST
            float3(1, 1, 1),    // TROPICAL_FOREST
            float3(0.0, 0.0, 0.0),   // SCORCHED
            float3(0.33, 0.33, 0.33), // SHRUBLAND
            float3(0.0, 0.0, 0.0),    // SNOW
            float3(0.0, 0.0, 0.0),    // BARE
            float3(0.33, 0.33, 0.33),    // TAIGA
            float3(0.66, 0.66, 0.66), // GRASSLAND_COLD
            float3(1, 1, 1), // GRASSLAND_HOT
            float3(0.33, 0.33, 0.33), // DESERT_COLD
            float3(0.66, 0.66, 0.66), // DESERT_WARM
            float3(1, 1, 1)     // DESERT_HOT
        };

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
        float _MaxHeight;

        struct Input {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
        };

        float inverseLerp(float a, float b, float value) {
            return saturate((value-a)/(b-a));
        }

        float GetTemperature(float latitude, float2 uv, float height) {
            float temp = lerp(-30, 30, latitude);
            float distortion = tex2D(_TempNoiseTex, uv * _TemperatureNoiseScale).r;
            float heightPerturb = height;

            temp = temp - heightPerturb + distortion * 0.01;
            return temp;
        }

        int GetBiomeType(float moisture, float temperature) {
            if (temperature < -10.0) {
                if (moisture < 0.1) return SCORCHED;
                if (moisture < 0.2) return BARE;
                if (moisture < 0.5) return TUNDRA;
                return SNOW;
            }
            if (temperature < 0.0) {
                if (moisture < 0.33) return DESERT_COLD;
                if (moisture < 0.66) return SHRUBLAND;
                return TAIGA;
            }
            if (temperature < 10.0) {
                if (moisture < 0.16) return DESERT_WARM;
                if (moisture < 0.50) return GRASSLAND_COLD;
                return FOREST;
            }
            if (moisture < 0.16) return DESERT_HOT;
            if (moisture < 0.33) return GRASSLAND_HOT;
            return TROPICAL_FOREST;
        }


        float3 lerpTemperatureColor(float temperature, float moisture) {

            float normalizedTempRange = inverseLerp(-30, 30, temperature);

            int tempIndex = floor(normalizedTempRange * 4.0);
            int nextTempIndex = min(tempIndex + 1, 4);
            float epsilon = 10E-4;
            float tempFloor = tempIndex;
            float tempCeil = min(ceil(normalizedTempRange * 4.0), 4);
            float tempStrength = inverseLerp(tempFloor-epsilon, tempCeil, normalizedTempRange * 4);

            const int biomes[16] = {
                SNOW, SNOW, SNOW, SNOW,
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

            float colderDryMinMoist = biomeMinMoist[dryBiomes[0]];
            float hotterDryMinMoist = biomeMinMoist[dryBiomes[1]];
            float dryMoistStrength = inverseLerp(colderDryMinMoist - epsilon, hotterDryMinMoist, tempStrength);
            float3 dryColor = lerp(BIOME_COLORS[dryBiomes[0]], BIOME_COLORS[dryBiomes[1]], tempStrength);
            
            float colderMedMinMoist = biomeMinMoist[medBiomes[0]];
            float hotterMedMinMoist = biomeMinMoist[medBiomes[1]];
            float medMoistStrength = inverseLerp(colderMedMinMoist - epsilon, hotterMedMinMoist, tempStrength);
            float3 medColor = lerp(BIOME_COLORS[medBiomes[0]], BIOME_COLORS[medBiomes[1]], tempStrength);

            float colderWetMinMoist = biomeMinMoist[wetBiomes[0]];
            float hotterWetMinMoist = biomeMinMoist[wetBiomes[1]];
            float wetMoistStrength = inverseLerp(colderWetMinMoist - epsilon, hotterWetMinMoist, tempStrength);
            float3 wetColor = lerp(BIOME_COLORS[wetBiomes[0]], BIOME_COLORS[wetBiomes[1]], tempStrength);

            return medColor;
        }

        float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
            float3 scaledWorldPos = worldPos / scale;
            float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
            float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
            float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
            return xProjection + yProjection + zProjection;
        }

        float3 sampleBiomeColor(int biome, float3 worldPos, float3 blendAxes) {
            // if (worldPos.y < _WaterHeight)
            //     return UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3((worldPos/_TextureNoiseScale).y, (worldPos/_TextureNoiseScale).z, OCEAN));
            // if (worldPos.y > _SnowHeight)
            //     return triplanar(worldPos, _TextureNoiseScale, blendAxes, SNOW);
            return triplanar(worldPos, _TextureNoiseScale, blendAxes, biome);
        }

        float3 lerpBiomeColor(float moisture, float moistInterval1, float moistInterval2, int biome1, int biome2, float3 worldPos, float3 blendAxes) {
            float3 color1 = sampleBiomeColor(biome1, worldPos, blendAxes);
            float3 color2 = sampleBiomeColor(biome2, worldPos, blendAxes);
            return lerp(color1, color2, (moisture - moistInterval1) / (moistInterval2 - moistInterval1));
        }

        float3 ComputeBiomeColour(float temperature, float moisture, Input IN) {
            float3 blendAxes = abs(IN.worldNormal);
            blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

            if (temperature < -10.0) {
                if (moisture < 0.1) return sampleBiomeColor(SCORCHED, IN.worldPos, blendAxes);
                if (moisture < 0.2) return lerpBiomeColor(moisture, 0.1, 0.2, SCORCHED, BARE, IN.worldPos, blendAxes);
                if (moisture < 0.5) return lerpBiomeColor(moisture, 0.2, 0.5, BARE, TUNDRA, IN.worldPos, blendAxes);
                return lerpBiomeColor(moisture, 0.5, 1.0, TUNDRA, SNOW, IN.worldPos, blendAxes);
            }

            if (temperature < 0.0) {
                if (moisture < 0.33) return sampleBiomeColor(DESERT_COLD, IN.worldPos, blendAxes);
                if (moisture < 0.66) return lerpBiomeColor(moisture, 0.33, 0.66, DESERT_COLD, SHRUBLAND, IN.worldPos, blendAxes);
                return lerpBiomeColor(moisture, 0.66, 1.0, SHRUBLAND, TAIGA, IN.worldPos, blendAxes);
            }

            if (temperature < 10.0) {
                if (moisture < 0.16) return sampleBiomeColor(DESERT_WARM, IN.worldPos, blendAxes);
                if (moisture < 0.5) return lerpBiomeColor(moisture, 0.16, 0.5, DESERT_WARM, GRASSLAND_COLD, IN.worldPos, blendAxes);
                return lerpBiomeColor(moisture, 0.5, 1.0, GRASSLAND_COLD, FOREST, IN.worldPos, blendAxes);
            }

            if (moisture < 0.16) return sampleBiomeColor(DESERT_HOT, IN.worldPos, blendAxes);
            if (moisture < 0.33) return lerpBiomeColor(moisture, 0.16, 0.33, DESERT_HOT, GRASSLAND_HOT, IN.worldPos, blendAxes);
            return lerpBiomeColor(moisture, 0.33, 1.0, GRASSLAND_HOT, TROPICAL_FOREST, IN.worldPos, blendAxes);
        }

        void surf(Input IN, inout SurfaceOutputStandard o) {
            float latitude = tex2D(_MainTex, IN.uv_MainTex).r;
            float temp = GetTemperature(latitude, IN.uv_MainTex, IN.worldPos.y);
            float moisture = tex2D(_MoistureNoiseTex, IN.uv_MainTex * _MoistureNoiseScale).r;
            float3 color = ComputeBiomeColour(temp, moisture, IN);
            float3 debugColor = float3(temp, temp, temp);
            o.Albedo = lerpTemperatureColor(temp, 0.8);
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
