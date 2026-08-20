Shader "CanopyKin/HeroLeaf"
{
    Properties
    {
        _Color("Tint", Color) = (1,1,1,1)
        _MainTex("Dead Leaf Atlas", 2D) = "white" {}
        _BumpMap("Micro Normal", 2D) = "bump" {}
        _RoughnessMap("Roughness", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0,1)) = .28
        _NormalStrength("Normal Strength", Range(0,2)) = .7
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest+4" }
        Cull Off
        LOD 320
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows addshadow alphatest:_Cutoff
        #include "UnityStandardUtils.cginc"
        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _RoughnessMap;
        fixed4 _Color;
        half _NormalStrength;
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 viewDir;
        };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 leaf = tex2D(_MainTex, IN.uv_MainTex);
            half3 normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_BumpMap * 1.8),
                _NormalStrength);
            half roughness = tex2D(_RoughnessMap, IN.uv_BumpMap * 1.8).r;
            half rim = pow(1.0h - saturate(abs(dot(normalize(IN.viewDir), half3(0,0,1)))), 2.0h);
            o.Albedo = leaf.rgb * _Color.rgb;
            o.Normal = normal;
            o.Metallic = 0;
            o.Smoothness = saturate((1.0h - roughness) * .38h + .06h);
            o.Occlusion = .92h;
            o.Emission = leaf.rgb * rim * .035h;
            o.Alpha = leaf.a;
        }
        ENDCG
    }
    Fallback "Transparent/Cutout/Diffuse"
}
