Shader "Shallows/Mirror" {
	Properties {
		[NoScaleOffset] _LeftTex ("Left Eye", 2D) = "black" {}
		[NoScaleOffset] _RightTex ("Right Eye", 2D) = "black" {}
		_Tint ("Tint", Color) = (0.93,0.96,0.97,1)
		_Fade ("Fade", Range(0, 1)) = 1
		_ImageScale ("Image Scale", Vector) = (1,1,0,0)
	}
	SubShader {
		Tags { "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
		LOD 0

		Pass
		{
			Name "Unlit"
			Tags { "LightMode"="UniversalForward" }

			Blend One Zero, One Zero
			BlendOp Add
			ColorMask RGBA
			ZTest LEqual
			ZWrite On
			Cull Back
			Offset 0, 0

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			// Level fog globals (Poolrooms).
			float4 _ShallowsDressed;            // x > 0.5 when the level map is valid
			float4 _ShallowsMapRect;            // x world min x, z 1 / world width
			float4 _ShallowsLevelInfo[16];      // z edge-fog flag
			float4 _ShallowsLevelFogRange[16];  // x start, y end, z > 0.5 uses ranged fog
			float4 _ShallowsLevelSpan[16];      // x min world x, y max world x

			TEXTURE2D(_ShallowsLevelMap);
			SAMPLER(sampler_ShallowsLevelMap);

			// Mirror camera renders, one per eye (Mirror.cs).
			TEXTURE2D(_LeftTex);
			SAMPLER(sampler_LeftTex);
			TEXTURE2D(_RightTex);
			SAMPLER(sampler_RightTex);

			CBUFFER_START(UnityPerMaterial)
				half4 _Tint;
				half _Fade;
				float4 _ImageScale;    // xy uv scale, zw uv clamp margin
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
				float3 positionWS : TEXCOORD1;
			};

		#if defined(FOG_LINEAR)
			// Level index under a world x, read from the 16-entry level strip.
			int ShallowsLevelIndex(float3 positionWS)
			{
				float2 mapUV;
				mapUV.x = (positionWS.x - _ShallowsMapRect.x) * _ShallowsMapRect.z;
				mapUV.y = 0.5;
				half levelSample = SAMPLE_TEXTURE2D_LOD(_ShallowsLevelMap, sampler_ShallowsLevelMap, mapUV, 0.0).x;
				half levelScaled = levelSample * 16.0;
				return min((int)levelScaled, 15);
			}

			// Linear distance fog, extended per level: fog can start at the level's span edge
			// (camera outside the level) and can ramp over a per-level distance range.
			half3 ApplyShallowsFog(half3 color, float3 positionWS, bool dressed, int level)
			{
				float3 toCamera = positionWS - _WorldSpaceCameraPos;
				float viewDistance = sqrt(dot(toCamera, toCamera));
				float baseFog = saturate(viewDistance * unity_FogParams.z + unity_FogParams.w);

				half fogFactor = baseFog;
				half4 fogTint = half4(unity_FogColor.xyz, 1.0 - fogFactor);
				if (dressed)
				{
					bool cameraInside = _WorldSpaceCameraPos.x >= _ShallowsLevelSpan[level].x && _WorldSpaceCameraPos.x < _ShallowsLevelSpan[level].y;
					float edgeX = (_WorldSpaceCameraPos.x < _ShallowsLevelSpan[level].x) ? _ShallowsLevelSpan[level].x : _ShallowsLevelSpan[level].y;
					float deltaX = positionWS.x - _WorldSpaceCameraPos.x;
					bool deltaValid = 0.0000999999975 < abs(deltaX);
					float edgeDistance = viewDistance * saturate((edgeX - _WorldSpaceCameraPos.x) / deltaX);
					edgeDistance = deltaValid ? edgeDistance : 0.0;
					float outsideDistance = cameraInside ? -1.0 : edgeDistance;

					if (0.5 < _ShallowsLevelFogRange[level].z)
					{
						half edgeFog = (outsideDistance < 0.0) ? (half)1.0 : (half)saturate(outsideDistance * unity_FogParams.z + unity_FogParams.w);
						float insideDistance = viewDistance - max(outsideDistance, 0.0);
						float ramp = saturate((_ShallowsLevelFogRange[level].y - insideDistance) / max(_ShallowsLevelFogRange[level].y - _ShallowsLevelFogRange[level].x, 0.00999999978));
						fogFactor = edgeFog * ramp;
						fogTint.w = 1.0 - fogFactor;
					}
					else
					{
						bool useEdgeFog = outsideDistance >= 0.0 && 0.5 < _ShallowsLevelInfo[level].z;
						half edgeFog = saturate(outsideDistance * unity_FogParams.z + unity_FogParams.w);
						half4 edgeTint;
						edgeTint.x = 1.0 - edgeFog;
						edgeTint.yzw = edgeTint.xxx * unity_FogColor.xyz;
						fogTint = useEdgeFog ? edgeTint.yzwx : fogTint;
					}
				}

				color = color * fogFactor + fogTint.www * unity_FogColor.xyz;
				half farFade = saturate((viewDistance - _ProjectionParams.z * 0.75) / (_ProjectionParams.z * 0.200000003));
				return color + farFade * (fogTint.xyz - color);
			}
		#endif

			Varyings vert(Attributes input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
				output.positionWS = positionWS;
				output.positionCS = TransformWorldToHClip(positionWS);
				output.uv = input.uv;
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float2 uv = clamp(input.uv * _ImageScale.xy, _ImageScale.zw, _ImageScale.xy - _ImageScale.zw);

				half3 color;
			#if defined(USING_STEREO_MATRICES)
				if (unity_StereoEyeIndex == 1)
					color = SAMPLE_TEXTURE2D(_RightTex, sampler_RightTex, uv).xyz;
				else
					color = SAMPLE_TEXTURE2D(_LeftTex, sampler_LeftTex, uv).xyz;
			#else
				color = SAMPLE_TEXTURE2D(_LeftTex, sampler_LeftTex, uv).xyz;
			#endif
				color *= _Tint.xyz;
				color *= _Fade;

			#if defined(FOG_LINEAR)
				bool dressed = 0.5 < _ShallowsDressed.x;
				int level = dressed ? ShallowsLevelIndex(input.positionWS) : 0;
				color = ApplyShallowsFog(color, input.positionWS, dressed, level);
			#endif
				return half4(color, 1.0);
			}
			ENDHLSL
		}
	}
}
