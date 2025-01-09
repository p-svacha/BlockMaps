Shader "Custom/EntityShaderOpaque"
{
    // Exactly the entity shader but it also supports transparency
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _TintColor("Texture Tint", Color) = (1,1,1,0)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        [Toggle] _UseTextures("Use Textures", Float) = 0
        [Toggle] _OnlyFlatShading("Only Flat Shading", Float) = 0
        [Toggle] _TextureIsTriplanar("Triplanar Textures", Float) = 0
        _TriplanarTextureScale("Triplanar Texture Scale", Float) = 1
        _Glossiness("Smoothness", Range(0,1)) = 0.0
        _Metallic("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        // --- OPAQUE RENDERING ---
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
        }
        LOD 200

        // Disable alpha blending
        Blend Off
        ZWrite On

        CGPROGRAM
        // Standard physically based lighting model
        #pragma surface surf Standard fullforwardshadows

        // Shader model 3.0
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
        float _UseTextures;
        float _OnlyFlatShading;
        float _TextureIsTriplanar;
        float _TriplanarTextureScale;

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c;
            // Same texture logic as before:
            if (_UseTextures == 1 && _OnlyFlatShading == 0)
            {
                if (_TextureIsTriplanar == 1)
                {
                    // Triplanar tiling based on world position
                    half2 yUV = IN.worldPos.xz / _TriplanarTextureScale;
                    half2 xUV = IN.worldPos.zy / _TriplanarTextureScale;
                    half2 zUV = IN.worldPos.xy / _TriplanarTextureScale;

                    // Blending weights based on absolute world normal
                    half3 blendWeights = abs(WorldNormalVector(IN, o.Normal));
                    blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z);

                    // Example slope-based blend for top vs sides
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

                    half3 yDiff = (1 - topTextureStrength) * tex2D(_MainTex, yUV) + topTextureStrength * tex2D(_MainTex, yUV);
                    half3 xDiff = (1 - topTextureStrength) * tex2D(_MainTex, xUV) + topTextureStrength * tex2D(_MainTex, xUV);
                    half3 zDiff = (1 - topTextureStrength) * tex2D(_MainTex, zUV) + topTextureStrength * tex2D(_MainTex, zUV);

                    c.rgb = xDiff * blendWeights.x + yDiff * blendWeights.y + zDiff * blendWeights.z;
                    c.a = 1;
                }
                else
                {
                    c = tex2D(_MainTex, IN.uv_MainTex);
                }
            }
            else
            {
                c = _Color;
            }

            // Apply tint
            c = _TintColor.a * _TintColor + (1 - _TintColor.a) * c;

            // Finalize
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
