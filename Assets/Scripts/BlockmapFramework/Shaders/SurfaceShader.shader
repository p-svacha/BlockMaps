Shader "Custom/SurfaceShader"
{
    Properties // Exposed to editor in material insepctor
    {
        // Draw mode
        [Toggle] _FullVisibility("Full Visibility", Float) = 1
        [Toggle] _UseTextures("Use Textures", Float) = 0

        // Terrain texture
        _TerrainTextures("Terrain Textures", 2DArray) = "" { }

        // Overlays
        _FogOfWarColor("Fog of war Color", Color) = (0,0,0,0.5)

        _GridTex("Grid Texture", 2D) = "none" {}
        [Toggle] _ShowGrid("Show Grid", Float) = 1
        _GridColor("Grid Color", Color) = (0,0,0,1)

        [Toggle] _ShowTileOverlay("Show Tile Overlay", Float) = 0
        _TileOverlayTex("Overlay Texture", 2D) = "none" {}
        _TileOverlayColor("Overlay Color", Color) = (0,0,0,0)
        _TileOverlayX("Overlay X Coord", Float) = 0
        _TileOverlayY("Overlay Y Coord", Float) = 0
        _TileOverlaySize("Overlay Size", Float) = 1

        _ZoneBorderWidth("Zone Border Width", Float) = 0.1

        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0

        // Blend values
        _BlendThreshhold("Blend Threshhold", Range(0,0.5)) = 0.4
        _BlendNoiseScale("Blend Noise Scale", Range(0.5, 50)) = 10
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows addshadow

        #pragma target 3.5
        #include "UnityCG.cginc"

        // Base values
        float _ChunkCoordinatesX;
        float _ChunkCoordinatesY;
        float _ChunkSize;

        // Blending
        float _BlendThreshhold;
        float _BlendNoiseScale;

        // Terrain colors
        fixed4 _TerrainColors[256];

        // Player colors
        fixed4 _PlayerColors[8];

        // Terrain textures (stored in an array)
        float _UseTextures;
        UNITY_DECLARE_TEX2DARRAY(_TerrainTextures);
        float _TerrainTextureScale[256];

        // Overlays
        fixed4 _FogOfWarColor;

        sampler2D _GridTex;
        fixed4 _GridColor;
        float _ShowGrid;

        // Overlay texture over a single area
        float _ShowTileOverlay;
        sampler2D _TileOverlayTex;
        fixed4 _TileOverlayColor;
        float _TileOverlayX;
        float _TileOverlayY;
        float _TileOverlaySize;

        // Overlay texture over multiple tiles repeated
        float _ShowMultiOverlay[256]; // bool for each tile if the overlay is shown
        sampler2D _MultiOverlayTex;
        fixed4 _MultiOverlayColor;

        // Material attributes
        half _Glossiness;
        half _Metallic;
        
        float _FullVisibility;
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

        // Zone borders
        // Each list element represents one node and the value represents the sides on which the border should be drawn. (0/1 for each side N/E/S/W)
        // i.e. a value of 1001 would draw a border on the north and west side of the node.
        float _ZoneBorderWidth;
        float _ZoneBorders[256];
        float _ZoneBorderColors[256]; // Contains player id for each tile, colors are taken from _PlayerColors


        struct Input
        {
            float2 uv_TerrainTextures;
            float2 uv2_TileOverlayTex;
            float2 uv2_MultiOverlayTex;
            float2 uv2_GridTex;
            float3 worldPos;
        };

        // Returns the pixel color for the given surface and position depending on drawmode
        fixed4 GetPixelColor(float2 worldPos2d, int surfaceIndex) {
            if (_UseTextures == 1) {
                return UNITY_SAMPLE_TEX2DARRAY(_TerrainTextures, float3(worldPos2d.x / _TerrainTextureScale[surfaceIndex], worldPos2d.y / _TerrainTextureScale[surfaceIndex], surfaceIndex));
            }
            else {
                return _TerrainColors[surfaceIndex];
            }
        }

        // Returns the pixel color on a pixel near the corner of a surface tile where 4 tiles blend
        // blendX and blendY are 0 at center and then go upwards up to 1 in the corner
        fixed4 Get4BlendColor(float relativePosX, float relativePosY, fixed4 baseColor, fixed4 adjXColor, fixed4 adjYColor, fixed4 adjCornerColor, float noiseX, float noiseY)
        {
            // Add noise to blending
            float blendNoiseStrengthX = (relativePosX * 0.5) * 4;
            float blendNoiseValueX = noiseX * blendNoiseStrengthX;
            float sideBlendX = (relativePosX * 0.5) * blendNoiseValueX;
            if (sideBlendX < 0) sideBlendX = 0;
            if (sideBlendX > 1) sideBlendX = 1;

            float blendNoiseStrengthY = (relativePosY * 0.5) * 4;
            float blendNoiseValueY = noiseY * blendNoiseStrengthY;
            float sideBlendY = (relativePosY * 0.5) * blendNoiseValueY;
            if (sideBlendY < 0) sideBlendY = 0;
            if (sideBlendY > 1) sideBlendY = 1;

            // Create lerped colors to both adjacent tiles and also between the two adjacent tiles
            float4 lerpedColor_ThisToAdjX = lerp(baseColor, adjXColor, sideBlendX);
            float4 lerpedColor_ThisToAdjY = lerp(baseColor, adjYColor, sideBlendY);
            float4 lerpedColor_AdjXToCorner = lerp(adjXColor, adjCornerColor, sideBlendY);
            float4 lerpedColor_AdjYToCorner = lerp(adjYColor, adjCornerColor, sideBlendX);

            // Calculate alpha values for the 4 lerped colors
            float distanceFromCorner = ((1 - relativePosX) + (1 - relativePosY)) * 0.5; // 0-1
            float distanceFromCenter = (relativePosX + relativePosY) * 0.5; // 0-1
            float distanceFromXSide = ((1 - relativePosX) + relativePosY) * 0.5; // 0-1
            float distanceFromYSide = (relativePosX + (1 - relativePosY)) * 0.5; // 0-1

            float alphaFromCorner = 0.25 * max((distanceFromCenter - 0.5) * 2, 0);

            float alpha_ThisToAdjX = distanceFromYSide - alphaFromCorner; // 0.5 at center, 1 at x side, 0 at y side, 0.25 at corner
            float alpha_ThisToAdjY = distanceFromXSide - alphaFromCorner; // 0.5 at center, 0 at x side, 1 at y side, 0.25 at corner
            float alpha_AdjXToCorner = alphaFromCorner; // 0 at center, 0 at x side, 0 at y side, 0.25 at corner
            float alpha_AdjYToCorner = alphaFromCorner; // 0 at center, 0 at x side, 0 at y side, 0.25 at corner
            
            //alpha_ThisToAdjX = 0;
            //alpha_ThisToAdjY = 0;
            //alpha_AdjXToCorner = 0;
            //alpha_AdjYToCorner = 0;

            return (alpha_ThisToAdjX * lerpedColor_ThisToAdjX) + (alpha_ThisToAdjY * lerpedColor_ThisToAdjY) + (alpha_AdjXToCorner * lerpedColor_AdjXToCorner) + (alpha_AdjYToCorner * lerpedColor_AdjYToCorner);
            //return (baseColorAlpha * baseColor) + (cornerColorAlpha * adjCornerColor) + (adjYColorAlpha * adjYColor) + (adjXColorAlpha * adjXColor);
        }

        int GetVisibilityArrayIndex(float x, float y)
        {
            return int((y + 1) + (x + 1) * (_ChunkSize + 2));
        }

        // Blend noise functions

        inline float unity_noise_randomValue(float2 uv)
        {
            return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
        }

        inline float unity_noise_interpolate(float a, float b, float t)
        {
            return (1.0 - t) * a + (t * b);
        }

        inline float unity_valueNoise(float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);

            uv = abs(frac(uv) - 0.5);
            float2 c0 = i + float2(0.0, 0.0);
            float2 c1 = i + float2(1.0, 0.0);
            float2 c2 = i + float2(0.0, 1.0);
            float2 c3 = i + float2(1.0, 1.0);
            float r0 = unity_noise_randomValue(c0);
            float r1 = unity_noise_randomValue(c1);
            float r2 = unity_noise_randomValue(c2);
            float r3 = unity_noise_randomValue(c3);

            float bottomOfGrid = unity_noise_interpolate(r0, r1, f.x);
            float topOfGrid = unity_noise_interpolate(r2, r3, f.x);
            float t = unity_noise_interpolate(bottomOfGrid, topOfGrid, f.y);
            return t;
        }

        void Unity_SimpleNoise_float(float2 UV, float Scale, out float Out)
        {
            float t = 0.0;

            float freq = pow(2.0, float(0));
            float amp = pow(0.5, float(3 - 0));
            t += unity_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;

            freq = pow(2.0, float(1));
            amp = pow(0.5, float(3 - 1));
            t += unity_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;

            freq = pow(2.0, float(2));
            amp = pow(0.5, float(3 - 2));
            t += unity_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;

            Out = t;
        }





        // ######################################################################### SURF START #########################################################################

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

            int tileIndex = int(localCoords.y + localCoords.x * _ChunkSize);
            

            // ######################################################################### VISIBILITY #########################################################################

            float visEpsilon = 0.101; // Pixels are drawn by this value over tile edges
            float tileVisibility = _TileVisibility[GetVisibilityArrayIndex(localCoords.x, localCoords.y)];
            float drawPixel = (_FullVisibility == 1 || tileVisibility > 0 ||

                (relativePos.x < visEpsilon&& relativePos.y < visEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y - 1)] > 0) || // extension ne
                (relativePos.x > 1 - visEpsilon && relativePos.y < visEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y - 1)] > 0) || // extension nw
                (relativePos.x > 1 - visEpsilon && relativePos.y > 1 - visEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y + 1)] > 0) || // extension sw
                (relativePos.x < visEpsilon&& relativePos.y > 1 - visEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y + 1)] > 0) || // extension se

                (relativePos.x < visEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y)] > 0) || // extension east
                (relativePos.x > 1 - visEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y)] > 0) || // extension west
                (relativePos.y < visEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x, localCoords.y - 1)] > 0) || // extension north
                (relativePos.y > 1 - visEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x, localCoords.y + 1)] > 0)); // extension south


            if (drawPixel == 0) {
                discard;
            }

            // ######################################################################### FOG OF WAR #########################################################################
            
            float fowEpsilon = 0.01; // Fog of war epsilon
            float fullVisible = (_FullVisibility == 1 || tileVisibility > 1 ||

                (relativePos.x < fowEpsilon&& relativePos.y < fowEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y - 1)] > 1) || // extension ne
                (relativePos.x > 1 - fowEpsilon && relativePos.y < fowEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y - 1)] > 1) || // extension nw
                (relativePos.x > 1 - fowEpsilon && relativePos.y > 1 - fowEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y + 1)] > 1) || // extension sw
                (relativePos.x < fowEpsilon&& relativePos.y > 1 - fowEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y + 1)] > 1) || // extension se

                (relativePos.x < fowEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x - 1, localCoords.y)] > 1) || // extension east
                (relativePos.x > 1 - fowEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x + 1, localCoords.y)] > 1) || // extension west
                (relativePos.y < fowEpsilon&& _TileVisibility[GetVisibilityArrayIndex(localCoords.x, localCoords.y - 1)] > 1) || // extension north
                (relativePos.y > 1 - fowEpsilon && _TileVisibility[GetVisibilityArrayIndex(localCoords.x, localCoords.y + 1)] > 1)); // extension south

            // ######################################################################### BASE #########################################################################
            
            fixed4 c;

            // Get base color
            int surfaceIndex = _TileSurfaces[tileIndex];
            fixed4 baseColor = GetPixelColor(IN.worldPos.xz, surfaceIndex);
            
            // ######################################################################### BLEND #########################################################################

            if (_BlendThreshhold > 0) {

                float noiseValue;
                Unity_SimpleNoise_float(IN.worldPos.xz, _BlendNoiseScale, noiseValue);

                if (relativePos.x < _BlendThreshhold && relativePos.y > 1 - _BlendThreshhold) // Blend nw
                {
                    fixed4 blendColor_nw = GetPixelColor(IN.worldPos.xz, _TileBlend_NW[tileIndex]);
                    fixed4 blendColor_n = GetPixelColor(IN.worldPos.xz, _TileBlend_N[tileIndex]);
                    fixed4 blendColor_w = GetPixelColor(IN.worldPos.xz, _TileBlend_W[tileIndex]);

                    float blendX = 1 - ((1 / _BlendThreshhold) * relativePos.x); // 1 in corner then fade out
                    float blendY = (1 / _BlendThreshhold) * relativePos.y - (1 / _BlendThreshhold - 1); // 1 in corner then fade out

                    c = Get4BlendColor(blendX, blendY, baseColor, blendColor_w, blendColor_n, blendColor_nw, noiseValue, 1 - noiseValue);
                }
                else if (relativePos.x > 1 - _BlendThreshhold && relativePos.y > 1 - _BlendThreshhold) // Blend ne
                {
                    fixed4 blendColor_ne = GetPixelColor(IN.worldPos.xz, _TileBlend_NE[tileIndex]);
                    fixed4 blendColor_n = GetPixelColor(IN.worldPos.xz, _TileBlend_N[tileIndex]);
                    fixed4 blendColor_e = GetPixelColor(IN.worldPos.xz, _TileBlend_E[tileIndex]);

                    float blendX = (1 / _BlendThreshhold) * relativePos.x - (1 / _BlendThreshhold - 1); // 1 in corner then fade out
                    float blendY = (1 / _BlendThreshhold) * relativePos.y - (1 / _BlendThreshhold - 1); // 1 in corner then fade out

                    c = Get4BlendColor(blendX, blendY, baseColor, blendColor_e, blendColor_n, blendColor_ne, 1 - noiseValue, 1 - noiseValue);
                }
                else if (relativePos.x > 1 - _BlendThreshhold && relativePos.y < _BlendThreshhold) // Blend se
                {
                    fixed4 blendColor_se = GetPixelColor(IN.worldPos.xz, _TileBlend_SE[tileIndex]);
                    fixed4 blendColor_s = GetPixelColor(IN.worldPos.xz, _TileBlend_S[tileIndex]);
                    fixed4 blendColor_e = GetPixelColor(IN.worldPos.xz, _TileBlend_E[tileIndex]);

                    float blendX = (1 / _BlendThreshhold) * relativePos.x - (1 / _BlendThreshhold - 1); // 1 in corner then fade out
                    float blendY = 1 - ((1 / _BlendThreshhold) * relativePos.y); // 1 in corner then fade out

                    c = Get4BlendColor(blendX, blendY, baseColor, blendColor_e, blendColor_s, blendColor_se, 1 - noiseValue, noiseValue);
                }

                else if (relativePos.x < _BlendThreshhold && relativePos.y < _BlendThreshhold) // Blend sw
                {
                    fixed4 blendColor_sw = GetPixelColor(IN.worldPos.xz, _TileBlend_SW[tileIndex]);
                    fixed4 blendColor_s = GetPixelColor(IN.worldPos.xz, _TileBlend_S[tileIndex]);
                    fixed4 blendColor_w = GetPixelColor(IN.worldPos.xz, _TileBlend_W[tileIndex]);

                    float blendX = 1 - ((1 / _BlendThreshhold) * relativePos.x); // 1 in corner then fade out
                    float blendY = 1 - ((1 / _BlendThreshhold) * relativePos.y); // 1 in corner then fade out

                    c = Get4BlendColor(blendX, blendY, baseColor, blendColor_w, blendColor_s, blendColor_sw, noiseValue, noiseValue);
                }

                else if (relativePos.x < _BlendThreshhold) // Blend west
                {
                    fixed4 blendColor_w = GetPixelColor(IN.worldPos.xz, _TileBlend_W[tileIndex]);

                    float baseBlendValue = (0.5 - ((1 / (_BlendThreshhold * 2)) * relativePos.x)); // Lerp-Value for blending without noise [0 - 0.5]

                    float blendNoiseStrength = baseBlendValue * 4;
                    float blendNoiseValue = noiseValue * blendNoiseStrength;

                    float finalBlendValue = baseBlendValue * blendNoiseValue;
                    if (finalBlendValue < 0) finalBlendValue = 0;
                    if (finalBlendValue > 1) finalBlendValue = 1;

                    c = lerp(baseColor, blendColor_w, finalBlendValue);
                }
                else if (relativePos.x > 1 - _BlendThreshhold) // Blend east
                {
                    fixed4 blendColor_e = GetPixelColor(IN.worldPos.xz, _TileBlend_E[tileIndex]);
                    float baseBlendValue = (0.5 - ((1 / (_BlendThreshhold * 2)) * (1 - relativePos.x))); // Lerp-Value for blending without noise [0 - 0.5]

                    float blendNoiseStrength = baseBlendValue * 4;
                    float blendNoiseValue = (1 - noiseValue) * blendNoiseStrength;

                    float finalBlendValue = baseBlendValue * blendNoiseValue;
                    if (finalBlendValue < 0) finalBlendValue = 0;
                    if (finalBlendValue > 1) finalBlendValue = 1;

                    c = lerp(baseColor, blendColor_e, finalBlendValue);
                }
                else if (relativePos.y > 1 - _BlendThreshhold) // Blend north
                {
                    fixed4 blendColor_n = GetPixelColor(IN.worldPos.xz, _TileBlend_N[tileIndex]);
                    float baseBlendValue = (0.5 - ((1 / (_BlendThreshhold * 2)) * (1 - relativePos.y))); // Lerp-Value for blending without noise [0 - 0.5]

                    float blendNoiseStrength = baseBlendValue * 4;
                    float blendNoiseValue = (1 - noiseValue) * blendNoiseStrength;

                    float finalBlendValue = baseBlendValue * blendNoiseValue;
                    if (finalBlendValue < 0) finalBlendValue = 0;
                    if (finalBlendValue > 1) finalBlendValue = 1;

                    c = lerp(baseColor, blendColor_n, finalBlendValue);
                }
                else if (relativePos.y < _BlendThreshhold) // Blend south
                {
                    fixed4 blendColor_s = GetPixelColor(IN.worldPos.xz, _TileBlend_S[tileIndex]);
                    float baseBlendValue = (0.5 - ((1 / (_BlendThreshhold * 2)) * relativePos.y)); // Lerp-Value for blending without noise [0 - 0.5]

                    float blendNoiseStrength = baseBlendValue * 4;
                    float blendNoiseValue = noiseValue * blendNoiseStrength;

                    float finalBlendValue = baseBlendValue * blendNoiseValue;
                    if (finalBlendValue < 0) finalBlendValue = 0;
                    if (finalBlendValue > 1) finalBlendValue = 1;

                    c = lerp(baseColor, blendColor_s, finalBlendValue);
                }
                else // No blend
                {
                    c = baseColor;
                }
            }
            else // Blending is disabled
            {
                c = baseColor;
            }

            // ######################################################################### OVERLAYS #########################################################################

            // Single overlay texture that stretches across multiple tiles
            if (_ShowTileOverlay == 1)
            {
                // Get adjusted wolld position, clamping it within the chunk coordinates
                float adjustedWorldPosX = IN.worldPos.x;
                if (adjustedWorldPosX > (_ChunkCoordinatesX + 1) * _ChunkSize) adjustedWorldPosX = (_ChunkCoordinatesX + 1) * _ChunkSize - 0.001;
                if (adjustedWorldPosX < _ChunkCoordinatesX * _ChunkSize) adjustedWorldPosX = _ChunkCoordinatesX * _ChunkSize;

                float adjustedWorldPosY = IN.worldPos.z;
                if (adjustedWorldPosY > (_ChunkCoordinatesY + 1) * _ChunkSize) adjustedWorldPosY = (_ChunkCoordinatesY + 1) * _ChunkSize - 0.001;
                if (adjustedWorldPosY < _ChunkCoordinatesY * _ChunkSize) adjustedWorldPosY = _ChunkCoordinatesY * _ChunkSize;

                // Calculate exact local coordinate on the chunk that we're on
                float exactLocalCoordinatesX = adjustedWorldPosX % _ChunkSize;
                float exactLocalCoordinatesY = adjustedWorldPosY % _ChunkSize;

                // If we are within the area that the overlay should be drawn, draw it
                if (exactLocalCoordinatesX >= _TileOverlayX &&
                    exactLocalCoordinatesX < _TileOverlayX + _TileOverlaySize &&
                    exactLocalCoordinatesY >= _TileOverlayY &&
                    exactLocalCoordinatesY < _TileOverlayY + _TileOverlaySize)
                {
                    float uvX = ((adjustedWorldPosX % _ChunkSize) - _TileOverlayX) / _TileOverlaySize;
                    float uvY = ((adjustedWorldPosY % _ChunkSize) - _TileOverlayY) / _TileOverlaySize;
                    float2 uv = float2(uvX, uvY);

                    fixed4 tileOverlayColor = tex2D(_TileOverlayTex, uv) * _TileOverlayColor;
                    c = (tileOverlayColor.a * tileOverlayColor) + ((1 - tileOverlayColor.a) * c); // Add overlay to current color based on overlay alpha
                }
            }

            // Overlay texture that gets repeated over multiple tiles
            if (_ShowMultiOverlay[tileIndex] == 1)
            {
                fixed4 overlayColor = tex2D(_MultiOverlayTex, IN.uv2_MultiOverlayTex) * _MultiOverlayColor;
                c = (overlayColor.a * overlayColor) + ((1 - overlayColor.a) * c);
            }
            
            // ######################################################################### ZONE BORDER #########################################################################
            uint zoneValue = _ZoneBorders[tileIndex];
            uint borderWest = zoneValue % 10;
            zoneValue /= 10;
            uint borderSouth = zoneValue % 10;
            zoneValue /= 10;
            uint borderEast = zoneValue % 10;
            zoneValue /= 10;
            uint borderNorth = zoneValue % 10;

            bool drawBorderPattern = false;
            if (borderNorth == 1 && relativePos.y > 1 - _ZoneBorderWidth) drawBorderPattern = true;
            if (borderEast == 1 && relativePos.x > 1 - _ZoneBorderWidth) drawBorderPattern = true;
            if (borderSouth == 1 && relativePos.y < _ZoneBorderWidth) drawBorderPattern = true;
            if (borderWest == 1 && relativePos.x < _ZoneBorderWidth) drawBorderPattern = true;

            if (drawBorderPattern)
            {
                float squareSize = 0.1;
                float xRel = IN.worldPos.x % (squareSize * 2);
                float zRel = IN.worldPos.z % (squareSize * 2);

                if ((xRel < squareSize && zRel < squareSize) || (xRel > squareSize && zRel > squareSize))
                {
                    int colorIndex = _ZoneBorderColors[tileIndex];
                    c = _PlayerColors[colorIndex];
                }
            }

            // ######################################################################### GRID #########################################################################
            if (_ShowGrid == 1) 
            {
                fixed4 gridColor = tex2D(_GridTex, IN.uv2_GridTex) * _GridColor;
                c = (gridColor.a * gridColor) + ((1 - gridColor.a) * c);
            }

            // ######################################################################### FOG OF WAR TINT #########################################################################
            if (fullVisible != 1)
            {
                c = (_FogOfWarColor.a * _FogOfWarColor) + ((1 - _FogOfWarColor.a) * c);
            }

            // ######################################################################### FINALIZE #########################################################################

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
