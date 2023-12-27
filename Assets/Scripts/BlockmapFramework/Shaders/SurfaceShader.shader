Shader "Custom/SurfaceShader"
{
    Properties // Exposed to editor in material insepctor
    {
        // Draw mode
        [Toggle] _UseTextures("Use Textures", Float) = 0
        _BlendThreshhold("Blend Threshhold", Float) = 0.4

        // Terrain texture
        _TerrainTextures("Terrain Textures", 2DArray) = "" { }
        _TerrainTextureScale("Terrain Texture Scale", Float) = 0.2

        // Overlays
        _FogOfWarColor("Fog of war Color", Color) = (0,0,0,0.5)

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
        float _ChunkCoordinatesX;
        float _ChunkCoordinatesY;
        float _ChunkSize;

        // Draw mode
        float _UseTextures;
        float _BlendThreshhold;

        // Terrain colors
        fixed4 _TerrainColors[256];

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

        half _Glossiness;
        half _Metallic;
        fixed4 _FogOfWarColor;

        float _TileVisibility[324];
        float _TileSurfaces[256];
        float _TileBlend_W[256];
        float _TileBlend_E[256];
        float _TileBlend_N[256];
        float _TileBlend_S[256];
        float _TileBlend_NW[256];
        float _TileBlend_NE[256];
        float _TileBlend_SW[256];
        float _TileBlend_SE[256];

        struct Input
        {
            float2 uv_BlockOverlayTex;
            float2 uv_TerrainTextures;
            float2 uv2_TileOverlayTex;
            float2 uv2_GridTex;
            float3 worldPos;
        };

        // Returns the pixel color for the given surface and position depending on drawmode
        fixed4 GetPixelColor(float2 worldPos2d, int surfaceIndex) {
            if (_UseTextures == 1) {
                return UNITY_SAMPLE_TEX2DARRAY(_TerrainTextures, float3(worldPos2d.x * _TerrainTextureScale, worldPos2d.y * _TerrainTextureScale, surfaceIndex));
            }
            else {
                return _TerrainColors[surfaceIndex];
            }
        }

        // Returns the pixel color on a pixel near the corner of a surface tile where 4 tiles blend
        fixed4 Get4BlendColor(float blendX, float blendY, fixed4 baseColor, fixed4 adjXColor, fixed4 adjYColor, fixed4 adjCornerColor) 
        {
            float blendBase = 0.25 + 0.25 * (max(1 - blendX, 1 - blendY)) + 0.5 * (1 - max(blendX, blendY)); // 1 in center, 0.5 on side, 0.25 in corner 
            float blendCorner = min(blendX, blendY) * 0.25; // 0.25 in corner, 0 on side, 0 in center
            float blendSideY = (blendY * 0.5) - blendCorner; //max((blendY * 0.5) - (blendX * 0.5), 0); // 0.25 in corner, 0.5 on side, 0 in center
            float blendSideX = (blendX * 0.5) - blendCorner; // max((blendX * 0.5) - (blendY * 0.5), 0); // 0.25 in corner, 0.5 on side, 0 in center

            return (blendBase * baseColor) + (blendCorner * adjCornerColor) + (blendSideY * adjYColor) + (blendSideX * adjXColor);
        }

        int GetVisibilityArrayIndex(float x, float y)
        {
            return int((y + 1) + (x + 1) * (_ChunkSize + 2));
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Find out where we exactly are on the chunk
            float2 relativePos = frac(IN.worldPos.xz); // relative position on current tile (i.e. 13.4/14.8 => 0.4/0.8)
            float2 localCoords = max(floor(IN.worldPos.xz % _ChunkSize), 0); // Local coordinates of current position

            // Fix local coordinates when exactly on edge of chunk (else coordinates loop back around)
            if (IN.worldPos.x <= _ChunkCoordinatesX * _ChunkSize)
            {
                localCoords.x = 0;
                relativePos.x = 0;
            }
            if (IN.worldPos.z <= _ChunkCoordinatesY * _ChunkSize)
            {
                localCoords.y = 0;
                relativePos.y = 0;
            }
            if (IN.worldPos.x >= ((_ChunkCoordinatesX + 1) * _ChunkSize))
            {
                localCoords.x = _ChunkSize - 1;
                relativePos.x = 1;
            }
            if (IN.worldPos.z >= ((_ChunkCoordinatesY + 1) * _ChunkSize))
            {
                localCoords.y = _ChunkSize - 1;
                relativePos.y = 1;
            }
            

            // Check visiblity
            float visEpsilon = 0.1; // Pixels are drawn by this value over tile edges
            float tileVisibility = _TileVisibility[GetVisibilityArrayIndex(localCoords.x, localCoords.y)];
            float drawPixel = (tileVisibility > 0 ||

                (relativePos.x < visEpsilon && relativePos.y < visEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y - 1)] > 0) || // extension ne
                (relativePos.x > 1 - visEpsilon && relativePos.y < visEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y - 1)] > 0) || // extension nw
                (relativePos.x > 1 - visEpsilon && relativePos.y > 1 - visEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y + 1)] > 0) || // extension sw
                (relativePos.x < visEpsilon && relativePos.y > 1 - visEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y + 1)] > 0) || // extension se

                (relativePos.x < visEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y)] > 0) || // extension east
                (relativePos.x > 1 - visEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y)] > 0) || // extension west
                (relativePos.y < visEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x, localCoords.y - 1)] > 0) || // extension north
                (relativePos.y > 1 - visEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x, localCoords.y + 1)] > 0)); // extension south


            if (drawPixel == 0) {
                discard;
            }

            float fowEpsilon = 0.01; // Fog of war epsilon
            float fullVisible = (tileVisibility > 1 ||

                (relativePos.x < fowEpsilon && relativePos.y < fowEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y - 1)] > 1) || // extension ne
                (relativePos.x > 1 - fowEpsilon && relativePos.y < fowEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y - 1)] > 1) || // extension nw
                (relativePos.x > 1 - fowEpsilon && relativePos.y > 1 - fowEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y + 1)] > 1) || // extension sw
                (relativePos.x < fowEpsilon && relativePos.y > 1 - fowEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y + 1)] > 1) || // extension se

                (relativePos.x < fowEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y)] > 1) || // extension east
                (relativePos.x > 1 - fowEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y)] > 1) || // extension west
                (relativePos.y < fowEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x, localCoords.y - 1)] > 1) || // extension north
                (relativePos.y > 1 - fowEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x, localCoords.y + 1)] > 1)); // extension south

            // c is the color of the current pixel
            fixed4 c;

            // Get base color
            int tileIndex = int(localCoords.y + localCoords.x * _ChunkSize);
            int surfaceIndex = _TileSurfaces[tileIndex];
            fixed4 baseColor = GetPixelColor(IN.worldPos.xz, surfaceIndex);
            
            
            if (relativePos.x < _BlendThreshhold && relativePos.y > 1 - _BlendThreshhold) // Blend nw
            {
                fixed4 blendColor_nw = GetPixelColor(IN.worldPos.xz, _TileBlend_NW[tileIndex]);
                fixed4 blendColor_n = GetPixelColor(IN.worldPos.xz, _TileBlend_N[tileIndex]);
                fixed4 blendColor_w = GetPixelColor(IN.worldPos.xz, _TileBlend_W[tileIndex]);

                float blendX = 1 - ((1 / _BlendThreshhold) * relativePos.x); // 1 in corner then fade out
                float blendY = (1 / _BlendThreshhold) * relativePos.y - (1 / _BlendThreshhold - 1); // 1 in corner then fade out

                c = Get4BlendColor(blendX, blendY, baseColor, blendColor_w, blendColor_n, blendColor_nw);
            }

            else if (relativePos.x > 1 - _BlendThreshhold && relativePos.y > 1 - _BlendThreshhold) // Blend ne
            {
                fixed4 blendColor_ne = GetPixelColor(IN.worldPos.xz, _TileBlend_NE[tileIndex]);
                fixed4 blendColor_n = GetPixelColor(IN.worldPos.xz, _TileBlend_N[tileIndex]);
                fixed4 blendColor_e = GetPixelColor(IN.worldPos.xz, _TileBlend_E[tileIndex]);

                float blendX = (1 / _BlendThreshhold) * relativePos.x - (1 / _BlendThreshhold - 1); // 1 in corner then fade out
                float blendY = (1 / _BlendThreshhold) * relativePos.y - (1 / _BlendThreshhold - 1); // 1 in corner then fade out

                c = Get4BlendColor(blendX, blendY, baseColor, blendColor_e, blendColor_n, blendColor_ne);
            }
            else if (relativePos.x > 1 - _BlendThreshhold && relativePos.y < _BlendThreshhold) // Blend se
            {
                fixed4 blendColor_se = GetPixelColor(IN.worldPos.xz, _TileBlend_SE[tileIndex]);
                fixed4 blendColor_s = GetPixelColor(IN.worldPos.xz, _TileBlend_S[tileIndex]);
                fixed4 blendColor_e = GetPixelColor(IN.worldPos.xz, _TileBlend_E[tileIndex]);

                float blendX = (1 / _BlendThreshhold) * relativePos.x - (1 / _BlendThreshhold - 1); // 1 in corner then fade out
                float blendY = 1 - ((1 / _BlendThreshhold) * relativePos.y); // 1 in corner then fade out

                c = Get4BlendColor(blendX, blendY, baseColor, blendColor_e, blendColor_s, blendColor_se);
            }
            else if (relativePos.x < _BlendThreshhold && relativePos.y < _BlendThreshhold) // Blend sw
            {
                fixed4 blendColor_sw = GetPixelColor(IN.worldPos.xz, _TileBlend_SW[tileIndex]);
                fixed4 blendColor_s = GetPixelColor(IN.worldPos.xz, _TileBlend_S[tileIndex]);
                fixed4 blendColor_w = GetPixelColor(IN.worldPos.xz, _TileBlend_W[tileIndex]);

                float blendX = 1 - ((1 / _BlendThreshhold) * relativePos.x); // 1 in corner then fade out
                float blendY = 1 - ((1 / _BlendThreshhold) * relativePos.y); // 1 in corner then fade out

                c = Get4BlendColor(blendX, blendY, baseColor, blendColor_w, blendColor_s, blendColor_sw);
            }

            else if (relativePos.x < _BlendThreshhold) // Blend west
            {
                
                fixed4 blendColor_w = GetPixelColor(IN.worldPos.xz, _TileBlend_W[tileIndex]);
                c = lerp(baseColor, blendColor_w, 0.5 - ((1 / (_BlendThreshhold * 2)) * relativePos.x));
            }
            else if (relativePos.x > 1 - _BlendThreshhold) // Blend east
            {
                fixed4 blendColor_e = GetPixelColor(IN.worldPos.xz, _TileBlend_E[tileIndex]);
                c = lerp(baseColor, blendColor_e, 0.5 - ((1 / (_BlendThreshhold * 2)) * (1 - relativePos.x)));
            }
            else if (relativePos.y > 1 - _BlendThreshhold) // Blend north
            {
                fixed4 blendColor_n = GetPixelColor(IN.worldPos.xz, _TileBlend_N[tileIndex]);
                c = lerp(baseColor, blendColor_n, 0.5 - ((1 / (_BlendThreshhold * 2)) * (1 - relativePos.y)));
            }
            else if (relativePos.y < _BlendThreshhold) // Blend south
            {
                fixed4 blendColor_s = GetPixelColor(IN.worldPos.xz, _TileBlend_S[tileIndex]);
                c = lerp(baseColor, blendColor_s, 0.5 - ((1 / (_BlendThreshhold * 2)) * relativePos.y));
            }
            else // No blend
            {
                c = baseColor;
            }


            // Selection Overlay
            if (_ShowTileOverlay == 1)
            {
                if (localCoords.x == _TileOverlayX && localCoords.y == _TileOverlayY)
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

            // Fog of war
            if (fullVisible != 1)
            {
                c = (_FogOfWarColor.a * _FogOfWarColor) + ((1 - _FogOfWarColor.a) * c);
            }

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
