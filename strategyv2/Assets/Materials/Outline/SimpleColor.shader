Shader "Unlit / SimpleColor"
{
	Subshader
	{
		Tags {"RenderType" = "Opaque"}
		LOD 100
		Pass
		{
Tags {"LightMode" = "LightweightForward"}
Stencil
{
Ref 2
Comp always
Pass replace
}
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

			struct appdata
			{
				float4 vertex: POSITION;
			};

			struct v2f
			{
				float4 vertex: SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
o.vertex = TransformObjectToHClip(v.vertex.xyz);
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				return half4 (0.5h, 0.0h, 0.0h, 1.0h);
			}
			ENDHLSL
		}
	}
}