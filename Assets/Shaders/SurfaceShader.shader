Shader "Custom/SurfaceShader"
{
    Properties // Exposed to editor in material insepctor
    {
        // Draw mode
        [Toggle] _UseTextures("Use Textures", Float) = 0

        // Terrain texture
        _TerrainTextures("Terrain Textures", 2DArray) = "" { }
        _TerrainTextureScale("Terrain Texture Scale", Float) = 0.2

        // Overlays
        _TintColor("Color Overlay", Color) = (1,1,1,0)

        _GridTex("Grid Texture", 2D) = "none" {}
        [Toggle] _ShowGrid("Show Grid", Float) = 1
        _GridColor("Grid Color", Color) = (0,0,0,1)

        [Toggle] _ShowBlockOverlay("Show Block Overlay", Float) = 0
        _BlockOverlayTex("Block Overlay Texture", 2D) = "none" {}
        _BlockOverlayColor("Block Overlay Color", Color) = (0,0,0,0)

        [Toggle] _ShowTileOverlay("Show Tile Overlay", Float) = 0
        _TileOverlayTex("Overlay Texture", 2D) = "none" {}
        _TileOverlayColor("Overlay Color", Color) = (0,0,0,0)
        _TileOverlayX("Overlay X Coord", Float) = 0
        _TileOverlayY("Overlay Y Coord", Float) = 0

        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        #pragma target 3.5
        #include "UnityCG.cginc"

        // Base values
        float _ChunkSize;

        // Draw mode
        float _UseTextures;

        // Terrain colors
        fixed4 _TerrainColors[100];

        // Terrain textures (stored in an array)
        UNITY_DECLARE_TEX2DARRAY(_TerrainTextures);
        float _TerrainTextureScale;

        // Grid overlay
        sampler2D _GridTex;
        fixed4 _GridColor;
        float _ShowGrid;

        // Tile selection Overlay
        float _ShowTileOverlay;
        sampler2D _TileOverlayTex;
        fixed4 _TileOverlayColor;
        float _TileOverlayX;
        float _TileOverlayY;
        
        // Chunk selection overlay
        float _ShowBlockOverlay;
        sampler2D _BlockOverlayTex;
        fixed4 _BlockOverlayColor;

        struct Input
        {
            float2 uv_BlockOverlayTex;
            float2 uv_TerrainTextures;
            float2 uv2_TileOverlayTex;
            float2 uv2_GridTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _TintColor;

        float _TileSurfaces[1000];

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Find out where we exactly are on the chunk
            float2 localPos = floor(IN.worldPos.xz % _ChunkSize);
            int tileIndex = int(localPos.y + localPos.x * _ChunkSize);
            int surfaceIndex = _TileSurfaces[tileIndex];

            
            // Take input texture as main color
            fixed4 c; // = fixed4(1.0, 1.0, 1.0, 1.0); // white

            /*
            // Tint depending on surface
            if (_TileSurfaces[tileIndex] == 0) c *= _GrassColor;
            if (_TileSurfaces[tileIndex] == 1) c *= _SandColor;
            if (_TileSurfaces[tileIndex] == 2) c *= _TarmacColor;
            */

            // Texture mode         
            if (_UseTextures == 1) {
                c = UNITY_SAMPLE_TEX2DARRAY(_TerrainTextures, float3(IN.worldPos.x * _TerrainTextureScale, IN.worldPos.z * _TerrainTextureScale, surfaceIndex));
            }
            else {
                c = _TerrainColors[surfaceIndex];
            }
            

            // Selection Overlay
            if (_ShowTileOverlay == 1)
            {
                if (localPos.x == _TileOverlayX && localPos.y == _TileOverlayY)
                {
                    fixed4 tileOverlayColor = tex2D(_TileOverlayTex, IN.uv2_GridTex) * _TileOverlayColor;
                    c = (tileOverlayColor.a * tileOverlayColor) + ((1 - tileOverlayColor.a) * c);
                }
            }

            // Block Overlay
            if (_ShowBlockOverlay == 1) 
            {
                fixed4 blockOverlayColor = tex2D(_BlockOverlayTex, IN.uv_BlockOverlayTex) * _BlockOverlayColor;
                c = (blockOverlayColor.a * blockOverlayColor) + ((1 - blockOverlayColor.a) * c);
            }

            // Grid
            if (_ShowGrid == 1) 
            {
                fixed4 gridColor = tex2D(_GridTex, IN.uv2_GridTex) * _GridColor;
                c = (gridColor.a * gridColor) + ((1 - gridColor.a) * c);
            }

            // Tint
            c = (_TintColor.a * _TintColor) + ((1 - _TintColor.a) * c);

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
