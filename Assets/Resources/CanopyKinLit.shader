Shader "CanopyKin/ProceduralLit"
{
    Properties { _Color("Color", Color)=(1,1,1,1) _Smoothness("Smoothness",Range(0,1))=.2 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; };
            half4 _Color; half _Smoothness;
            Varyings vert(Attributes i){Varyings o;o.positionCS=UnityObjectToClipPos(i.positionOS);o.normalWS=UnityObjectToWorldNormal(i.normalOS);return o;}
            half4 frag(Varyings i):SV_Target{half ndl=saturate(dot(normalize(i.normalWS),normalize(_WorldSpaceLightPos0.xyz)));return half4(_Color.rgb*(UNITY_LIGHTMODEL_AMBIENT.rgb+ndl*.8h+.2h),_Color.a);}
            ENDCG
        }
    }
}
