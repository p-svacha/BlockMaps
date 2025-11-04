// Simple shader that supports triplanar and fog of war.
Shader "Custom/GPUInstancedMeshShader"
{
    Properties
    {
        // Texture
        [Toggle] _UseTextures("Use Textures", Float) = 1
        _MainTex("Texture", 2D) = "none" {}
        _TextureRotation("Texture Rotation", Range(0, 360)) = 0
        _TextureScale("Texture Scale", Float) = 1
        _TextureTint("Texture Tint", Color) = (1,1,1,0)

        _Color("Color (when not using texture mode)", Color) = (1,1,1,1)
        _Offset("Render Priority (lowest renders first, use 0.004 steps)", float) = 0

        // Triplanar
        _TriplanarBlendSharpness("Blend Sharpness", Range(0.1, 8)) = 1

        // Fog Of War
        [Toggle] _FogOfWar("Fog Of War", Float) = 0
        _FogOfWarColor("Fog of war Color", Color) = (0,0,0,0.5)

        // Base
        _Smoothness("Smoothness", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Offset[_Offset],[_Offset]
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.5

        // Enable GPU instancing
        #pragma multi_compile_instancing
        #pragma instancing_options assumeuniformscaling

        #include "UnityCG.cginc"

        // ---- Per-instance buffer (exists in all generated passes) ----
        UNITY_INSTANCING_BUFFER_START(PerInst)
            UNITY_DEFINE_INSTANCED_PROP(float, _InstanceTag)
        UNITY_INSTANCING_BUFFER_END(PerInst)

        // Dummy vertex modifier to force instanced variant usage in all passes (GPU instancing fix)
        void vert(inout appdata_full v)
        {
            UNITY_SETUP_INSTANCE_ID(v);
            float tag = UNITY_ACCESS_INSTANCED_PROP(PerInst, _InstanceTag);
            v.vertex.xyz += tag * 0.0; // no-op but counts as a read in ALL passes
        }

        struct Input {
            float2 uv_MainTex;
            float2 uv2_TileOverlayTex;
            float2 uv2_MultiOverlayTex;
            float2 uv2_GridTex;
            float3 worldPos;
            float3 worldNormal;
            float3 viewDir;
            INTERNAL_DATA
        };

        // Texture
        sampler2D _MainTex;
        float _TextureRotation;
        float _TextureScale;
        fixed4 _TextureTint;

        float _UseTextures;
        fixed4 _Color;
        float _Smoothness;

        // Triplanar
        float _TriplanarBlendSharpness;

        // Fog Of War
        float _FogOfWar;
        fixed4 _FogOfWarColor;

        // Helpers
        float2 RotateUV(float2 uv, float rotationAngle) {
            float rad = radians(rotationAngle);
            float cosTheta = cos(rad);
            float sinTheta = sin(rad);
            float2x2 rotationMatrix = float2x2(cosTheta, -sinTheta, sinTheta, cosTheta);
            return mul(rotationMatrix, uv);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // ######################################################################### BASE #########################################################################
            
            fixed4 c = _Color;

            float dotProduct = dot(WorldNormalVector(IN, o.Normal), float3(0, 1, 0));
            float isFacingUpwards = (dotProduct > 0.4);

            // ######################################################################### TRIPLANAR TEXTURE #########################################################################

            if (_UseTextures == 1)
            {
                // Find our UVs for each axis based on world position of the fragment.
                half2 yUV = IN.worldPos.xz / _TextureScale;
                half2 xUV = IN.worldPos.zy / _TextureScale;
                half2 zUV = IN.worldPos.xy / _TextureScale;

                // Get rotated UVs
                float rotation = _TextureRotation;
                yUV = RotateUV(yUV, rotation);
                xUV = RotateUV(xUV, rotation);
                zUV = RotateUV(zUV, rotation);

                // Get the absolute value of the world normal, put the blend weights to the power of the sharpness
                half3 blendWeights = pow(abs(WorldNormalVector(IN, o.Normal)), _TriplanarBlendSharpness);

                // Normalize blend weights so x+y+z=1
                blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);

                o.Occlusion = 1.0;
                o.Metallic = 0.0;

                // Diffuse blending (triplanar)
                half3 yDiff = tex2D(_MainTex, yUV).rgb;
                half3 xDiff = tex2D(_MainTex, xUV).rgb;
                half3 zDiff = tex2D(_MainTex, zUV).rgb;

                c.rgb = xDiff * blendWeights.x + yDiff * blendWeights.y + zDiff * blendWeights.z;
                o.Smoothness = o.Smoothness = _Smoothness;


                // Tint
                c.rgb *= _TextureTint.rgb;
            }

            // ######################################################################### FOG OF WAR TINT ###############################################################

            if (_FogOfWar == 1)
            {
                c = (_FogOfWarColor.a * _FogOfWarColor) + ((1 - _FogOfWarColor.a) * c);
            }

            // ######################################################################### FINALIZE #########################################################################

            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }

        ENDCG
    }

    FallBack Off
}
