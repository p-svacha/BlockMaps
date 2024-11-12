/// Should never be used directly, simply acts as data storage that the SurfaceShader then uses
Shader "Custom/BlendSurfaceReferenceShader"
{
    Properties // Exposed to editor in material inspector
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Texture", 2D) = "none" {}
        _TextureScale("Texture Scale", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard

        #pragma target 3.5

        sampler2D _MainTex;
        fixed4 _Color;
        float _TextureScale;

        struct Input 
        {
            float2 uv_MainTex;
            float3 worldPos;
        };


        void surf (Input IN, inout SurfaceOutputStandard o)
        {   
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex / _TextureScale);
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }

        ENDCG
    }

    FallBack "Diffuse"
}
