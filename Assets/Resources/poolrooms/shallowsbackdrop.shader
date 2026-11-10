Shader "Shallows/Backdrop" {
	Properties {
	}
	SubShader {
		Tags { "IgnoreProjector" = "True" "Queue" = "Geometry+440" "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
		LOD 0

		Pass
		{
			Name "Backdrop"
			Tags { "LightMode" = "UniversalForward" }
			Blend One Zero, One Zero
			BlendOp Add
			ColorMask RGBA
			ZTest LEqual
			ZWrite Off
			Cull Front

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			// Level atmosphere, set by Poolrooms.
			float4 _ShallowsDressed;                // x > 0.5 when the level is dressed
			float4 _ShallowsMapRect;                // x: world x of the level map's left edge, z: 1 / map width
			float4 _ShallowsLevelInfo[16];          // z: level darkness; > 0.5 also fogs the stretch past the span
			float4 _ShallowsLevelFogRange[16];      // z > 0.5: level uses its own fog range
			float4 _ShallowsLevelSpan[16];          // xy: world x extent of the level

			TEXTURE2D(_ShallowsLevelMap);
			SAMPLER(sampler_ShallowsLevelMap);

			struct Attributes
			{
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
				output.positionWS = positionWS;
				output.positionCS = TransformWorldToHClip(positionWS);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				half3 color;
			#if defined(FOG_LINEAR)
				if (0.5 < _ShallowsDressed.x)
				{
					float2 levelMapUV = float2((input.positionWS.x - _ShallowsMapRect.x) * _ShallowsMapRect.z, 0.5);
					half levelSample = SAMPLE_TEXTURE2D_LOD(_ShallowsLevelMap, sampler_ShallowsLevelMap, levelMapUV, 0.0).x;
					int level = min(int(levelSample * half(16.0)), 15);
					if (0.5 < _ShallowsLevelFogRange[level].z)
					{
						color = unity_FogColor.rgb;
					}
					else
					{
						// Fog only the stretch of the ray that lies outside the camera's level span.
						float3 cameraToPixel = input.positionWS - _WorldSpaceCameraPos;
						float pixelDistance = sqrt(dot(cameraToPixel, cameraToPixel));
						float cameraX = _WorldSpaceCameraPos.x;
						bool cameraInSpan = cameraX >= _ShallowsLevelSpan[level].x && cameraX < _ShallowsLevelSpan[level].y;
						float spanEdgeX = cameraX < _ShallowsLevelSpan[level].x ? _ShallowsLevelSpan[level].x : _ShallowsLevelSpan[level].y;
						bool crossesX = 9.99999975e-05 < abs(cameraToPixel.x);
						float fogDistance = saturate((spanEdgeX - cameraX) / cameraToPixel.x) * pixelDistance;
						fogDistance = crossesX ? fogDistance : 0.0;
						fogDistance = cameraInSpan ? -1.0 : fogDistance;
						bool fogged = fogDistance >= 0.0 && 0.5 < _ShallowsLevelInfo[level].z;
						float fogFactor = saturate(fogDistance * unity_FogParams.z + unity_FogParams.w);
						half fogClear = 1.0 - (half)fogFactor;
						half3 foggedColor = fogClear * unity_FogColor.rgb;
						color = fogged ? foggedColor : unity_FogColor.rgb;
					}
				}
				else
				{
					color = unity_FogColor.rgb;
				}
			#else
				color = unity_FogColor.rgb;
			#endif
				return half4(color, 1.0);
			}
			ENDHLSL
		}
	}
}
