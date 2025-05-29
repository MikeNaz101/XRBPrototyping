Shader "Custom/HolographicHexForceField"
{
    Properties
    {
        _MainTex ("Hexagon Texture (RGBA)", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0,1,1,1) // Cyan-ish default
        _FresnelColor ("Fresnel Color", Color) = (1,1,1,1)
        _FresnelPower ("Fresnel Power", Range(0.1, 20)) = 5.0
        _ScrollSpeedX ("Scroll Speed X", Float) = 0.1
        _ScrollSpeedY ("Scroll Speed Y", Float) = 0.1
        _TileFactor ("Tile Factor", Float) = 1.0
        _Opacity ("Overall Opacity", Range(0,1)) = 0.5
        _EdgeFade ("Edge Fade Power", Range(0.1, 5)) = 1.0 // Controls how quickly edges become more transparent
        _HitEffectColor ("Hit Effect Color", Color) = (1,0,0,1) // Red for hit
        _HitEffectIntensity ("Hit Effect Intensity", Range(0,1)) = 0.0
        _HitPosition ("Hit Position (World Space)", Vector) = (0,0,0,0)
        _HitRadius ("Hit Radius", Float) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha // Alpha blending
            ZWrite Off // Don't write to depth buffer for transparency
            Cull Back // Cull back faces (standard for force fields)

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST; // For tiling and offset
            fixed4 _TintColor;
            fixed4 _FresnelColor;
            float _FresnelPower;
            float _ScrollSpeedX;
            float _ScrollSpeedY;
            float _TileFactor;
            float _Opacity;
            float _EdgeFade;
            fixed4 _HitEffectColor;
            float _HitEffectIntensity;
            float3 _HitPosition;
            float _HitRadius;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));

                // Scrolling UVs
                float2 scrolledUV = v.uv;
                scrolledUV.x += _Time.y * _ScrollSpeedX;
                scrolledUV.y += _Time.y * _ScrollSpeedY;
                o.uv = TRANSFORM_TEX(scrolledUV, _MainTex) * _TileFactor;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample hexagon texture
                fixed4 texColor = tex2D(_MainTex, i.uv);

                // Fresnel Effect
                float fresnel = 1.0 - saturate(dot(i.worldNormal, i.viewDir));
                fresnel = pow(fresnel, _FresnelPower);
                fixed3 fresnelEmission = _FresnelColor.rgb * fresnel;

                // Edge Fade (based on fresnel, but could also be proximity to geometry edge if needed)
                float edgeAlpha = pow(saturate(dot(i.worldNormal, i.viewDir)), _EdgeFade);

                // Hit Effect
                float distToHit = distance(i.worldPos, _HitPosition);
                float hitAmount = 1.0 - saturate(distToHit / _HitRadius); // Inverse relationship: closer = more effect
                hitAmount *= _HitEffectIntensity; // Modulate by overall intensity

                fixed3 finalColor = _TintColor.rgb * texColor.rgb; // Base color from texture and tint
                finalColor = lerp(finalColor, _HitEffectColor.rgb, hitAmount); // Mix in hit color
                finalColor += fresnelEmission; // Add fresnel glow

                // Combine alphas
                float finalAlpha = texColor.a * _TintColor.a * _Opacity * edgeAlpha;
                finalAlpha = lerp(finalAlpha, _HitEffectColor.a * hitAmount, hitAmount); // Hit effect can also affect alpha

                return fixed4(finalColor, saturate(finalAlpha));
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit" // Fallback for older hardware
}