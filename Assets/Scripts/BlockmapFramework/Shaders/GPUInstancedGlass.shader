// Transparent, instancing-friendly triplanar glass with fog-of-war tint.
Shader "Custom/GPUInstancedGlass"
{
    Properties
    {
        // Texture / Triplanar
        [Toggle] _UseTextures("Use Textures", Float) = 1
        _MainTex("Texture", 2D) = "none" {}
        _TextureRotation("Texture Rotation", Range(0, 360)) = 0
        _TextureScale("Texture Scale", Float) = 1
        _TextureTint("Texture Tint", Color) = (1,1,1,0)

        // Base tint (used when not using textures)
        _Color("Base Color", Color) = (1,1,1,1)

        // Sorting nudge (keep small)
        _Offset("Render Priority (use ~0.004 steps)", float) = 0

        // Triplanar
        _TriplanarBlendSharpness("Blend Sharpness", Range(0.1, 8)) = 1

        // Fog Of War
        [Toggle] _FogOfWar("Fog Of War", Float) = 0
        _FogOfWarColor("Fog of war Color", Color) = (0,0,0,0.5)

        // PBR
        _Smoothness("Smoothness", Range(0,1)) = 0.9
        _Metallic("Metallic", Range(0,1)) = 0.0

        // Transparency
        _Transparency("Transparency", Range(0,1)) = 0.4

        // Common toggles
        [Enum(Back,2, Front,1, Off,0)] _Cull("Cull", Float) = 2
        [Toggle] _ZWrite("ZWrite (0 off, 1 on)", Float) = 0
    }

    SubShader
    {
        // Transparent queue and type
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        // Sorting nudge
        Offset[_Offset],[_Offset]

        // Standard alpha blending
        Blend SrcAlpha OneMinusSrcAlpha

        // Typically off for transparent glass to avoid sorting artifacts behind it
        ZWrite [_ZWrite]
        Cull [_Cull]

        CGPROGRAM
        // Surface shader with transparency; do not add shadows for transparent
        #pragma surface surf Standard alpha:fade
        #pragma target 3.5

        // GPU instancing
        #pragma multi_compile_instancing
        #pragma instancing_options assumeuniformscaling

        #include "UnityCG.cginc"

        // Per-instance buffer + dummy prop to force instanced variant usage
        UNITY_INSTANCING_BUFFER_START(PerInst)
        UNITY_DEFINE_INSTANCED_PROP(float, _InstanceTag)
        UNITY_INSTANCING_BUFFER_END(PerInst)

        struct Input
        {
            float2 uv_MainTex;
            float2 uv2_TileOverlayTex;
            float2 uv2_MultiOverlayTex;
            float2 uv2_GridTex;
            float3 worldPos;
            float3 worldNormal;
            float3 viewDir;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            INTERNAL_DATA
        };

        // Props
        sampler2D _MainTex;
        float _TextureRotation;
        float _TextureScale;
        fixed4 _TextureTint;

        float _UseTextures;
        fixed4 _Color;

        float _Smoothness;
        float _Metallic;

        float _TriplanarBlendSharpness;

        float _FogOfWar;
        fixed4 _FogOfWarColor;

        float _Transparency;

        // Helpers
        float2 RotateUV(float2 uv, float deg)
        {
            float r = radians(deg);
            float s = sin(r), c = cos(r);
            return mul(float2x2(c, -s, s, c), uv);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Make sure instancing is active on this pass
            UNITY_SETUP_INSTANCE_ID(IN);
            float tag = UNITY_ACCESS_INSTANCED_PROP(PerInst, _InstanceTag);
            // No-op read so DrawMeshInstanced is happy:
            o.Emission += tag * 0.0;

            // Base color
            fixed4 c = _Color;

            // Triplanar texture sampling (optional)
            if (_UseTextures == 1)
            {
                half2 yUV = IN.worldPos.xz / _TextureScale;
                half2 xUV = IN.worldPos.zy / _TextureScale;
                half2 zUV = IN.worldPos.xy / _TextureScale;

                yUV = RotateUV(yUV, _TextureRotation);
                xUV = RotateUV(xUV, _TextureRotation);
                zUV = RotateUV(zUV, _TextureRotation);

                half3 wN = abs(WorldNormalVector(IN, float3(0,1,0))); // use o.Normal if you pre-set; here a dummy vector
                half3 blendW = pow(wN, _TriplanarBlendSharpness);
                blendW /= (blendW.x + blendW.y + blendW.z + 1e-5);

                half3 yDiff = tex2D(_MainTex, yUV).rgb;
                half3 xDiff = tex2D(_MainTex, xUV).rgb;
                half3 zDiff = tex2D(_MainTex, zUV).rgb;

                c.rgb = xDiff * blendW.x + yDiff * blendW.y + zDiff * blendW.z;
                c.rgb *= _TextureTint.rgb;
            }

            // Fog-of-war tint (simple lerp)
            if (_FogOfWar == 1)
            {
                c = _FogOfWarColor.a * _FogOfWarColor + (1 - _FogOfWarColor.a) * c;
            }

            // PBR params for glassy look
            o.Metallic   = _Metallic;
            o.Smoothness = _Smoothness;
            o.Occlusion  = 1.0;

            // Final color & alpha
            o.Albedo = c.rgb;
            o.Alpha  = saturate(1.0 - _Transparency); // higher Transparency => lower alpha
        }
        ENDCG
    }
}
