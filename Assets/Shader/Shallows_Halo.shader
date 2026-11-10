Shader "Shallows/Halo" {
	Properties {
		_BaseColor ("Color", Color) = (1,0.85,0.35,1)
		_Size ("Size (m)", Float) = 0.8
		_Steps ("Steps", Float) = 4
		_Pulse ("Pulse", Range(0, 1)) = 0.15
	}
	SubShader {
		Tags { "IgnoreProjector" = "True" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" }
		LOD 0

		Pass
		{
			Name "Halo"
			Tags { "LightMode" = "UniversalForward" }
			Blend One One, One One
			BlendOp Add
			ColorMask RGBA
			ZTest LEqual
			ZWrite Off
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				half4 _BaseColor;
				float _Size;
				float _Steps;
				float _Pulse;
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
			};

			Varyings vert(Attributes input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				// Camera-facing quad of _Size metres around the object's origin, pulsing with time.
				float size = (_Pulse * sin(_Time.y * 3.0) + 1.0) * _Size;
				float3 cornerOffset = float3(size.xx * input.positionOS.xy, 0.0);
				float3 originVS = TransformWorldToView(GetObjectToWorldMatrix()._m03_m13_m23);
				float4 positionVS = float4(cornerOffset + originVS, 1.0);

				// Projection written out row by row with only the terms the shipped program reads
				// (x skips P[0][1], y skips P[1][0], w reads P[3][2] and P[3][3]).
				float4x4 projection = GetViewToHClipMatrix();
				float4 positionCS;
				positionCS.z = dot(projection[2], positionVS);
				positionCS.x = dot(float3(projection[0][0], projection[0][2], projection[0][3]), positionVS.xzw);
				positionCS.y = dot(float3(projection[1][1], projection[1][2], projection[1][3]), positionVS.yzw);
				positionCS.w = dot(float2(projection[3][2], projection[3][3]), positionVS.zw);
				output.positionCS = positionCS;
				output.uv = input.uv;
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				// 24x24 pixel grid, radial falloff quantised into _Steps bands.
				float2 cell = (floor(input.uv * 24.0) + 0.5) * 0.0416666679 - 0.5;
				float radius = sqrt(dot(cell, cell));
				float glow = max(1.0 - radius * 2.0, 0.0);
				glow *= glow;
				glow = floor(glow * _Steps + 0.5) / _Steps;
				float3 color = glow.xxx * _BaseColor.rgb;
				color *= _BaseColor.aaa;
				return half4(color, 0.0);
			}
			ENDHLSL
		}
	}
}
