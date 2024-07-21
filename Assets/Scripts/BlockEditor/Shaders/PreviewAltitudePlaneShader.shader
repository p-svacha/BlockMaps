Shader "Custom/PreviewAltitudePlaneShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _GridTex ("Repeating Grid Texture", 2D) = "white" {}
        _GridColor("Grid Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Offset 1,1
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off // Disable depth writing

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _GridTex;
            fixed4 _GridColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Base color
                fixed4 c = _Color;

                // Add grid overlay
                fixed4 gridColor = tex2D(_GridTex, i.worldPos.xz) * _GridColor;
                c.rgb = (gridColor.a * gridColor.rgb) + ((1 - gridColor.a) * c.rgb);

                // Transparency
                c.a = max(_Color.a, gridColor.a);

                return c;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
