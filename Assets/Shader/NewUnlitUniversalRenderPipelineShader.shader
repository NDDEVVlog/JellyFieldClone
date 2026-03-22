Shader "Custom/JellyToonShader"
{
    Properties
    {
        [Header(Colors)]
        _BaseColor ("Body Color", Color) = (1, 0.8, 0.4, 1)
        _TopColor ("Top Cap Color", Color) = (0.4, 0.2, 0.1, 1)
        _CapHeight ("Cap Height Mask", Range(-0.6, 1.0)) = 0.3
        _Smoothness ("Cap Smoothness", Range(0.01, 0.2)) = 0.02
        
        [Header(Lighting)]
        _Glossiness ("Glossiness", Range(1, 100)) = 50
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(1, 10)) = 4
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewDirWS : TEXCOORD1; float3 localPos : TEXCOORD3; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float4 _TopColor; float _CapHeight; float _Smoothness;
                float _Glossiness; float4 _RimColor; float _RimPower;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;
                // Đã loại bỏ toàn bộ code biến dạng (Wobble) ở đây
                float3 pos = input.positionOS.xyz;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(pos);
                
                output.positionCS = vertexInput.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.localPos = pos;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                Light mainLight = GetMainLight();
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                float3 lightDir = normalize(mainLight.direction);

                // 1. Phối màu 2 lớp dựa trên chiều cao Local (Caramel Cap)
                float capMask = smoothstep(_CapHeight, _CapHeight + _Smoothness, input.localPos.y);
                float3 finalBaseColor = lerp(_BaseColor.rgb, _TopColor.rgb, capMask);

                // 2. Toon Shading (Diffuse)
                float NdotL = dot(normal, lightDir);
                float toonDiffuse = smoothstep(-0.2, 0.1, NdotL); // Cắt sắc cạnh kiểu Toon
                float3 diffuse = finalBaseColor * toonDiffuse;

                // 3. Specular (Bóng loáng kiểu nhựa/thạch)
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(max(0, dot(normal, halfDir)), _Glossiness);
                float3 specular = smoothstep(0.5, 0.55, spec) * float3(1,1,1);

                // 4. Rim Light (Viền sáng)
                float rim = pow(1.0 - max(0, dot(normal, viewDir)), _RimPower);
                float3 finalRim = rim * _RimColor.rgb;
                
                // Mix lại tất cả
                float3 color = (diffuse + specular + finalRim * 0.5) * mainLight.color;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}