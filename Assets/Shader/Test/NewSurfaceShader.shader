Shader "Custom/SpriteLit3DOutline"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _EdgeColor("Edge Color", Color) = (0.2, 0.2, 0.2, 1)
        _EdgeThickness("Edge Thickness", Range(0, 5)) = 1
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
            "UniversalMaterialType"="Lit"
            "ShaderModel"="4.5"
        }

        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode"="UniversalForward"}

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
            sampler2D _NormalMap;
            float4 _MainTex_ST;
            float4 _EdgeColor;
            float _EdgeThickness;
            float _Cutoff;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseCol = tex2D(_MainTex, IN.uv) * IN.color;

                // 透明剔除
                if (baseCol.a < _Cutoff) discard;

                // 法线采样（透明部分不影响）
                half4 normalSample = tex2D(_NormalMap, IN.uv);
                normalSample.a = baseCol.a;

                // 模拟立体边缘
                float2 edgeOffset = _EdgeThickness / _ScreenParams.xy;
                half4 edgeCol = tex2D(_MainTex, IN.uv + edgeOffset);
                if (edgeCol.a < _Cutoff) edgeCol = _EdgeColor;

                // 光照（URP 2D 风格简单 Lambert）
                Light mainLight = GetMainLight();
                half3 normal = UnpackNormal(normalSample);
                half NdotL = saturate(dot(normal, mainLight.direction));
                half3 litColor = baseCol.rgb * (mainLight.color * NdotL + 0.2); // 环境光 0.2

                // 合成立体边缘
                litColor = lerp(litColor, _EdgeColor.rgb, step(edgeCol.a, _Cutoff));

                // Premultiplied alpha 输出
                return half4(litColor * baseCol.a, baseCol.a);
            }
            ENDHLSL
        }
    }
}