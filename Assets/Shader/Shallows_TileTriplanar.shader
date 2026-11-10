Shader "Shallows/TileTriplanar" {
	Properties {
		_MainTex ("Tiles", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_TilesPerMeter ("Texture Repeats Per Meter", Float) = 1.25
		_Ambient ("Ambient Light", Color) = (0.62,0.62,0.58,1)
		_UnderwaterTint ("Underwater Tint", Color) = (0.6,0.86,0.88,1)
		_CausticStrength ("Caustics", Float) = 0.3
		_Wear ("Wear (poolrooms look)", Range(0, 1.5)) = 1
		_TexturePixels ("Texture Pixels Across", Float) = 64
		[Toggle] _ObjectSpace ("Tiles Stick To The Object (moving platforms)", Float) = 0
	}
	SubShader {
		Tags { "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		// Member order follows the shipped UnityPerMaterial block (not the Properties order).
		CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			half4 _Color;
			float _TilesPerMeter;
			half4 _Ambient;
			half4 _UnderwaterTint;
			float _CausticStrength;
			float _ObjectSpace;
			float _Wear;
			float _TexturePixels;
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

			// Scene globals driven by the game (Atmosphere, Poolrooms, ShallowsLights, WaterVolume).
			float _ShallowsWaterY;
			float _ShallowsTime;
			float _ShallowsProbeFlat;           // > 0.5 while rendering reflection probes: flat ambient only
			float4 _ShallowsLampPos;            // xyz position, w range
			float4 _ShallowsLampDir;            // xyz direction, w outer cone cosine
			float4 _ShallowsLampColor;          // rgb colour, w inner cone cosine
			float4 _ShallowsDark;
			float4 _ShallowsGlowPos[4];         // xyz position, w range
			float4 _ShallowsGlowColor[4];
			float4 _ShallowsGlowInfo;           // x active glow count
			float4 _ShallowsDressed;            // x dressed flag, z level lamp strength, w light steps
			float4 _ShallowsMapRect;            // xy map origin (world xz), zw inverse size
			float4 _ShallowsLevelTint[16];      // rgb tile tint, w wear scale
			float4 _ShallowsLevelLamp[16];
			float4 _ShallowsLevelInfo[16];      // x floor height, y ambient scale, z darkness
			float4 _ShallowsLevelFogRange[16];
			float4 _ShallowsLevelSpan[16];

			TEXTURE2D(_MainTex);
			TEXTURE2D(_ShallowsLevelMap);
			TEXTURE2D(_ShallowsLightMap);
			TEXTURE2D(_ShallowsNoiseTex);       // 64x64 RGBAHalf white noise, bilinear, repeat
			SAMPLER(sampler_MainTex);
			SAMPLER(sampler_ShallowsLevelMap);
			SAMPLER(sampler_ShallowsLightMap);
			SAMPLER(sampler_ShallowsNoiseTex);
			SAMPLER(sampler_linear_repeat);

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float3 triplanarPosition : TEXCOORD3;
				float3 triplanarNormal : TEXCOORD4;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// Hash of a seed already multiplied by its scale (normally 123.34, 456.21).
			float HashScaled(float2 scaledSeed)
			{
				float2 h = frac(scaledSeed);
				h = dot(h, h + 45.32) + h;
				return frac(h.y * h.x);
			}

			// Value noise: smoothstepped texel coordinate into the bilinear noise texture.
			half SmoothNoise(float2 coord)
			{
				float2 cell = floor(coord);
				float2 f = coord - cell;
				float2 smoothCoord = f * f * (f * -2.0 + 3.0) + cell;
				return SAMPLE_TEXTURE2D_LOD(_ShallowsNoiseTex, sampler_linear_repeat, (smoothCoord + 0.5) * 0.015625, 0.0).w;
			}

			// 2x2 ordered dither index (0, 2, 3, 1).
			float Bayer2(float2 cell)
			{
				return cell.y * 3.0 + (cell.x + cell.x) - cell.x * cell.y * 4.0;
			}

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
				wallTerm = weighted.w * wallTerm;
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
				float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.normalWS = normalWS;

				// Object-space tiling keeps tiles stuck to moving platforms at the object's scale.
				float4x4 objectToWorld = GetObjectToWorldMatrix();
				float3 objectScale;
				objectScale.x = length(objectToWorld._m00_m10_m20);
				objectScale.y = length(objectToWorld._m01_m11_m21);
				objectScale.z = length(objectToWorld._m02_m12_m22);
				objectScale *= input.positionOS.xyz;
				bool objectSpace = 0.5 < _ObjectSpace;
				output.triplanarPosition = objectSpace ? objectScale : positionWS;
				output.triplanarNormal = objectSpace ? input.normalOS : normalWS;
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				// Triplanar projection: pick the dominant axis.
				float3 tilePosition = input.triplanarPosition * _TilesPerMeter;
				float3 absNormal = abs(input.triplanarNormal);
				bool yDominant = absNormal.y >= absNormal.x && absNormal.y >= absNormal.z;
				bool xOverZ = absNormal.x >= absNormal.z;
				float2 uv = xOverZ ? tilePosition.zy : tilePosition.xy;
				uv = yDominant ? tilePosition.xz : uv;

				half3 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).xyz;
				half3 tinted = baseColor * _Color.xyz;

				if (0.5 < _ShallowsProbeFlat)
				{
					return half4(tinted * _Ambient.xyz, 1.0);
				}

				float3 normalWS = normalize(input.normalWS);

				// Level lookup along world x.
				bool dressed = 0.5 < _ShallowsDressed.x;
				float2 levelUV = float2((input.positionWS.x - _ShallowsMapRect.x) * _ShallowsMapRect.z, 0.5);
				half levelSample = SAMPLE_TEXTURE2D_LOD(_ShallowsLevelMap, sampler_ShallowsLevelMap, levelUV, 0.0).x;
				half levelCoord = levelSample * 16.0;
				int level = min((int)levelCoord, 15);
				level = dressed ? level : 0;

				half darkness;
				UNITY_BRANCH
				if (dressed)
					darkness = max(_ShallowsDark.x, _ShallowsLevelInfo[level].z);
				else
					darkness = _ShallowsDark.x;
				darkness = saturate(darkness);
				half brightness = 1.0 - darkness;

				half3 color;
				half3 levelLight;
				half levelAmbientScale;
				if (dressed)
				{
					// Poolrooms wear: per-tile shade, stains, cracks, chips, missing tiles, streaks, waterline.
					float faceAxis = xOverZ ? 1.0 : 2.0;
					faceAxis = yDominant ? 0.0 : faceAxis;
					float faceSide = (0.0 < dot(input.triplanarNormal, float3(1.0, 1.0, 1.0))) ? 0.0 : 3.0;
					float face = faceAxis + faceSide;
					float wear = _Wear * _ShallowsLevelTint[level].w;

					float2 pixel = floor(uv * _TexturePixels);
					float2 tileUV = uv * 4.0;
					float2 tile = floor(tileUV);
					float2 tilePixel = floor(frac(tileUV) * 16.0);
					float2 pixelQuad = pixel * 0.25;

					half4 tileHash = SAMPLE_TEXTURE2D_LOD(_ShallowsNoiseTex, sampler_ShallowsNoiseTex, (face * float2(17.0, 29.0) + tile + 0.5) * 0.015625, 0.0);
					bool grout = tilePixel.x < 0.5 || 14.5 < tilePixel.y;
					half missingThreshold = wear * 0.008;
					bool missingTile = tileHash.z < missingThreshold && !grout;

					if (!missingTile)
					{
						color = tinted * _ShallowsLevelTint[level].xyz;
						float faceOffset = face * 31.7;
						half tileShade = tileHash.x - 0.5;
						tileShade = tileShade * 0.1;
						tileShade = tileShade * wear + 1.0;
						color *= tileShade;

						float stainScale = 0.9 / _TexturePixels;
						half stainNoise = SmoothNoise(pixel * stainScale + faceOffset);
						half stain = smoothstep(0.0, 1.0, (stainNoise - 0.35) * 2.0);
						float lowDirt = saturate(-(input.positionWS.y - 0.3) * 0.5 + 1.0) * 0.6;
						half baseDirt = yDominant ? 0.27 : lowDirt;
						half dirt = saturate(wear * (stain * 0.8 + baseDirt));
						half3 groutDirt = dirt * half3(-0.36, -0.399999976, -0.53) + 1.0;
						half3 tileDirt = dirt * half3(-0.0250000060, -0.0349999964, -0.0749999881) + 1.0;
						color *= grout ? groutDirt : tileDirt;

						UNITY_BRANCH
						if (!grout)
						{
							half crackThreshold = -wear * 0.035 + 1.0;
							bool cracked = crackThreshold < tileHash.y && tilePixel.x < 4.5 && 10.5 < tilePixel.y
								&& (tilePixel.x - tilePixel.y + 15.0) < 5.5;
							UNITY_BRANCH
							if (cracked)
							{
								float crackShade = HashScaled(pixel * float2(123.34, 456.21)) * 0.3 + 0.85;
								color = crackShade * float3(0.45, 0.42, 0.36);
							}
							else
							{
								half chipChance = wear * 0.05;
								bool chipped = tileHash.y < chipChance;
								half chipSlope = tileHash.w * 1.8 - 0.9;
								half chipBase = floor(frac(tileHash.x * 91.7 + tileHash.z) * 9.0) + 3.0;
								float tileId = tile.y * 7.0 + tile.x;
								float chipJitter = (float)(0.65 < HashScaled(float2(tilePixel.x, tileId) * float2(123.34, 456.21)));
								float chipLine = (tilePixel.x - 1.0) * chipSlope + chipBase;
								float chipRow = floor(chipJitter + chipLine);
								bool chipInRange = chipRow < 14.5 && (0.5 < chipRow && 0.5 < tilePixel.x);
								bool onChipRow = chipRow == tilePixel.y;
								half3 chipShadow = color * 0.45;
								bool belowChipRow = chipRow - 1.0 == tilePixel.y;
								half3 chipLit = color * 1.08;
								half3 chipColor = belowChipRow ? chipLit : color;
								chipColor = onChipRow ? chipShadow : chipColor;
								chipColor = chipInRange ? chipColor : color;
								color = chipped ? chipColor : color;
							}
						}

						if (!yDominant)
						{
							// Vertical streaks hanging from the ceiling, per pixel column pair.
							float columnHash = HashScaled(float2(floor(pixel.x * 0.5), face * 31.7 + 5.3) * float2(123.34, 456.21));
							bool streakedColumn = 0.86 < columnHash;
							float streakLength = frac(columnHash * 37.3) * 0.5 + 0.15;
							float levelHeight = _ShallowsLevelInfo[level].x;
							float streakSpan = streakLength * levelHeight;
							float streak = input.positionWS.y - (levelHeight - streakLength * levelHeight);
							streak /= streakSpan;
							bool inStreak = 0.0 < streak;
							streak = saturate(streak) * 0.55;
							half wearAmount = saturate(wear);
							streak *= wearAmount;
							float3 streakColor = color * (streak * float3(-0.220000029, -0.3, -0.480000019) + 1.0);
							half3 streaked = inStreak ? streakColor : (float3)color;
							streaked = streakedColumn ? streaked : color;

							// Ragged waterline band; seed is (pixel.x, face * 31.7) scaled by (123.34, 456.21).
							float2 waterBand = _ShallowsWaterY + float2(0.03, -0.01);
							float waterlineTop = HashScaled(float2(pixel.x, face) * float2(123.34, 14461.857)) * 0.04 + waterBand.x;
							bool atWaterline = input.positionWS.y < waterlineTop && waterBand.y < input.positionWS.y;
							half3 waterlineColor = streaked * (wearAmount * half3(-0.25, -0.3, -0.5) + 1.0);
							color = atWaterline ? waterlineColor : streaked;
						}
					}
					else
					{
						// Missing tile: dark backing, darker at the grout edges. Seed is pixel * 0.37.
						float holeShade = HashScaled(pixel * float2(45.6357994, 168.797699)) * 0.3 + 0.85;
						half3 holeColor = (half)holeShade * half3(0.42, 0.39, 0.33);
						bool holeEdge = tilePixel.x < 1.5 || 13.5 < tilePixel.y;
						half3 holeEdgeColor = (half)holeShade * half3(0.252, 0.234, 0.198000014);
						color = holeEdge ? holeEdgeColor : holeColor;
					}

					// Level light, posterized with a 4x4 ordered dither in texture pixels.
					float lightSteps = max(_ShallowsDressed.w, 1.0);
					half term = LevelLightTerm(input.positionWS, normalWS, level);
					float2 ditherCell = pixel - floor(pixelQuad) * 4.0;
					float2 ditherHigh = floor(ditherCell * 0.5);
					float2 ditherLow = ditherCell - ditherHigh * 2.0;
					float ditherIndex = Bayer2(ditherLow) * 4.0 + Bayer2(ditherHigh);
					float ditherThreshold = (ditherIndex + 0.5) * 0.0625;
					term = term * lightSteps + ditherThreshold;
					term = floor(term);
					term /= lightSteps;
					float3 levelLampColor = _ShallowsDressed.zzz * _ShallowsLevelLamp[level].xyz;
					levelLight = term * levelLampColor;
					levelAmbientScale = _ShallowsLevelInfo[level].y;
				}
				else
				{
					color = tinted;
					levelLight = half3(0.0, 0.0, 0.0);
					levelAmbientScale = 1.0;
				}

				float mainNdotL = saturate(dot(normalWS, _MainLightPosition.xyz));
				float3 light = _MainLightColor.xyz * mainNdotL + _Ambient.xyz;
				light = light * levelAmbientScale + levelLight;
				half3 localLight = LocalLights(input.positionWS, normalWS);
				light = light * brightness + localLight;
				color = light * color;

				// Underwater: tint toward _UnderwaterTint and add animated caustics.
				float waterDepth = _ShallowsWaterY - input.positionWS.y;
				if (0.0 < waterDepth)
				{
					float depthFade = saturate(waterDepth * 8.0);
					float2 causticTime = _ShallowsTime * float2(0.5, 0.65);
					half causticA = SmoothNoise(input.positionWS.xz * 1.6 + causticTime.x);
					half causticB = SmoothNoise(input.positionWS.xz * 2.72 - causticTime.y);
					half caustic = causticA - causticB;
					caustic = -abs(caustic) * 5.0 + 1.0;
					caustic = max(caustic, 0.0);
					half3 underwaterTint = depthFade * (_UnderwaterTint.xyz - 1.0) + 1.0;
					caustic *= caustic;
					float causticLight = depthFade * (caustic * _CausticStrength);
					float3 causticColor = brightness * (causticLight * _MainLightColor.xyz);
					color = color * underwaterTint + causticColor;
				}

				#if defined(FOG_LINEAR)
				color = ApplyShallowsFog(color, input.positionWS, dressed, level);
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

		Pass {
			Name "DepthNormals"
			Tags { "LightMode"="DepthNormals" }
			Blend One Zero, One Zero
			BlendOp Add
			ColorMask RGBA
			ZTest LEqual
			ZWrite On
			Cull Back

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 normalWS : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				return half4(normalize(input.normalWS), 0.0);
			}
			ENDHLSL
		}
	}
	Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
