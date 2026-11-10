Shader "Shallows/Water" {
	Properties {
		_WaterColor ("Water Color", Color) = (0.23,0.67,0.82,0.8)
		_LineColor ("Caustic Line Color", Color) = (0.78,0.96,1,1)
		_FoamColor ("Foam Color", Color) = (0.96,1,1,1)
		_PixelsPerMeter ("Pixels Per Meter", Float) = 40
		_Frames ("Animation Frames Per Second", Float) = 12
		_WaveStrength ("Wave Crest Strength", Range(0, 1)) = 0.55
		_WaveSize ("Wave Size (m)", Float) = 2.2
		_WaveSpeed ("Wave Speed", Float) = 1
		[NoScaleOffset] _FoamMap ("Foam Distance Map", 2D) = "white" {}
		_FoamMapRect ("Foam Map Rect (min x, min z, size x, size z)", Vector) = (0,0,1,1)
		_FoamWidth ("Foam Width (m)", Float) = 0.12
		_RippleSpeed ("Ripple Speed (m/s)", Float) = 1.1
		_RippleWidth ("Ripple Width (m)", Float) = 0.045
		_RippleLife ("Ripple Life (s)", Float) = 2.2
	}
	SubShader {
		Tags { "IgnoreProjector" = "True" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" }
		LOD 0

		Pass
		{
			Name "Water"
			Tags { "LightMode" = "UniversalForward" }
			Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
			BlendOp Add
			ColorMask RGBA
			ZTest LEqual
			ZWrite Off
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			#define SHALLOWS_MAX_GLOWS 4
			#define SHALLOWS_MAX_LEVELS 16
			#define SHALLOWS_MAX_RIPPLES 16

			CBUFFER_START(UnityPerMaterial)
				half4 _WaterColor;
				half4 _LineColor;
				half4 _FoamColor;
				float _PixelsPerMeter;
				float _Frames;
				float _WaveStrength;
				float _WaveSize;
				float _WaveSpeed;
				float4 _FoamMapRect;
				float _FoamWidth;
				float _RippleSpeed;
				float _RippleWidth;
				float _RippleLife;
			CBUFFER_END

			// Head lamp (Headlamp).
			float4 _ShallowsLampPos;     // xyz: position, w: range
			float4 _ShallowsLampDir;     // xyz: direction, w: cone cosine where the light starts
			float4 _ShallowsLampColor;   // rgb: colour, w: cone cosine where the light is full
			// Lights-out darkness (Atmosphere, LightsOutBlackout).
			float4 _ShallowsDark;        // x: darkness 0..1
			// Glow sticks (GlowStick).
			float4 _ShallowsGlowPos[SHALLOWS_MAX_GLOWS];     // xyz: position, w: radius
			float4 _ShallowsGlowColor[SHALLOWS_MAX_GLOWS];
			float4 _ShallowsGlowInfo;    // x: active count
			// Level atmosphere (Poolrooms).
			float4 _ShallowsDressed;     // x > 0.5 when the level is dressed
			float4 _ShallowsMapRect;     // x: world x of the level map's left edge, z: 1 / map width
			float4 _ShallowsLevelInfo[SHALLOWS_MAX_LEVELS];       // z: level darkness; > 0.5 also fogs the stretch past the span
			float4 _ShallowsLevelFogRange[SHALLOWS_MAX_LEVELS];   // xy: fog start/end inside the level, z > 0.5: use it
			float4 _ShallowsLevelSpan[SHALLOWS_MAX_LEVELS];       // xy: world x extent of the level
			// Water ripples and clock (WaterVolume).
			float4 _ShallowsRipples[SHALLOWS_MAX_RIPPLES];      // xz: centre, w: start time
			float4 _ShallowsRippleData[SHALLOWS_MAX_RIPPLES];   // x: strength
			float _ShallowsTime;
			// Simulated foam field (WaterFoam).
			float4 _ShallowsFoamRect;    // xy: world xz of the field origin, z: 1 / field size, w > 0.5: active

			TEXTURE2D(_ShallowsLevelMap);
			SAMPLER(sampler_ShallowsLevelMap);
			TEXTURE2D(_FoamMap);
			SAMPLER(sampler_FoamMap);
			TEXTURE2D(_ShallowsFoamField);

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
				UNITY_VERTEX_OUTPUT_STEREO
				float4 uvAndEdgeScale : TEXCOORD1;   // xy: mesh uv, zw: metres across the mesh in x and z
			};

			Varyings vert(Attributes input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
				output.positionWS = positionWS;
				output.positionCS = TransformWorldToHClip(positionWS);
				// The water mesh is a 10 m plane; its world size is the object scale times 10.
				float4x4 objectToWorld = GetObjectToWorldMatrix();
				output.uvAndEdgeScale.z = length(objectToWorld._m00_m10_m20) * 10.0;
				output.uvAndEdgeScale.w = length(objectToWorld._m02_m12_m22) * 10.0;
				output.uvAndEdgeScale.xy = input.uv;
				return output;
			}

			float Hash21(float2 p)
			{
				p = frac(p * float2(123.339996, 456.209991));
				p += dot(p, p + 45.3199997);
				return frac(p.x * p.y);
			}

			float ValueNoise(float2 p)
			{
				float2 cell = floor(p);
				float2 f = frac(p);
				float2 blend = f * f * (3.0 - 2.0 * f);
				float a = Hash21(cell);
				float b = Hash21(cell + float2(1.0, 0.0));
				float c = Hash21(cell + float2(0.0, 1.0));
				float d = Hash21(cell + float2(1.0, 1.0));
				return lerp(lerp(a, b, blend.x), lerp(c, d, blend.x), blend.y);
			}

			int ShallowsLevelIndex(float positionX)
			{
				float2 levelMapUV;
				levelMapUV.x = (positionX - _ShallowsMapRect.x) * _ShallowsMapRect.z;
				levelMapUV.y = 0.5;
				half levelSample = SAMPLE_TEXTURE2D_LOD(_ShallowsLevelMap, sampler_ShallowsLevelMap, levelMapUV, 0.0).x;
				return min(int(levelSample * half(16.0)), 15);
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float3 positionWS = input.positionWS;

				// Snap to the pixel-art grid and step time at _Frames per second.
				float metresPerPixel = 1.0 / _PixelsPerMeter;
				float2 pixelCell = floor(positionWS.xz * _PixelsPerMeter);
				float2 pixelCentre = pixelCell + 0.5;
				float2 snappedXZ = metresPerPixel.xx * pixelCentre;
				float frameTime = floor(_ShallowsTime * _Frames) / _Frames;
				float waveTime = frameTime * _WaveSpeed;

				// Wave crest lines: banded contours of two noise octaves, gated by a third.
				float2 waveUV = snappedXZ / max(_WaveSize, 0.100000001).xx;
				float4 drift = waveTime.xxxx * float4(0.0599999987, 0.0350000001, -0.0500000007, 0.100000001);
				float noiseA = ValueNoise(waveUV + drift.xy);
				float2 rotatedUV;
				rotatedUV.x = waveUV.x * 0.600000024 - waveUV.y * 0.800000012;
				rotatedUV.y = dot(waveUV, float2(0.800000012, 0.600000024));
				float noiseB = ValueNoise(rotatedUV * 1.70000005 + drift.zx);
				float waveBands = (noiseA * 0.600000024 + noiseB * 0.400000006) * 4.0 - drift.w;
				float bandOffset = frac(waveBands) - 0.5;

				float2 gateUV;
				gateUV.x = waveUV.x * 0.449999988 - drift.y;
				gateUV.y = waveUV.y * 0.449999988 + waveTime * 0.0250000004;

				float gateNoise = ValueNoise(gateUV + float2(13.1000004, 7.69999981));
				float gate = floor(smoothstep(0.379999995, 0.680000007, gateNoise) * 4.0) * 0.25;
				float2 crest;
				crest.x = abs(bandOffset) < 0.100000001 ? gate : 0.0;
				crest.y = abs(bandOffset) < 0.0350000001 ? crest.x : 0.0;

				// Shore foam from the baked distance map and the mesh edges, wobbled by a fine noise.
				float foamNoise = ValueNoise(snappedXZ * 2.5 + waveTime.xx * float2(0.150000006, -0.100000001));
				float2 foamMapUV = (metresPerPixel * pixelCentre - _FoamMapRect.xy) / _FoamMapRect.zw;
				// The map stores half the distance to the shore.
				float halfShoreDistance = SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap, foamMapUV).x;
				float shoreDistance = halfShoreDistance + halfShoreDistance;
				float2 edgeDistances = min(1.0 - input.uvAndEdgeScale.xy, input.uvAndEdgeScale.xy) * input.uvAndEdgeScale.zw;
				shoreDistance = min(shoreDistance, min(edgeDistances.x, edgeDistances.y));

				half foamCore = shoreDistance < foamNoise * 0.0599999987 + _FoamWidth ? 1.0 : 0.0;
				half foamOuter = shoreDistance < _FoamWidth * 2.20000005 + foamNoise * 0.0899999961 ? 1.0 : 0.0;

				// Simulated foam (wakes, splashes) from WaterFoam's field.
				if (0.5 < _ShallowsFoamRect.w)
				{
					float2 fieldOffset = metresPerPixel * pixelCentre - _ShallowsFoamRect.xy;
					float2 fieldUV = fieldOffset * _ShallowsFoamRect.zz;
					float2 fieldEdge = min(fieldUV, 1.0 - fieldOffset * _ShallowsFoamRect.zz);
					if (0.0 < fieldEdge.x && 0.0 < fieldEdge.y)
					{
						half fieldSample = SAMPLE_TEXTURE2D(_ShallowsFoamField, sampler_FoamMap, fieldUV).x;
						float edgeFade = saturate(min(fieldEdge.x, fieldEdge.y) / (_ShallowsFoamRect.z * 3.0));
						float field = fieldSample * edgeFade;
						float sparkle = Hash21(floor(frameTime * 3.0).xx * 7.0 + pixelCell);
						float2 thresholds = foamNoise.xx * 0.180000007 + float2(0.5, 0.219999999);
						bool2 aboveThreshold = thresholds < field;
						bool strongField = 0.300000012 < field;
						bool sparkleOn = aboveThreshold.x || (strongField && 1.25 - fieldSample * edgeFade < sparkle);
						foamCore = max(foamCore, sparkleOn ? 1.0 : 0.0);
						foamOuter = max(foamOuter, aboveThreshold.y ? 1.0 : 0.0);
					}
				}

				// Ripple rings and their fainter echo ring.
				half rippleRing = 0.0;
				half rippleEcho = 0.0;
				for (int i = 0; i < SHALLOWS_MAX_RIPPLES; i++)
				{
					float age = _ShallowsTime - _ShallowsRipples[i].w;
					if (0.0 < age && age < _RippleLife)
					{
						float strength = _ShallowsRippleData[i].x;
						float radius = age * _RippleSpeed;
						float spread = 0.5 + strength;
						float2 toCentre = metresPerPixel * pixelCentre - _ShallowsRipples[i].xz;

						float ringDistance = sqrt(dot(toCentre, toCentre)) - radius * spread;
						float width = max(metresPerPixel, (age * 0.5 + 1.0) * _RippleWidth);
						float life = 1.0 - age / _RippleLife;
						float fade = (life * life * strength) * (3.0 - 2.0 * life);
						bool onRing = abs(ringDistance) < width;
						half ringCandidate = max(rippleRing, fade);
						bool onEcho = abs(abs(ringDistance) - width * 3.0) < width * 0.800000012;
						half echoCandidate = max(rippleEcho, fade * 0.699999988);
						if (onRing)
						{
							rippleRing = ringCandidate;
						}
						else if (onEcho)
						{
							rippleEcho = echoCandidate;
						}

					}
				}

				// Surface colour.
				crest *= _WaveStrength;
				float3 baseColor = lerp(_WaterColor.rgb, _LineColor.rgb, crest.x * 0.25);
				baseColor = lerp(baseColor, _LineColor.rgb, crest.y);
				half2 outerFoam = foamOuter.xx * half2(0.550000012, 0.5);
				half3 ripple = half3(rippleEcho, rippleRing, rippleRing) * half3(0.600000024, 0.850000024, 0.699999988);
				half lineAmount = max(outerFoam.x, ripple.x);
				half3 albedo = lerp((half3)baseColor, _LineColor.rgb, lineAmount);
				albedo = lerp(albedo, _FoamColor.rgb, ripple.y);
				albedo = lerp(albedo, _FoamColor.rgb, foamCore);
				half alphaBoost = max(max(foamCore, ripple.z), outerFoam.y) * 0.400000006 + _WaterColor.a;
				float alpha = saturate(crest.y * 0.100000001 + alphaBoost);

				// Ambient brightness: global darkness, raised to the current level's darkness when dressed.
				bool dressed = 0.5 < _ShallowsDressed.x;
				int level = dressed ? ShallowsLevelIndex(positionWS.x) : 0;
				half darkness = _ShallowsDark.x;
				if (dressed)
				{
					darkness = max(_ShallowsDark.x, _ShallowsLevelInfo[level].z);
				}
				half ambient = 1.0 - saturate(darkness);

				// Glow sticks.
				int glowCount = int(_ShallowsGlowInfo.x);
				half3 light = 0.0;
				for (int g = 0; g < SHALLOWS_MAX_GLOWS; g++)
				{
					if (g >= glowCount)
					{
						break;
					}
					float3 toGlow = _ShallowsGlowPos[g].xyz - positionWS;
					float2 distanceAndFalloff;
					distanceAndFalloff.x = sqrt(dot(toGlow, toGlow));
					distanceAndFalloff.y = 1.0 - distanceAndFalloff.x / max(0.00999999978, _ShallowsGlowPos[g].w);
					distanceAndFalloff = max(distanceAndFalloff, float2(9.99999975e-05, 0.0));
					float facing = saturate(toGlow.y / distanceAndFalloff.x) * 0.699999988 + 0.300000012;
					light = _ShallowsGlowColor[g].rgb * ((distanceAndFalloff.y * distanceAndFalloff.y) * facing) + light;
				}

				// Head lamp: spot cone with a squared range falloff.
				bool lampOn = 0.0 < dot(_ShallowsLampColor.rgb, float3(1.0, 1.0, 1.0));
				float3 toLamp = _ShallowsLampPos.xyz - positionWS;
				float lampDistance = sqrt(dot(toLamp, toLamp));
				float3 lampDirection = toLamp / max(lampDistance, 9.99999975e-05);
				float cone = smoothstep(_ShallowsLampDir.w, _ShallowsLampColor.w, dot(-lampDirection, _ShallowsLampDir.xyz));

				float range = max(1.0 - lampDistance / max(_ShallowsLampPos.w, 0.00999999978), 0.0);
				float facingUp = saturate(lampDirection.y) * 0.800000012 + 0.200000003;
				float lampAmount = ((range * range) * cone) * facingUp;
				if (lampOn)
				{
					light = _ShallowsLampColor.rgb * lampAmount + light;
				}

				half3 color = albedo * ambient + albedo * light;


			#if defined(FOG_LINEAR)
				// Linear fog, with per-level overrides: fog only past the level span, or a level's own fog range.
				// fog.rgb is the colour the far fade blends to, fog.a the weight of unity_FogColor in the mix and
				// visibility the weight of the lit colour.
				float viewDistance = length(positionWS - _WorldSpaceCameraPos);
				float linearFog = saturate(viewDistance * unity_FogParams.z + unity_FogParams.w);
				half4 fog;
				fog.a = 1.0 - (half)linearFog;
				half visibility;
				if (dressed)
				{
					float cameraX = _WorldSpaceCameraPos.x;
					bool cameraInSpan = cameraX >= _ShallowsLevelSpan[level].x && cameraX < _ShallowsLevelSpan[level].y;
					float spanEdgeX = cameraX < _ShallowsLevelSpan[level].x ? _ShallowsLevelSpan[level].x : _ShallowsLevelSpan[level].y;
					float cameraToPixelX = positionWS.x - cameraX;
					bool crossesX = 9.99999975e-05 < abs(cameraToPixelX);
					float outsideDistance = saturate((spanEdgeX - cameraX) / cameraToPixelX) * viewDistance;
					outsideDistance = crossesX ? outsideDistance : 0.0;
					outsideDistance = cameraInSpan ? -1.0 : outsideDistance;
					if (0.5 < _ShallowsLevelFogRange[level].z)
					{
						half outsideVisibility = outsideDistance < 0.0 ? 1.0 : (half)saturate(outsideDistance * unity_FogParams.z + unity_FogParams.w);
						float insideDistance = viewDistance - max(outsideDistance, 0.0);
						float levelVisibility = saturate((_ShallowsLevelFogRange[level].y - insideDistance) / max(_ShallowsLevelFogRange[level].y - _ShallowsLevelFogRange[level].x, 0.00999999978));
						visibility = outsideVisibility * levelVisibility;
						fog.a = 1.0 - outsideVisibility * (half)levelVisibility;
						fog.rgb = unity_FogColor.rgb;
					}
					else
					{
						bool outsideFogged = outsideDistance >= 0.0;
						bool levelFogged = 0.5 < _ShallowsLevelInfo[level].z;
						bool fogged = levelFogged && outsideFogged;


						half outsideClear = 1.0 - (half)saturate(outsideDistance * unity_FogParams.z + unity_FogParams.w);
						fog.rgb = unity_FogColor.rgb;
						fog = fogged ? half4(outsideClear * unity_FogColor.rgb, outsideClear) : fog;
						visibility = linearFog;
					}
				}
				else
				{
					fog.rgb = unity_FogColor.rgb;
					visibility = linearFog;
				}
				color = color * visibility + fog.a * unity_FogColor.rgb;
				// Fade to the fog colour over the last stretch before the far plane.
				float farFade = saturate((viewDistance - _ProjectionParams.z * 0.75) / (_ProjectionParams.z * 0.200000003));
				half3 towardFog = fog.rgb - color;
				return half4(farFade * towardFog + color, alpha);
			#endif

				return half4(color, alpha);
			}
			ENDHLSL
		}
	}
	FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
