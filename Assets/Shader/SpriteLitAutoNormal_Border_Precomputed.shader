Shader "Custom/SpriteLitAutoNormal_Border_Precomputed"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _EdgeStrength("Normal Strength", Range(0,5)) = 1
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.001
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            sampler2D _NormalMap;
            float _EdgeStrength;
            float _Cutoff;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 采样基础颜色
                half4 baseCol = tex2D(_MainTex, IN.uv) * IN.color;
                if (baseCol.a < _Cutoff) discard;

                // 从预生成的法线贴图采样
                half4 normalSample = tex2D(_NormalMap, IN.uv);
                
                // 解码法线 (从0-1范围转换回-1到1)
                float3 normal = float3(
                    normalSample.r * 2.0 - 1.0,
                    normalSample.g * 2.0 - 1.0,
                    normalSample.b * 2.0 - 1.0
                );
                
                // 应用边缘强度
                normal.xy *= _EdgeStrength;
                normal = normalize(normal);
                
                // 从Alpha通道获取高度信息
                float height = normalSample.a;

                // 光照计算 - 与原着色器保持一致
                Light mainLight = GetMainLight();
                half NdotL = dot(normal, mainLight.direction);
                
                // 增强对比度的光照计算
                half lightIntensity = saturate(NdotL * 1.5 + 0.1);
                half shadowIntensity = saturate(-NdotL * 0.8 + 0.3);
                
                // 混合光照和阴影
                half3 lightColor = baseCol.rgb * mainLight.color * lightIntensity;
                half3 shadowColor = baseCol.rgb * 0.4;
                half3 litColor = lerp(shadowColor, lightColor, saturate(NdotL + 0.5));

                return half4(litColor, baseCol.a);
            }
            ENDHLSL
        }
    }
}