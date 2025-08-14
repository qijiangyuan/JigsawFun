Shader "Custom/SpriteLit_DynamicNormal_Final"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint Color", Color) = (1,1,1,1)
        _EdgeWidth("Edge Width", Range(0.001,0.1)) = 0.02
        _Roundness("Edge Roundness", Range(0.1,4.0)) = 1.0
        _BumpStrength("Bump Strength", Range(0,2)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _Color;
            float _EdgeWidth;
            float _Roundness;
            float _BumpStrength;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            float smoothEdge(float alpha)
            {
                // 圆角边缘函数，平滑过渡
                return pow(saturate(1.0 - alpha), _Roundness);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 采样当前像素
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                if(col.a < 0.01) discard;

                // Sobel 样式透明度梯度
                float2 texel = float2(_EdgeWidth, _EdgeWidth);

                float aL = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv - float2(texel.x, 0)).a;
                float aR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(texel.x, 0)).a;
                float aU = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(0, texel.y)).a;
                float aD = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv - float2(0, texel.y)).a;

                float dx = smoothEdge(aR) - smoothEdge(aL);
                float dy = smoothEdge(aU) - smoothEdge(aD);

                float3 normalTS = normalize(float3(dx * _BumpStrength, dy * _BumpStrength, 1.0));

                // 转到世界空间
                float3 normalWS = normalize(TransformTangentToWorld(normalTS, float3x3(1,0,0, 0,1,0, 0,0,1)));

                // 采集主光源
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 diffuse = NdotL * mainLight.color.rgb;

                // 最终颜色
                float3 finalColor = col.rgb * diffuse;

                return float4(finalColor, col.a);
            }

            ENDHLSL
        }
    }
}
