Shader "Hidden/Shallows/Ripple Camera YUV" {
	Properties {
		_MainTex ("Camera view", 2D) = "black" {}
	}
	SubShader {
		LOD 0

		// Packs the linear RGB camera view into planar I420 (BT.709 limited range): a Width/4 x Height*1.5
		// target where each texel holds four consecutive bytes. Bytes [0, W*H) are Y, then U and V at
		// quarter resolution, each chroma sample averaging a 2x2 block.
		Pass
		{
			Blend One Zero, One Zero
			BlendOp Add
			ColorMask RGBA
			ZTest Always
			ZWrite Off
			Cull Off
			Offset 0, 0

			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			#include "UnityCG.cginc"

			float _Width;     // source width in pixels
			float _Height;    // source height in pixels

			sampler2D _MainTex;

			// Linear to sRGB-style gamma (pure 1/2.4 power, no linear toe).
			float3 EncodeGamma(float3 linearColor)
			{
				return saturate(pow(linearColor, 0.416666657) * 1.05499995 - 0.0549999997);
			}

			float3 SampleGamma(float2 uv)
			{
				half3 linearColor = tex2Dlod(_MainTex, float4(uv, 0.0, 0.0)).xyz;
				return EncodeGamma(max(linearColor, 0.0));
			}

			// Value of byte 'byteIndex' of the I420 buffer, normalised to [0, 1].
			float PackedByte(float byteIndex)
			{
				float pixelCount = _Height * _Width;
				if (byteIndex < pixelCount)
				{
					// Y plane: one source pixel per byte, rows stored top-down.
					float column = fmod(byteIndex, _Width);
					float row = floor(byteIndex / _Width);
					float2 uv;
					uv.x = (column + 0.5) / _Width;
					uv.y = 1.0 - (row + 0.5) / _Height;
					float luma = dot(SampleGamma(uv), float3(0.212599993, 0.715200007, 0.0722000003));
					return (luma * 219.0 + 16.0) * 0.00392156886;
				}

				// U plane starts at W*H, V plane at W*H*1.25; x = plane start factor, yzw = RGB weights.
				bool isV = byteIndex >= pixelCount * 1.25;
				float4 plane = isV ? float4(1.25, 0.5, -0.454153001, -0.0458469987)
				                   : float4(1.0, -0.114572003, -0.385428011, 0.5);
				float chromaIndex = byteIndex - pixelCount * plane.x;
				float halfWidth = _Width * 0.5;
				float chromaColumn = fmod(chromaIndex, halfWidth) * 2.0;
				float chromaRow = floor(chromaIndex / halfWidth);
				float2 columnsU = (chromaColumn + float2(0.5, 1.5)) / _Width;
				float2 rowsV = 1.0 - (chromaRow * 2.0 + float2(0.5, 1.5)) / _Height;

				float3 average = SampleGamma(float2(columnsU.x, rowsV.x));
				average += SampleGamma(float2(columnsU.y, rowsV.x));
				average += SampleGamma(float2(columnsU.x, rowsV.y));
				average += SampleGamma(float2(columnsU.y, rowsV.y));
				average *= 0.25;

				float chroma = dot(average, plane.yzw);
				return (chroma * 224.0 + 128.0) * 0.00392156886;
			}

			float4 frag(v2f_img i) : SV_Target
			{
				float2 packedSize = float2(_Height, _Width) * float2(1.5, 0.25);
				float2 texel = floor(packedSize * i.uv.yx);    // x row, y column
				float firstByte = (texel.x * packedSize.y + texel.y) * 4.0;

				float4 packedBytes;
				packedBytes.x = PackedByte(firstByte);
				packedBytes.y = PackedByte(firstByte + 1.0);
				packedBytes.z = PackedByte(firstByte + 2.0);
				packedBytes.w = PackedByte(firstByte + 3.0);
				return packedBytes;
			}
			ENDCG
		}
	}
}
