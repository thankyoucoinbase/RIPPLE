Shader "Shallows/Text" {
	Properties {
		_MainTex ("Font Texture", 2D) = "white" {}
		_Color ("Text Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "IgnoreProjector"="True" "PreviewType"="Plane" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" }
		LOD 0

		Pass
		{
			Name "Unlit"
			Tags { "LightMode"="UniversalForward" }

			Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
			BlendOp Add
			ColorMask RGBA
			ZTest LEqual
			ZWrite Off
			Cull Off
			Offset 0, 0

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			// Scene lighting globals (Headlamp, GlowStick, Atmosphere, Poolrooms).
			float4 _ShallowsLampPos;            // xyz position, w range
			float4 _ShallowsLampDir;            // xyz direction, w cone outer cosine
			float4 _ShallowsLampColor;          // rgb colour, w cone inner cosine
			float4 _ShallowsDark;               // x darkness
			float4 _ShallowsGlowPos[4];         // xyz position, w range
			float4 _ShallowsGlowColor[4];
			float4 _ShallowsGlowInfo;           // x active glow count
			float4 _ShallowsDressed;            // x > 0.5 when the level map is valid
			float4 _ShallowsMapRect;            // x world min x, z 1 / world width
			float4 _ShallowsLevelInfo[16];      // z level darkness / edge-fog flag
			float4 _ShallowsLevelFogRange[16];  // x start, y end, z > 0.5 uses ranged fog
			float4 _ShallowsLevelSpan[16];      // x min world x, y max world x

			TEXTURE2D(_ShallowsLevelMap);
			SAMPLER(sampler_ShallowsLevelMap);

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				half4 _Color;
			CBUFFER_END

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

		#if defined(FOG_LINEAR)
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
					float edgeDistance = saturate((edgeX - _WorldSpaceCameraPos.x) / deltaX) * viewDistance;
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
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);
				output.color = input.color * _Color;
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).w * input.color.w;

				float3 positionWS = input.positionWS;

				// Headlamp direction and distance.
				float3 toLamp = _ShallowsLampPos.xyz - positionWS;
				float lampDistance = sqrt(dot(toLamp, toLamp));
				toLamp /= max(lampDistance, 0.0000999999975);

				// Glow sticks.
				int glowCount = (int)_ShallowsGlowInfo.x;
				half3 light = half3(0.0, 0.0, 0.0);
				for (int i = 0; i < 4; i++)
				{
					if (i >= glowCount)
						break;
					float3 toGlow = _ShallowsGlowPos[i].xyz - positionWS;
					float glowDistance = sqrt(dot(toGlow, toGlow));
					float glowFalloff = max(1.0 - glowDistance / max(0.00999999978, _ShallowsGlowPos[i].w), 0.0);
					toGlow /= max(glowDistance, 0.0000999999975);
					float glowWrap = saturate(dot(toLamp, toGlow)) * 0.699999988 + 0.300000012;
					light += _ShallowsGlowColor[i].xyz * (glowWrap * (glowFalloff * glowFalloff));
				}

				// Headlamp spot cone and range.
				bool lampOn = dot(_ShallowsLampColor.xyz, float3(1.0, 1.0, 1.0)) > 0.0;
				float lampCone = smoothstep(_ShallowsLampDir.w, _ShallowsLampColor.w, dot(-toLamp, _ShallowsLampDir.xyz));
				float lampRange = max(1.0 - lampDistance / max(_ShallowsLampPos.w, 0.00999999978), 0.0);
				float lampWrap = min(dot(toLamp, toLamp), 1.0) * 0.800000012 + 0.200000003;
				if (lampOn)
					light += _ShallowsLampColor.xyz * (((lampRange * lampRange) * lampCone) * lampWrap);

				// Level darkness.
				bool dressed = 0.5 < _ShallowsDressed.x;
				int level = dressed ? ShallowsLevelIndex(positionWS) : 0;
				half darkness = _ShallowsDark.x;
				if (dressed)
					darkness = max(_ShallowsDark.x, _ShallowsLevelInfo[level].z);
				darkness = saturate(darkness);
				half ambient = 1.0 - darkness;
				half brightness = saturate(ambient + dot(light, half3(0.300000012, 0.5, 0.200000003)));

				half3 color = brightness * input.color.xyz;
			#if defined(FOG_LINEAR)
				color = ApplyShallowsFog(color, positionWS, dressed, level);
			#endif
				return half4(color, alpha);
			}
			ENDHLSL
		}
	}
}
