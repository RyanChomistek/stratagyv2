Shader "Unlit/Outline"
{
    Properties
    {
		_FillColor("_FillColor", Color) = (1,1,1,1)

		//outline properties
		_OutlineColor("_OutlineColor", Color) = (1,1,1,1)
		_ObjectScale("_ObjectScale", Vector) = (1,1,1,0)
		_OutlineThickness("_OutlineThickness", Float) = .1

		//dither Properties
		_MainTex("Main Texture", 2D) = "white" {}
		_DitherScale("Dither Scale", Float) = 10
		[NoScaleOffset]_DitherTex("Dither Texture", 2D) = "white" {}
    }
    SubShader
    {
		Tags{ "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			#include "Dither/Shaders/Dither Functions.cginc"

			uniform fixed4 _LightColor0;

            float4 _FillColor;
			float4 _OutlineColor;
			float4 _ObjectScale;
			float _OutlineThickness;

			float4 _MainTex_ST;         // For the Main Tex UV transform
			sampler2D _MainTex;         // Texture used for the line

			float _DitherScale;
			sampler2D _DitherTex;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				UNITY_FOG_COORDS(1)
				float4 pos : SV_POSITION;
				float4 col      : COLOR;
				float4 ObjectPos : POSITION1;
				float2 uv       : TEXCOORD0;
				float4 spos     : TEXCOORD1;
			};

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
				o.ObjectPos = v.vertex;

				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.spos = ComputeScreenPos(o.pos);

				float4 norm = mul(unity_ObjectToWorld, v.normal);
				float3 normalDirection = normalize(norm.xyz);
				float4 AmbientLight = UNITY_LIGHTMODEL_AMBIENT;
				float4 LightDirection = normalize(_WorldSpaceLightPos0);
				float4 DiffuseLight = saturate(dot(LightDirection, -normalDirection))*_LightColor0;
				o.col = float4(AmbientLight + DiffuseLight);

                return o;
            }

            fixed4 frag (v2f i) : COLOR
            {
				float4 fillThickness = _ObjectScale - float4(_OutlineThickness,_OutlineThickness,_OutlineThickness,0);
				
				float4 scaledPosition = abs(i.ObjectPos) * _ObjectScale;
				if (scaledPosition.x > fillThickness.x / 2 || scaledPosition.y > fillThickness.y/2) 
				{
					return _OutlineColor;
				}
				
				float4 col = _FillColor * tex2D(_MainTex, i.uv);
				ditherClip(i.spos.xy / i.spos.w, col.a, _DitherTex, _DitherScale);
				return col * i.col;
            }
            ENDCG
        }
    }

	SubShader
	{
		Tags { "RenderType" = "ShadowCaster" }
		UsePass "Hidden/Dithered Transparent/Shadow/SHADOW"
	}
}
