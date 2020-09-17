Shader "Custom/NewTerrain"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _SedimentPower("Rim Power", Range(.1,10)) = .5
        _SedimentFac("Rim Fac", Range(0,2)) = 1
        _SedimentTex("Albedo (RGB)", 2D) = "white" {}
        _SedimentHeightPower("Height Power", Range(.1,3)) = .5
        _SedimentHeightFac("Height Fac", Range(0,2)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _SedimentTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_SedimentTex;
            float3 worldPos;
            float3 worldNormal;
            float3 viewDir;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        half _MaxHeight;
        half _SedimentFac;
        half _SedimentPower;
        half _SedimentHeightPower;
        half _SedimentHeightFac;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float height = IN.worldPos.y;
            float normalizedHeight = height / _MaxHeight;
            float slope = 1 - IN.worldNormal.y; // slope = 0 when terrain is completely flat

            fixed4 terrainColor = tex2D(_MainTex, IN.uv_MainTex); // terraincolor;
            fixed4 sedimentColor = tex2D(_SedimentTex, IN.uv_SedimentTex); // terraincolor;

            float rockWeight = pow(sedimentColor.a, _SedimentPower) * _SedimentFac;
            float heightWeight = pow(normalizedHeight, _SedimentHeightPower) * _SedimentHeightFac;

            rockWeight = rockWeight * heightWeight;

            float3 finalColor = lerp(terrainColor.rgb, sedimentColor.rgb, rockWeight);


            o.Albedo = finalColor;

            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = terrainColor.a;
        }
        ENDCG
    }
        FallBack "Diffuse"
}