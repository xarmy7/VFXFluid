
void ApplyVelocityField(inout VFXAttributes attributes, VFXSampler2D velocityField, in float advection)
{
    float2 pos = (-attributes.position.xz + 5.0) / 10.0;
    float4 v = velocityField.t.SampleLevel(velocityField.s, pos, 0.0);
        
    attributes.velocity = float3(advection* -v.r, 0.0, advection * -v.g);
}