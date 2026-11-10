Shader "Shallows/Prop" {
	Properties {
		_MainTex ("Atlas", 2D) = "white" {}
		_Ambient ("Ambient Light", Color) = (0.62,0.62,0.58,1)
		_Shine ("Chrome Shine", Range(0, 1)) = 0.35
	}
	SubShader {
		Tags { "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			half4 _Ambient;
			half _Shine;
		CBUFFER_END
		ENDHLSL

		Pass {
			Name "ForwardLit"
			Tags { "LightMode"="UniversalForward" }
			Blend One Zero, One Zero
			BlendOp Add
			ColorMask RGBA
			ZTest LEqual
			ZWrite On
			Cull Back

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

			// Scene lighting globals driven by the game (Atmosphere, Poolrooms, ShallowsLights).
			float4 _ShallowsLampPos;            // xyz position, w range
			float4 _ShallowsLampDir;            // xyz direction, w outer cone cosine
			float4 _ShallowsLampColor;          // rgb colour, w inner cone cosine
			float4 _ShallowsDark;
			float4 _ShallowsGlowPos[4];         // xyz position, w range
			float4 _ShallowsGlowColor[4];
			float4 _ShallowsGlowInfo;           // x active glow count
			float4 _ShallowsDressed;            // x dressed flag, z level lamp strength
			float4 _ShallowsMapRect;            // xy map origin (world xz), zw inverse size
			float4 _ShallowsLevelLamp[16];
			float4 _ShallowsLevelInfo[16];      // x floor height, y ambient scale, z darkness
			float4 _ShallowsLevelFogRange[16];
			float4 _ShallowsLevelSpan[16];
			float _ShallowsWaterY;

			TEXTURE2D(_ShallowsLevelMap);
			SAMPLER(sampler_ShallowsLevelMap);
			TEXTURE2D(_ShallowsLightMap);
			SAMPLER(sampler_ShallowsLightMap);
			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float2 uv : TEXCOORD0;
				half4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float2 uv : TEXCOORD2;
				half4 color : COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// Lamp/wall/floor/ceiling term from the level light map, picked by surface orientation.
			half LevelLightTerm(float3 positionWS, float3 normalWS, int level)
			{
				float2 mapUV = (positionWS.xz - _ShallowsMapRect.xy) * _ShallowsMapRect.zw;
				half4 lightMap = SAMPLE_TEXTURE2D_LOD(_ShallowsLightMap, sampler_ShallowsLightMap, mapUV, 0.0);
				float4 lightMap2 = lightMap + lightMap;
				half4 channels = lightMap2.zwwz * _ShallowsLevelLamp[level].wwww + lightMap2.xyyx;
				float floorHeight = max(1.0, _ShallowsLevelInfo[level].x);
				float heightFraction = saturate(positionWS.y / floorHeight);
				half4 weighted = channels * half4(0.9, 0.35, 0.75, 0.7);
				half ceilingTerm = weighted.x + weighted.y;
				bool notFacingDown = normalWS.y >= -0.5;
				bool facingUp = 0.5 < normalWS.y;
				half wallScale = (half)heightFraction * 0.6 + 0.3;
				float wallTerm = saturate((positionWS.y - floorHeight + 2.5) * 0.4);
				wallTerm *= weighted.w;
				wallTerm = channels.z * wallScale + wallTerm;
				half upperTerm = facingUp ? weighted.z : (half)wallTerm;
				half term = notFacingDown ? upperTerm : ceilingTerm;
				return term;
			}

			// Point glows plus the spot lamp.
			half3 LocalLights(float3 positionWS, float3 normalWS)
			{
				int glowCount = (int)_ShallowsGlowInfo.x;
				half3 glow = half3(0.0, 0.0, 0.0);
				for (int i = 0; i < 4; i++)
				{
					if (i >= glowCount)
						break;
					float3 toGlow = _ShallowsGlowPos[i].xyz - positionWS;
					float glowDistance = length(toGlow);
					float glowFalloff = glowDistance / max(0.01, _ShallowsGlowPos[i].w);
					glowFalloff = max(1.0 - glowFalloff, 0.0);
					toGlow /= max(glowDistance, 0.0001);
					float glowWrap = saturate(dot(normalWS, toGlow)) * 0.7 + 0.3;
					glowWrap *= glowFalloff * glowFalloff;
					glow = _ShallowsGlowColor[i].xyz * glowWrap + glow;
				}

				bool lampOn = 0.0 < dot(_ShallowsLampColor.xyz, float3(1.0, 1.0, 1.0));
				float3 toLamp = _ShallowsLampPos.xyz - positionWS;
				float lampDistance = length(toLamp);
				toLamp /= max(lampDistance, 0.0001);
				float cone = smoothstep(_ShallowsLampDir.w, _ShallowsLampColor.w, dot(-toLamp, _ShallowsLampDir.xyz));
				float lampFalloff = lampDistance / max(_ShallowsLampPos.w, 0.01);
				lampFalloff = max(1.0 - lampFalloff, 0.0);
				float lampWrap = saturate(dot(normalWS, toLamp)) * 0.8 + 0.2;
				lampWrap *= cone * (lampFalloff * lampFalloff);
				float3 withLamp = _ShallowsLampColor.xyz * lampWrap + glow;
				return lampOn ? withLamp : (float3)glow;
			}

			// Linear distance fog with per-level fog volumes and a far fade into the fog target.
			half3 ApplyShallowsFog(half3 color, float3 positionWS, bool dressed, int level)
			{
				float viewDistance = length(positionWS - _WorldSpaceCameraPos);
				float fogKeep = saturate(viewDistance * unity_FogParams.z + unity_FogParams.w);
				half4 fogTarget;
				fogTarget.a = 1.0 - (half)fogKeep;    // rgb: far fade colour, a: weight of the fog colour
				half colorKeep;
				if (dressed)
				{
					float4 span = _ShallowsLevelSpan[level];
					bool cameraInSpan = _WorldSpaceCameraPos.x >= span.x && _WorldSpaceCameraPos.x < span.y;
					float spanEdge = (_WorldSpaceCameraPos.x < span.x) ? span.x : span.y;
					float deltaX = positionWS.x - _WorldSpaceCameraPos.x;
					bool crossesX = 0.0001 < abs(deltaX);
					float distanceInSpan = saturate((spanEdge - _WorldSpaceCameraPos.x) / deltaX) * viewDistance;
					distanceInSpan = crossesX ? distanceInSpan : 0.0;
					float outsideDistance = cameraInSpan ? -1.0 : distanceInSpan;
					float4 fogRange = _ShallowsLevelFogRange[level];
					if (0.5 < fogRange.z)
					{
						float outsideFog = saturate(outsideDistance * unity_FogParams.z + unity_FogParams.w);
						half outsideKeep = (outsideDistance < 0.0) ? 1.0 : (half)outsideFog;
						float rangeKeep = fogRange.y - (viewDistance - max(outsideDistance, 0.0));
						rangeKeep /= max(fogRange.y - fogRange.x, 0.01);
						rangeKeep = saturate(rangeKeep);
						colorKeep = rangeKeep * outsideKeep;
						fogTarget.a = -outsideKeep * rangeKeep + 1.0;
						fogTarget = half4(unity_FogColor.rgb, fogTarget.a);
					}
					else
					{
						bool fogFromSpan = outsideDistance >= 0.0 && 0.5 < _ShallowsLevelInfo[level].z;
						float spanKeep = saturate(outsideDistance * unity_FogParams.z + unity_FogParams.w);
						half spanFog = 1.0 - (half)spanKeep;
						half4 spanTarget = half4(spanFog * unity_FogColor.rgb, spanFog);
						fogTarget = half4(unity_FogColor.rgb, fogTarget.a);
						fogTarget = fogFromSpan ? spanTarget : fogTarget;
						colorKeep = fogKeep;
					}
				}
				else
				{
					fogTarget = half4(unity_FogColor.rgb, fogTarget.a);
					colorKeep = fogKeep;
				}
				color = color * colorKeep + fogTarget.a * unity_FogColor.rgb;
				float farFade = saturate((viewDistance - _ProjectionParams.z * 0.75) / (_ProjectionParams.z * 0.2));
				return farFade * (fogTarget.rgb - color) + color;
			}

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
				output.positionCS = TransformWorldToHClip(positionWS);
				output.positionWS = positionWS;
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.uv = input.uv;
				output.color = input.color;
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float3 normalWS = normalize(input.normalWS);
				half3 atlas = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).xyz;
				float3 albedo = atlas * input.color.xyz;

				// Level lookup along world x.
				bool dressed = 0.5 < _ShallowsDressed.x;
				float2 levelUV = float2((input.positionWS.x - _ShallowsMapRect.x) * _ShallowsMapRect.z, 0.5);
				half levelSample = SAMPLE_TEXTURE2D_LOD(_ShallowsLevelMap, sampler_ShallowsLevelMap, levelUV, 0.0).x;
				half levelCoord = levelSample * 16.0;
				int level = min((int)levelCoord, 15);
				int shadeLevel = dressed ? level : 0;    // darkness and fog use the level zeroed when undressed

				half darkness;
				UNITY_BRANCH
				if (dressed)
					darkness = max(_ShallowsDark.x, _ShallowsLevelInfo[shadeLevel].z);
				else
					darkness = _ShallowsDark.x;
				darkness = saturate(darkness);
				half brightness = 1.0 - darkness;

				half3 levelLight;
				half levelAmbientScale;
				if (dressed)
				{
					float3 levelLampColor = _ShallowsDressed.zzz * _ShallowsLevelLamp[level].xyz;
					half term = LevelLightTerm(input.positionWS, normalWS, level);
					levelLight = term * levelLampColor;
					levelAmbientScale = _ShallowsLevelInfo[level].y;
				}
				else
				{
					levelLight = half3(0.0, 0.0, 0.0);
					levelAmbientScale = 1.0;
				}

				float mainNdotL = saturate(dot(normalWS, _MainLightPosition.xyz));
				float3 light = _MainLightColor.xyz * mainNdotL + _Ambient.xyz;
				light = light * levelAmbientScale + levelLight;
				half3 localLight = LocalLights(input.positionWS, normalWS);
				float3 totalLight = light * brightness + localLight;

				// Chrome highlight: vertex alpha masks it, upward faces get it strongest.
				half chrome = input.color.w * _Shine;
				chrome = brightness * chrome;
				bool2 chromeUp = float2(0.55, 0.1) < normalWS.yy;
				float chromeFacing = chromeUp.y ? 0.45 : 0.0;
				chromeFacing = chromeUp.x ? 1.0 : chromeFacing;
				chrome = chromeFacing * chrome;
				half3 chromeColor = (levelAmbientScale.xxx * 0.4 + levelLight) * chrome;
				half3 color = albedo * totalLight + chromeColor;

				bool underwater = input.positionWS.y < _ShallowsWaterY;
				color = underwater ? color * half3(0.6, 0.86, 0.88) : color;

				#if defined(FOG_LINEAR)
				color = ApplyShallowsFog(color, input.positionWS, dressed, shadeLevel);
				#endif
				return half4(color, 1.0);
			}
			ENDHLSL
		}

		Pass {
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }
			Blend One Zero, One Zero
			BlendOp Add
			ColorMask R
			ZTest LEqual
			ZWrite On
			Cull Back

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct Attributes
			{
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				return output;
			}

			half frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				return input.positionCS.z;
			}
			ENDHLSL
		}
	}
	Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
