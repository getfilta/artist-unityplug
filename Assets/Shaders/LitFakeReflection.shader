Shader "Filta/LitFakeReflection"
    {
        Properties
        {
            [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 0.0
            _BaseTexture("BaseTexture", 2D) = "white" {}
            [HDR]_BaseColor("BaseColor", Color) = (1, 1, 1, 1)
            _Metallic("Metallic", Range(0, 1)) = 0
            _Smoothness("Smoothness", Range(0, 1)) = 0.5
            _AmbientOcclusion("AmbientOcclusion", Range(0, 1)) = 1
            _AlphaClipThreshold("AlphaClipThreshold", Range(0, 1)) = 0.5
            [Normal]_Normal_Map("Normal Map", 2D) = "bump" {}
            _NormalStrength("NormalStrength", Float) = 0
            [NoScaleOffset]_ReflectionMap("ReflectionMap", CUBE) = "" {}
            _ReflectionStrength("ReflectionStrength", Range(0, 1)) = 0.5
            _ReflectionBlur("ReflectionBlur", Range(0, 10)) = 0
            _Fresnel("Fresnel", Range(0, 5)) = 1
            [HDR]_Emission_Color("Emission Color", Color) = (0, 0, 0, 0)
            _Emission_Map("Emission Map", 2D) = "white" {}
            [HideInInspector]_WorkflowMode("_WorkflowMode", Float) = 1
            [HideInInspector]_CastShadows("_CastShadows", Float) = 1
            [HideInInspector]_ReceiveShadows("_ReceiveShadows", Float) = 1
            [HideInInspector]_Surface("_Surface", Float) = 0
            [HideInInspector]_Blend("_Blend", Float) = 0
            [HideInInspector]_AlphaClip("_AlphaClip", Float) = 0
            [HideInInspector]_SrcBlend("_SrcBlend", Float) = 1
            [HideInInspector]_DstBlend("_DstBlend", Float) = 0
            [HideInInspector][ToggleUI]_ZWrite("_ZWrite", Float) = 1
            [HideInInspector]_ZWriteControl("_ZWriteControl", Float) = 0
            [HideInInspector]_ZTest("_ZTest", Float) = 4
            [HideInInspector]_Cull("_Cull", Float) = 2
            [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
            [HideInInspector]_QueueControl("_QueueControl", Float) = -1
            [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
            [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
            [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
        }
        SubShader
        {
            Tags
            {
                "RenderPipeline"="UniversalPipeline"
                "RenderType"="Opaque"
                "UniversalMaterialType" = "Lit"
                "Queue"="Geometry"
                "ShaderGraphShader"="true"
                "ShaderGraphTargetId"="UniversalLitSubTarget"
            }
            Pass
            {
                Name "Universal Forward"
                Tags
                {
                    "LightMode" = "UniversalForward"
                }
            
            // Render State
            Cull [_Cull]
                Blend [_SrcBlend] [_DstBlend]
                ZTest [_ZTest]
                ZWrite [_ZWrite]
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 4.5
                #pragma exclude_renderers gles gles3 glcore
                #pragma multi_compile_instancing
                #pragma multi_compile_fog
                #pragma instancing_options renderinglayer
                #pragma multi_compile _ DOTS_INSTANCING_ON
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
                #pragma multi_compile _ LIGHTMAP_ON
                #pragma multi_compile _ DYNAMICLIGHTMAP_ON
                #pragma multi_compile _ DIRLIGHTMAP_COMBINED
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
                #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
                #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
                #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
                #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
                #pragma multi_compile _ _SHADOWS_SOFT
                #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
                #pragma multi_compile _ SHADOWS_SHADOWMASK
                #pragma multi_compile _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
                #pragma multi_compile _ _LIGHT_LAYERS
                #pragma multi_compile _ DEBUG_DISPLAY
                #pragma multi_compile _ _LIGHT_COOKIES
                #pragma multi_compile _ _CLUSTERED_RENDERING
                #pragma shader_feature_fragment _ _SURFACE_TYPE_TRANSPARENT
                #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON
                #pragma shader_feature_local_fragment _ _ALPHATEST_ON
                #pragma shader_feature_local_fragment _ _SPECULAR_SETUP
                #pragma shader_feature_local _ _RECEIVE_SHADOWS_OFF
                #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_VIEWDIRECTION_WS
            #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
            #define VARYINGS_NEED_SHADOW_COORD
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_FORWARD
                #define _FOG_FRAGMENT 1
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                     float4 uv1 : TEXCOORD1;
                     float4 uv2 : TEXCOORD2;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 positionWS;
                     float3 normalWS;
                     float4 tangentWS;
                     float4 texCoord0;
                     float3 viewDirectionWS;
                    #if defined(LIGHTMAP_ON)
                     float2 staticLightmapUV;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                     float2 dynamicLightmapUV;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                     float3 sh;
                    #endif
                     float4 fogFactorAndVertexLight;
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                     float4 shadowCoord;
                    #endif
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 WorldSpaceNormal;
                     float3 TangentSpaceNormal;
                     float3 ObjectSpaceViewDirection;
                     float3 WorldSpaceViewDirection;
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float3 interp0 : INTERP0;
                     float3 interp1 : INTERP1;
                     float4 interp2 : INTERP2;
                     float4 interp3 : INTERP3;
                     float3 interp4 : INTERP4;
                     float2 interp5 : INTERP5;
                     float2 interp6 : INTERP6;
                     float3 interp7 : INTERP7;
                     float4 interp8 : INTERP8;
                     float4 interp9 : INTERP9;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyz =  input.positionWS;
                    output.interp1.xyz =  input.normalWS;
                    output.interp2.xyzw =  input.tangentWS;
                    output.interp3.xyzw =  input.texCoord0;
                    output.interp4.xyz =  input.viewDirectionWS;
                    #if defined(LIGHTMAP_ON)
                    output.interp5.xy =  input.staticLightmapUV;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                    output.interp6.xy =  input.dynamicLightmapUV;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                    output.interp7.xyz =  input.sh;
                    #endif
                    output.interp8.xyzw =  input.fogFactorAndVertexLight;
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.interp9.xyzw =  input.shadowCoord;
                    #endif
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.positionWS = input.interp0.xyz;
                    output.normalWS = input.interp1.xyz;
                    output.tangentWS = input.interp2.xyzw;
                    output.texCoord0 = input.interp3.xyzw;
                    output.viewDirectionWS = input.interp4.xyz;
                    #if defined(LIGHTMAP_ON)
                    output.staticLightmapUV = input.interp5.xy;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                    output.dynamicLightmapUV = input.interp6.xy;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                    output.sh = input.interp7.xyz;
                    #endif
                    output.fogFactorAndVertexLight = input.interp8.xyzw;
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = input.interp9.xyzw;
                    #endif
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
                
                void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
                {
                    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
                }
                
                void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
                {
                    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
                }
                
                void Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power, out float Out)
                {
                    Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
                }
                
                void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
                {
                    Out = lerp(A, B, T);
                }
                
                void Unity_Add_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A + B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 BaseColor;
                    float3 NormalTS;
                    float3 Emission;
                    float Metallic;
                    float3 Specular;
                    float Smoothness;
                    float Occlusion;
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    UnityTexture2D _Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0 = UnityBuildTexture2DStruct(_Normal_Map);
                    #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
                      float4 _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
                    #else
                      float4 _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0.tex, _Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0.samplerstate, _Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
                    #endif
                    _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.rgb = UnpackNormal(_SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0);
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_R_5 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.r;
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_G_6 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.g;
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_B_7 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.b;
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_A_8 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.a;
                    float _Property_8b766139f69a45838f9c3225f987b805_Out_0 = _NormalStrength;
                    float3 _NormalStrength_35f15cab4e5b40bfb98ab5c553b148f2_Out_2;
                    Unity_NormalStrength_float((_SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.xyz), _Property_8b766139f69a45838f9c3225f987b805_Out_0, _NormalStrength_35f15cab4e5b40bfb98ab5c553b148f2_Out_2);
                    float3 _NormalBlend_53d463b3a3d149b2b8781e24e525f79d_Out_2;
                    Unity_NormalBlend_float(IN.ObjectSpaceNormal, _NormalStrength_35f15cab4e5b40bfb98ab5c553b148f2_Out_2, _NormalBlend_53d463b3a3d149b2b8781e24e525f79d_Out_2);
                    float4 _Property_a45d582cfcc6470bbc159ec46aee4232_Out_0 = IsGammaSpace() ? LinearToSRGB(_Emission_Color) : _Emission_Color;
                    UnityTexture2D _Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0 = UnityBuildTexture2DStruct(_Emission_Map);
                    float4 _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0 = SAMPLE_TEXTURE2D(_Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0.tex, _Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0.samplerstate, _Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_R_4 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.r;
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_G_5 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.g;
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_B_6 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.b;
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_A_7 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.a;
                    float4 _Multiply_ba04fd89b7814d8fb113ccad3d4c8983_Out_2;
                    Unity_Multiply_float4_float4(_Property_a45d582cfcc6470bbc159ec46aee4232_Out_0, _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0, _Multiply_ba04fd89b7814d8fb113ccad3d4c8983_Out_2);
                    UnityTextureCube _Property_3faaacbd799c4183ac76e7734fa12f06_Out_0 = UnityBuildTextureCubeStruct(_ReflectionMap);
                    float _Property_66fcc8bf022b426f8caf633f5047a880_Out_0 = _ReflectionBlur;
                    float4 _SampleReflectedCubemap_6f488a8de9824970ae8293b6b7cdb249_Out_0 = SAMPLE_TEXTURECUBE_LOD(_Property_3faaacbd799c4183ac76e7734fa12f06_Out_0.tex, _Property_3faaacbd799c4183ac76e7734fa12f06_Out_0.samplerstate, reflect(-IN.ObjectSpaceViewDirection, IN.ObjectSpaceNormal), _Property_66fcc8bf022b426f8caf633f5047a880_Out_0);
                    float _Property_b3ddb2e217d44708b4809f38a7076c13_Out_0 = _ReflectionStrength;
                    float4 _Multiply_6e83b7bead5a4a58820cabea3070544b_Out_2;
                    Unity_Multiply_float4_float4(_SampleReflectedCubemap_6f488a8de9824970ae8293b6b7cdb249_Out_0, (_Property_b3ddb2e217d44708b4809f38a7076c13_Out_0.xxxx), _Multiply_6e83b7bead5a4a58820cabea3070544b_Out_2);
                    float _Property_a341c49e136544dba79e08df1500f38a_Out_0 = _Fresnel;
                    float _FresnelEffect_17988048113d428e8e20e61b86e4dd7e_Out_3;
                    Unity_FresnelEffect_float(IN.WorldSpaceNormal, IN.WorldSpaceViewDirection, _Property_a341c49e136544dba79e08df1500f38a_Out_0, _FresnelEffect_17988048113d428e8e20e61b86e4dd7e_Out_3);
                    float4 _Multiply_963350bef93a4c75bccae43d4fa195a0_Out_2;
                    Unity_Multiply_float4_float4(_Multiply_6e83b7bead5a4a58820cabea3070544b_Out_2, (_FresnelEffect_17988048113d428e8e20e61b86e4dd7e_Out_3.xxxx), _Multiply_963350bef93a4c75bccae43d4fa195a0_Out_2);
                    float _Property_923a8c2c4f594bb599a1ec81f0a14dc3_Out_0 = _Metallic;
                    float4 _Lerp_c4a3f1e42e45428685b72e4fea51760f_Out_3;
                    Unity_Lerp_float4(float4(1, 1, 1, 1), _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2, (_Property_923a8c2c4f594bb599a1ec81f0a14dc3_Out_0.xxxx), _Lerp_c4a3f1e42e45428685b72e4fea51760f_Out_3);
                    float4 _Multiply_cb475d772e1b4b859c3cb8ddcdc638be_Out_2;
                    Unity_Multiply_float4_float4(_Multiply_963350bef93a4c75bccae43d4fa195a0_Out_2, _Lerp_c4a3f1e42e45428685b72e4fea51760f_Out_3, _Multiply_cb475d772e1b4b859c3cb8ddcdc638be_Out_2);
                    float4 _Add_ed388982523a4feeb56764cbb134f284_Out_2;
                    Unity_Add_float4(_Multiply_ba04fd89b7814d8fb113ccad3d4c8983_Out_2, _Multiply_cb475d772e1b4b859c3cb8ddcdc638be_Out_2, _Add_ed388982523a4feeb56764cbb134f284_Out_2);
                    float _Property_dd2d4b2f1ffe45f28bd020d7ee60ba8e_Out_0 = _Metallic;
                    float _Property_2dbcf226c6b44efeb4a39bd557723f89_Out_0 = _Smoothness;
                    float _Property_52d78f7cc5504dc59468b0a041ec6df6_Out_0 = _AmbientOcclusion;
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.BaseColor = (_Multiply_b57de817a55745fd8c03aba79eba566e_Out_2.xyz);
                    surface.NormalTS = _NormalBlend_53d463b3a3d149b2b8781e24e525f79d_Out_2;
                    surface.Emission = (_Add_ed388982523a4feeb56764cbb134f284_Out_2.xyz);
                    surface.Metallic = _Property_dd2d4b2f1ffe45f28bd020d7ee60ba8e_Out_0;
                    surface.Specular = IsGammaSpace() ? float3(0.5, 0.5, 0.5) : SRGBToLinear(float3(0.5, 0.5, 0.5));
                    surface.Smoothness = _Property_2dbcf226c6b44efeb4a39bd557723f89_Out_0;
                    surface.Occlusion = _Property_52d78f7cc5504dc59468b0a041ec6df6_Out_0;
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
                    float3 unnormalizedNormalWS = input.normalWS;
                    const float renormFactor = 1.0 / length(unnormalizedNormalWS);
                
                
                    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
                    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
                    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
                
                
                    output.WorldSpaceViewDirection = normalize(input.viewDirectionWS);
                    output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "GBuffer"
                Tags
                {
                    "LightMode" = "UniversalGBuffer"
                }
            
            // Render State
            Cull [_Cull]
                Blend [_SrcBlend] [_DstBlend]
                ZTest [_ZTest]
                ZWrite [_ZWrite]
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 4.5
                #pragma exclude_renderers gles gles3 glcore
                #pragma multi_compile_instancing
                #pragma multi_compile_fog
                #pragma instancing_options renderinglayer
                #pragma multi_compile _ DOTS_INSTANCING_ON
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma multi_compile _ LIGHTMAP_ON
                #pragma multi_compile _ DYNAMICLIGHTMAP_ON
                #pragma multi_compile _ DIRLIGHTMAP_COMBINED
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
                #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
                #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
                #pragma multi_compile _ _SHADOWS_SOFT
                #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
                #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
                #pragma multi_compile _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
                #pragma multi_compile _ _GBUFFER_NORMALS_OCT
                #pragma multi_compile _ _LIGHT_LAYERS
                #pragma multi_compile _ _RENDER_PASS_ENABLED
                #pragma multi_compile _ DEBUG_DISPLAY
                #pragma shader_feature_fragment _ _SURFACE_TYPE_TRANSPARENT
                #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON
                #pragma shader_feature_local_fragment _ _ALPHATEST_ON
                #pragma shader_feature_local_fragment _ _SPECULAR_SETUP
                #pragma shader_feature_local _ _RECEIVE_SHADOWS_OFF
                #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_VIEWDIRECTION_WS
            #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
            #define VARYINGS_NEED_SHADOW_COORD
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_GBUFFER
                #define _FOG_FRAGMENT 1
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                     float4 uv1 : TEXCOORD1;
                     float4 uv2 : TEXCOORD2;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 positionWS;
                     float3 normalWS;
                     float4 tangentWS;
                     float4 texCoord0;
                     float3 viewDirectionWS;
                    #if defined(LIGHTMAP_ON)
                     float2 staticLightmapUV;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                     float2 dynamicLightmapUV;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                     float3 sh;
                    #endif
                     float4 fogFactorAndVertexLight;
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                     float4 shadowCoord;
                    #endif
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 WorldSpaceNormal;
                     float3 TangentSpaceNormal;
                     float3 ObjectSpaceViewDirection;
                     float3 WorldSpaceViewDirection;
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float3 interp0 : INTERP0;
                     float3 interp1 : INTERP1;
                     float4 interp2 : INTERP2;
                     float4 interp3 : INTERP3;
                     float3 interp4 : INTERP4;
                     float2 interp5 : INTERP5;
                     float2 interp6 : INTERP6;
                     float3 interp7 : INTERP7;
                     float4 interp8 : INTERP8;
                     float4 interp9 : INTERP9;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyz =  input.positionWS;
                    output.interp1.xyz =  input.normalWS;
                    output.interp2.xyzw =  input.tangentWS;
                    output.interp3.xyzw =  input.texCoord0;
                    output.interp4.xyz =  input.viewDirectionWS;
                    #if defined(LIGHTMAP_ON)
                    output.interp5.xy =  input.staticLightmapUV;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                    output.interp6.xy =  input.dynamicLightmapUV;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                    output.interp7.xyz =  input.sh;
                    #endif
                    output.interp8.xyzw =  input.fogFactorAndVertexLight;
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.interp9.xyzw =  input.shadowCoord;
                    #endif
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.positionWS = input.interp0.xyz;
                    output.normalWS = input.interp1.xyz;
                    output.tangentWS = input.interp2.xyzw;
                    output.texCoord0 = input.interp3.xyzw;
                    output.viewDirectionWS = input.interp4.xyz;
                    #if defined(LIGHTMAP_ON)
                    output.staticLightmapUV = input.interp5.xy;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                    output.dynamicLightmapUV = input.interp6.xy;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                    output.sh = input.interp7.xyz;
                    #endif
                    output.fogFactorAndVertexLight = input.interp8.xyzw;
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = input.interp9.xyzw;
                    #endif
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
                
                void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
                {
                    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
                }
                
                void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
                {
                    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
                }
                
                void Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power, out float Out)
                {
                    Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
                }
                
                void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
                {
                    Out = lerp(A, B, T);
                }
                
                void Unity_Add_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A + B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 BaseColor;
                    float3 NormalTS;
                    float3 Emission;
                    float Metallic;
                    float3 Specular;
                    float Smoothness;
                    float Occlusion;
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    UnityTexture2D _Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0 = UnityBuildTexture2DStruct(_Normal_Map);
                    #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
                      float4 _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
                    #else
                      float4 _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0.tex, _Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0.samplerstate, _Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
                    #endif
                    _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.rgb = UnpackNormal(_SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0);
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_R_5 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.r;
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_G_6 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.g;
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_B_7 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.b;
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_A_8 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.a;
                    float _Property_8b766139f69a45838f9c3225f987b805_Out_0 = _NormalStrength;
                    float3 _NormalStrength_35f15cab4e5b40bfb98ab5c553b148f2_Out_2;
                    Unity_NormalStrength_float((_SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.xyz), _Property_8b766139f69a45838f9c3225f987b805_Out_0, _NormalStrength_35f15cab4e5b40bfb98ab5c553b148f2_Out_2);
                    float3 _NormalBlend_53d463b3a3d149b2b8781e24e525f79d_Out_2;
                    Unity_NormalBlend_float(IN.ObjectSpaceNormal, _NormalStrength_35f15cab4e5b40bfb98ab5c553b148f2_Out_2, _NormalBlend_53d463b3a3d149b2b8781e24e525f79d_Out_2);
                    float4 _Property_a45d582cfcc6470bbc159ec46aee4232_Out_0 = IsGammaSpace() ? LinearToSRGB(_Emission_Color) : _Emission_Color;
                    UnityTexture2D _Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0 = UnityBuildTexture2DStruct(_Emission_Map);
                    float4 _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0 = SAMPLE_TEXTURE2D(_Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0.tex, _Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0.samplerstate, _Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_R_4 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.r;
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_G_5 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.g;
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_B_6 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.b;
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_A_7 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.a;
                    float4 _Multiply_ba04fd89b7814d8fb113ccad3d4c8983_Out_2;
                    Unity_Multiply_float4_float4(_Property_a45d582cfcc6470bbc159ec46aee4232_Out_0, _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0, _Multiply_ba04fd89b7814d8fb113ccad3d4c8983_Out_2);
                    UnityTextureCube _Property_3faaacbd799c4183ac76e7734fa12f06_Out_0 = UnityBuildTextureCubeStruct(_ReflectionMap);
                    float _Property_66fcc8bf022b426f8caf633f5047a880_Out_0 = _ReflectionBlur;
                    float4 _SampleReflectedCubemap_6f488a8de9824970ae8293b6b7cdb249_Out_0 = SAMPLE_TEXTURECUBE_LOD(_Property_3faaacbd799c4183ac76e7734fa12f06_Out_0.tex, _Property_3faaacbd799c4183ac76e7734fa12f06_Out_0.samplerstate, reflect(-IN.ObjectSpaceViewDirection, IN.ObjectSpaceNormal), _Property_66fcc8bf022b426f8caf633f5047a880_Out_0);
                    float _Property_b3ddb2e217d44708b4809f38a7076c13_Out_0 = _ReflectionStrength;
                    float4 _Multiply_6e83b7bead5a4a58820cabea3070544b_Out_2;
                    Unity_Multiply_float4_float4(_SampleReflectedCubemap_6f488a8de9824970ae8293b6b7cdb249_Out_0, (_Property_b3ddb2e217d44708b4809f38a7076c13_Out_0.xxxx), _Multiply_6e83b7bead5a4a58820cabea3070544b_Out_2);
                    float _Property_a341c49e136544dba79e08df1500f38a_Out_0 = _Fresnel;
                    float _FresnelEffect_17988048113d428e8e20e61b86e4dd7e_Out_3;
                    Unity_FresnelEffect_float(IN.WorldSpaceNormal, IN.WorldSpaceViewDirection, _Property_a341c49e136544dba79e08df1500f38a_Out_0, _FresnelEffect_17988048113d428e8e20e61b86e4dd7e_Out_3);
                    float4 _Multiply_963350bef93a4c75bccae43d4fa195a0_Out_2;
                    Unity_Multiply_float4_float4(_Multiply_6e83b7bead5a4a58820cabea3070544b_Out_2, (_FresnelEffect_17988048113d428e8e20e61b86e4dd7e_Out_3.xxxx), _Multiply_963350bef93a4c75bccae43d4fa195a0_Out_2);
                    float _Property_923a8c2c4f594bb599a1ec81f0a14dc3_Out_0 = _Metallic;
                    float4 _Lerp_c4a3f1e42e45428685b72e4fea51760f_Out_3;
                    Unity_Lerp_float4(float4(1, 1, 1, 1), _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2, (_Property_923a8c2c4f594bb599a1ec81f0a14dc3_Out_0.xxxx), _Lerp_c4a3f1e42e45428685b72e4fea51760f_Out_3);
                    float4 _Multiply_cb475d772e1b4b859c3cb8ddcdc638be_Out_2;
                    Unity_Multiply_float4_float4(_Multiply_963350bef93a4c75bccae43d4fa195a0_Out_2, _Lerp_c4a3f1e42e45428685b72e4fea51760f_Out_3, _Multiply_cb475d772e1b4b859c3cb8ddcdc638be_Out_2);
                    float4 _Add_ed388982523a4feeb56764cbb134f284_Out_2;
                    Unity_Add_float4(_Multiply_ba04fd89b7814d8fb113ccad3d4c8983_Out_2, _Multiply_cb475d772e1b4b859c3cb8ddcdc638be_Out_2, _Add_ed388982523a4feeb56764cbb134f284_Out_2);
                    float _Property_dd2d4b2f1ffe45f28bd020d7ee60ba8e_Out_0 = _Metallic;
                    float _Property_2dbcf226c6b44efeb4a39bd557723f89_Out_0 = _Smoothness;
                    float _Property_52d78f7cc5504dc59468b0a041ec6df6_Out_0 = _AmbientOcclusion;
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.BaseColor = (_Multiply_b57de817a55745fd8c03aba79eba566e_Out_2.xyz);
                    surface.NormalTS = _NormalBlend_53d463b3a3d149b2b8781e24e525f79d_Out_2;
                    surface.Emission = (_Add_ed388982523a4feeb56764cbb134f284_Out_2.xyz);
                    surface.Metallic = _Property_dd2d4b2f1ffe45f28bd020d7ee60ba8e_Out_0;
                    surface.Specular = IsGammaSpace() ? float3(0.5, 0.5, 0.5) : SRGBToLinear(float3(0.5, 0.5, 0.5));
                    surface.Smoothness = _Property_2dbcf226c6b44efeb4a39bd557723f89_Out_0;
                    surface.Occlusion = _Property_52d78f7cc5504dc59468b0a041ec6df6_Out_0;
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
                    float3 unnormalizedNormalWS = input.normalWS;
                    const float renormFactor = 1.0 / length(unnormalizedNormalWS);
                
                
                    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
                    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
                    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
                
                
                    output.WorldSpaceViewDirection = normalize(input.viewDirectionWS);
                    output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "ShadowCaster"
                Tags
                {
                    "LightMode" = "ShadowCaster"
                }
            
            // Render State
            Cull [_Cull]
                ZTest LEqual
                ZWrite On
                ColorMask 0
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 4.5
                #pragma exclude_renderers gles gles3 glcore
                #pragma multi_compile_instancing
                #pragma multi_compile _ DOTS_INSTANCING_ON
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW
                #pragma shader_feature_local_fragment _ _ALPHATEST_ON
                #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_SHADOWCASTER
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 normalWS;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float3 interp0 : INTERP0;
                     float4 interp1 : INTERP1;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyz =  input.normalWS;
                    output.interp1.xyzw =  input.texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.normalWS = input.interp0.xyz;
                    output.texCoord0 = input.interp1.xyzw;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                
                
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "DepthOnly"
                Tags
                {
                    "LightMode" = "DepthOnly"
                }
            
            // Render State
            Cull [_Cull]
                ZTest LEqual
                ZWrite On
                ColorMask 0
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 4.5
                #pragma exclude_renderers gles gles3 glcore
                #pragma multi_compile_instancing
                #pragma multi_compile _ DOTS_INSTANCING_ON
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHONLY
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 interp0 : INTERP0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyzw =  input.texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.texCoord0 = input.interp0.xyzw;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                
                
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "DepthNormals"
                Tags
                {
                    "LightMode" = "DepthNormals"
                }
            
            // Render State
            Cull [_Cull]
                ZTest LEqual
                ZWrite On
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 4.5
                #pragma exclude_renderers gles gles3 glcore
                #pragma multi_compile_instancing
                #pragma multi_compile _ DOTS_INSTANCING_ON
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHNORMALS
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                     float4 uv1 : TEXCOORD1;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 normalWS;
                     float4 tangentWS;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 WorldSpaceNormal;
                     float3 TangentSpaceNormal;
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float3 interp0 : INTERP0;
                     float4 interp1 : INTERP1;
                     float4 interp2 : INTERP2;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyz =  input.normalWS;
                    output.interp1.xyzw =  input.tangentWS;
                    output.interp2.xyzw =  input.texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.normalWS = input.interp0.xyz;
                    output.tangentWS = input.interp1.xyzw;
                    output.texCoord0 = input.interp2.xyzw;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
                {
                    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
                }
                
                void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
                {
                    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
                }
                
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 NormalTS;
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    UnityTexture2D _Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0 = UnityBuildTexture2DStruct(_Normal_Map);
                    #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
                      float4 _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
                    #else
                      float4 _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0.tex, _Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0.samplerstate, _Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
                    #endif
                    _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.rgb = UnpackNormal(_SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0);
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_R_5 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.r;
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_G_6 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.g;
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_B_7 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.b;
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_A_8 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.a;
                    float _Property_8b766139f69a45838f9c3225f987b805_Out_0 = _NormalStrength;
                    float3 _NormalStrength_35f15cab4e5b40bfb98ab5c553b148f2_Out_2;
                    Unity_NormalStrength_float((_SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.xyz), _Property_8b766139f69a45838f9c3225f987b805_Out_0, _NormalStrength_35f15cab4e5b40bfb98ab5c553b148f2_Out_2);
                    float3 _NormalBlend_53d463b3a3d149b2b8781e24e525f79d_Out_2;
                    Unity_NormalBlend_float(IN.ObjectSpaceNormal, _NormalStrength_35f15cab4e5b40bfb98ab5c553b148f2_Out_2, _NormalBlend_53d463b3a3d149b2b8781e24e525f79d_Out_2);
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.NormalTS = _NormalBlend_53d463b3a3d149b2b8781e24e525f79d_Out_2;
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
                    float3 unnormalizedNormalWS = input.normalWS;
                    const float renormFactor = 1.0 / length(unnormalizedNormalWS);
                
                
                    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
                    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
                    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
                
                
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "Meta"
                Tags
                {
                    "LightMode" = "Meta"
                }
            
            // Render State
            Cull Off
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 4.5
                #pragma exclude_renderers gles gles3 glcore
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma shader_feature _ EDITOR_VISUALIZATION
                #pragma shader_feature_local_fragment _ _ALPHATEST_ON
                #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_VIEWDIRECTION_WS
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_META
                #define _FOG_FRAGMENT 1
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                     float4 uv1 : TEXCOORD1;
                     float4 uv2 : TEXCOORD2;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 normalWS;
                     float4 texCoord0;
                     float4 texCoord1;
                     float4 texCoord2;
                     float3 viewDirectionWS;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 WorldSpaceNormal;
                     float3 ObjectSpaceViewDirection;
                     float3 WorldSpaceViewDirection;
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float3 interp0 : INTERP0;
                     float4 interp1 : INTERP1;
                     float4 interp2 : INTERP2;
                     float4 interp3 : INTERP3;
                     float3 interp4 : INTERP4;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyz =  input.normalWS;
                    output.interp1.xyzw =  input.texCoord0;
                    output.interp2.xyzw =  input.texCoord1;
                    output.interp3.xyzw =  input.texCoord2;
                    output.interp4.xyz =  input.viewDirectionWS;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.normalWS = input.interp0.xyz;
                    output.texCoord0 = input.interp1.xyzw;
                    output.texCoord1 = input.interp2.xyzw;
                    output.texCoord2 = input.interp3.xyzw;
                    output.viewDirectionWS = input.interp4.xyz;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
                
                void Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power, out float Out)
                {
                    Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
                }
                
                void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
                {
                    Out = lerp(A, B, T);
                }
                
                void Unity_Add_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A + B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 BaseColor;
                    float3 Emission;
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    float4 _Property_a45d582cfcc6470bbc159ec46aee4232_Out_0 = IsGammaSpace() ? LinearToSRGB(_Emission_Color) : _Emission_Color;
                    UnityTexture2D _Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0 = UnityBuildTexture2DStruct(_Emission_Map);
                    float4 _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0 = SAMPLE_TEXTURE2D(_Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0.tex, _Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0.samplerstate, _Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_R_4 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.r;
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_G_5 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.g;
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_B_6 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.b;
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_A_7 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.a;
                    float4 _Multiply_ba04fd89b7814d8fb113ccad3d4c8983_Out_2;
                    Unity_Multiply_float4_float4(_Property_a45d582cfcc6470bbc159ec46aee4232_Out_0, _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0, _Multiply_ba04fd89b7814d8fb113ccad3d4c8983_Out_2);
                    UnityTextureCube _Property_3faaacbd799c4183ac76e7734fa12f06_Out_0 = UnityBuildTextureCubeStruct(_ReflectionMap);
                    float _Property_66fcc8bf022b426f8caf633f5047a880_Out_0 = _ReflectionBlur;
                    float4 _SampleReflectedCubemap_6f488a8de9824970ae8293b6b7cdb249_Out_0 = SAMPLE_TEXTURECUBE_LOD(_Property_3faaacbd799c4183ac76e7734fa12f06_Out_0.tex, _Property_3faaacbd799c4183ac76e7734fa12f06_Out_0.samplerstate, reflect(-IN.ObjectSpaceViewDirection, IN.ObjectSpaceNormal), _Property_66fcc8bf022b426f8caf633f5047a880_Out_0);
                    float _Property_b3ddb2e217d44708b4809f38a7076c13_Out_0 = _ReflectionStrength;
                    float4 _Multiply_6e83b7bead5a4a58820cabea3070544b_Out_2;
                    Unity_Multiply_float4_float4(_SampleReflectedCubemap_6f488a8de9824970ae8293b6b7cdb249_Out_0, (_Property_b3ddb2e217d44708b4809f38a7076c13_Out_0.xxxx), _Multiply_6e83b7bead5a4a58820cabea3070544b_Out_2);
                    float _Property_a341c49e136544dba79e08df1500f38a_Out_0 = _Fresnel;
                    float _FresnelEffect_17988048113d428e8e20e61b86e4dd7e_Out_3;
                    Unity_FresnelEffect_float(IN.WorldSpaceNormal, IN.WorldSpaceViewDirection, _Property_a341c49e136544dba79e08df1500f38a_Out_0, _FresnelEffect_17988048113d428e8e20e61b86e4dd7e_Out_3);
                    float4 _Multiply_963350bef93a4c75bccae43d4fa195a0_Out_2;
                    Unity_Multiply_float4_float4(_Multiply_6e83b7bead5a4a58820cabea3070544b_Out_2, (_FresnelEffect_17988048113d428e8e20e61b86e4dd7e_Out_3.xxxx), _Multiply_963350bef93a4c75bccae43d4fa195a0_Out_2);
                    float _Property_923a8c2c4f594bb599a1ec81f0a14dc3_Out_0 = _Metallic;
                    float4 _Lerp_c4a3f1e42e45428685b72e4fea51760f_Out_3;
                    Unity_Lerp_float4(float4(1, 1, 1, 1), _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2, (_Property_923a8c2c4f594bb599a1ec81f0a14dc3_Out_0.xxxx), _Lerp_c4a3f1e42e45428685b72e4fea51760f_Out_3);
                    float4 _Multiply_cb475d772e1b4b859c3cb8ddcdc638be_Out_2;
                    Unity_Multiply_float4_float4(_Multiply_963350bef93a4c75bccae43d4fa195a0_Out_2, _Lerp_c4a3f1e42e45428685b72e4fea51760f_Out_3, _Multiply_cb475d772e1b4b859c3cb8ddcdc638be_Out_2);
                    float4 _Add_ed388982523a4feeb56764cbb134f284_Out_2;
                    Unity_Add_float4(_Multiply_ba04fd89b7814d8fb113ccad3d4c8983_Out_2, _Multiply_cb475d772e1b4b859c3cb8ddcdc638be_Out_2, _Add_ed388982523a4feeb56764cbb134f284_Out_2);
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.BaseColor = (_Multiply_b57de817a55745fd8c03aba79eba566e_Out_2.xyz);
                    surface.Emission = (_Add_ed388982523a4feeb56764cbb134f284_Out_2.xyz);
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
                    float3 unnormalizedNormalWS = input.normalWS;
                    const float renormFactor = 1.0 / length(unnormalizedNormalWS);
                
                
                    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
                    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
                
                
                    output.WorldSpaceViewDirection = normalize(input.viewDirectionWS);
                    output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "SceneSelectionPass"
                Tags
                {
                    "LightMode" = "SceneSelectionPass"
                }
            
            // Render State
            Cull Off
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 4.5
                #pragma exclude_renderers gles gles3 glcore
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHONLY
                #define SCENESELECTIONPASS 1
                #define ALPHA_CLIP_THRESHOLD 1
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 interp0 : INTERP0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyzw =  input.texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.texCoord0 = input.interp0.xyzw;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                
                
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "ScenePickingPass"
                Tags
                {
                    "LightMode" = "Picking"
                }
            
            // Render State
            Cull [_Cull]
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 4.5
                #pragma exclude_renderers gles gles3 glcore
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHONLY
                #define SCENEPICKINGPASS 1
                #define ALPHA_CLIP_THRESHOLD 1
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 interp0 : INTERP0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyzw =  input.texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.texCoord0 = input.interp0.xyzw;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                
                
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                // Name: <None>
                Tags
                {
                    "LightMode" = "Universal2D"
                }
            
            // Render State
            Cull [_Cull]
                Blend [_SrcBlend] [_DstBlend]
                ZTest [_ZTest]
                ZWrite [_ZWrite]
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 4.5
                #pragma exclude_renderers gles gles3 glcore
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_2D
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 interp0 : INTERP0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyzw =  input.texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.texCoord0 = input.interp0.xyzw;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 BaseColor;
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.BaseColor = (_Multiply_b57de817a55745fd8c03aba79eba566e_Out_2.xyz);
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                
                
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
        }
        SubShader
        {
            Tags
            {
                "RenderPipeline"="UniversalPipeline"
                "RenderType"="Opaque"
                "UniversalMaterialType" = "Lit"
                "Queue"="Geometry"
                "ShaderGraphShader"="true"
                "ShaderGraphTargetId"="UniversalLitSubTarget"
            }
            Pass
            {
                Name "Universal Forward"
                Tags
                {
                    "LightMode" = "UniversalForward"
                }
            
            // Render State
            Cull [_Cull]
                Blend [_SrcBlend] [_DstBlend]
                ZTest [_ZTest]
                ZWrite [_ZWrite]
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma only_renderers gles gles3 glcore d3d11
                #pragma multi_compile_instancing
                #pragma multi_compile_fog
                #pragma instancing_options renderinglayer
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
                #pragma multi_compile _ LIGHTMAP_ON
                #pragma multi_compile _ DYNAMICLIGHTMAP_ON
                #pragma multi_compile _ DIRLIGHTMAP_COMBINED
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
                #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
                #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
                #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
                #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
                #pragma multi_compile _ _SHADOWS_SOFT
                #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
                #pragma multi_compile _ SHADOWS_SHADOWMASK
                #pragma multi_compile _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
                #pragma multi_compile _ _LIGHT_LAYERS
                #pragma multi_compile _ DEBUG_DISPLAY
                #pragma multi_compile _ _LIGHT_COOKIES
                #pragma multi_compile _ _CLUSTERED_RENDERING
                #pragma shader_feature_fragment _ _SURFACE_TYPE_TRANSPARENT
                #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON
                #pragma shader_feature_local_fragment _ _ALPHATEST_ON
                #pragma shader_feature_local_fragment _ _SPECULAR_SETUP
                #pragma shader_feature_local _ _RECEIVE_SHADOWS_OFF
                #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_VIEWDIRECTION_WS
            #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
            #define VARYINGS_NEED_SHADOW_COORD
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_FORWARD
                #define _FOG_FRAGMENT 1
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                     float4 uv1 : TEXCOORD1;
                     float4 uv2 : TEXCOORD2;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 positionWS;
                     float3 normalWS;
                     float4 tangentWS;
                     float4 texCoord0;
                     float3 viewDirectionWS;
                    #if defined(LIGHTMAP_ON)
                     float2 staticLightmapUV;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                     float2 dynamicLightmapUV;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                     float3 sh;
                    #endif
                     float4 fogFactorAndVertexLight;
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                     float4 shadowCoord;
                    #endif
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 WorldSpaceNormal;
                     float3 TangentSpaceNormal;
                     float3 ObjectSpaceViewDirection;
                     float3 WorldSpaceViewDirection;
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float3 interp0 : INTERP0;
                     float3 interp1 : INTERP1;
                     float4 interp2 : INTERP2;
                     float4 interp3 : INTERP3;
                     float3 interp4 : INTERP4;
                     float2 interp5 : INTERP5;
                     float2 interp6 : INTERP6;
                     float3 interp7 : INTERP7;
                     float4 interp8 : INTERP8;
                     float4 interp9 : INTERP9;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyz =  input.positionWS;
                    output.interp1.xyz =  input.normalWS;
                    output.interp2.xyzw =  input.tangentWS;
                    output.interp3.xyzw =  input.texCoord0;
                    output.interp4.xyz =  input.viewDirectionWS;
                    #if defined(LIGHTMAP_ON)
                    output.interp5.xy =  input.staticLightmapUV;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                    output.interp6.xy =  input.dynamicLightmapUV;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                    output.interp7.xyz =  input.sh;
                    #endif
                    output.interp8.xyzw =  input.fogFactorAndVertexLight;
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.interp9.xyzw =  input.shadowCoord;
                    #endif
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.positionWS = input.interp0.xyz;
                    output.normalWS = input.interp1.xyz;
                    output.tangentWS = input.interp2.xyzw;
                    output.texCoord0 = input.interp3.xyzw;
                    output.viewDirectionWS = input.interp4.xyz;
                    #if defined(LIGHTMAP_ON)
                    output.staticLightmapUV = input.interp5.xy;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                    output.dynamicLightmapUV = input.interp6.xy;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                    output.sh = input.interp7.xyz;
                    #endif
                    output.fogFactorAndVertexLight = input.interp8.xyzw;
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = input.interp9.xyzw;
                    #endif
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
                
                void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
                {
                    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
                }
                
                void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
                {
                    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
                }
                
                void Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power, out float Out)
                {
                    Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
                }
                
                void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
                {
                    Out = lerp(A, B, T);
                }
                
                void Unity_Add_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A + B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 BaseColor;
                    float3 NormalTS;
                    float3 Emission;
                    float Metallic;
                    float3 Specular;
                    float Smoothness;
                    float Occlusion;
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    UnityTexture2D _Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0 = UnityBuildTexture2DStruct(_Normal_Map);
                    #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
                      float4 _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
                    #else
                      float4 _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0.tex, _Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0.samplerstate, _Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
                    #endif
                    _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.rgb = UnpackNormal(_SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0);
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_R_5 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.r;
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_G_6 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.g;
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_B_7 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.b;
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_A_8 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.a;
                    float _Property_8b766139f69a45838f9c3225f987b805_Out_0 = _NormalStrength;
                    float3 _NormalStrength_35f15cab4e5b40bfb98ab5c553b148f2_Out_2;
                    Unity_NormalStrength_float((_SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.xyz), _Property_8b766139f69a45838f9c3225f987b805_Out_0, _NormalStrength_35f15cab4e5b40bfb98ab5c553b148f2_Out_2);
                    float3 _NormalBlend_53d463b3a3d149b2b8781e24e525f79d_Out_2;
                    Unity_NormalBlend_float(IN.ObjectSpaceNormal, _NormalStrength_35f15cab4e5b40bfb98ab5c553b148f2_Out_2, _NormalBlend_53d463b3a3d149b2b8781e24e525f79d_Out_2);
                    float4 _Property_a45d582cfcc6470bbc159ec46aee4232_Out_0 = IsGammaSpace() ? LinearToSRGB(_Emission_Color) : _Emission_Color;
                    UnityTexture2D _Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0 = UnityBuildTexture2DStruct(_Emission_Map);
                    float4 _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0 = SAMPLE_TEXTURE2D(_Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0.tex, _Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0.samplerstate, _Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_R_4 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.r;
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_G_5 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.g;
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_B_6 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.b;
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_A_7 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.a;
                    float4 _Multiply_ba04fd89b7814d8fb113ccad3d4c8983_Out_2;
                    Unity_Multiply_float4_float4(_Property_a45d582cfcc6470bbc159ec46aee4232_Out_0, _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0, _Multiply_ba04fd89b7814d8fb113ccad3d4c8983_Out_2);
                    UnityTextureCube _Property_3faaacbd799c4183ac76e7734fa12f06_Out_0 = UnityBuildTextureCubeStruct(_ReflectionMap);
                    float _Property_66fcc8bf022b426f8caf633f5047a880_Out_0 = _ReflectionBlur;
                    float4 _SampleReflectedCubemap_6f488a8de9824970ae8293b6b7cdb249_Out_0 = SAMPLE_TEXTURECUBE_LOD(_Property_3faaacbd799c4183ac76e7734fa12f06_Out_0.tex, _Property_3faaacbd799c4183ac76e7734fa12f06_Out_0.samplerstate, reflect(-IN.ObjectSpaceViewDirection, IN.ObjectSpaceNormal), _Property_66fcc8bf022b426f8caf633f5047a880_Out_0);
                    float _Property_b3ddb2e217d44708b4809f38a7076c13_Out_0 = _ReflectionStrength;
                    float4 _Multiply_6e83b7bead5a4a58820cabea3070544b_Out_2;
                    Unity_Multiply_float4_float4(_SampleReflectedCubemap_6f488a8de9824970ae8293b6b7cdb249_Out_0, (_Property_b3ddb2e217d44708b4809f38a7076c13_Out_0.xxxx), _Multiply_6e83b7bead5a4a58820cabea3070544b_Out_2);
                    float _Property_a341c49e136544dba79e08df1500f38a_Out_0 = _Fresnel;
                    float _FresnelEffect_17988048113d428e8e20e61b86e4dd7e_Out_3;
                    Unity_FresnelEffect_float(IN.WorldSpaceNormal, IN.WorldSpaceViewDirection, _Property_a341c49e136544dba79e08df1500f38a_Out_0, _FresnelEffect_17988048113d428e8e20e61b86e4dd7e_Out_3);
                    float4 _Multiply_963350bef93a4c75bccae43d4fa195a0_Out_2;
                    Unity_Multiply_float4_float4(_Multiply_6e83b7bead5a4a58820cabea3070544b_Out_2, (_FresnelEffect_17988048113d428e8e20e61b86e4dd7e_Out_3.xxxx), _Multiply_963350bef93a4c75bccae43d4fa195a0_Out_2);
                    float _Property_923a8c2c4f594bb599a1ec81f0a14dc3_Out_0 = _Metallic;
                    float4 _Lerp_c4a3f1e42e45428685b72e4fea51760f_Out_3;
                    Unity_Lerp_float4(float4(1, 1, 1, 1), _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2, (_Property_923a8c2c4f594bb599a1ec81f0a14dc3_Out_0.xxxx), _Lerp_c4a3f1e42e45428685b72e4fea51760f_Out_3);
                    float4 _Multiply_cb475d772e1b4b859c3cb8ddcdc638be_Out_2;
                    Unity_Multiply_float4_float4(_Multiply_963350bef93a4c75bccae43d4fa195a0_Out_2, _Lerp_c4a3f1e42e45428685b72e4fea51760f_Out_3, _Multiply_cb475d772e1b4b859c3cb8ddcdc638be_Out_2);
                    float4 _Add_ed388982523a4feeb56764cbb134f284_Out_2;
                    Unity_Add_float4(_Multiply_ba04fd89b7814d8fb113ccad3d4c8983_Out_2, _Multiply_cb475d772e1b4b859c3cb8ddcdc638be_Out_2, _Add_ed388982523a4feeb56764cbb134f284_Out_2);
                    float _Property_dd2d4b2f1ffe45f28bd020d7ee60ba8e_Out_0 = _Metallic;
                    float _Property_2dbcf226c6b44efeb4a39bd557723f89_Out_0 = _Smoothness;
                    float _Property_52d78f7cc5504dc59468b0a041ec6df6_Out_0 = _AmbientOcclusion;
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.BaseColor = (_Multiply_b57de817a55745fd8c03aba79eba566e_Out_2.xyz);
                    surface.NormalTS = _NormalBlend_53d463b3a3d149b2b8781e24e525f79d_Out_2;
                    surface.Emission = (_Add_ed388982523a4feeb56764cbb134f284_Out_2.xyz);
                    surface.Metallic = _Property_dd2d4b2f1ffe45f28bd020d7ee60ba8e_Out_0;
                    surface.Specular = IsGammaSpace() ? float3(0.5, 0.5, 0.5) : SRGBToLinear(float3(0.5, 0.5, 0.5));
                    surface.Smoothness = _Property_2dbcf226c6b44efeb4a39bd557723f89_Out_0;
                    surface.Occlusion = _Property_52d78f7cc5504dc59468b0a041ec6df6_Out_0;
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
                    float3 unnormalizedNormalWS = input.normalWS;
                    const float renormFactor = 1.0 / length(unnormalizedNormalWS);
                
                
                    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
                    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
                    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
                
                
                    output.WorldSpaceViewDirection = normalize(input.viewDirectionWS);
                    output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "ShadowCaster"
                Tags
                {
                    "LightMode" = "ShadowCaster"
                }
            
            // Render State
            Cull [_Cull]
                ZTest LEqual
                ZWrite On
                ColorMask 0
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma only_renderers gles gles3 glcore d3d11
                #pragma multi_compile_instancing
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW
                #pragma shader_feature_local_fragment _ _ALPHATEST_ON
                #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_SHADOWCASTER
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 normalWS;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float3 interp0 : INTERP0;
                     float4 interp1 : INTERP1;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyz =  input.normalWS;
                    output.interp1.xyzw =  input.texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.normalWS = input.interp0.xyz;
                    output.texCoord0 = input.interp1.xyzw;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                
                
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "DepthOnly"
                Tags
                {
                    "LightMode" = "DepthOnly"
                }
            
            // Render State
            Cull [_Cull]
                ZTest LEqual
                ZWrite On
                ColorMask 0
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma only_renderers gles gles3 glcore d3d11
                #pragma multi_compile_instancing
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHONLY
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 interp0 : INTERP0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyzw =  input.texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.texCoord0 = input.interp0.xyzw;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                
                
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "DepthNormals"
                Tags
                {
                    "LightMode" = "DepthNormals"
                }
            
            // Render State
            Cull [_Cull]
                ZTest LEqual
                ZWrite On
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma only_renderers gles gles3 glcore d3d11
                #pragma multi_compile_instancing
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHNORMALS
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                     float4 uv1 : TEXCOORD1;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 normalWS;
                     float4 tangentWS;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 WorldSpaceNormal;
                     float3 TangentSpaceNormal;
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float3 interp0 : INTERP0;
                     float4 interp1 : INTERP1;
                     float4 interp2 : INTERP2;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyz =  input.normalWS;
                    output.interp1.xyzw =  input.tangentWS;
                    output.interp2.xyzw =  input.texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.normalWS = input.interp0.xyz;
                    output.tangentWS = input.interp1.xyzw;
                    output.texCoord0 = input.interp2.xyzw;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
                {
                    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
                }
                
                void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
                {
                    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
                }
                
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 NormalTS;
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    UnityTexture2D _Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0 = UnityBuildTexture2DStruct(_Normal_Map);
                    #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
                      float4 _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
                    #else
                      float4 _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0.tex, _Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0.samplerstate, _Property_04f8fce8d6ce4e929224e39afe1b60e5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
                    #endif
                    _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.rgb = UnpackNormal(_SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0);
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_R_5 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.r;
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_G_6 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.g;
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_B_7 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.b;
                    float _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_A_8 = _SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.a;
                    float _Property_8b766139f69a45838f9c3225f987b805_Out_0 = _NormalStrength;
                    float3 _NormalStrength_35f15cab4e5b40bfb98ab5c553b148f2_Out_2;
                    Unity_NormalStrength_float((_SampleTexture2DLOD_02314d228273486ab4318d393afa10ef_RGBA_0.xyz), _Property_8b766139f69a45838f9c3225f987b805_Out_0, _NormalStrength_35f15cab4e5b40bfb98ab5c553b148f2_Out_2);
                    float3 _NormalBlend_53d463b3a3d149b2b8781e24e525f79d_Out_2;
                    Unity_NormalBlend_float(IN.ObjectSpaceNormal, _NormalStrength_35f15cab4e5b40bfb98ab5c553b148f2_Out_2, _NormalBlend_53d463b3a3d149b2b8781e24e525f79d_Out_2);
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.NormalTS = _NormalBlend_53d463b3a3d149b2b8781e24e525f79d_Out_2;
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
                    float3 unnormalizedNormalWS = input.normalWS;
                    const float renormFactor = 1.0 / length(unnormalizedNormalWS);
                
                
                    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
                    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
                    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
                
                
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "Meta"
                Tags
                {
                    "LightMode" = "Meta"
                }
            
            // Render State
            Cull Off
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma only_renderers gles gles3 glcore d3d11
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma shader_feature _ EDITOR_VISUALIZATION
                #pragma shader_feature_local_fragment _ _ALPHATEST_ON
                #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_VIEWDIRECTION_WS
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_META
                #define _FOG_FRAGMENT 1
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                     float4 uv1 : TEXCOORD1;
                     float4 uv2 : TEXCOORD2;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 normalWS;
                     float4 texCoord0;
                     float4 texCoord1;
                     float4 texCoord2;
                     float3 viewDirectionWS;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 WorldSpaceNormal;
                     float3 ObjectSpaceViewDirection;
                     float3 WorldSpaceViewDirection;
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float3 interp0 : INTERP0;
                     float4 interp1 : INTERP1;
                     float4 interp2 : INTERP2;
                     float4 interp3 : INTERP3;
                     float3 interp4 : INTERP4;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyz =  input.normalWS;
                    output.interp1.xyzw =  input.texCoord0;
                    output.interp2.xyzw =  input.texCoord1;
                    output.interp3.xyzw =  input.texCoord2;
                    output.interp4.xyz =  input.viewDirectionWS;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.normalWS = input.interp0.xyz;
                    output.texCoord0 = input.interp1.xyzw;
                    output.texCoord1 = input.interp2.xyzw;
                    output.texCoord2 = input.interp3.xyzw;
                    output.viewDirectionWS = input.interp4.xyz;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
                
                void Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power, out float Out)
                {
                    Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
                }
                
                void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
                {
                    Out = lerp(A, B, T);
                }
                
                void Unity_Add_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A + B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 BaseColor;
                    float3 Emission;
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    float4 _Property_a45d582cfcc6470bbc159ec46aee4232_Out_0 = IsGammaSpace() ? LinearToSRGB(_Emission_Color) : _Emission_Color;
                    UnityTexture2D _Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0 = UnityBuildTexture2DStruct(_Emission_Map);
                    float4 _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0 = SAMPLE_TEXTURE2D(_Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0.tex, _Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0.samplerstate, _Property_1b3eb446b9e54fb3bb5fceb38de040ae_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_R_4 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.r;
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_G_5 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.g;
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_B_6 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.b;
                    float _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_A_7 = _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0.a;
                    float4 _Multiply_ba04fd89b7814d8fb113ccad3d4c8983_Out_2;
                    Unity_Multiply_float4_float4(_Property_a45d582cfcc6470bbc159ec46aee4232_Out_0, _SampleTexture2D_f0549c0e3d564362ba9b7426c74f3d98_RGBA_0, _Multiply_ba04fd89b7814d8fb113ccad3d4c8983_Out_2);
                    UnityTextureCube _Property_3faaacbd799c4183ac76e7734fa12f06_Out_0 = UnityBuildTextureCubeStruct(_ReflectionMap);
                    float _Property_66fcc8bf022b426f8caf633f5047a880_Out_0 = _ReflectionBlur;
                    float4 _SampleReflectedCubemap_6f488a8de9824970ae8293b6b7cdb249_Out_0 = SAMPLE_TEXTURECUBE_LOD(_Property_3faaacbd799c4183ac76e7734fa12f06_Out_0.tex, _Property_3faaacbd799c4183ac76e7734fa12f06_Out_0.samplerstate, reflect(-IN.ObjectSpaceViewDirection, IN.ObjectSpaceNormal), _Property_66fcc8bf022b426f8caf633f5047a880_Out_0);
                    float _Property_b3ddb2e217d44708b4809f38a7076c13_Out_0 = _ReflectionStrength;
                    float4 _Multiply_6e83b7bead5a4a58820cabea3070544b_Out_2;
                    Unity_Multiply_float4_float4(_SampleReflectedCubemap_6f488a8de9824970ae8293b6b7cdb249_Out_0, (_Property_b3ddb2e217d44708b4809f38a7076c13_Out_0.xxxx), _Multiply_6e83b7bead5a4a58820cabea3070544b_Out_2);
                    float _Property_a341c49e136544dba79e08df1500f38a_Out_0 = _Fresnel;
                    float _FresnelEffect_17988048113d428e8e20e61b86e4dd7e_Out_3;
                    Unity_FresnelEffect_float(IN.WorldSpaceNormal, IN.WorldSpaceViewDirection, _Property_a341c49e136544dba79e08df1500f38a_Out_0, _FresnelEffect_17988048113d428e8e20e61b86e4dd7e_Out_3);
                    float4 _Multiply_963350bef93a4c75bccae43d4fa195a0_Out_2;
                    Unity_Multiply_float4_float4(_Multiply_6e83b7bead5a4a58820cabea3070544b_Out_2, (_FresnelEffect_17988048113d428e8e20e61b86e4dd7e_Out_3.xxxx), _Multiply_963350bef93a4c75bccae43d4fa195a0_Out_2);
                    float _Property_923a8c2c4f594bb599a1ec81f0a14dc3_Out_0 = _Metallic;
                    float4 _Lerp_c4a3f1e42e45428685b72e4fea51760f_Out_3;
                    Unity_Lerp_float4(float4(1, 1, 1, 1), _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2, (_Property_923a8c2c4f594bb599a1ec81f0a14dc3_Out_0.xxxx), _Lerp_c4a3f1e42e45428685b72e4fea51760f_Out_3);
                    float4 _Multiply_cb475d772e1b4b859c3cb8ddcdc638be_Out_2;
                    Unity_Multiply_float4_float4(_Multiply_963350bef93a4c75bccae43d4fa195a0_Out_2, _Lerp_c4a3f1e42e45428685b72e4fea51760f_Out_3, _Multiply_cb475d772e1b4b859c3cb8ddcdc638be_Out_2);
                    float4 _Add_ed388982523a4feeb56764cbb134f284_Out_2;
                    Unity_Add_float4(_Multiply_ba04fd89b7814d8fb113ccad3d4c8983_Out_2, _Multiply_cb475d772e1b4b859c3cb8ddcdc638be_Out_2, _Add_ed388982523a4feeb56764cbb134f284_Out_2);
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.BaseColor = (_Multiply_b57de817a55745fd8c03aba79eba566e_Out_2.xyz);
                    surface.Emission = (_Add_ed388982523a4feeb56764cbb134f284_Out_2.xyz);
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
                    float3 unnormalizedNormalWS = input.normalWS;
                    const float renormFactor = 1.0 / length(unnormalizedNormalWS);
                
                
                    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
                    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
                
                
                    output.WorldSpaceViewDirection = normalize(input.viewDirectionWS);
                    output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "SceneSelectionPass"
                Tags
                {
                    "LightMode" = "SceneSelectionPass"
                }
            
            // Render State
            Cull Off
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma only_renderers gles gles3 glcore d3d11
                #pragma multi_compile_instancing
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHONLY
                #define SCENESELECTIONPASS 1
                #define ALPHA_CLIP_THRESHOLD 1
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 interp0 : INTERP0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyzw =  input.texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.texCoord0 = input.interp0.xyzw;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                
                
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "ScenePickingPass"
                Tags
                {
                    "LightMode" = "Picking"
                }
            
            // Render State
            Cull [_Cull]
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma only_renderers gles gles3 glcore d3d11
                #pragma multi_compile_instancing
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHONLY
                #define SCENEPICKINGPASS 1
                #define ALPHA_CLIP_THRESHOLD 1
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 interp0 : INTERP0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyzw =  input.texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.texCoord0 = input.interp0.xyzw;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                
                
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                // Name: <None>
                Tags
                {
                    "LightMode" = "Universal2D"
                }
            
            // Render State
            Cull [_Cull]
                Blend [_SrcBlend] [_DstBlend]
                ZTest [_ZTest]
                ZWrite [_ZWrite]
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma only_renderers gles gles3 glcore d3d11
                #pragma multi_compile_instancing
                #pragma vertex vert
                #pragma fragment frag
            
            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>
            
            // Keywords
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_2D
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 interp0 : INTERP0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.interp0.xyzw =  input.texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.texCoord0 = input.interp0.xyzw;
                    #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_TexelSize;
                float4 _BaseTexture_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _AmbientOcclusion;
                float _AlphaClipThreshold;
                float4 _Normal_Map_TexelSize;
                float4 _Normal_Map_ST;
                float4 _Emission_Color;
                float4 _Emission_Map_TexelSize;
                float4 _Emission_Map_ST;
                float _ReflectionStrength;
                float _Fresnel;
                float _ReflectionBlur;
                float _NormalStrength;
                CBUFFER_END
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_BaseTexture);
                SAMPLER(sampler_BaseTexture);
                TEXTURE2D(_Normal_Map);
                SAMPLER(sampler_Normal_Map);
                TEXTURECUBE(_ReflectionMap);
                SAMPLER(sampler_ReflectionMap);
                TEXTURE2D(_Emission_Map);
                SAMPLER(sampler_Emission_Map);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 BaseColor;
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_6a1adda745294d059fffadfaa863638c_Out_0 = IsGammaSpace() ? LinearToSRGB(_BaseColor) : _BaseColor;
                    UnityTexture2D _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0 = UnityBuildTexture2DStruct(_BaseTexture);
                    float4 _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0 = SAMPLE_TEXTURE2D(_Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.tex, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.samplerstate, _Property_4e579567d25a40f99cc05baf1b086c1f_Out_0.GetTransformedUV(IN.uv0.xy));
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_R_4 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.r;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_G_5 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.g;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_B_6 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.b;
                    float _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_A_7 = _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0.a;
                    float4 _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2;
                    Unity_Multiply_float4_float4(_Property_6a1adda745294d059fffadfaa863638c_Out_0, _SampleTexture2D_d831fcca0a814660807b2e939539e4f8_RGBA_0, _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2);
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_R_1 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[0];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_G_2 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[1];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_B_3 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[2];
                    float _Split_e3ef57d67fc5429498762ceb5acabc91_A_4 = _Multiply_b57de817a55745fd8c03aba79eba566e_Out_2[3];
                    float _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0 = _AlphaClipThreshold;
                    surface.BaseColor = (_Multiply_b57de817a55745fd8c03aba79eba566e_Out_2.xyz);
                    surface.Alpha = _Split_e3ef57d67fc5429498762ceb5acabc91_A_4;
                    surface.AlphaClipThreshold = _Property_4fc2498a850c41a8a7526f9d97e2552c_Out_0;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                    // FragInputs from VFX come from two places: Interpolator or CBuffer.
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                
                
                    output.uv0 = input.texCoord0;
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
        }
        CustomEditorForRenderPipeline "UnityEditor.ShaderGraphLitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
        CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
        FallBack "Hidden/Shader Graph/FallbackError"
    }