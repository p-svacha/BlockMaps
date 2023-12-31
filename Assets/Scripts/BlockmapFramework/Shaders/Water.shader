Shader "Custom/WaterShader"
{
    Properties
    {
        _Color("Water Color", Color) = (.0,.5,1,1)
        _MainTex("Water Texture", 2D) = "white" { }
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }

        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 100

            CGPROGRAM
            #pragma surface surf Standard

            // User-defined properties
            fixed4 _Color;
            sampler2D _MainTex;
            fixed _Glossiness;
            fixed _Metallic;

            struct Input
            {
                float2 uv_MainTex;
            };

            void surf(Input IN, inout SurfaceOutputStandard o)
            {
                // Albedo comes from a texture tinted by color
                fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
                o.Albedo = c.rgb;

                // Metallic and smoothness come from slider variables
                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;
                o.Alpha = c.a;
            }
            ENDCG
        }

            // Note that a vertex shader is specified here, but its using Unity's built-in vertex shader
            // that just transforms the input vertices.
                Fallback "Diffuse"
}