// 애니메풍 하늘: 짙은 천정 그라데이션 + 계단식 음영의 큰 적운 + 태양 글로우
// 태양 방향은 씬의 Directional Light(_WorldSpaceLightPos0)를 따름
Shader "Skybox/AoTSky"
{
    Properties
    {
        [Header(Sky Gradient)]
        _ZenithColor ("Zenith Color", Color) = (0.07, 0.25, 0.62, 1)
        _MidColor ("Mid Color", Color) = (0.25, 0.52, 0.85, 1)
        _HorizonColor ("Horizon Color", Color) = (0.78, 0.86, 0.92, 1)
        _GroundColor ("Below Horizon Color", Color) = (0.55, 0.62, 0.62, 1)
        _GradientPower ("Gradient Power", Range(0.1, 2)) = 0.55

        [Header(Sun)]
        _SunColor ("Sun Color", Color) = (1, 0.86, 0.6, 1)
        _SunGlow ("Sun Glow", Range(0, 2)) = 0.7
        _SunDiscSize ("Sun Disc Size", Range(0.9, 1)) = 0.9985

        [Header(Clouds)]
        _CloudLitColor ("Cloud Lit Color", Color) = (1, 0.97, 0.9, 1)
        _CloudShadowColor ("Cloud Shadow Color", Color) = (0.5, 0.58, 0.72, 1)
        _CloudScale ("Cloud Scale", Range(0.2, 4)) = 1.3
        _CloudCoverage ("Cloud Coverage", Range(0, 1)) = 0.5
        _CloudSoftness ("Cloud Edge Softness", Range(0.01, 0.5)) = 0.08
        _CloudSpeed ("Cloud Speed", Range(0, 0.1)) = 0.004
        _CloudShadeSteps ("Cloud Shade Hardness", Range(0.01, 0.5)) = 0.08
        _CloudBrightness ("Cloud Brightness", Range(0.5, 3)) = 1.7
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _ZenithColor, _MidColor, _HorizonColor, _GroundColor;
            half _GradientPower;
            fixed4 _SunColor;
            half _SunGlow, _SunDiscSize;
            fixed4 _CloudLitColor, _CloudShadowColor;
            half _CloudScale, _CloudCoverage, _CloudSoftness, _CloudSpeed, _CloudShadeSteps, _CloudBrightness;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0;
                float amp = 0.5;
                const float2x2 rot = float2x2(0.8, -0.6, 0.6, 0.8);
                for (int i = 0; i < 5; i++)
                {
                    v += amp * ValueNoise(p);
                    p = mul(rot, p) * 2.03;
                    amp *= 0.5;
                }
                return v;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float3 sunDir = normalize(_WorldSpaceLightPos0.xyz);
                float h = dir.y;

                // 하늘 그라데이션: 지평선 -> 중간 -> 천정
                float t = pow(saturate(h), _GradientPower);
                float3 sky = lerp(_HorizonColor.rgb, _MidColor.rgb, saturate(t * 2.0));
                sky = lerp(sky, _ZenithColor.rgb, saturate(t * 2.0 - 1.0));
                sky = lerp(sky, _GroundColor.rgb, smoothstep(0.0, -0.25, h));

                // 태양: 넓은 글로우 + 선명한 원반
                float sunDot = saturate(dot(dir, sunDir));
                float glow = pow(sunDot, 6.0) * 0.5 + pow(sunDot, 48.0) * 0.5;
                sky += _SunColor.rgb * glow * _SunGlow;
                float disc = smoothstep(_SunDiscSize, _SunDiscSize + 0.0008, sunDot);
                sky = lerp(sky, _SunColor.rgb * 2.0, disc);

                // 구름: 하늘 평면에 투영한 fbm 노이즈를 계단식으로 음영 처리
                float cloudMask = smoothstep(0.02, 0.3, h);
                float2 uv = dir.xz / (h + 0.25) * _CloudScale + _Time.y * _CloudSpeed;
                float density = Fbm(uv);
                float cloud = smoothstep(1.0 - _CloudCoverage, 1.0 - _CloudCoverage + _CloudSoftness, density);

                // 태양 쪽으로 살짝 이동한 지점과 밀도 비교해 구름 안쪽의 명암 결정
                float densityTowardSun = Fbm(uv + sunDir.xz * 0.12);
                float lit = saturate(0.5 + (density - densityTowardSun) * 6.0);
                lit = smoothstep(0.5 - _CloudShadeSteps, 0.5 + _CloudShadeSteps, lit);

                // 톤매핑으로 눌리는 만큼 밝기를 보정해 구름을 하얗게 유지
                float3 cloudColor = lerp(_CloudShadowColor.rgb, _CloudLitColor.rgb, lit) * _CloudBrightness;
                // 태양 가장자리 실버라이닝
                cloudColor += _SunColor.rgb * pow(sunDot, 10.0) * (1.0 - cloud * 0.4) * 0.35;

                float3 col = lerp(sky, cloudColor, cloud * cloudMask);
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
