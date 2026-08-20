Shader "CanopyKin/HeroWater"
{
    Properties
    {
        _MainTex("Surface Mask", 2D) = "white" {}
        _Color("Water Color", Color) = (.055,.19,.17,.78)
        _EdgeColor("Surface Tension Edge", Color) = (.46,.72,.62,.9)
        _Smoothness("Smoothness", Range(0,1)) = .96
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        LOD 300
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard alpha:fade
        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _EdgeColor;
        half _Smoothness;
        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 viewDir;
        };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 centered = IN.uv_MainTex - .5;
            half radius = length(centered) * 2.0;
            half edge = smoothstep(.76h, 1.0h, radius);
            half rippleA = sin(IN.worldPos.x * 9.1 + _Time.y * 1.15);
            half rippleB = cos(IN.worldPos.z * 10.7 - _Time.y * .83);
            half ripple = rippleA * rippleB;
            half fresnel = pow(1.0h - saturate(dot(normalize(IN.viewDir), half3(0,0,1))), 3.0h);
            fixed3 water = lerp(_Color.rgb, _EdgeColor.rgb, edge * .48h + fresnel * .2h);
            o.Albedo = water + ripple * .012h;
            o.Normal = normalize(half3(rippleA * .11h, rippleB * .11h, 1));
            o.Metallic = .03h;
            o.Smoothness = _Smoothness;
            o.Emission = _EdgeColor.rgb * edge * .045h;
            o.Alpha = saturate(_Color.a + edge * .16h + fresnel * .08h);
        }
        ENDCG
    }
    Fallback "Transparent/Diffuse"
}
