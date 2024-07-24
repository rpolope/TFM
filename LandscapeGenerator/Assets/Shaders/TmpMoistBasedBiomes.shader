Shader "Custom/TmpMoistBasedBiomes"
{
    Properties
    {
        _MainTex ("Gradient Texture", 2D) = "white" {}
        _TempNoiseTex ("Temperature Noise Texture", 2D) = "white" {}
        _TemperatureNoiseScale ("Temperature Distorsion Scale", Float) = 1.0
        _MoistureNoiseTex ("Moisture Noise Texture", 2D) = "white" {}
        _MoistureNoiseScale ("Moisture Noise Scale", Float) = 1.0
        _BumpMap ("Bump Map", 2D) = "bump" {}
        _TextureNoiseScale ("Texture Scale", Float) = 1.0
        _WaterLevel ("Water Level", Float) = 10
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        // Define biomes
        #define SNOW 0
        #define TUNDRA 1
        #define BOREAL_FOREST 2
        #define TEMPERATE_CONIFEROUS_FOREST 3
        #define TEMPERATE_SEASONAL_FOREST 4
        #define TROPICAL_SEASONAL_FOREST_SAVANNA 5
        #define TROPICAL_RAINFOREST 6
        #define WOODLAND_SHRUBLAND 7
        #define TEMPERATE_GRASSLAND_COLD_DESERT 8
        #define SUBTROPICAL_DESERT 9


        static const int NUM_BIOMES = 10;

        float biomeMinTemp[NUM_BIOMES];
        float biomeMaxTemp[NUM_BIOMES];
        float biomeMinMoist[NUM_BIOMES];
        float biomeMaxMoist[NUM_BIOMES];

        UNITY_DECLARE_TEX2DARRAY(groundTextures);

        sampler2D _MainTex;
        sampler2D _TempNoiseTex;
        sampler2D _MoistureNoiseTex;
        sampler2D _BumpMap;

        float _TextureNoiseScale;
        float _TemperatureNoiseScale;
        float _MoistureNoiseScale;
        float _WaterLevel;

        struct Input {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 worldPos;
            float3 worldNormal; INTERNAL_DATA
        };



        float inverseLerp(float a, float b, float value) {
            return saturate((value-a)/(b-a));
        }

        float getTemperature(float latitude, float2 uv, float height) {
            float temp = lerp(-30, 30, latitude);
            float distortion = tex2D(_TempNoiseTex, uv * _TemperatureNoiseScale).r;
            float heightPerturb = height * 0.5f;
            temp = temp - heightPerturb + distortion * 0.01;
            return temp;
        }

        float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
            float3 scaledWorldPos = worldPos / scale;
            float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(groundTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
            float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(groundTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
            float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(groundTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
            return xProjection + yProjection + zProjection;
        }

        int GetBiome(float moisture, float heat)
        {
            float maxTemp = 30;
            float minTemp = -30;
            
            float temperature = heat * (maxTemp - minTemp) + minTemp;

            int closestBiome = -1;
            float closestDistance = 10E5;

            for (int biome = 0; biome < NUM_BIOMES; biome++)
            {
                if (temperature >= biomeMinTemp[biome] && temperature <= biomeMaxTemp[biome] &&
                    moisture >= biomeMinMoist[biome] && moisture <= biomeMaxMoist[biome]) 
                {
                    return biome;
                }
            
                float tempDistance = min(abs(temperature - biomeMinTemp[biome]), abs(temperature - biomeMaxTemp[biome]));
                float moistDistance = min(abs(moisture - biomeMinMoist[biome]), abs(moisture - biomeMaxMoist[biome]));
                float distance = tempDistance + moistDistance;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBiome = biome;
                }
            }

            return closestBiome;
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

        float3 lerpMoistureColor(float3 dryColor, float3 medColor, float3 wetColor, float moisture) {
            
            return getInterpolatedValue(moisture, 0.3f, 0.5f, 0.66f, dryColor, medColor, wetColor);
        }

        float3 lerpTemperatureColor(float temperature, float moisture, float3 worldPos, float3 worldNormal) {

            float normalizedTempRange = inverseLerp(-30, 30, temperature);

            int tempIndex = floor(normalizedTempRange * 3.0);
            int nextTempIndex = min(tempIndex + 1, 3);
            float tempStrength = inverseLerp(tempIndex / 3.0, nextTempIndex / 3.0, normalizedTempRange);

            const int biomes[12] = {
                SNOW, SNOW, SNOW,
                TEMPERATE_GRASSLAND_COLD_DESERT, TUNDRA, BOREAL_FOREST,
                SUBTROPICAL_DESERT, TEMPERATE_SEASONAL_FOREST, TEMPERATE_CONIFEROUS_FOREST,
                WOODLAND_SHRUBLAND, TROPICAL_SEASONAL_FOREST_SAVANNA, TROPICAL_RAINFOREST
            };

            int moistureLevelsPerTemp = 3;
            int index = tempIndex * moistureLevelsPerTemp;
            int nextIndex = nextTempIndex * moistureLevelsPerTemp;

            int dryBiomes[] = {biomes[index], biomes[nextIndex]};
            int medBiomes[] = {biomes[index + 1], biomes[nextIndex + 1]};
            int wetBiomes[] = {biomes[index + 2], biomes[nextIndex + 2]};

            float3 blendAxes = abs(worldNormal);
            blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

            float3 dryColor = lerp(triplanar(worldPos, _TextureNoiseScale, blendAxes, dryBiomes[0]), triplanar(worldPos, _TextureNoiseScale, blendAxes, dryBiomes[1]), tempStrength);
            float3 medColor = lerp(triplanar(worldPos, _TextureNoiseScale, blendAxes, medBiomes[0]), triplanar(worldPos, _TextureNoiseScale, blendAxes, medBiomes[1]), tempStrength);
            float3 wetColor = lerp(triplanar(worldPos, _TextureNoiseScale, blendAxes, wetBiomes[0]), triplanar(worldPos, _TextureNoiseScale, blendAxes, wetBiomes[1]), tempStrength);

            return lerpMoistureColor(dryColor, medColor, wetColor, moisture);
        }

        float3 setBeachColor(Input IN, float3 worldNormal)
        {
            float3 color;
            float texScale = 5;
                if (IN.worldPos.y > _WaterLevel && IN.worldPos.y < _WaterLevel + 1) {
                    float3 blendAxes = abs(worldNormal);
                    blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
                    float3 beachColor = triplanar(IN.worldPos, texScale, blendAxes, TEMPERATE_GRASSLAND_COLD_DESERT) * float3(1, 0.99,0.65);
                    float blendStrength = inverseLerp(_WaterLevel, _WaterLevel + 0.95f, IN.worldPos.y);
                    color = lerp(beachColor, color, blendStrength);
                }
        
            return color;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 normalFromMap = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex * _TextureNoiseScale)).rgb;
            float3 worldNormal = WorldNormalVector (IN, o.Normal);
            
            float2 uv = IN.uv_MainTex;
            // float3 normal = normalize(normalFromMap + IN.worldNormal);
            float height = IN.worldPos.y;
            float latitude = tex2D(_MainTex, uv).r;
            float temperature = getTemperature(latitude, uv, height);
            float moisture = tex2D(_MoistureNoiseTex, uv * _MoistureNoiseScale).r;
            
            float slope = dot(IN.worldNormal, float3(0, 1, 0));
            float3 color = lerpTemperatureColor(temperature, moisture, IN.worldPos, worldNormal);
            
            // if (temperature > 0 && slope > 0.5)
            // {
            //     color = setBeachColor(IN, worldNormal);
            // }

            float3 blendAxes = abs(worldNormal);
            blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
            float3 debugColor = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
            o.Albedo = color;
            o.Metallic = 0.0;
            o.Smoothness = 0.0;
            o.Alpha = 1.0;
            o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_MainTex * _TextureNoiseScale));
        }
        ENDCG
    }
    FallBack "Diffuse"
}
