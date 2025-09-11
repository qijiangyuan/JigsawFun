Shader "UI/JigsawGrid"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Line Color", Color) = (0,0,0,1)
        _LineWidth ("Line Width", Range(0.001,0.05)) = 0.01
        _ToothSize ("Tooth Radius", Range(0.01,0.2)) = 0.05
        _GridCount ("Grid Count (n)", Float) = 2
        _AdaptiveStrength ("Adaptive Color Strength", Range(0,1)) = 0.8
        _ContrastThreshold ("Contrast Threshold", Range(0,1)) = 0.3
        
        // UI遮罩支持
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        // UI遮罩支持
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            // 将材质属性放入 UnityPerMaterial CBUFFER，确保在URP+SRP Batcher下按材质正确更新（特别是Android）
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _LineWidth;
                float _ToothSize;
                float _GridCount;
                float _AdaptiveStrength;
                float _ContrastThreshold;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex);
                o.uv = v.uv; // uv in [0,1]
                return o;
            }

            // ���뺯����ֱ��
            float lineDist(float2 uv, float axis, bool vertical)
            {
                return vertical ? abs(uv.x - axis) : abs(uv.y - axis);
            }

            // 计算到点的距离
            float circleDist(float2 uv, float2 center, float r)
            {
                return length(uv - center) - r;
            }
            
            // 移除随机函数，改为使用网格索引的奇偶性来确定方向

            half4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float lineDistance = 1.0;
                float circleDistance = 1.0;

                // 计算网格线位置
                for (int k=1; k<(int)_GridCount; k++)
                {
                    float t = k / (float)_GridCount;

                    // --- 垂直线 ---
                    float verticalLineDist = lineDist(uv, t, true);
                    
                    // 检查是否在圆形区域内，如果在圆形内则不绘制直线
                    // 圆心位置：对于N格网格，圆心在 (2i-1)/(2N) 处，i=1,2,...,N-1
                    // 圆的半径：直线长度/grid count/6
                    float circleRadius = (1.0 / _GridCount) / 6.0;
                    
                    bool inAnyCircle = false;
                    
                    // 遍历所有应该存在的圆心
                    for (int i = 1; i <= (int)_GridCount; i++) {
                        float2 circleCenter = float2(t, (2.0*i-1.0)/(2.0*_GridCount));
                        float circleDistValue = circleDist(uv, circleCenter, circleRadius);
                        
                        // 如果在圆形内，标记为在圆形区域
                        if (circleDistValue <= 0.0) {
                            inAnyCircle = true;
                        }
                        
                        // 绘制圆形边界（相邻部分方向相反）
                        // 对于垂直线上的圆形，根据x坐标分割
                        // 使用网格索引的奇偶性确定方向，确保相邻网格方向相反
                        bool keepLeft = ((k + i) % 2 == 0);
                        float tolerance = _LineWidth * 0.5; // 对称轴容差
                        
                        if (keepLeft) {
                            // 保留左半部分（x < center.x + tolerance）
                            if (uv.x <= circleCenter.x + tolerance) {
                                circleDistance = min(circleDistance, abs(circleDistValue));
                            }
                        } else {
                            // 保留右半部分（x > center.x - tolerance）
                            if (uv.x >= circleCenter.x - tolerance) {
                                circleDistance = min(circleDistance, abs(circleDistValue));
                            }
                        }
                    }
                    
                    // 如果不在任何圆形内，则考虑垂直线
                    if (!inAnyCircle) {
                        lineDistance = min(lineDistance, verticalLineDist);
                    }

                    // --- 水平线 ---
                    float horizontalLineDist = lineDist(uv, t, false);
                    
                    inAnyCircle = false;
                    
                    // 遍历所有应该存在的圆心
                    for (int j = 1; j <= (int)_GridCount; j++) {
                        float2 circleCenter = float2((2.0*j-1.0)/(2.0*_GridCount), t);
                        float circleDistValue = circleDist(uv, circleCenter, circleRadius);
                        
                        // 如果在圆形内，标记为在圆形区域
                        if (circleDistValue <= 0.0) {
                            inAnyCircle = true;
                        }
                        
                        // 绘制圆形边界（相邻部分方向相反）
                        // 对于水平线上的圆形，根据y坐标分割
                        // 使用网格索引的奇偶性确定方向，确保相邻网格方向相反
                        bool keepTop = ((k + j) % 2 == 0);
                        float tolerance = _LineWidth * 0.5; // 对称轴容差
                        
                        if (keepTop) {
                            // 保留上半部分（y > center.y - tolerance）
                            if (uv.y >= circleCenter.y - tolerance) {
                                circleDistance = min(circleDistance, abs(circleDistValue));
                            }
                        } else {
                            // 保留下半部分（y < center.y + tolerance）
                            if (uv.y <= circleCenter.y + tolerance) {
                                circleDistance = min(circleDistance, abs(circleDistValue));
                            }
                        }
                    }
                    
                    // 如果不在任何圆形内，则考虑水平线
                    if (!inAnyCircle) {
                        lineDistance = min(lineDistance, horizontalLineDist);
                    }
                }

                // 合并直线和圆形的距离
                float finalDistance = min(lineDistance, circleDistance);
                float alpha = smoothstep(_LineWidth, 0.0, finalDistance);
                
                
                // 采样背景图片并计算亮度
                float4 bgColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float bgLuminance = dot(bgColor.rgb, float3(0.299, 0.587, 0.114));
                
                // 计算当前网格颜色与背景的对比度
                float gridLuminance = dot(_Color.rgb, float3(0.299, 0.587, 0.114));
                float contrast = abs(bgLuminance - gridLuminance);
                
                // 根据背景亮度和对比度自动调整网格颜色
                float3 adaptiveColor = _Color.rgb;
                
                // 如果对比度不足，则进行自适应调整
                if (contrast < _ContrastThreshold) {
                    if (bgLuminance > 0.5) {
                        // 背景较亮时，使用深色网格
                        adaptiveColor = lerp(_Color.rgb, float3(0.1, 0.1, 0.1), _AdaptiveStrength);
                    } else {
                        // 背景较暗时，使用亮色网格
                        adaptiveColor = lerp(_Color.rgb, float3(0.9, 0.9, 0.9), _AdaptiveStrength);
                    }
                }
                
                return half4(adaptiveColor, alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}
