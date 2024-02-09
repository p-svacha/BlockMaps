Shader "Custom/NodeMaterialShader"
{
    Properties // Exposed to editor in material insepctor
    {
        [Toggle] _UseTextures("Use Textures", Float) = 0
        _MainTex("Texture", 2D) = "none" {}
        _Color("Color", Color) = (1,1,1,1)
        _Offset("Render Priority (lowest renders first, use 0.1 steps)", float) = 0

        // Texture mode values
        _TextureScale("Texture Scale", Float) = 1
        _TriplanarBlendSharpness("Blend Sharpness",float) = 1
        _SideStartSteepness("Side Texture Start Steepness",float) = 0.3 // The steepness where side texture starts to show through
        _SideOnlySteepness("Side Texture Only Steepness",float) = 0.7 // The steepness where only side texture is shown

        // Overlays
        _FogOfWarColor("Fog of war Color", Color) = (0,0,0,0.5)

        _GridTex("Grid Texture", 2D) = "none" {}
        [Toggle] _ShowGrid("Show Grid", Float) = 1
        _GridColor("Grid Color", Color) = (0,0,0,1)

        [Toggle] _CanShowTileOverlay("Can Show Tile Overlay", Float) = 1

        /* Should not be set in inspector
        [Toggle] _ShowTileOverlay("Show Tile Overlay", Float) = 0
        _TileOverlayTex("Overlay Texture", 2D) = "none" {}
        _TileOverlayColor("Overlay Color", Color) = (0,0,0,0)
        _TileOverlayX("Overlay X Coord", Float) = 0
        _TileOverlayY("Overlay Y Coord", Float) = 0
        */

        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
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

        // Base values
        float _ChunkCoordinatesX;
        float _ChunkCoordinatesY;
        float _ChunkSize;

        sampler2D _MainTex;
        fixed4 _Color;
        float _UseTextures;
        float _TextureScale;
        float _TriplanarBlendSharpness;
        float _SideStartSteepness;
        float _SideOnlySteepness;

        // Overlays
        fixed4 _FogOfWarColor;

        sampler2D _GridTex;
        fixed4 _GridColor;
        float _ShowGrid;

        // Overlay texture over a single area
        float _CanShowTileOverlay;
        float _ShowTileOverlay;
        sampler2D _TileOverlayTex;
        fixed4 _TileOverlayColor;
        float _TileOverlayX;
        float _TileOverlayY;

        // Overlay texture over multiple tiles repeated
        float _ShowMultiOverlay[256]; // bool for each tile if the overlay is shown
        sampler2D _MultiOverlayTex;
        fixed4 _MultiOverlayColor;

        // Material attributes
        half _Glossiness;
        half _Metallic;

        float _TileVisibility[324];

        struct Input
        {
            float2 uv_MainTex;
            float2 uv2_TileOverlayTex;
            float2 uv2_MultiOverlayTex;
            float2 uv2_GridTex;
            float3 worldPos;
            float3 worldNormal;
        };

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

            int tileIndex = int(localCoords.y + localCoords.x * _ChunkSize);

            // ######################################################################### VISIBILITY #########################################################################

            float visEpsilon = 0.101; // Pixels are drawn by this value over tile edges
            float tileVisibility = _TileVisibility[GetVisibilityArrayIndex(localCoords.x, localCoords.y)];
                float drawPixel = (tileVisibility > 0 ||

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
            float fullVisible = (tileVisibility > 1 ||

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

            // ######################################################################### TRIPLANAR TEXTURE #########################################################################

            if (_UseTextures == 1)
            {
                // Find our UVs for each axis based on world position of the fragment.
                half2 yUV = IN.worldPos.xz / _TextureScale;
                half2 xUV = IN.worldPos.zy / _TextureScale;
                half2 zUV = IN.worldPos.xy / _TextureScale;

                // Get the absolute value of the world normal.
                // Put the blend weights to the power of BlendSharpness: The higher the value, the sharper the transition between the planar maps will be.
                half3 blendWeights = pow(abs(IN.worldNormal), _TriplanarBlendSharpness);

                // ----- TRIPLANAR ------
                // Divide our blend mask by the sum of it's components, this will make x+y+z=1
                blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);

                // Define how much of alpha is top texture
                float steepness = 1 - blendWeights.y;
                float topTextureStrength;
                if (steepness < _SideStartSteepness)
                {
                    topTextureStrength = 1;
                }
                else if (steepness < _SideOnlySteepness)
                {
                    topTextureStrength = 1 - ((steepness - _SideStartSteepness) * (1 / (_SideOnlySteepness - _SideStartSteepness)));
                }
                else
                {
                    topTextureStrength = 0;
                }

                // Now do texture samples from our diffuse maps with each of the 3 UV set's we've just made.
                // Blend top with side texture according to how much the surface normal points vertically (y-direction)
                half3 yDiff = (1 - topTextureStrength) * tex2D(_MainTex, yUV) + topTextureStrength * tex2D(_MainTex, yUV);
                half3 xDiff = (1 - topTextureStrength) * tex2D(_MainTex, xUV) + topTextureStrength * tex2D(_MainTex, xUV);
                half3 zDiff = (1 - topTextureStrength) * tex2D(_MainTex, zUV) + topTextureStrength * tex2D(_MainTex, zUV);

                c.rgb = xDiff * blendWeights.x + yDiff * blendWeights.y + zDiff * blendWeights.z;
            }
            else c = _Color;


            // ######################################################################### OVERLAYS #########################################################################

            if (_CanShowTileOverlay == 1 && _ShowTileOverlay == 1)
            {
                if (localCoords.x == _TileOverlayX && localCoords.y == _TileOverlayY)
                {
                    fixed4 tileOverlayColor = tex2D(_TileOverlayTex, IN.uv2_GridTex) * _TileOverlayColor;
                    c = (tileOverlayColor.a * tileOverlayColor) + ((1 - tileOverlayColor.a) * c);
                }
            }

            if (_ShowMultiOverlay[tileIndex] == 1)
            {
                float dotProduct = dot(IN.worldNormal, float3(0, 1, 0));
                if (dotProduct > 0.9) // only draw when facing (almost upwards
                {
                    fixed4 overlayColor = tex2D(_MultiOverlayTex, IN.uv2_GridTex) * _MultiOverlayColor;
                    c = (overlayColor.a * overlayColor) + ((1 - overlayColor.a) * c);
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
