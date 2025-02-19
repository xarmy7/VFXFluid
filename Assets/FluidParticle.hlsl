void MonGrosProut(inout VFXAttributes attributes, in VFXSampler2D velocityField, in int particleCount)
{
    attributes.velocity.xyz = SampleTexture(velocityField, attributes.position.xyz);
}
