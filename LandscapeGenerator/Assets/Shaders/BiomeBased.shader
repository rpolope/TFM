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
 
        float getTemperature(float latitude, float height, float distorsion)
        {
            float baseTemperature = lerp(30, -30, latitude);
            float altitudeEffect = height * .5;
            return baseTemperature - altitudeEffect + distorsion*0.01;
        }

        int getBiome(float moisture, float temperature)
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

        float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
			float3 scaledWorldPos = worldPos / scale;
			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
			return xProjection + yProjection + zProjection;
		}
        
        float3 sampleBiomeTexture(int biome, float3 worldPos, float3 blendAxes) {
            if (worldPos.y < _WaterHeight)
                return UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3((worldPos/_TextureNoiseScale).y, (worldPos/_TextureNoiseScale).z, OCEAN));
            if (worldPos.y > _SnowHeight)
                return triplanar(worldPos, _TextureNoiseScale, blendAxes, SNOW);
            return triplanar(worldPos, _TextureNoiseScale, blendAxes, biome);
        }
        

        float3 lerpBiomeColor(float value, int biome1, int biome2, float3 worldPos, float3 blendAxes) {
            float3 color1 = sampleBiomeTexture(biome1, worldPos, blendAxes);
            float3 color2 = sampleBiomeTexture(biome2, worldPos, blendAxes);
            
            return lerp(color1, color2, value);
        }

        float3 ComputeBiomeColour(float temperature, float moisture, Input IN) {
            float3 blendAxes = abs(IN.worldNormal);
            blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

            if (temperature < -10.0) {
                if (moisture < 0.1) return sampleBiomeTexture(SCORCHED, IN.worldPos, blendAxes);
                if (moisture < 0.2) return lerpBiomeColor(inverseLerp(0.1, 0.2, moisture), SCORCHED, BARE, IN.worldPos, blendAxes);
                if (moisture < 0.5) return lerpBiomeColor(inverseLerp(0.2, 0.5, moisture), BARE, TUNDRA, IN.worldPos, blendAxes);
                return lerpBiomeColor(inverseLerp(0.5, 1.0, moisture),TUNDRA, SNOW, IN.worldPos, blendAxes);
            }

            if (temperature < 0.0) {
                if (moisture < 0.33) return sampleBiomeTexture(DESERT, IN.worldPos, blendAxes);
                if (moisture < 0.66) return lerpBiomeColor(inverseLerp(0.33, 0.66, moisture), DESERT, SHRUBLAND, IN.worldPos, blendAxes);
                return lerpBiomeColor(inverseLerp(0.66, 1.0, moisture),SHRUBLAND, TAIGA, IN.worldPos, blendAxes);
            }

            if (temperature < 10.0) {
                if (moisture < 0.16) return sampleBiomeTexture(DESERT, IN.worldPos, blendAxes);
                if (moisture < 0.5) return lerpBiomeColor(inverseLerp(0.16, 0.5, moisture), DESERT, GRASSLAND, IN.worldPos, blendAxes);
                return lerpBiomeColor(inverseLerp(0.5, 1.0, moisture),GRASSLAND, FOREST, IN.worldPos, blendAxes);
            }

            if (moisture < 0.16) return sampleBiomeTexture(DESERT, IN.worldPos, blendAxes);
            if (moisture < 0.33) return lerpBiomeColor(inverseLerp(0.16, 0.33, moisture), DESERT, GRASSLAND, IN.worldPos, blendAxes);
            return lerpBiomeColor(inverseLerp(0.33, 1.0, moisture), GRASSLAND, TROPICAL_FOREST, IN.worldPos, blendAxes);
        }
        
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float latitude = tex2D(_MainTex, IN.uv_MainTex).r;
            float distortion = tex2D(_MoistureNoiseTex, IN.uv_MainTex * _TemperatureNoiseScale).r;
            float temp = getTemperature(latitude, IN.worldPos.y, distortion);

            float moisture = tex2D(_MoistureNoiseTex, IN.uv_MainTex * _MoistureNoiseScale).r;

            int biome = getBiome(moisture, temp);

            float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
            float3 color = sampleBiomeTexture(biome, IN.worldPos, blendAxes);
            float3 debuColor = triplanar(IN.worldPos, _TextureNoiseScale, blendAxes, biome);
            o.Albedo = ComputeBiomeColour(temp, moisture, IN);
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
