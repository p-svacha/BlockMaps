Shader "Custom/NodeMaterialShaderTransparent"
{
    Properties // Exposed to editor in material insepctor
    {
        [Toggle] _FullVisibility("Full Visibility", Float) = 1
        [Toggle] _UseTextures("Use Textures", Float) = 0
        _MainTex("Texture", 2D) = "none" {}
        _TextureTint("Texture Tint", Color) = (1,1,1,0)
        _Color("Color", Color) = (1,1,1,1)
        _Offset("Render Priority (lowest renders first, use 0.1 steps)", float) = 0

        // Texture mode values
        _TextureScale("Texture Scale", Float) = 1
        _Transparency("Transparency", Range(0, 1)) = 1.0

        _TriplanarBlendSharpness("Blend Sharpness", Range(0.1, 8)) = 1

        // Overlays
        _FogOfWarColor("Fog of war Color", Color) = (0,0,0,0.5)

        _GridTex("Grid Texture", 2D) = "none" {}
        [Toggle] _ShowGrid("Show Grid", Float) = 1
        _GridColor("Grid Color", Color) = (0,0,0,1)

        _ZoneBorderWidth("Zone Border Width", Float) = 0.1

        /* Should not be set in inspector
        [Toggle] _ShowTileOverlay("Show Tile Overlay", Float) = 0
        _TileOverlayTex("Overlay Texture", 2D) = "none" {}
        _TileOverlayColor("Overlay Color", Color) = (0,0,0,0)
        _TileOverlayX("Overlay X Coord", Float) = 0
        _TileOverlayY("Overlay Y Coord", Float) = 0
         _TileOverlaySize("Overlay Size", Float) = 1
        */

        _RoughnessTex("Roughness Texture", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 0.0

        _NormalMap("Normal Map", 2D) = "bump" {}  // New property for the normal map
        _NormalStrength("Normal Strength", Range(-2, 2)) = -0.2  // New property for normal strength

        _HeightMap("Height Map", 2D) = "white" {}
        _HeightPower("Height Power", Range(0,.125)) = 0.03
    }

        SubShader
        {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Offset [_Offset], [_Offset]
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows addshadow alpha:fade

        #pragma target 3.5
        
            struct Input
        {
            float2 uv_MainTex;
            float2 uv2_TileOverlayTex;
            float2 uv2_MultiOverlayTex;
            float2 uv2_GridTex;
            float3 worldPos;
            float3 worldNormal;
            float2 uv_NormalMap;
            float2 uv_HeightMap;
            float3 viewDir;
            INTERNAL_DATA
        };


        #include "Assets/Scripts/BlockmapFramework/Shaders/NodeMaterialShaderBase.cginc"

        // From Transparency property
        float _Transparency;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Base node material shader
            NodeMaterialSurf(IN, o);

            // Transparency
            o.Alpha = _Transparency;
        }

        ENDCG
    }

    FallBack "Diffuse"
}
