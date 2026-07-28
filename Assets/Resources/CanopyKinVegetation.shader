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

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma multi_compile_instancing
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        fixed4 _Color;
        half _Smoothness;
        half _WindStrength;

        struct Input
        {
            float2 uv_MainTex;
            fixed4 color : COLOR;
        };

        void vert(inout appdata_full v)
        {
            float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
            float gust = sin(_Time.y * 1.17 + world.x * .21 + world.z * .17)
                       + sin(_Time.y * .63 + world.x * .08 - world.z * .19) * .45;
            v.vertex.xz += float2(gust, gust * .31) * _WindStrength * v.color.r;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed3 baseColor = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color.rgb;
            o.Albedo = baseColor * lerp(.72, 1.08, IN.color.g);
            o.Normal = half3(0,0,1);
            o.Metallic = 0;
            o.Smoothness = _Smoothness;
            o.Occlusion = .86;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
