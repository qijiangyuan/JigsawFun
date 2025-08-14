Shader "Custom/SpriteLitAutoNormal_Border_Fused"
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

            // 边界距离和高度计算（完全参考原版）
            float ComputeHeight(float2 uv)
            {
                float alpha = tex2D(_MainTex, uv).a;
                if(alpha < _Cutoff) return 0.0;

                float2 texelSize = _MainTex_TexelSize.xy;
                float2 textureSize = 1.0 / texelSize;
                float2 pixelPos = uv * textureSize;

                float edgePixels = _BorderSize;
                float distToUVEdge = min(min(pixelPos.x, pixelPos.y), min(textureSize.x - pixelPos.x, textureSize.y - pixelPos.y));
                float minDistanceToTransparent = edgePixels;

                int searchRadius = min((int)_BorderSize, 16); // 移动端建议最大16
                for(int x = -searchRadius; x <= searchRadius; x++) {
                    for(int y = -searchRadius; y <= searchRadius; y++) {
                        if(length(float2(x, y)) > searchRadius) continue; // 只采样圆形区域
                        float2 offset = float2(x, y) * texelSize;
                        float2 sampleUV = uv + offset;
                        if(sampleUV.x >= 0 && sampleUV.x <= 1 && sampleUV.y >= 0 && sampleUV.y <= 1) {
                            float sampleAlpha = tex2D(_MainTex, sampleUV).a;
                            if(sampleAlpha < 0.1) {
                                float distance = length(float2(x, y));
                                minDistanceToTransparent = min(minDistanceToTransparent, distance);
                            }
                        }
                    }
                }

                float finalEdgeDistance = min(distToUVEdge, minDistanceToTransparent);
                float height = saturate(finalEdgeDistance / edgePixels);
                height = smoothstep(0.0, 1.0, height);
                if(finalEdgeDistance >= edgePixels) height = 1.0;
                height *= alpha;
                return height;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseCol = tex2D(_MainTex, IN.uv) * IN.color;
                if (baseCol.a < _Cutoff) discard;

                float2 texelSize = _MainTex_TexelSize.xy;
                float hC = ComputeHeight(IN.uv);
                float hL = ComputeHeight(IN.uv + float2(-texelSize.x, 0));
                float hR = ComputeHeight(IN.uv + float2(texelSize.x, 0));
                float hD = ComputeHeight(IN.uv + float2(0, -texelSize.y));
                float hU = ComputeHeight(IN.uv + float2(0, texelSize.y));

                float dx = (hR - hL) * _EdgeStrength * 2.0;
                float dy = (hU - hD) * _EdgeStrength * 2.0;
                float3 normal = normalize(float3(-dx, -dy, 0.5));

                // 光照
                Light mainLight = GetMainLight();
                half NdotL = dot(normal, mainLight.direction);

                half lightIntensity = saturate(NdotL * 1.5 + 0.1);
                half shadowIntensity = saturate(-NdotL * 0.8 + 0.3);

                half3 lightColor = baseCol.rgb * mainLight.color * lightIntensity;
                half3 shadowColor = baseCol.rgb * 0.4;
                half3 litColor = lerp(shadowColor, lightColor, saturate(NdotL + 0.5));

                return half4(litColor, baseCol.a);
            }
            ENDHLSL
        }
    }
}