Shader "Custom/SpriteLitAlphaNormal"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _EdgeStrength("Edge Normal Strength", Range(0,5)) = 1
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.001
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
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
            #pragma multi_compile_fog

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
                // ����ͼ����
                half4 baseCol = tex2D(_MainTex, IN.uv) * IN.color;

                // ͸���޳�
                if (baseCol.a < _Cutoff) discard;

                // �� alpha �����ݶ�
                float alphaL = tex2D(_MainTex, IN.uv + float2(-_MainTex_TexelSize.x, 0)).a;
                float alphaR = tex2D(_MainTex, IN.uv + float2(_MainTex_TexelSize.x, 0)).a;
                float alphaD = tex2D(_MainTex, IN.uv + float2(0, -_MainTex_TexelSize.y)).a;
                float alphaU = tex2D(_MainTex, IN.uv + float2(0, _MainTex_TexelSize.y)).a;

                float dx = (alphaR - alphaL) * _EdgeStrength;
                float dy = (alphaU - alphaD) * _EdgeStrength;

                // ת��Ϊ���ߣ�Z �ᳯ�⣩
                float3 normal = normalize(float3(-dx, -dy, 1.0));

                // ��ȡ����Դ
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normal, mainLight.direction));
                half3 litColor = baseCol.rgb * (mainLight.color * NdotL + 0.2); // + ������

                return half4(litColor * baseCol.a, baseCol.a); // Ԥ�� alpha
            }
            ENDHLSL
        }
    }
}
