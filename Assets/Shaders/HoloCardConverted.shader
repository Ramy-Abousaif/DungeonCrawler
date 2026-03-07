Shader "Custom/HoloCardConverted"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _HoloTexture("HoloTexture", 2D) = "white" {}
        [Toggle] _OpaqueMode("Opaque Mode", Float) = 0
        _DistortionAmount("DistortionAmount", Float) = 20.0
        _OffsetAmount("OffsetAmount", Float) = 0.1
        _AngleUVBlend("Angle UV Blend", Range(0, 1)) = 0.75
        _AngleUVScale("Angle UV Scale", Float) = 1.0
        _DistortionStrength("Distortion Strength", Range(0, 1)) = 0.08
        _SideDistortionDamping("Side Distortion Damping", Range(0, 6)) = 2.0
        [Toggle] _EnableLitShading("Enable Lit Shading", Float) = 1
        [Toggle(_RECEIVE_SHADOWS_OFF)] _ReceiveShadows("Receive Shadows", Float) = 1
        _LitStrength("Lit Strength", Range(0, 1)) = 1
        _EmissionStrength("Emission Strength", Range(0, 5)) = 1
        _Smoothness("Smoothness", Range(0, 1)) = 0
        _Metallic("Metallic", Range(0, 1)) = 0
        [NoScaleOffset] _Cubemap("Cubemap", Cube) = "" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One OneMinusSrcAlpha
            ZWrite [_OpaqueMode]
            Cull Off

            Stencil
            {
                Ref 1
                Comp Equal
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _ _RECEIVE_SHADOWS_OFF

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            TEXTURE2D(_HoloTexture);
            SAMPLER(sampler_HoloTexture);
            TEXTURECUBE(_Cubemap);
            SAMPLER(sampler_Cubemap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _HoloTexture_ST;
                float _OpaqueMode;
                float _DistortionAmount;
                float _OffsetAmount;
                float _AngleUVBlend;
                float _AngleUVScale;
                float _DistortionStrength;
                float _SideDistortionDamping;
                float _EnableLitShading;
                float _LitStrength;
                float _EmissionStrength;
                float _Smoothness;
                float _Metallic;
            CBUFFER_END

            float SimpleNoiseValueDeterministic(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float2 c0 = i + float2(0.0, 0.0);
                float2 c1 = i + float2(1.0, 0.0);
                float2 c2 = i + float2(0.0, 1.0);
                float2 c3 = i + float2(1.0, 1.0);

                float r0;
                float r1;
                float r2;
                float r3;

                Hash_Tchou_2_1_float(c0, r0);
                Hash_Tchou_2_1_float(c1, r1);
                Hash_Tchou_2_1_float(c2, r2);
                Hash_Tchou_2_1_float(c3, r3);

                float bottom = lerp(r0, r1, f.x);
                float top = lerp(r2, r3, f.x);
                return lerp(bottom, top, f.y);
            }

            float SimpleNoiseDeterministic(float2 uv, float scale)
            {
                float outNoise = 0.0;
                for (int octave = 0; octave < 3; octave++)
                {
                    float freq = pow(2.0, (float)octave);
                    float amp = pow(0.5, (float)(3 - octave));
                    outNoise += SimpleNoiseValueDeterministic(uv * (scale / freq)) * amp;
                }
                return outNoise;
            }

            float4 BlendDodge(float4 baseColor, float4 blendColor, float opacity)
            {
                float4 dodged = baseColor / (1.0 - clamp(blendColor, 0.000001, 0.999999));
                return lerp(baseColor, dodged, opacity);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = normalInputs.normalWS;

                real sign = IN.tangentOS.w * GetOddNegativeScale();
                OUT.tangentWS = float4(normalInputs.tangentWS.xyz, sign);

                OUT.uv = TRANSFORM_TEX(IN.uv, _HoloTexture);
                OUT.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                return OUT;
            }

            half4 frag(Varyings IN, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
            {
                half faceSign = IS_FRONT_VFACE(facing, 1.0h, -1.0h);

                half3 normalWS = normalize(IN.normalWS) * faceSign;
                half3 tangentWS = normalize(IN.tangentWS.xyz);
                half tangentSign = IN.tangentWS.w * faceSign;

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                half3 bitangentWS = normalize(tangentSign * cross(normalWS, tangentWS));
                half3 viewDirTS = normalize(half3(
                    dot(viewDirWS, tangentWS),
                    dot(viewDirWS, bitangentWS),
                    dot(viewDirWS, normalWS)
                ));

                // Blend mesh UVs with camera-angle UVs to get a holographic, view-reactive map.
                float2 angleUV = normalize(viewDirWS).xy * 0.5 + 0.5;
                angleUV = (angleUV - 0.5) * _AngleUVScale + 0.5;
                float2 baseUV = lerp(IN.uv, angleUV, saturate(_AngleUVBlend));

                // Reduce distortion at grazing angles to avoid side-face smearing.
                half ndotv = saturate(abs(dot(normalWS, viewDirWS)));
                half sideDamping = pow(ndotv, _SideDistortionDamping);

                float2 uvWithOffset = baseUV + (float2)viewDirTS.xy * _OffsetAmount;
                float distortionNoiseX = SimpleNoiseDeterministic(uvWithOffset, _DistortionAmount);
                float distortionNoiseY = SimpleNoiseDeterministic(uvWithOffset + float2(37.13, 17.97), _DistortionAmount);
                float2 distortion = (float2(distortionNoiseX, distortionNoiseY) - 0.5) * (2.0 * _DistortionStrength * sideDamping);
                float2 holoUV = baseUV + distortion;

                float4 holoSample = SAMPLE_TEXTURE2D(_HoloTexture, sampler_HoloTexture, holoUV);
                float4 tintedHolo = holoSample * _BaseColor;
                float3 cubeDirWS = reflect(-normalize(viewDirWS), normalWS);
                float4 cubeSample = SAMPLE_TEXTURECUBE(_Cubemap, sampler_Cubemap, cubeDirWS);

                float4 emissionBlend = BlendDodge(tintedHolo, cubeSample, 1.0);
                half litMask = saturate(_EnableLitShading) * saturate(_LitStrength);
                half3 litAlbedo = tintedHolo.rgb * litMask;
                half3 finalEmission = emissionBlend.rgb * _EmissionStrength;
                half transparentAlpha = saturate(holoSample.a * _BaseColor.a);
                half finalAlpha = lerp(transparentAlpha, 1.0h, saturate(_OpaqueMode));

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = NormalizeNormalPerPixel(normalWS);
                inputData.viewDirectionWS = viewDirWS;
#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
#else
                inputData.shadowCoord = float4(0.0, 0.0, 0.0, 0.0);
#endif
                inputData.fogCoord = IN.fogFactor;
                inputData.vertexLighting = half3(0.0, 0.0, 0.0);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionHCS);
                inputData.shadowMask = half4(1.0, 1.0, 1.0, 1.0);

                half4 color = UniversalFragmentPBR(
                    inputData,
                    litAlbedo,
                    _Metallic,
                    half3(0.0h, 0.0h, 0.0h),
                    _Smoothness,
                    1.0h,
                    finalEmission,
                    finalAlpha
                );

                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }
    }
}
