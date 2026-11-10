Shader "Hidden/Shallows/Foam Field" {
	Properties {
		_MainTex ("Last Frame", 2D) = "black" {}
	}
	SubShader {
		Tags { "RenderPipeline" = "UniversalPipeline" }
		LOD 0

		Pass
		{
			Blend One Zero, One Zero
			BlendOp Add
			ColorMask RGBA
			ZTest Always
			ZWrite Off
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			#define FOAM_MAX_SHAPES 64

			// Set by WaterFoam each step.
			float4 _FoamShift;    // xy: scroll since last frame (uv), z: one texel (uv), w: decay per step
			float4 _FoamOrigin;   // xy: world xz of uv (0,0), z: world size of the field
			float _FoamFade;
			float _FoamSpread;
			float _FoamCount;
			float4 _FoamShapes[FOAM_MAX_SHAPES];   // xy: centre (world xz), zw: half extents
			float4 _FoamLooks[FOAM_MAX_SHAPES];    // xy: cos/sin of the rotation, z: corner radius, w: edge softness
			float4 _FoamAmounts[FOAM_MAX_SHAPES];  // x: strength, y: interior floor

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

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
			};

			Varyings vert(Attributes input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);
				// No stereo output: the shipped program does not pass the eye index (this pass renders the foam
				// field render texture, not the eye buffers).

				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.uv = input.uv;
				return output;
			}

			half frag(Varyings input) : SV_Target
			{
				// Carry last frame's foam along with the scroll, blur it toward its neighbours and decay it.
				float2 previousUV = input.uv + _FoamShift.xy;
				float carried;
				if (all(0.0 < previousUV) && all(previousUV < 1.0))
				{
					float texel = _FoamShift.z;
					float centre = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, previousUV).x;
					float neighbours = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, previousUV + float2(texel, 0.0)).x
						+ SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, previousUV - float2(texel, 0.0)).x
						+ SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, previousUV + float2(0.0, texel)).x
						+ SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, previousUV - float2(0.0, texel)).x;
					float blurred = lerp(centre, neighbours * 0.25, _FoamSpread);
					carried = max(blurred * _FoamFade - _FoamShift.w, 0.0);
				}
				else
				{
					carried = 0.0;
				}

				// Stamp each foam shape (a rotated rounded box) on top.
				float2 positionXZ = input.uv * _FoamOrigin.zz + _FoamOrigin.xy;
				int shapeCount = int(_FoamCount);
				float foam = carried;
				[loop]
				for (int i = 0; i < FOAM_MAX_SHAPES; i++)
				{
					if (i >= shapeCount)
					{
						break;
					}
					float2 offset = positionXZ - _FoamShapes[i].xy;
					float2 local;
					local.x = offset.x * _FoamLooks[i].x - offset.y * _FoamLooks[i].y;
					local.y = offset.x * _FoamLooks[i].y + offset.y * _FoamLooks[i].x;

					float cornerRadius = _FoamLooks[i].z;
					float2 q = abs(local) - (_FoamShapes[i].zw - cornerRadius.xx);
					float2 outside = max(q, 0.0);
					float outsideDistance = sqrt(dot(outside, outside));
					float signedDistance = outsideDistance + min(max(q.x, q.y), 0.0);
					signedDistance = signedDistance - cornerRadius;

					float softness = max(0.00999999978, _FoamLooks[i].w);
					bool outsideShape = signedDistance >= 0.0;
					float outerFoam = saturate(1.0 - signedDistance / softness) * _FoamAmounts[i].x;
					float innerFoam = saturate((signedDistance + signedDistance) / softness + 1.0) * _FoamAmounts[i].x;
					innerFoam = max(innerFoam, _FoamAmounts[i].y);
					float shapeFoam = outsideShape ? outerFoam : innerFoam;
					foam = max(foam, shapeFoam);
				}
				return half(saturate(foam));
			}
			ENDHLSL
		}
	}
}
