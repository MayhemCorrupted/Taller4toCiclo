Shader "CustomRenderTexture/IsometricSeeThrough"
{
    Properties
    {
        _BaseMap ("Albedo (RGB)", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

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
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 screenPos   : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
            CBUFFER_END

            float4 _PlayerViewportPos;
            float _CutoutRadius;
            float _CutoutFalloff;
            float _ScreenAspect;

            float BayerDither4x4(float2 pixelPos)
            {
                int x = int(fmod(pixelPos.x, 4.0));
                int y = int(fmod(pixelPos.y, 4.0));
                int index = y * 4 + x;

                const float bayer[16] = {
                     0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                    12.0/16.0,  4.0/16.0, 14.0/16.0,  6.0/16.0,
                     3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                    15.0/16.0,  7.0/16.0, 13.0/16.0,  5.0/16.0
                };
                return bayer[index];
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = normInputs.normalWS;
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                if (_CutoutRadius > 0.001)
                {
                    float2 viewportUV = input.screenPos.xy / input.screenPos.w;
                    float2 diff = viewportUV - _PlayerViewportPos.xy;
                    diff.x *= _ScreenAspect;

                    float dist = length(diff);
                    float mask = smoothstep(_CutoutRadius - _CutoutFalloff, _CutoutRadius + _CutoutFalloff, dist);
                    float dither = BayerDither4x4(input.positionCS.xy);

                    clip(mask - dither);
                }

                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalize(input.normalWS), mainLight.direction));
                half3 ambient = SampleSH(normalize(input.normalWS));
                half3 lighting = mainLight.color * NdotL + ambient;

                return half4(texColor.rgb * lighting, 1.0);
            }
            ENDHLSL
        }
    }
}
