Shader "Shallows/Sky" {
	Properties {
		[NoScaleOffset] _MainTex ("Sky (equirectangular)", 2D) = "white" {}
		_Spin ("Turn (degrees)", Float) = 0
	}
	SubShader {
		Tags { "Queue" = "Geometry+450" "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
		LOD 0

		Pass
		{
			Name "Sky"
			Tags { "LightMode" = "UniversalForward" }
			Blend One Zero, One Zero
			BlendOp Add
			ColorMask RGBA
			ZTest LEqual
			ZWrite On
			Cull Back

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float _Spin;
			CBUFFER_END

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

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

				// Equirectangular lookup along the view ray, turned by _Spin degrees.
				float3 viewDirection = normalize(input.positionWS - _WorldSpaceCameraPos);
				float longitude = atan2(viewDirection.x, viewDirection.z);
				float2 uv;
				uv.x = frac(longitude * 0.159154937 + 0.5 + _Spin * 0.00277777785);
				float latitude = 1.57079637 - acos(clamp(viewDirection.y, -1.0, 1.0));
				uv.y = latitude * 0.318309873 + 0.5;
				half3 sky = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0.0).rgb;
				return half4(sky, 1.0);
			}
			ENDHLSL
		}
	}
}
