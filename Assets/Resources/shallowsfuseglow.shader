Shader "Shallows/Fuse Glow" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_BaseColor ("Color", Color) = (1,0.6,0.15,1)
		_Disc ("Floor Disc (1) or Particles (0)", Float) = 1
		_Phase ("Pulse Phase", Float) = 0
		_Steps ("Steps", Float) = 5
	}
	SubShader {
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+5" "IgnoreProjector"="True" }
		LOD 0

		Pass {
			Name "FuseGlow"
			Tags { "LightMode"="UniversalForward" }
			Blend One One, One One
			BlendOp Add
			ZTest LEqual
			ZWrite Off
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			// The shipped program keeps the material values in $Globals next to _Time, not in UnityPerMaterial.
			float4 _MainTex_TexelSize;
			half4 _BaseColor;
			float _Disc;
			float _Phase;
			float _Steps;

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

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
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.uv = input.uv;
				output.color = input.color;
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				if (_Disc > 0.5)
				{
					// Floor disc drawn on a 32x32 pixel grid.
					float2 cellOffset = (floor(input.uv * 32.0) + 0.5) * 0.03125 - 0.5;
					float radius = length(cellOffset);

					// Stepped radial glow that breathes with the pulse.
					float glow = max(1.0 - radius * 2.0, 0.0);
					float pulse = sin(_Time.y * 4.0 + _Phase) * 0.25 + 0.75;
					glow *= glow;
					glow *= _Steps;
					glow = floor(glow * pulse + 0.5);
					glow /= _Steps;

					// Ring sweeping outward, fading as it travels.
					float ringPosition = frac(_Phase * 0.17 + _Time.y * 0.625);
					float onRing = (0.045 >= abs(radius * 2.0 - ringPosition)) ? 1.0 : 0.0;
					float ring = onRing * (1.0 - ringPosition);

					// Bright core.
					float core = (0.08 >= radius) ? 0.35 : 0.0;

					glow = ring * 0.55 + glow;
					float intensity = glow + core;
					intensity *= _BaseColor.a;
					return half4(intensity * _BaseColor.rgb, 0.0);
				}

				// Particles: point-sampled texel centre.
				float2 texelUV = (floor(input.uv * _MainTex_TexelSize.zw) + 0.5) * _MainTex_TexelSize.xy;
				half4 color = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, texelUV, 0);
				color *= input.color;
				color *= _BaseColor;
				return half4(color.rgb * color.a, 0.0);
			}
			ENDHLSL
		}
	}
}
