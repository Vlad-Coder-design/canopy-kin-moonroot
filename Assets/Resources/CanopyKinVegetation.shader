Shader "CanopyKin/Vegetation"
{
    Properties
    {
        _Color("Tint", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0,1)) = .08
        _WindStrength("Wind Strength", Range(0,.3)) = .09
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+5" }
        Cull Off
        LOD 190

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            half _WindStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 lighting : TEXCOORD1;
                fixed variation : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                float gust = sin(_Time.y * 1.17 + world.x * .21 + world.z * .17)
                           + sin(_Time.y * .63 + world.x * .08 - world.z * .19) * .45;
                v.vertex.xz += float2(gust, gust * .31) * _WindStrength * v.color.r;

                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                half3 normal = normalize(UnityObjectToWorldNormal(v.normal));
                half diffuse = saturate(dot(normal, _WorldSpaceLightPos0.xyz));
                half backLight = saturate(dot(-normal, _WorldSpaceLightPos0.xyz)) * .22;
                o.lighting = max(ShadeSH9(half4(normal, 1)), half3(.08, .09, .06))
                           + _LightColor0.rgb * (diffuse + backLight);
                o.variation = v.color.g;
                UNITY_TRANSFER_FOG(o, o.position);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed3 textureColor = tex2D(_MainTex, i.uv).rgb;
                fixed3 baseColor = lerp(
                    textureColor,
                    textureColor * _Color.rgb * 1.55,
                    .62);
                fixed3 color = saturate(
                    baseColor * max(i.lighting, half3(.42, .46, .38))
                    * lerp(.95, 1.35, i.variation)
                    + baseColor * .065);
                fixed4 result = fixed4(color, 1);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            half _WindStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
            };

            v2f vertShadow(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                float gust = sin(_Time.y * 1.17 + world.x * .21 + world.z * .17)
                           + sin(_Time.y * .63 + world.x * .08 - world.z * .19) * .45;
                v.vertex.xz += float2(gust, gust * .31) * _WindStrength * v.color.r;
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 fragShadow(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
