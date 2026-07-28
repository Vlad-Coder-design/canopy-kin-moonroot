Shader "CanopyKin/ForestPBR"
{
    Properties
    {
        _Color("Tint", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}
        _BumpMap("Normal", 2D) = "bump" {}
        _RoughnessMap("Roughness", 2D) = "white" {}
        _NormalStrength("Normal Strength", Range(0,2)) = 1
        _Smoothness("Smoothness", Range(0,1)) = .22
        _Metallic("Metallic", Range(0,1)) = 0
        _Occlusion("Occlusion", Range(.35,1)) = .92
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 260

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma multi_compile_instancing

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _RoughnessMap;
        fixed4 _Color;
        half _NormalStrength;
        half _Smoothness;
        half _Metallic;
        half _Occlusion;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 albedo = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            half3 sampledNormal = tex2D(_BumpMap, IN.uv_MainTex).rgb * 2.0h - 1.0h;
            sampledNormal.xy *= _NormalStrength;
            sampledNormal.z = sqrt(saturate(1.0h - dot(sampledNormal.xy, sampledNormal.xy)));
            half roughness = tex2D(_RoughnessMap, IN.uv_MainTex).r;
            o.Albedo = albedo.rgb;
            o.Normal = normalize(sampledNormal);
            o.Metallic = _Metallic;
            o.Smoothness = saturate((1.0h - roughness) * .72h + _Smoothness * .28h);
            o.Occlusion = _Occlusion;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Standard"
}
