Shader "Custom/DoubleSidedLit"
{
    Properties
    {
        [Header(Outside Surface)]
        _BaseMap("Outside Texture", 2D) = "white" {}
        _BaseColor("Outside Color", Color) = (1, 1, 1, 1)
        _Smoothness("Outside Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic("Outside Metallic", Range(0.0, 1.0)) = 0.0
        
        [Header(Inside Surface)]
        _InsideMap("Inside Texture", 2D) = "white" {}
        _InsideColor("Inside Color", Color) = (1, 1, 1, 1)
        _InsideSmoothness("Inside Smoothness", Range(0.0, 1.0)) = 0.5
        _InsideMetallic("Inside Metallic", Range(0.0, 1.0)) = 0.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull Off  // 양면 렌더링 - 이게 핵심!
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 texcoord     : TEXCOORD0;
                float2 lightmapUV   : TEXCOORD1;
            };
            
            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float4 positionCS   : SV_POSITION;
                float fogFactor     : TEXCOORD3;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_InsideMap);
            SAMPLER(sampler_InsideMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Smoothness;
                half _Metallic;
                float4 _InsideMap_ST;
                half4 _InsideColor;
                half _InsideSmoothness;
                half _InsideMetallic;
            CBUFFER_END
            
            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.normalWS = normalInput.normalWS;
                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
                
                return output;
            }
            
            half4 LitPassFragment(Varyings input, half facing : VFACE) : SV_Target
            {
                // facing > 0 = 바깥쪽 면, facing < 0 = 안쪽 면
                bool isInside = facing < 0;
                
                // 양면 노말 처리
                input.normalWS = normalize(input.normalWS) * facing;
                
                // 안쪽/바깥쪽에 따라 다른 텍스처와 속성 사용
                half4 albedoAlpha;
                half3 albedo;
                half metallic;
                half smoothness;
                
                if (isInside)
                {
                    // 안쪽 면 - 돌 질감
                    float2 insideUV = TRANSFORM_TEX(input.uv, _InsideMap);
                    albedoAlpha = SAMPLE_TEXTURE2D(_InsideMap, sampler_InsideMap, insideUV);
                    albedo = albedoAlpha.rgb * _InsideColor.rgb;
                    metallic = _InsideMetallic;
                    smoothness = _InsideSmoothness;
                }
                else
                {
                    // 바깥쪽 면
                    albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                    albedo = albedoAlpha.rgb * _BaseColor.rgb;
                    metallic = _Metallic;
                    smoothness = _Smoothness;
                }
                
                // Lighting
                InputData inputData;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = input.normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
                
                SurfaceData surfaceData;
                surfaceData.albedo = albedo;
                surfaceData.specular = half3(0.0h, 0.0h, 0.0h);
                surfaceData.metallic = metallic;
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
                surfaceData.emission = half3(0, 0, 0);
                surfaceData.occlusion = 1.0h;
                surfaceData.alpha = 1.0h;
                surfaceData.clearCoatMask = 0.0h;
                surfaceData.clearCoatSmoothness = 1.0h;
                
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                
                return color;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}
            
            Cull Off  // 그림자도 양면
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            float3 _LightDirection;
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}
            
            Cull Off
            ZWrite On
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 position     : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };
            
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.position.xyz);
                return output;
            }
            
            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
