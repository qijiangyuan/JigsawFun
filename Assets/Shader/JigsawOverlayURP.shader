Shader "UI/JigsawOverlayURP"
{
    Properties
    {
        // 颜色包含透明度：建议 A=0.5~0.9
        _LineColor ("Line Color", Color) = (0,0,0,0.75)

        // 网格数 n（nxn）
        _Grid      ("Grid Size (n)", Range(2,64)) = 6

        // 线宽（相对格子尺寸；越大越粗）
        _LineWidth ("Line Width (rel.)", Range(0.2, 4.0)) = 1.0

        // 齿的振幅（相对格子尺寸；0~0.5）
        _ToothAmp  ("Tooth Amplitude (rel.)", Range(0.0, 0.5)) = 0.18

        // 齿在边上的相对宽度（0~1；越大越宽）
        _ToothWidth("Tooth Width (0..1)", Range(0.2, 1.0)) = 0.65

        // 随机种子（改变凸/凹分布）
        _Seed      ("Random Seed", Float) = 123.0

        // 勾选保留外边框（1=有外框，0=只画内部缝隙）
        _OuterFrame("Outer Frame (0/1)", Float) = 1.0

        // 兼容 UGUI 的属性（不实际采样）
        [HideInInspector]_MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector]_Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="False"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "JigsawOverlayUIPass"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ---- 宏兜底（有些环境不提供 UNITY_PI）----
            #ifndef UNITY_PI
                #define UNITY_PI 3.14159265359
            #endif
            #ifndef UNITY_TWO_PI
                #define UNITY_TWO_PI 6.28318530718
            #endif

            // ---------- 属性 ----------
            CBUFFER_START(UnityPerMaterial)
                float4 _LineColor;
                float  _Grid;
                float  _LineWidth;
                float  _ToothAmp;
                float  _ToothWidth;
                float  _Seed;
                float  _OuterFrame;
                float4 _Color;      // UGUI tint（未使用）
            CBUFFER_END

            sampler2D _MainTex;    // 兼容 UGUI（未使用）
            float4 _MainTex_ST;

            struct appdata
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color    : COLOR;     // 兼容 UGUI tint（未使用）
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv  = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            // ---------- 工具 ----------
            // 简单哈希 -> 0..1（用于决定每段边的凸/凹）
            float hash31(float x, float y, float z)
            {
                float3 p = float3(x, y, z);
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            // 钟形包络（齿形宽度控制；t:0..1）
            float bell(float t, float width)
            {
                float a = 0.5 - width * 0.5;
                float b = 0.5 + width * 0.5;
                float x = saturate((t - a) / max(1e-4, (b - a)));
                return sin(UNITY_PI * x) * step(a, t) * step(t, b);
            }

            // 抗锯齿；d=到边的距离（UV 尺），w=半宽（UV 尺）
            float aa(float d, float w)
            {
                float fw = fwidth(d) + 1e-5;
                return 1.0 - smoothstep(w - fw, w + fw, d);
            }

            // 计算水平边（当前 cell 局部坐标 lc:0..1）
            float horiz_edge(float2 lc, int ix, int iy, int N, float seed, float ampRel, float widthRel, float outerFrame)
            {
                float t = lc.x; // 沿边 0..1
                float env = bell(t, widthRel);
                float amp = ampRel;

                // 顶边（edge_row=iy），底边（edge_row=iy+1）
                float signTop    = (hash31(ix,     iy,     seed) > 0.5) ? 1.0 : -1.0;
                float signBottom = (hash31(ix,     iy + 1, seed) > 0.5) ? 1.0 : -1.0;

                // 外边框若关闭，则顶/底边的 amp 设为 0
                if (outerFrame < 0.5)
                {
                    if (iy == 0)       signTop    = 0.0;
                    if (iy == N - 1)   signBottom = 0.0;
                }

                float yEdge0 = 0.0 + signTop    * amp * env;   // 顶边曲线
                float yEdge1 = 1.0 - signBottom * amp * env;   // 底边曲线（镜像）

                float d0 = abs(lc.y - yEdge0);
                float d1 = abs(lc.y - yEdge1);
                return min(d0, d1);
            }

            // 计算竖直边
            float vert_edge(float2 lc, int ix, int iy, int N, float seed, float ampRel, float widthRel, float outerFrame)
            {
                float t = lc.y;
                float env = bell(t, widthRel);
                float amp = ampRel;

                float signLeft  = (hash31(ix,     iy,     seed + 7.0) > 0.5) ? 1.0 : -1.0;
                float signRight = (hash31(ix + 1, iy,     seed + 7.0) > 0.5) ? 1.0 : -1.0;

                if (outerFrame < 0.5)
                {
                    if (ix == 0)       signLeft  = 0.0;
                    if (ix == N - 1)   signRight = 0.0;
                }

                float xEdge0 = 0.0 + signLeft  * amp * env;   // 左边曲线
                float xEdge1 = 1.0 - signRight * amp * env;   // 右边曲线

                float d0 = abs(lc.x - xEdge0);
                float d1 = abs(lc.x - xEdge1);
                return min(d0, d1);
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                int   N   = (int)max(2.0, floor(_Grid + 0.5));
                float2 uvN = uv * N;

                int ix = (int)floor(uvN.x);
                int iy = (int)floor(uvN.y);

                // 当前 cell 局部坐标 0..1
                float2 lc = frac(uvN);

                // 将“像素参数”折算到 UV 尺度里，使其随 N 自适应
                float relWidth = _LineWidth / N;      // 线宽（半宽约=relWidth）
                float relAmp   = _ToothAmp / N;       // 齿幅
                float widthRel = saturate(_ToothWidth);

                // 距离到水平/竖直边
                float dh = horiz_edge(lc, ix, iy, N, _Seed, relAmp, widthRel, _OuterFrame);
                float dv = vert_edge (lc, ix, iy, N, _Seed, relAmp, widthRel, _OuterFrame);

                float d = min(dh, dv);

                // 抗锯齿线遮罩
                float mask = aa(d, relWidth);

                // 只输出线条（背景完全透明）
                float4 col = float4(_LineColor.rgb, _LineColor.a * mask);
                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
