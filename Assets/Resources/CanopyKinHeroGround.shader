Shader "CanopyKin/HeroGroundBlend"
{
    Properties
    {
        _SoilAlbedo("Forest Soil Albedo", 2D) = "white" {}
        _SoilNormal("Forest Soil Normal", 2D) = "bump" {}
        _SoilRoughness("Forest Soil Roughness", 2D) = "white" {}
        _SoilAO("Forest Soil AO", 2D) = "white" {}
        _MossAlbedo("Moss Albedo", 2D) = "white" {}
        _MossNormal("Moss Normal", 2D) = "bump" {}
        _LeafAlbedo("Leaf Litter Albedo", 2D) = "white" {}
        _LeafNormal("Leaf Litter Normal", 2D) = "bump" {}
        _Tint("Ecological Tint", Color) = (1,1,1,1)
        _NormalStrength("Normal Strength", Range(0,2)) = 1.15
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" }
        LOD 360
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows addshadow
        #include "UnityStandardUtils.cginc"

        sampler2D _SoilAlbedo;
        sampler2D _SoilNormal;
        sampler2D _SoilRoughness;
        sampler2D _SoilAO;
        sampler2D _MossAlbedo;
        sampler2D _MossNormal;
        sampler2D _LeafAlbedo;
        sampler2D _LeafNormal;
        fixed4 _Tint;
        half _NormalStrength;

        struct Input
        {
            float3 worldPos;
            fixed4 color : COLOR;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 soilUv = IN.worldPos.xz / 3.35;
            float2 macroUv = IN.worldPos.xz / 9.7 + float2(.17, .31);
            float2 mossUv = IN.worldPos.xz / 1.18 + float2(.41, .08);
            float2 leafUv = IN.worldPos.xz / 2.25 + float2(.13, .57);

            fixed3 soil = tex2D(_SoilAlbedo, soilUv).rgb;
            fixed3 macro = tex2D(_SoilAlbedo, macroUv).rgb;
            soil = lerp(soil, soil * macro * 1.23, .34);
            fixed3 moss = tex2D(_MossAlbedo, mossUv).rgb * fixed3(.78, .92, .68);
            fixed3 leaves = tex2D(_LeafAlbedo, leafUv).rgb * fixed3(.86, .8, .68);

            half mossMask = saturate(IN.color.r * (1.08 - IN.color.g * .4));
            half leafMask = saturate(IN.color.g * (1.0 - mossMask * .72));
            half wetMask = saturate(IN.color.b);
            fixed3 albedo = lerp(soil, leaves, leafMask * .72);
            albedo = lerp(albedo, moss, mossMask * .88);
            albedo *= lerp(1.0, .52, wetMask);

            half3 soilNormal = UnpackScaleNormal(tex2D(_SoilNormal, soilUv), _NormalStrength);
            half3 mossNormal = UnpackScaleNormal(tex2D(_MossNormal, mossUv), _NormalStrength * .78);
            half3 leafNormal = UnpackScaleNormal(tex2D(_LeafNormal, leafUv), _NormalStrength * .72);
            half3 blendedNormal = normalize(lerp(soilNormal, leafNormal, leafMask * .72));
            blendedNormal = normalize(lerp(blendedNormal, mossNormal, mossMask * .82));

            half roughness = tex2D(_SoilRoughness, soilUv).r;
            half ao = tex2D(_SoilAO, soilUv).r;
            o.Albedo = albedo * _Tint.rgb;
            o.Normal = blendedNormal;
            o.Metallic = 0;
            o.Smoothness = saturate(lerp((1.0h - roughness) * .72h, .86h, wetMask));
            o.Occlusion = lerp(ao, .72h, mossMask * .24h);
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "CanopyKin/ForestPBR"
}
