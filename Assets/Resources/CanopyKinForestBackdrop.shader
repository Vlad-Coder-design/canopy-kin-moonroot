Shader "CanopyKin/ForestBackdrop"
{
    Properties
    {
        _MainTex("Forest Panorama", 2D) = "white" {}
        _Color("Base Color", Color) = (.18,.25,.13,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-12" }
        Cull Back
        ZWrite On
        LOD 100
        Pass
        {
            CGPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                UNITY_TRANSFER_FOG(output, output.vertex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.uv) * _Color;
                UNITY_APPLY_FOG(input.fogCoord, color);
                color.a = 1;
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
