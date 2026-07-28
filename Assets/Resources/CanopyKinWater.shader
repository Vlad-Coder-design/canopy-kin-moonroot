Shader "CanopyKin/Water"
{
    Properties
    {
        _Color("Water Color", Color) = (.05,.19,.17,.7)
        _Smoothness("Smoothness", Range(0,1)) = .92
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard alpha:fade vertex:vert
        fixed4 _Color;
        half _Smoothness;
        struct Input { float3 worldPos; };
        void vert(inout appdata_full v)
        {
            v.vertex.y += (sin(v.vertex.x * 2.4 + _Time.y * 1.2) + cos(v.vertex.z * 2.1 - _Time.y)) * .012;
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            half ripple = sin(IN.worldPos.x * 3.3 + _Time.y) * cos(IN.worldPos.z * 2.7 - _Time.y * .8);
            o.Albedo = _Color.rgb + ripple * .015;
            o.Normal = normalize(half3(ripple * .12, -ripple * .08, 1));
            o.Metallic = .04;
            o.Smoothness = _Smoothness;
            o.Alpha = _Color.a;
        }
        ENDCG
    }
    Fallback "Transparent/Diffuse"
}
