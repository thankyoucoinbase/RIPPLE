Shader "Hidden/Shallows/Ripple Camera Desktop" {
	Properties {
		_BaseMap ("Camera feed", 2D) = "black" {}
	}
	SubShader {
		Tags { "RenderPipeline"="UniversalPipeline" }
		LOD 0

		// Full-screen triangle drawn by RippleCameraDesktop.PresentDesktop (DrawProcedural, 3 vertices).
		Pass
		{
			Blend One Zero, One Zero
			BlendOp Add
			ColorMask RGBA
			ZTest Always
			ZWrite Off
			Cull Off
			Offset 0, 0

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			float _OutputAspect;    // screen width / height

			TEXTURE2D(_BaseMap);
			SAMPLER(sampler_BaseMap);

			struct Attributes
			{
				uint vertexID : SV_VertexID;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Varyings vert(Attributes input)
			{
				Varyings output;
				output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
				output.uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				// Fit the 16:9 feed inside the screen, letterboxed or pillarboxed.
				float feedScaleX = min(1.77777779 / _OutputAspect, 1.0);
				float feedScaleY = min(_OutputAspect * 0.5625, 1.0);
				float2 uv = (input.uv - 0.5) / float2(feedScaleX, feedScaleY) + 0.5;
				if (any(uv < 0.0) || any(uv > 1.0))
					return half4(0.0, 0.0, 0.0, 1.0);
				// Plain sample: the shipped program applies no _GlobalMipBias here.
				return half4(PLATFORM_SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).xyz, 1.0);
			}
			ENDHLSL
		}
	}
}
