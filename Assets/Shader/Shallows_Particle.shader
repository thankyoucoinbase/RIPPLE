Shader "Shallows/Particle" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_Cutoff ("Pixel Cut-out (0 = soft)", Range(0, 1)) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Float) = 4
	}
	SubShader {
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
		LOD 0

		Pass {
			Name "Unlit"
			Tags { "LightMode"="UniversalForward" }
			Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
			BlendOp Add
			ZTest [_ZTest]
			ZWrite Off
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float4 _MainTex_TexelSize;
				half4 _Color;
				half _Cutoff;
			CBUFFER_END

			// Shallows world state (Headlamp, GlowStick, Atmosphere, Poolrooms).
			float4 _ShallowsLampPos;            // xyz position, w range
			float4 _ShallowsLampDir;            // xyz forward, w cos(outer cone angle)
			float4 _ShallowsLampColor;          // rgb color, w cos(inner cone angle)
			float4 _ShallowsDark;               // x darkness
			float4 _ShallowsGlowPos[4];         // xyz position, w radius
			float4 _ShallowsGlowColor[4];
			float4 _ShallowsGlowInfo;           // x glow count
			float4 _ShallowsDressed;            // x > 0.5 when the level map is live
			float4 _ShallowsMapRect;            // xy origin, zw 1 / size
			float4 _ShallowsLevelInfo[16];
			float4 _ShallowsLevelFogRange[16];
			float4 _ShallowsLevelSpan[16];
			TEXTURE2D_HALF(_ShallowsLevelMap);
			SAMPLER(sampler_ShallowsLevelMap);

			TEXTURE2D_HALF(_MainTex);
			SAMPLER(sampler_MainTex);

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

			// Headlamp spot plus glow sticks; the surface is treated as facing the lamp.
			half3 ShallowsLight(float3 positionWS)
			{
				float3 toLamp = _ShallowsLampPos.xyz - positionWS;
				float lampDistance = length(toLamp);
				toLamp /= max(lampDistance, 0.0001);

				int glowCount = (int)_ShallowsGlowInfo.x;
				half3 light = 0.0;
				for (int i = 0; i < 4; i++)
				{
					if (i >= glowCount)
						break;
					float3 toGlow = _ShallowsGlowPos[i].xyz - positionWS;
					float glowDistance = length(toGlow);
					float glowFalloff = max(1.0 - glowDistance / max(0.01, _ShallowsGlowPos[i].w), 0.0);
					toGlow /= max(glowDistance, 0.0001);
					float glowWrap = saturate(dot(toLamp, toGlow)) * 0.7 + 0.3;
					light = _ShallowsGlowColor[i].rgb * ((glowFalloff * glowFalloff) * glowWrap) + light;
				}

				bool lampOn = dot(_ShallowsLampColor.rgb, float3(1.0, 1.0, 1.0)) > 0.0;
				float cone = smoothstep(_ShallowsLampDir.w, _ShallowsLampColor.w, dot(-toLamp, _ShallowsLampDir.xyz));
				float range = max(1.0 - lampDistance / max(_ShallowsLampPos.w, 0.01), 0.0);
				float lampFacing = min(dot(toLamp, toLamp), 1.0) * 0.8 + 0.2;
				float3 lit = _ShallowsLampColor.rgb * (((range * range) * cone) * lampFacing) + light;
				return lampOn ? (half3)lit : light;
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

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				half4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				half4 color : COLOR;
				float3 positionWS : TEXCOORD1;
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
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);
				output.color = input.color * _Color;
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				half4 albedo;
				if (_Cutoff > 0.0)
				{
					// Pixel cut-out: point-sampled texel centre, hard alpha test, opaque.
					float2 texelUV = (floor(input.uv * _MainTex_TexelSize.zw) + 0.5) * _MainTex_TexelSize.xy;
					albedo = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, texelUV, 0);
					clip(albedo.a - _Cutoff);
					albedo.a = 1.0;
				}
				else
				{
					albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
				}
				albedo *= input.color;

				half3 light = ShallowsLight(input.positionWS);
				bool dressed = 0.5 < _ShallowsDressed.x;
				int level = dressed ? ShallowsLevelIndex(input.positionWS) : 0;
				half brightness = 1.0 - ShallowsDarkness(dressed, level);
				brightness = saturate(dot(light, half3(0.3, 0.5, 0.2)) + brightness);
				half3 color = albedo.rgb * brightness;

				#if defined(FOG_LINEAR)
				color = ApplyShallowsFog(color, input.positionWS, dressed, level);
				#endif
				return half4(color, albedo.a);
			}
			ENDHLSL
		}
	}
}
