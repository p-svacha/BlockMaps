Shader "Custom/SurfaceShader"
{
    Properties // Exposed to editor in material insepctor
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}

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

            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0


        float _ChunkSize;
        sampler2D _MainTex;

        fixed4 _GrassColor;
        fixed4 _SandColor;
        fixed4 _TarmacColor;

        sampler2D _GridTex;
        fixed4 _GridColor;
        float _ShowGrid;

        float _ShowTileOverlay;
        sampler2D _TileOverlayTex;
        fixed4 _TileOverlayColor;
        float _TileOverlayX;
        float _TileOverlayY;
        
        float _ShowBlockOverlay;
        sampler2D _BlockOverlayTex;
        fixed4 _BlockOverlayColor;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BlockOverlayTex;
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

            // Take input texture as main color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex); 

            
            // Tint depending on surface
            if (_TileSurfaces[tileIndex] == 0) c *= _GrassColor;
            if (_TileSurfaces[tileIndex] == 1) c *= _SandColor;
            if (_TileSurfaces[tileIndex] == 2) c *= _TarmacColor;
            

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
