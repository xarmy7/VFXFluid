
void ApplyVelocityField(inout VFXAttributes attributes, VFXSampler2D velocityField)
{
    float4 v = velocityField.t.SampleLevel(velocityField.s, attributes.position.xy, 0.0);
    attributes.velocity = float3(v.x, 0.0, v.y);
}