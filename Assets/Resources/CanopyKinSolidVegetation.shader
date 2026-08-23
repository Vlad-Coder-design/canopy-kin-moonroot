Shader "CanopyKin/SolidVegetation"
{
    Properties
    {
        _Color("Living Colour", Color) = (.35,.58,.16,1)
        _MainTex("Surface Albedo", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0,1)) = .12
        _WindStrength("Wind Strength", Range(0,.25)) = .06
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        ZWrite On
        LOD 340
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma multi_compile_instancing

        sampler2D _MainTex;
        fixed4 _Color;
        half _Smoothness;
        half _WindStrength;
        float4 _CanopyKinPlayerPosition;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            fixed4 color : COLOR;
        };

        void vert(inout appdata_full v)
        {
            float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
            float phase = v.color.b * 6.28318;
            float breeze = sin(_Time.y * .72 + world.x * .13 - world.z * .09 + phase);
            float detail = sin(_Time.y * 1.91 + world.x * .37 + world.z * .21 + phase * 1.7) * .31;
            float response = v.color.r * lerp(1.0, .58, v.color.g);
            float2 away = world.xz - _CanopyKinPlayerPosition.xz;
            float distanceToPlayer = max(length(away), .001);
            float contact = saturate(1.0 - distanceToPlayer / .72) * response;
            float2 contactDirection = away / distanceToPlayer;
            float gust = (breeze + detail) * _WindStrength * response;
            v.vertex.xz += float2(gust, gust * (.22 + v.color.b * .38));
            v.vertex.xz += contactDirection * contact * .16;
            v.vertex.y -= contact * .045;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed3 detail = tex2D(_MainTex, IN.uv_MainTex * float2(1.8, 2.7)).rgb;
            fixed variation = lerp(.84, 1.12, IN.color.g);
            o.Albedo = detail * _Color.rgb * variation;
            o.Smoothness = _Smoothness;
            o.Occlusion = lerp(.76, 1.0, IN.color.g);
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
