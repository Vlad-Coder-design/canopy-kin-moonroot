Shader "CanopyKin/ProceduralLit"
{
    Properties { _Color("Color", Color)=(1,1,1,1) _Smoothness("Smoothness",Range(0,1))=.2 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 normalWS:TEXCOORD0;
                float3 positionWS:TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };
            half4 _Color; half _Smoothness;
            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS=UnityObjectToClipPos(i.positionOS);
                o.normalWS=UnityObjectToWorldNormal(i.normalOS);
                o.positionWS=mul(unity_ObjectToWorld,i.positionOS).xyz;
                UNITY_TRANSFER_FOG(o,o.positionCS);
                return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                half3 normalWS=normalize(i.normalWS);
                half ndl=saturate(dot(normalWS,normalize(_WorldSpaceLightPos0.xyz)));
                half3 ambient=ShadeSH9(half4(normalWS,1));
                half3 viewDirection=normalize(_WorldSpaceCameraPos-i.positionWS);
                half3 halfDirection=normalize(viewDirection+normalize(_WorldSpaceLightPos0.xyz));
                half specular=pow(saturate(dot(normalWS,halfDirection)),lerp(6h,48h,_Smoothness))*_Smoothness;
                half4 result=half4(_Color.rgb*(ambient+_LightColor0.rgb*(ndl*.78h+.32h))+_LightColor0.rgb*specular*.35h,_Color.a);
                UNITY_APPLY_FOG(i.fogCoord,result);
                return result;
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
