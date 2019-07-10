Shader "Unlit / SimpleOutline"
{
	Subshader
	{
		Tags {"RenderType" = "Opaque"}
		LOD 100

		Pass
		{
			Stencil {
				Ref 2
				Comp notequal
				Pass keep
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

			half4 _OutlineColor;
			v2f vert(appdata v)
			{
				v2f o;
				v.vertex.xyz += 0.2 * normalize(v.vertex.xyz);
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				return _OutlineColor;
			}
			ENDHLSL
		}
	}
}