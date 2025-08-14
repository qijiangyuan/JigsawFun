Shader "Custom/SpriteLit_DynamicNormal"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _EdgeWidth ("Edge Width", Range(0.0, 5.0)) = 1.0
        _EdgeStrength ("Edge Strength", Range(0.0, 2.0)) = 1.0
    }

    SubShader
    {
        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            // 纹理声明（URP规范）
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _EdgeWidth;
                float _EdgeStrength;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            // Sobel 核来计算透明边缘的法线
            float3 ComputeDynamicNormal(float2 uv)
            {
                float2 texelSize = 1.0 / _ScreenParams.xy * _EdgeWidth;

                // 取周围像素的alpha值
                float a00 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texelSize.x, -texelSize.y)).a;
                float a10 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 0, -texelSize.y)).a;
                float a20 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( texelSize.x, -texelSize.y)).a;

                float a01 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texelSize.x, 0)).a;
                float a21 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( texelSize.x, 0)).a;

                float a02 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texelSize.x, texelSize.y)).a;
                float a12 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 0, texelSize.y)).a;
                float a22 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( texelSize.x, texelSize.y)).a;

                // Sobel 核
                float gx = (a20 + 2.0 * a21 + a22) - (a00 + 2.0 * a01 + a02);
                float gy = (a02 + 2.0 * a12 + a22) - (a00 + 2.0 * a10 + a20);

                float3 normal = normalize(float3(gx, gy, _EdgeStrength));
                return normal * 0.5 + 0.5; // 转为0~1存储
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                if (baseCol.a < 0.01) discard;

                // 根据 alpha 边缘生成动态法线
                float3 normalTS = ComputeDynamicNormal(IN.uv) * 2.0 - 1.0;

                // 转世界空间
                float3 normalWS = normalize(TransformTangentToWorld(normalTS, float3x3(1,0,0, 0,1,0, 0,0,1)));

                // 主光
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 litColor = mainLight.color * NdotL;

                return half4(baseCol.rgb * litColor, baseCol.a);
            }
            ENDHLSL
        }
    }
}
