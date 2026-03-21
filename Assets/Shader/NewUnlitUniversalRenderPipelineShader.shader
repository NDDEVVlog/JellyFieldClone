Shader "Custom/URP_FixedBasePudding"
{
    Properties
    {
        [Header(Colors)]
        _BaseColor ("Body Color", Color) = (1, 0.8, 0.4, 1)
        _TopColor ("Top Cap Color", Color) = (0.4, 0.2, 0.1, 1)
        _CapHeight ("Cap Height Mask", Range(0, 0.5)) = 0.4
        
        [Header(Lighting)]
        _Glossiness ("Glossiness", Range(1, 100)) = 50
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        
        [Header(Physics)]
        _WobbleVector ("Wobble Vector (XZ)", Vector) = (0,0,0,0) // Nhận X và Z từ C#
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
                float4 _BaseColor; float4 _TopColor; float _CapHeight;
                float _Glossiness; float4 _RimColor; float4 _WobbleVector;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;
                float3 pos = input.positionOS.xyz;
                
                // 1. FIXED BASE LOGIC (Cố định phần đế)
                // Giả sử Mesh của bạn có đáy nằm ở y = -0.5
                // mask = 0 ở đáy, mask = 1 ở đỉnh.
                // Dùng hàm pow() để phần đáy "cứng" hơn, chỉ rung mạnh ở nửa trên.
                float mask = saturate(pos.y + 0.5); 
                mask = pow(mask, 1.5); // Làm cho chân pudding bám chặt hơn vào đĩa

                // 2. WOBBLE TRÊN CẢ X VÀ Z
                // Chúng ta dùng _WobbleVector.x và _WobbleVector.z truyền từ C#
                pos.x += _WobbleVector.x * mask;
                pos.z += _WobbleVector.z * mask;
                
                // Hiệu ứng "Squish" - Khi nghiêng thì khối hơi lùn xuống một chút cho tự nhiên
                pos.y -= length(_WobbleVector.xz) * 0.2 * mask;

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

                // Màu Caramel trên đỉnh
                float capMask = smoothstep(_CapHeight, _CapHeight + 0.02, input.localPos.y);
                float3 finalBaseColor = lerp(_BaseColor.rgb, _TopColor.rgb, capMask);

                // Toon Shading
                float NdotL = dot(normal, lightDir);
                float3 diffuse = finalBaseColor * smoothstep(-0.2, 0.4, NdotL);

                // Specular (Bóng loáng)
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(max(0, dot(normal, halfDir)), _Glossiness);
                float3 specular = smoothstep(0.5, 0.55, spec) * float3(1,1,1);

                // Rim Light
                float rim = pow(1.0 - max(0, dot(normal, viewDir)), 4.0);
                
                float3 color = (diffuse + specular + rim * _RimColor.rgb * 0.5) * mainLight.color;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}