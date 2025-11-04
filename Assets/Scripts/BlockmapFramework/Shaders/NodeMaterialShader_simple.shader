Shader "Custom/NodeMaterialShaderSimple"
{
    Properties
    {
        [Toggle] _FullVisibility("Full Visibility", Float) = 1
        [Toggle] _UseTextures("Use Textures", Float) = 1
        _MainTex("Texture", 2D) = "none" {}
        _TextureRotation("Texture Rotation", Range(0, 360)) = 0
        _TextureScale("Texture Scale", Float) = 1
        _TextureTint("Texture Tint", Color) = (1,1,1,0)

        _Color("Color", Color) = (1,1,1,1)
        _Offset("Render Priority (lowest renders first, use 0.004 steps)", float) = 0

        _TriplanarBlendSharpness("Blend Sharpness", Range(0.1, 8)) = 1

        // Overlays
        _FogOfWarColor("Fog of war Color", Color) = (0,0,0,0.5)

        _GridTex("Grid Texture", 2D) = "none" {}
        [Toggle] _ShowGrid("Show Grid", Float) = 1
        _GridColor("Grid Color", Color) = (0,0,0,1)

        _ZoneBorderWidth("Zone Border Width", Float) = 0.1

        _Smoothness("Smoothness", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Offset[_Offset],[_Offset]
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.5

        // Enable GPU instancing (for walls)
        #pragma multi_compile_instancing
        #pragma instancing_options assumeuniformscaling

        // Tell the cginc to skip height/normal/roughness/metallic/AO work
        #define NODEMATERIAL_SIMPLE 1

        struct Input
        {
            float2 uv_MainTex;
            float2 uv2_TileOverlayTex;
            float2 uv2_MultiOverlayTex;
            float2 uv2_GridTex;
            float3 worldPos;
            float3 worldNormal;
            float3 viewDir;
            INTERNAL_DATA
        };

        float _Smoothness;

        #include "Assets/Scripts/BlockmapFramework/Shaders/NodeMaterialShaderBase.cginc"

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            NodeMaterialSurf(IN, o);
        }

        ENDCG
    }

    FallBack "Diffuse"
}
