Shader "Custom/NodeMaterialShader"
{
    Properties // Exposed to editor in material insepctor
    {
        _MainTex("Texture", 2D) = "none" {}
        _Color("Color", Color) = (1,1,1,1)
        [Toggle] _UseTextures("Use Textures", Float) = 0

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

        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0

        // Z-Fighting-Priority
        _ZPriority("Z-Priority", Float) = 0
    }

        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert

        #pragma target 3.5

        // Base values
        float _ChunkCoordinatesX;
        float _ChunkCoordinatesY;
        float _ChunkSize;

        sampler2D _MainTex;
        fixed4 _Color;
        float _UseTextures;

        // Overlays
        fixed4 _FogOfWarColor;

        sampler2D _GridTex;
        fixed4 _GridColor;
        float _ShowGrid;

        float _ShowTileOverlay;
        sampler2D _TileOverlayTex;
        fixed4 _TileOverlayColor;
        float _TileOverlayX;
        float _TileOverlayY;

        // Material attributes
        half _Glossiness;
        half _Metallic;

        float _TileVisibility[324];

        // Z-Fighting fix
        float _ZPriority;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv2_TileOverlayTex;
            float2 uv2_GridTex;
            float3 worldPos;
        };

        int GetVisibilityArrayIndex(float x, float y)
        {
            return int((y + 1) + (x + 1) * (_ChunkSize + 2));
        }

        void vert(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            // Add a small offset to the vertex position in the view direction
            v.vertex.xyz += -_ZPriority * 0.00001 * _WorldSpaceCameraPos.xyz;
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

            // Check visiblity
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

            // Base color
            fixed4 c;

            if (_UseTextures == 1) c = tex2D(_MainTex, IN.uv_MainTex);
            else c = _Color;


            // Selection Overlay
            if (_ShowTileOverlay == 1)
            {
                if (localCoords.x == _TileOverlayX && localCoords.y == _TileOverlayY)
                {
                    fixed4 tileOverlayColor = tex2D(_TileOverlayTex, IN.uv2_GridTex) * _TileOverlayColor;
                    c = (tileOverlayColor.a * tileOverlayColor) + ((1 - tileOverlayColor.a) * c);
                }
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
