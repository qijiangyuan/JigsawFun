Shader "UI/JigsawOverlayURP"
{
    Properties
    {
        // ��ɫ����͸���ȣ����� A=0.5~0.9
        _LineColor ("Line Color", Color) = (0,0,0,0.75)

        // ������ n��nxn��
        _Grid      ("Grid Size (n)", Range(2,64)) = 6

        // �߿���Ը��ӳߴ磻Խ��Խ�֣�
        _LineWidth ("Line Width (rel.)", Range(0.2, 4.0)) = 1.0

        // �ݵ��������Ը��ӳߴ磻0~0.5��
        _ToothAmp  ("Tooth Amplitude (rel.)", Range(0.0, 0.5)) = 0.18

        // ���ڱ��ϵ���Կ�ȣ�0~1��Խ��Խ��
        _ToothWidth("Tooth Width (0..1)", Range(0.2, 1.0)) = 0.65

        // ������ӣ��ı�͹/���ֲ���
        _Seed      ("Random Seed", Float) = 123.0

        // ��ѡ������߿�1=�����0=ֻ���ڲ���϶��
        _OuterFrame("Outer Frame (0/1)", Float) = 1.0

        // ���� UGUI �����ԣ���ʵ�ʲ�����
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

            // ---- �궵�ף���Щ�������ṩ UNITY_PI��----
            #ifndef UNITY_PI
                #define UNITY_PI 3.14159265359
            #endif
            #ifndef UNITY_TWO_PI
                #define UNITY_TWO_PI 6.28318530718
            #endif

            // ---------- ���� ----------
            CBUFFER_START(UnityPerMaterial)
                float4 _LineColor;
                float  _Grid;
                float  _LineWidth;
                float  _ToothAmp;
                float  _ToothWidth;
                float  _Seed;
                float  _OuterFrame;
                float4 _Color;      // UGUI tint��δʹ�ã�
            CBUFFER_END

            sampler2D _MainTex;    // ���� UGUI��δʹ�ã�
            float4 _MainTex_ST;

            struct appdata
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color    : COLOR;     // ���� UGUI tint��δʹ�ã�
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

            // ---------- ���� ----------
            // �򵥹�ϣ -> 0..1�����ھ���ÿ�αߵ�͹/����
            float hash31(float x, float y, float z)
            {
                float3 p = float3(x, y, z);
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            // ���ΰ��磨���ο�ȿ��ƣ�t:0..1��
            float bell(float t, float width)
            {
                float a = 0.5 - width * 0.5;
                float b = 0.5 + width * 0.5;
                float x = saturate((t - a) / max(1e-4, (b - a)));
                return sin(UNITY_PI * x) * step(a, t) * step(t, b);
            }

            // ����ݣ�d=���ߵľ��루UV �ߣ���w=���UV �ߣ�
            float aa(float d, float w)
            {
                float fw = fwidth(d) + 1e-5;
                return 1.0 - smoothstep(w - fw, w + fw, d);
            }

            // ����ˮƽ�ߣ���ǰ cell �ֲ����� lc:0..1��
            float horiz_edge(float2 lc, int ix, int iy, int N, float seed, float ampRel, float widthRel, float outerFrame)
            {
                float t = lc.x; // �ر� 0..1
                float env = bell(t, widthRel);
                float amp = ampRel;

                // ���ߣ�edge_row=iy�����ױߣ�edge_row=iy+1��
                float signTop    = (hash31(ix,     iy,     seed) > 0.5) ? 1.0 : -1.0;
                float signBottom = (hash31(ix,     iy + 1, seed) > 0.5) ? 1.0 : -1.0;

                // ��߿����رգ���/�ױߵ� amp ��Ϊ 0
                if (outerFrame < 0.5)
                {
                    if (iy == 0)       signTop    = 0.0;
                    if (iy == N - 1)   signBottom = 0.0;
                }

                float yEdge0 = 0.0 + signTop    * amp * env;   // ��������
                float yEdge1 = 1.0 - signBottom * amp * env;   // �ױ����ߣ�����

                float d0 = abs(lc.y - yEdge0);
                float d1 = abs(lc.y - yEdge1);
                return min(d0, d1);
            }

            // ������ֱ��
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

                float xEdge0 = 0.0 + signLeft  * amp * env;   // �������
                float xEdge1 = 1.0 - signRight * amp * env;   // �ұ�����

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

                // ��ǰ cell �ֲ����� 0..1
                float2 lc = frac(uvN);

                // �������ز��������㵽 UV �߶��ʹ���� N ����Ӧ
                float relWidth = _LineWidth / N;      // �߿����Լ=relWidth��
                float relAmp   = _ToothAmp / N;       // �ݷ�
                float widthRel = saturate(_ToothWidth);

                // ���뵽ˮƽ/��ֱ��
                float dh = horiz_edge(lc, ix, iy, N, _Seed, relAmp, widthRel, _OuterFrame);
                float dv = vert_edge (lc, ix, iy, N, _Seed, relAmp, widthRel, _OuterFrame);

                float d = min(dh, dv);

                // �����������
                float mask = aa(d, relWidth);

                // ֻ���������������ȫ͸����
                float4 col = float4(_LineColor.rgb, _LineColor.a * mask);
                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
