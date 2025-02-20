
void ApplyVelocityField(inout VFXAttributes attributes, VFXSampler2D velocityField, in float advection, in float deltaTime)
{
    float2 pos = (-attributes.position.xz + 5.0) / 10.0;
    float4 v = velocityField.t.SampleLevel(velocityField.s, pos, 0.0);
    
    //float forceR = abs(v.r) * 10.0;
    //float forceG = abs(v.g) * 10.0;
    
    //attributes.velocity += (advection - attributes.velocity) * min(1.0f, 1 * deltaTime / attributes.mass);
    
    attributes.velocity = float3(advection* -v.r, 0.0, advection * -v.g);
}