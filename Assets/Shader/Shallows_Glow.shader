Shader "Shallows/Glow" {
	Properties {
		_BaseColor ("Color", Color) = (1,1,1,1)
		_Pixels ("Panel Pixels (across, along)", Vector) = (32,16,0,0)
	}
	SubShader {
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
		LOD 0

		Pass {
			Name "Unlit"
			Tags { "LightMode"="UniversalForward" }
			Blend One Zero, One Zero
			BlendOp Add
			ZTest LEqual
			ZWrite On
			Cull Back

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				half4 _BaseColor;
				float4 _Pixels;
			CBUFFER_END

			// Shallows world state (Atmosphere, Poolrooms).
			float4 _ShallowsDark;               // x darkness
			float4 _ShallowsDressed;            // x > 0.5 when the level map is live
			float4 _ShallowsMapRect;            // xy origin, zw 1 / size
			float4 _ShallowsLevelLamp[16];      // rgb lamp tint, w lamp level
			float4 _ShallowsLevelInfo[16];
			float4 _ShallowsLevelFogRange[16];
			float4 _ShallowsLevelSpan[16];
			TEXTURE2D_HALF(_ShallowsLevelMap);
			SAMPLER(sampler_ShallowsLevelMap);
			TEXTURE2D_HALF(_ShallowsLightMap);
			SAMPLER(sampler_ShallowsLightMap);

			int ShallowsLevelIndex(float3 positionWS)
			{
				float2 mapUV = float2((positionWS.x - _ShallowsMapRect.x) * _ShallowsMapRect.z, 0.5);
				half levelCode = SAMPLE_TEXTURE2D_LOD(_ShallowsLevelMap, sampler_ShallowsLevelMap, mapUV, 0).x;
				return min((int)(levelCode * 16.0), 15);
			}

			half ShallowsDarkness(bool dressed, int level)
			{
				half darkness;
				if (dressed)
					darkness = max(_ShallowsDark.x, _ShallowsLevelInfo[level].z);
				else
					darkness = _ShallowsDark.x;
				return saturate(darkness);
			}

			#if defined(FOG_LINEAR)
			// Linear fog by view distance, clipped to the span of the level the point is in, then a fade to the
			// fog color over the last quarter of the far plane.
			half3 ApplyShallowsFog(half3 color, float3 positionWS, bool dressed, int level)
			{
				float3 fromCamera = positionWS - _WorldSpaceCameraPos;
				float viewDistance = sqrt(dot(fromCamera, fromCamera));
				float fogKeep = saturate(viewDistance * unity_FogParams.z + unity_FogParams.w);

				half keep = fogKeep;
				half4 fogTarget = half4(unity_FogColor.rgb, 1.0 - keep);    // rgb far color, a fog amount
				if (dressed)
				{
					float4 span = _ShallowsLevelSpan[level];
					bool cameraInSpan = _WorldSpaceCameraPos.x >= span.x && _WorldSpaceCameraPos.x < span.y;
					float spanEdge = (_WorldSpaceCameraPos.x < span.x) ? span.x : span.y;
					float deltaX = positionWS.x - _WorldSpaceCameraPos.x;
					bool deltaValid = 0.0001 < abs(deltaX);
					float edgeDistance = viewDistance * saturate((spanEdge - _WorldSpaceCameraPos.x) / deltaX);
					edgeDistance = deltaValid ? edgeDistance : 0.0;
					float outsideDistance = cameraInSpan ? -1.0 : edgeDistance;

					float4 fogRange = _ShallowsLevelFogRange[level];
					if (0.5 < fogRange.z)
					{
						half outsideKeep = (outsideDistance < 0.0) ? 1.0 : saturate(outsideDistance * unity_FogParams.z + unity_FogParams.w);
						float insideDistance = -max(outsideDistance, 0.0) + viewDistance;
						float insideFade = saturate((fogRange.y - insideDistance) / max(fogRange.y - fogRange.x, 0.01));
						keep = outsideKeep * insideFade;
						fogTarget.a = 1.0 - keep;
					}
					else
					{
						bool tintFog = outsideDistance >= 0.0 && 0.5 < _ShallowsLevelInfo[level].z;
						half outsideKeep = saturate(outsideDistance * unity_FogParams.z + unity_FogParams.w);
						half4 tinted;    // x fog amount, yzw tinted far color
						tinted.x = 1.0 - outsideKeep;
						tinted.yzw = tinted.xxx * unity_FogColor.rgb;
						fogTarget = tintFog ? tinted.yzwx : fogTarget;
					}
				}

				color = color * keep + fogTarget.aaa * unity_FogColor.rgb;
				float farFade = saturate((viewDistance - _ProjectionParams.z * 0.75) / (_ProjectionParams.z * 0.2));
				return color + farFade * (fogTarget.rgb - color);
			}
			#endif

			float PanelHash(float2 p)
			{
				p = frac(p * float2(123.34, 456.21));
				p += dot(p, p + 45.32);
				return frac(p.x * p.y);
			}

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float2 uv : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
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

				// Light panel made of _Pixels cells; edgeCells is each cell's distance to the panel border.
				float2 panelPixels = max(_Pixels.xy, 1.0);
				float2 lastCell = panelPixels - 1.0;
				float2 cell = min(lastCell, floor(panelPixels * saturate(input.uv)));
				float2 edgeCells = min(lastCell - cell, cell);

				// Level lamp state from the level map and light map.
				bool dressed = 0.5 < _ShallowsDressed.x;
				half3 lampTint = 1.0;
				half lampLevel = 1.0;
				bool brokenCell = false;
				if (dressed)
				{
					int lampLevelIndex = ShallowsLevelIndex(input.positionWS);
					float2 lightMapUV = (input.positionWS.xz - _ShallowsMapRect.xy) * _ShallowsMapRect.zw;
					lampTint = (_ShallowsLevelLamp[lampLevelIndex].rgb - 1.0) * 0.5 + 1.0;
					half2 lightCode = SAMPLE_TEXTURE2D_LOD(_ShallowsLightMap, sampler_ShallowsLightMap, lightMapUV, 0).xz;
					float2 lightCode2 = (float2)lightCode * 2.0;
					if (lightCode2.x < lightCode2.y)
						lampLevel = _ShallowsLevelLamp[lampLevelIndex].w * 0.9 + 0.1;
					else
						brokenCell = lightCode.x < 0.25;
				}

				half3 color;
				[branch] if (edgeCells.x < 1.0 || edgeCells.y < 1.0)
				{
					// Frame.
					color = half3(0.42, 0.42, 0.4);
				}
				else
				{
					[branch] if (brokenCell)
					{
						// Dead panel: dim grey cells with the odd darker one.
						float flicker = PanelHash(cell) * 0.2 + 0.9;
						float3 dimCell = flicker * float3(0.3, 0.29, 0.26);
						half3 deadCell = dimCell * 0.4;
						color = PanelHash(cell * 1.7 + 3.0) > 0.93 ? deadCell : dimCell;
					}
					else
					{
						float along = (cell.y + 0.5) / panelPixels.y - 0.5;
						float stripe = abs(abs(along) - 0.22) < 0.12 ? 1.0 : 0.9;
						float checker = frac((cell.y + cell.x) * 0.5) < 0.5 ? 1.0 : 0.95;
						float edgeShade = edgeCells.x < 2.0 ? 0.85 : 1.0;
						float shade = (1.0 - abs(along) * 0.12) * (edgeShade * (checker * stripe));
						half3 tinted = lampTint * _BaseColor.rgb;
						float3 lit = shade * tinted;
						lit = lampLevel * lit;
						color = (edgeCells.y < 2.0 || edgeCells.x < 2.0) ? lit * 0.9 : lit;
					}
				}

				int level = dressed ? ShallowsLevelIndex(input.positionWS) : 0;
				color = color * (1.0 - ShallowsDarkness(dressed, level));
				#if defined(FOG_LINEAR)
				color = ApplyShallowsFog(color, input.positionWS, dressed, level);
				#endif
				return half4(color, 1.0);
			}
			ENDHLSL
		}
	}
}
