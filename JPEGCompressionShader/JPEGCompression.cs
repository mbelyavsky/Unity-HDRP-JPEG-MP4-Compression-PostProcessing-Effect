using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;
using UnityEngine.Experimental.Rendering;

[Serializable, VolumeComponentMenu("Post-processing/Custom/JPEGCompression")]
public sealed class JPEGCompression : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Header("Compute Shader - Insert Compute Shader")]
    [Tooltip("Insert Compute Shader")]
    public ComputeShaderParameter JPEGComputeShaderParameter = new ComputeShaderParameter(null);
    
    [Header("Spatial Compression")]
    [Tooltip("Use Spatial Compression")]
    public BoolParameter useSpatialCompression = new BoolParameter (true);
    
    [Tooltip("Updates on Play")]
    public IntParameter screenDownsampling = new IntParameter (0);
    
    [Tooltip("Updates on Play")]
    public BoolParameter usePointFiltering = new BoolParameter (false);
    
    [Range(0.0f, 2.0f), Tooltip("Compression Threshold")]
    public FloatParameter compressionThreshold = new FloatParameter (0.0f);
    
    [Tooltip("Performance Mode")]
    public ClampedIntParameter fastPerformanceMode = new ClampedIntParameter(0,0,1);
    
    [Header("Temporal Compression (Playmode Only)")]
    
    [Tooltip("Use Temporal Compression")]
    public BoolParameter useTemporalCompression = new BoolParameter (false);
    
    [Tooltip("Use I-Frames")]
    public BoolParameter useIFrames = new BoolParameter (true);
    
    [Tooltip("Number of predicted frames")]
    public IntParameter numBFrames = new IntParameter (8);
    
    [Range(0.0f, 1.0f), Tooltip("Bitrate")]
    public FloatParameter bitrate = new FloatParameter (1.0f);
    
    [Range(0.0f, 0.95f)]
    public FloatParameter bitrateArtifacts = new FloatParameter (0.0f);
    //--------------------------------------------------------------------------------------------
    
    private RenderTargetIdentifier motionFrameIdentifier;
    private RenderTexture motionFrame;
    
    private RenderTargetIdentifier lastFrameIdentifier;
    private RenderTexture lastFrame;
    
    private Material motionMaterial;
    private Material bypassMaterial;
    
    private Vector2Int dimensions;
    
    private int frameIndex;

    public bool IsActive() => JPEGComputeShaderParameter.value != null;

    public RenderTexture motionRenderOutTexture;

    RTHandle lastFromCamera;
    
    // Do not forget to add this post process in the Custom Post Process Orders list (Project Settings > Graphics > HDRP Global Settings).
    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;
    
    //--------------------------------------------------------------------------------------------

    public override void Setup()
    {
        Camera.main.depthTextureMode = DepthTextureMode.MotionVectors;
        
        motionMaterial = new Material(Shader.Find("Hidden/Shader/MotionPostProcess"));
        bypassMaterial = new Material(Shader.Find("Hidden/Shader/bypassShader"));
        
        int downSamplingRate = Mathf.Max(1, (screenDownsampling.value + 1));
        
        if(Camera.main.targetTexture == null)
            dimensions = new Vector2Int(Screen.width / downSamplingRate, Screen.height / downSamplingRate);
        else
            dimensions = new Vector2Int(Camera.main.targetTexture.width / downSamplingRate, 
                Camera.main.targetTexture.height / downSamplingRate);
        
        frameIndex = 0;
        
        motionFrame = new RenderTexture(dimensions.x, dimensions.y, 16, GraphicsFormat.R16G16B16A16_SNorm);
        motionFrame.enableRandomWrite = true;
        motionFrame.filterMode = usePointFiltering.value ? FilterMode.Point : FilterMode.Bilinear;
        motionFrame.Create();
        motionFrameIdentifier = new RenderTargetIdentifier(motionFrame);
        
        lastFrame = new RenderTexture(dimensions.x, dimensions.y, 16);
        lastFrame.filterMode = usePointFiltering.value ? FilterMode.Point : FilterMode.Bilinear;
        lastFrame.wrapMode = 0;
        lastFrame.Create();
        lastFrameIdentifier = new RenderTargetIdentifier(lastFrame);
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        lastFromCamera = camera.GetPreviousFrameRT(1);
        
        cmd.Blit(motionFrame, motionFrameIdentifier, motionMaterial);
        
        var JPEGComputeShader = JPEGComputeShaderParameter.value;
        var mainKernel = JPEGComputeShader.FindKernel("CSMain");
        JPEGComputeShader.GetKernelThreadGroupSizes(mainKernel, out uint xGroupSize, out uint yGroupSize, out _);
        cmd.SetComputeTextureParam(JPEGComputeShader, mainKernel, "Input", source.nameID);
        cmd.SetComputeTextureParam(JPEGComputeShader, mainKernel, "Result", destination.nameID);
        cmd.SetComputeTextureParam(JPEGComputeShader, mainKernel, "Last", lastFrameIdentifier);
        cmd.SetComputeTextureParam(JPEGComputeShader, mainKernel, "Motion", motionFrame);
        
        cmd.SetComputeIntParam(JPEGComputeShader, "FastPerfomanceMode", fastPerformanceMode.value);
        cmd.SetComputeFloatParam(JPEGComputeShader, "CompressionThreshold", compressionThreshold.value);
        
        cmd.SetComputeIntParam(JPEGComputeShader, "UseSpacial", Convert.ToInt32(useSpatialCompression.value));
        
        cmd.SetComputeIntParam(JPEGComputeShader, "UseTemporal", Convert.ToInt32(useTemporalCompression.value));
        
        cmd.SetComputeFloatParam(JPEGComputeShader, "Bitrate", bitrate.value);
        cmd.SetComputeFloatParam(JPEGComputeShader, "BitrateArtifacts", bitrateArtifacts.value);
        cmd.SetComputeIntParam(JPEGComputeShader, "ResultWidth", destination.rt.width);           //result width
        cmd.SetComputeIntParam(JPEGComputeShader, "ResultHeight", destination.rt.height);         //result hight
        
        if(frameIndex == 0 || !Application.isPlaying){
            cmd.SetComputeIntParam(JPEGComputeShader, "IsIFrame", 1);
            frameIndex = Mathf.Max(numBFrames.value, 0);
        }
        else{
            cmd.SetComputeIntParam(JPEGComputeShader, "IsIFrame", 0);
            if(useIFrames.value) frameIndex--;
        }
        
        cmd.DispatchCompute(JPEGComputeShader, mainKernel,
            Mathf.CeilToInt((destination.rt.width + (xGroupSize-1)) / xGroupSize),
            Mathf.CeilToInt((destination.rt.height + (yGroupSize-1)) / yGroupSize),
            1);
        
        cmd.Blit(destination, lastFrameIdentifier, bypassMaterial);
    }

    public override void Cleanup()
    {
        motionFrame.Release();
    }
}