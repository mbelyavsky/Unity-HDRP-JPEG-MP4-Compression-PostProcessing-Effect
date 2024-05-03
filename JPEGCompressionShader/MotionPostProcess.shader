Shader "Hidden/Shader/MotionPostProcess"
{
    Properties
    {
        // This property is necessary to make the CommandBuffer.Blit bind the source texture to _MainTex
        _MainTex("Main Texture", 2DArray) = "grey" {}
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

    

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    // List of properties to control your post process effect
    //float _Intensity;
    TEXTURE2D_X(_MainTex);
    //TEXTURE2D_X(_CameraMotionVectorsTexture);

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        // Note that if HDUtils.DrawFullScreen is not used to render the post process, you don't need to call ClampAndScaleUVForBilinearPostProcessTexture.

        //float4 sourceColor = SAMPLE_TEXTURE2D_X(_MainTex, s_linear_clamp_sampler, ClampAndScaleUVForBilinearPostProcessTexture(input.texcoord.xy)).xyz;
        //float4 motionColor = SAMPLE_TEXTURE2D_X(_CameraMotionVectorsTexture, s_linear_clamp_sampler, ClampAndScaleUVForBilinearPostProcessTexture(input.texcoord.xy)).xyza;
       // float4 motionVecBufferSample = LOAD_TEXTURE2D_X(_CameraMotionVectorsTexture, ClampAndScaleUVForBilinearPostProcessTexture(input.texcoord.xy));
        
        // Apply greyscale effect
        //float3 color = lerp(sourceColor, Luminance(sourceColor), _Intensity);

        //return float4(color, 1);

        float3 sourceColor = SAMPLE_TEXTURE2D_X(_MainTex, s_linear_clamp_sampler, input.texcoord).xyz;
        float2 motionVectorTexture;
        //DecodeMotionVector(LOAD_TEXTURE2D_X(_CameraMotionVectorsTexture, input.texcoord), motionVectorTexture);
        DecodeMotionVector(SAMPLE_TEXTURE2D_X(_CameraMotionVectorsTexture, s_linear_clamp_sampler, input.texcoord), motionVectorTexture);
        float4 motionVectorTextureTrue = SAMPLE_TEXTURE2D_X(_CameraMotionVectorsTexture, s_linear_clamp_sampler, input.texcoord);
        float4 outt = float4(sourceColor.xyz/6 + motionVectorTextureTrue.xyz, 1);
        return float4(motionVectorTexture.xy, 0, 1);
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "MotionPostProcess"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment CustomPostProcess
                #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}
