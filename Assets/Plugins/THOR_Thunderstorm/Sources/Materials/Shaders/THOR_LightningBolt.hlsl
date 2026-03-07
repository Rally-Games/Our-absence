void Surf_float
(
    float2 UV,
    float  Depth,
    float  FarPlane,
    float4 ScreenPos,

    out float3 Color
)
{
    float depthBlend = 1;
    if (_EnableDepthBlend >= 0.5)//#if defined(_ENABLEDEPTHBLEND)
    {
        depthBlend = saturate((Depth * FarPlane - ScreenPos.w) / _DepthBlend);
    }
    //#endif

    float alpha = _Alpha.Sample(sampler_Alpha, UV).a;
    alpha *= alpha;
    float  alphaPow = alpha * alpha;

    float3 color = _ColorCore.rgb * depthBlend * alphaPow + (1 - alphaPow) * _ColorGlow.rgb;

    float  evolution = saturate(_Evolution * -1.633333 + 1.653333);
    evolution = evolution * evolution * evolution;

    float evolution2 = 1.0 - saturate(_Evolution * 4.0);

    float uvAnimation = saturate(1 + -(UV.y - evolution2) / ((evolution2 - 0.1) - evolution2));
    uvAnimation *= uvAnimation;

    float3 finalColor = color * clamp(_Evolution * 7.5 + 3.0, 3.0, 10.0) * evolution * alpha * uvAnimation;

    Color = finalColor * _Multi * depthBlend;
}