Shader "Custom/SurfaceShader"
{
    // District Shader that supports blink and animated highlight but no borders. Mesh borders should be used with this shader
    Properties
    {
        _Color("Main Color", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}

        _TintColor("Tint Color", Color) = (1,1,1,0)

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


        sampler2D _MainTex;

        sampler2D _GridTex;
        fixed4 _GridColor;
        float _ShowGrid;

        float _ShowTileOverlay;
        sampler2D _TileOverlayTex;
        fixed4 _TileOverlayColor;
        
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
        fixed4 _Color;
        fixed4 _TintColor;

        float _TileSurfaces[1000];

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;

            // Selection Overlay
            if (_ShowTileOverlay == 1)
            {
                fixed4 tileOverlayColor = tex2D(_TileOverlayTex, IN.uv2_GridTex) * _TileOverlayColor;
                c = (tileOverlayColor.a * tileOverlayColor) + ((1 - tileOverlayColor.a) * c);
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
