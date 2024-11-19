Shader "Custom/EntityShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _TintColor("Texture Tint", Color) = (1,1,1,0)
        _Transparency("Transparency", Range(0,1)) = 0
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [Toggle] _UseTextures("Use Textures", Float) = 0
        [Toggle] _OnlyFlatShading("Only Flat Shading", Float) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.0
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows keepalpha

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _TintColor;
        half _Transparency;
        float _UseTextures;
        float _OnlyFlatShading;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c;
            if (_UseTextures == 1 && _OnlyFlatShading == 0) c = tex2D(_MainTex, IN.uv_MainTex);
            else c = _Color;

            // Tint
            c = _TintColor.a * _TintColor + (1 - _TintColor.a) * c;

            // Transparency
            c.a = 1 - _Transparency;

            // Finalize
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
