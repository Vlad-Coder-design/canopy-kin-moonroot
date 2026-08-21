Shader "CanopyKin/HeroVegetation"
{
    Properties
    {
        _Color("Tint", Color) = (1,1,1,1)
        _MainTex("Leaf Atlas", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0,1)) = .34
        _Smoothness("Smoothness", Range(0,1)) = .14
        _WindStrength("Wind Strength", Range(0,.25)) = .075
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest+8" }
        Cull Off
        LOD 320
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows addshadow alphatest:_Cutoff vertex:vert
        #pragma multi_compile_instancing

        sampler2D _MainTex;
        fixed4 _Color;
        half _Smoothness;
        half _WindStrength;
        float4 _CanopyKinPlayerPosition;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldPos;
            fixed4 color : COLOR;
        };

        void vert(inout appdata_full v)
        {
            float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
            float phase = v.color.b * 6.28318;
            float slow = sin(_Time.y * .72 + world.x * .13 - world.z * .09 + phase);
            float detail = sin(_Time.y * 1.91 + world.x * .37 + world.z * .21 + phase * 1.7) * .31;
            float response = v.color.r * lerp(1.0, .56, v.color.g);
            float gust = (slow + detail) * _WindStrength * response;
            v.vertex.x += gust;
            v.vertex.z += gust * (.22 + v.color.b * .38);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 atlas = tex2D(_MainTex, IN.uv_MainTex);
            fixed3 tint = lerp(fixed3(1,1,1), _Color.rgb, .34);
            fixed3 color = atlas.rgb * tint * 1.13;
            half cameraFade = smoothstep(.48h, 1.32h,
                distance(_WorldSpaceCameraPos, IN.worldPos));
            float3 cameraToPlayer = _CanopyKinPlayerPosition.xyz - _WorldSpaceCameraPos;
            float cameraToPlayerLengthSq = max(dot(cameraToPlayer, cameraToPlayer), .001);
            float corridorPosition = saturate(dot(IN.worldPos - _WorldSpaceCameraPos, cameraToPlayer) /
                                              cameraToPlayerLengthSq);
            float3 corridorPoint = _WorldSpaceCameraPos + cameraToPlayer * corridorPosition;
            half sightlineFade = smoothstep(.16h, .68h, distance(IN.worldPos, corridorPoint));
            half playerFade = smoothstep(.42h, 1.38h,
                distance(IN.worldPos.xz, _CanopyKinPlayerPosition.xz));
            half rim = pow(1.0h - saturate(abs(dot(normalize(IN.viewDir), half3(0,0,1)))), 2.0h);
            o.Albedo = color;
            o.Smoothness = _Smoothness;
            o.Occlusion = lerp(.72h, 1.0h, IN.color.g);
            o.Emission = color * rim * .055h;
            o.Alpha = atlas.a * min(cameraFade, min(sightlineFade, playerFade));
        }
        ENDCG
    }
    Fallback "Transparent/Cutout/Diffuse"
}
