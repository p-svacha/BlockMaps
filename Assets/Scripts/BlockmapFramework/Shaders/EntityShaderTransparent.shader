Shader "Custom/EntityShaderTransparent"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _TintColor("Texture Tint", Color) = (1,1,1,0)
        _Transparency("Transparency", Range(0,1)) = 0
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [Toggle] _UseTextures("Use Textures", Float) = 0
        [Toggle] _OnlyFlatShading("Only Flat Shading", Float) = 0
        [Toggle] _TextureIsTriplanar("Triplanar Textures", Float) = 0
        _TriplanarTextureScale("Triplanar Texture Scale", Float) = 1
        _Glossiness ("Smoothness", Range(0,1)) = 0.0
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows keepalpha

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _TintColor;
        half _Transparency;
        float _UseTextures;
        float _OnlyFlatShading;
        float _TextureIsTriplanar;
        float _TriplanarTextureScale;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c;
            if (_UseTextures == 1 && _OnlyFlatShading == 0)
            {
                if (_TextureIsTriplanar == 1) 
                {
                    // Find our UVs for each axis based on world position of the fragment.
                    half2 yUV = IN.worldPos.xz / _TriplanarTextureScale;
                    half2 xUV = IN.worldPos.zy / _TriplanarTextureScale;
                    half2 zUV = IN.worldPos.xy / _TriplanarTextureScale;

                    // Get the absolute value of the world normal
                    half3 blendWeights = abs(WorldNormalVector(IN, o.Normal));

                    // Normalize blend weights so x+y+z=1
                    blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);

                    // Determine top texture strength based on steepness
                    float sideStartSteepness = 0.3;
                    float sideOnlySteepness = 0.7;
                    float steepness = 1 - blendWeights.y;
                    float topTextureStrength;
                    if (steepness < sideStartSteepness)
                    {
                        topTextureStrength = 1;
                    }
                    else if (steepness < sideOnlySteepness)
                    {
                        topTextureStrength = 1 - ((steepness - sideStartSteepness) * (1 / (sideOnlySteepness - sideStartSteepness)));
                    }
                    else
                    {
                        topTextureStrength = 0;
                    }

                    // Diffuse blending
                    half3 yDiff = (1 - topTextureStrength) * tex2D(_MainTex, yUV) + topTextureStrength * tex2D(_MainTex, yUV);
                    half3 xDiff = (1 - topTextureStrength) * tex2D(_MainTex, xUV) + topTextureStrength * tex2D(_MainTex, xUV);
                    half3 zDiff = (1 - topTextureStrength) * tex2D(_MainTex, zUV) + topTextureStrength * tex2D(_MainTex, zUV);

                    c.rgb = xDiff * blendWeights.x + yDiff * blendWeights.y + zDiff * blendWeights.z;
                }
                else
                {
                    c = tex2D(_MainTex, IN.uv_MainTex);
                }
            }
            else c = _Color;

            // Tint
            c = _TintColor.a * _TintColor + (1 - _TintColor.a) * c;

            // Transparency
            c.a = 1 - _Transparency;

            // Finalize
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
