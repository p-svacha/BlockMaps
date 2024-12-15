Shader "Custom/NodeMaterialShader"
{
    Properties // Exposed to editor in material insepctor
    {
        [Toggle] _FullVisibility("Full Visibility", Float) = 1
        [Toggle] _UseTextures("Use Textures", Float) = 1
        _MainTex("Texture", 2D) = "none" {}
        _TextureRotation("Texture Rotation", Range(0, 360)) = 0
        _TextureScale("Texture Scale", Float) = 1
        _TextureTint("Texture Tint", Color) = (1,1,1,0)

        _Color("Color", Color) = (1,1,1,1)
        _Offset("Render Priority (lowest renders first, use 0.004 steps)", float) = 0

        // Texture mode values
        
        _TriplanarBlendSharpness("Blend Sharpness",float) = 1
        _SideStartSteepness("Side Texture Start Steepness",float) = 0.3 // The steepness where side texture starts to show through
        _SideOnlySteepness("Side Texture Only Steepness",float) = 0.7 // The steepness where only side texture is shown

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

        _NormalMap("Normal Map", 2D) = "bump" {}  // New property for the normal map
        _NormalStrength("Normal Strength", Range(-2, 2)) = 0  // New property for normal strength

        _HeightMap("Height Map", 2D) = "white" {}
        _HeightPower("Height Power", Range(0,.125)) = 0

        _MetallicMap("Metallic Map", 2D) = "black" {}
        _MetallicPower("Metallic Power", Range(0,1)) = 0

        [Toggle] _UseAO("Use Ambient Occlusion?", Float) = 0
        _AOMap("Ambient Occlusion Map", 2D) = "white" {}
    }

        SubShader
        {
        Tags { "RenderType" = "Opaque" }
        Offset [_Offset], [_Offset]
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows addshadow

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
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            NodeMaterialSurf(IN, o);
        }

        ENDCG
    }

    FallBack "Diffuse"
}
