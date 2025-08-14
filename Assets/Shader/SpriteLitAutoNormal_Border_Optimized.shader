Shader "Custom/SpriteLitAutoNormal_Border_Optimized"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _EdgeStrength("Normal Strength", Range(0,5)) = 1
        _BorderSize("Border Size (Pixels)", Range(1,100)) = 30
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
            float4 _MainTex_TexelSize;
            float _EdgeStrength;
            float _BorderSize;
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
                half4 baseCol = tex2D(_MainTex, IN.uv) * IN.color;
                if (baseCol.a < _Cutoff) discard;

                // 混合边缘检测算法：同时检测UV边界和不透明像素边缘
                float edgePixels = _BorderSize; // 可配置的边缘范围
                float2 texelSize = _MainTex_TexelSize.xy;
                
                // 1. 计算到UV边界的距离（像素单位）
                float2 textureSize = 1.0 / texelSize; // 纹理尺寸
                float2 pixelPos = IN.uv * textureSize; // 当前像素位置
                float distToUVEdge = min(min(pixelPos.x, pixelPos.y), min(textureSize.x - pixelPos.x, textureSize.y - pixelPos.y));
                
                // 2. 计算到最近透明像素的距离（以像素为单位）
                float minDistanceToTransparent = edgePixels;
                
                // 在_BorderSize像素范围内搜索透明像素
                int searchRadius = (int)_BorderSize;
                for(int x = -searchRadius; x <= searchRadius; x++) {
                    for(int y = -searchRadius; y <= searchRadius; y++) {
                        float2 offset = float2(x, y) * texelSize;
                        float2 sampleUV = IN.uv + offset;
                        
                        // 检查UV是否在有效范围内
                        if(sampleUV.x >= 0 && sampleUV.x <= 1 && sampleUV.y >= 0 && sampleUV.y <= 1) {
                            float sampleAlpha = tex2D(_MainTex, sampleUV).a;
                            
                            // 如果找到透明像素，计算距离
                            if(sampleAlpha < 0.1) {
                                float distance = length(float2(x, y));
                                minDistanceToTransparent = min(minDistanceToTransparent, distance);
                            }
                        }
                    }
                }
                
                // 3. 取两种边缘距离的最小值
                float finalEdgeDistance = min(distToUVEdge, minDistanceToTransparent);
                
                // 计算高度：距离边缘越近，高度越低
                float height = saturate(finalEdgeDistance / edgePixels);
                
                // 使用平滑曲线创建自然的立体过渡
                height = smoothstep(0.0, 1.0, height);
                
                // 只在_BorderSize像素边缘范围内应用效果
                if(finalEdgeDistance >= edgePixels) {
                    height = 1.0; // 内部区域保持平坦
                }
                
                height *= baseCol.a; // 考虑透明度影响

                // 使用相同的_BorderSize像素边缘检测算法计算梯度
                
                // 计算相邻像素的高度值用于梯度计算
                float2 uvL = IN.uv + float2(-texelSize.x, 0);
                float2 uvR = IN.uv + float2(texelSize.x, 0);
                float2 uvD = IN.uv + float2(0, -texelSize.y);
                float2 uvU = IN.uv + float2(0, texelSize.y);
                
                // 左侧采样点高度计算
                float2 pixelPosL = uvL * textureSize;
                float distToUVEdgeL = min(min(pixelPosL.x, pixelPosL.y), min(textureSize.x - pixelPosL.x, textureSize.y - pixelPosL.y));
                float minDistL = edgePixels;
                for(int x = -searchRadius; x <= searchRadius; x++) {
                    for(int y = -searchRadius; y <= searchRadius; y++) {
                        float2 offset = float2(x, y) * texelSize;
                        float2 sampleUV = uvL + offset;
                        if(sampleUV.x >= 0 && sampleUV.x <= 1 && sampleUV.y >= 0 && sampleUV.y <= 1) {
                            float sampleAlpha = tex2D(_MainTex, sampleUV).a;
                            if(sampleAlpha < 0.1) {
                                float distance = length(float2(x, y));
                                minDistL = min(minDistL, distance);
                            }
                        }
                    }
                }
                float finalEdgeDistL = min(distToUVEdgeL, minDistL);
                float hL = saturate(finalEdgeDistL / edgePixels);
                hL = smoothstep(0.0, 1.0, hL);
                if(finalEdgeDistL >= edgePixels) hL = 1.0;
                hL *= tex2D(_MainTex, uvL).a;
                
                // 右侧采样点高度计算
                float2 pixelPosR = uvR * textureSize;
                float distToUVEdgeR = min(min(pixelPosR.x, pixelPosR.y), min(textureSize.x - pixelPosR.x, textureSize.y - pixelPosR.y));
                float minDistR = edgePixels;
                for(int x = -searchRadius; x <= searchRadius; x++) {
                    for(int y = -searchRadius; y <= searchRadius; y++) {
                        float2 offset = float2(x, y) * texelSize;
                        float2 sampleUV = uvR + offset;
                        if(sampleUV.x >= 0 && sampleUV.x <= 1 && sampleUV.y >= 0 && sampleUV.y <= 1) {
                            float sampleAlpha = tex2D(_MainTex, sampleUV).a;
                            if(sampleAlpha < 0.1) {
                                float distance = length(float2(x, y));
                                minDistR = min(minDistR, distance);
                            }
                        }
                    }
                }
                float finalEdgeDistR = min(distToUVEdgeR, minDistR);
                float hR = saturate(finalEdgeDistR / edgePixels);
                hR = smoothstep(0.0, 1.0, hR);
                if(finalEdgeDistR >= edgePixels) hR = 1.0;
                hR *= tex2D(_MainTex, uvR).a;
                
                // 下方采样点高度计算
                float2 pixelPosD = uvD * textureSize;
                float distToUVEdgeD = min(min(pixelPosD.x, pixelPosD.y), min(textureSize.x - pixelPosD.x, textureSize.y - pixelPosD.y));
                float minDistD = edgePixels;
                for(int x = -searchRadius; x <= searchRadius; x++) {
                    for(int y = -searchRadius; y <= searchRadius; y++) {
                        float2 offset = float2(x, y) * texelSize;
                        float2 sampleUV = uvD + offset;
                        if(sampleUV.x >= 0 && sampleUV.x <= 1 && sampleUV.y >= 0 && sampleUV.y <= 1) {
                            float sampleAlpha = tex2D(_MainTex, sampleUV).a;
                            if(sampleAlpha < 0.1) {
                                float distance = length(float2(x, y));
                                minDistD = min(minDistD, distance);
                            }
                        }
                    }
                }
                float finalEdgeDistD = min(distToUVEdgeD, minDistD);
                float hD = saturate(finalEdgeDistD / edgePixels);
                hD = smoothstep(0.0, 1.0, hD);
                if(finalEdgeDistD >= edgePixels) hD = 1.0;
                hD *= tex2D(_MainTex, uvD).a;
                
                // 上方采样点高度计算
                float2 pixelPosU = uvU * textureSize;
                float distToUVEdgeU = min(min(pixelPosU.x, pixelPosU.y), min(textureSize.x - pixelPosU.x, textureSize.y - pixelPosU.y));
                float minDistU = edgePixels;
                for(int x = -searchRadius; x <= searchRadius; x++) {
                    for(int y = -searchRadius; y <= searchRadius; y++) {
                        float2 offset = float2(x, y) * texelSize;
                        float2 sampleUV = uvU + offset;
                        if(sampleUV.x >= 0 && sampleUV.x <= 1 && sampleUV.y >= 0 && sampleUV.y <= 1) {
                            float sampleAlpha = tex2D(_MainTex, sampleUV).a;
                            if(sampleAlpha < 0.1) {
                                float distance = length(float2(x, y));
                                minDistU = min(minDistU, distance);
                            }
                        }
                    }
                }
                float finalEdgeDistU = min(distToUVEdgeU, minDistU);
                float hU = saturate(finalEdgeDistU / edgePixels);
                hU = smoothstep(0.0, 1.0, hU);
                if(finalEdgeDistU >= edgePixels) hU = 1.0;
                hU *= tex2D(_MainTex, uvU).a;

                // Sobel 梯度 - 增强边缘效果
                float dx = (hR - hL) * _EdgeStrength * 2.0; // 增强水平梯度
                float dy = (hU - hD) * _EdgeStrength * 2.0; // 增强垂直梯度
                
                // 增加非线性增强
                dx = sign(dx) * pow(abs(dx), 0.8); // 使用幂函数增强梯度对比
                dy = sign(dy) * pow(abs(dy), 0.8);

                // 法线 - 调整Z分量使边缘更陡峭
                float3 normal = normalize(float3(-dx, -dy, 0.5)); // 减小Z分量使表面更陡峭

                // 光照源 - 增强立体效果
                Light mainLight = GetMainLight();
                half NdotL = dot(normal, mainLight.direction);
                
                // 增强对比度的光照计算
                half lightIntensity = saturate(NdotL * 1.5 + 0.1); // 增强光照强度
                half shadowIntensity = saturate(-NdotL * 0.8 + 0.3); // 添加阴影效果
                
                // 混合光照和阴影
                half3 lightColor = baseCol.rgb * mainLight.color * lightIntensity;
                half3 shadowColor = baseCol.rgb * 0.4; // 阴影颜色更暗
                half3 litColor = lerp(shadowColor, lightColor, saturate(NdotL + 0.5));

                return half4(litColor, baseCol.a);
            }
            ENDHLSL
        }
    }
}
