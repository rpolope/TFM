Shader "Custom/Terrain" {
	Properties {
		testTexture("Texture", 2D) = "white"{}
		testScale("Scale", Float) = 1	
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM

		#pragma surface surf Standard fullforwardshadows

		#pragma target 3.0

		const static int maxColourCount = 8;
		const static float epsilon = 1E-4;

		int baseColourCount;
		float3 baseColours[maxColourCount];
		float baseStartHeights[maxColourCount];
		float baseBlends[maxColourCount];

		float minHeight;
		float maxHeight;
		
		sampler2D testTexture;
		float testScale;
		
		struct Input {
			float3 worldPos;
			float3 worldNormal;
		};

		float inverseLerp(float a, float b, float value) {
			return saturate((value-a)/(b-a));
		}
		
		// float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
		// 	float3 scaledWorldPos = worldPos / scale;
		// 	float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
		// 	float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
		// 	float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
		// 	return xProjection + yProjection + zProjection;
		// }
		void surf (Input IN, inout SurfaceOutputStandard o) {
			float heightPercent = inverseLerp(minHeight,maxHeight, IN.worldPos.y);
			float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
			
			for (int i = 0; i < baseColourCount; i ++) {
				float drawStrength = inverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i]/2, heightPercent - baseStartHeights[i]);
				o.Albedo = o.Albedo * (1-drawStrength) + baseColours[i] * drawStrength;
			}

			float3 scaleWorldPos = IN.worldPos/testScale;
			float3 yProjection = tex2D(testTexture, scaleWorldPos.xz) * blendAxes.y;
			float3 xProjection = tex2D(testTexture, scaleWorldPos.yz) * blendAxes.x;
			float3 zProjection = tex2D(testTexture, scaleWorldPos.xy) * blendAxes.z;

			o.Albedo = xProjection + yProjection + zProjection;
		}

		ENDCG
	}
	FallBack "Diffuse"
}