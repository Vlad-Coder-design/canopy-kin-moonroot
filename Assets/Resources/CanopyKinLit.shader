Shader "CanopyKin/ForestPBR"
{
    Properties
    {
        _Color("Tint", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}
        _BumpMap("Normal", 2D) = "bump" {}
        _RoughnessMap("Roughness", 2D) = "white" {}
        _OcclusionMap("Ambient Occlusion", 2D) = "white" {}
        _HeightMap("Height", 2D) = "gray" {}
        _PackedArm("Packed AO/Roughness/Metallic", 2D) = "white" {}
        _UsePackedArm("Use Packed ARM", Range(0,1)) = 0
        _NormalStrength("Normal Strength", Range(0,2)) = 1
        _Parallax("Parallax", Range(0,.08)) = 0
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
        #include "UnityStandardUtils.cginc"

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _RoughnessMap;
        sampler2D _OcclusionMap;
        sampler2D _HeightMap;
        sampler2D _PackedArm;
        fixed4 _Color;
        half _NormalStrength;
        half _Parallax;
        half _Smoothness;
        half _Metallic;
        half _Occlusion;
        half _UsePackedArm;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;
            half height = tex2D(_HeightMap, uv).r - .5h;
            half3 view = normalize(IN.viewDir);
            uv += view.xy * height * _Parallax;
            fixed4 albedo = tex2D(_MainTex, uv) * _Color;
            half3 sampledNormal = UnpackScaleNormal(tex2D(_BumpMap, uv), _NormalStrength);
            half4 packedArm = tex2D(_PackedArm, uv);
            half roughness = lerp(tex2D(_RoughnessMap, uv).r, packedArm.g, _UsePackedArm);
            half occlusion = lerp(tex2D(_OcclusionMap, uv).r, packedArm.r, _UsePackedArm);
            half metallic = lerp(_Metallic, packedArm.b, _UsePackedArm);
            o.Albedo = albedo.rgb;
            o.Normal = sampledNormal;
            o.Metallic = metallic;
            o.Smoothness = saturate((1.0h - roughness) * .72h + _Smoothness * .28h);
            o.Occlusion = occlusion * _Occlusion;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Standard"
}
