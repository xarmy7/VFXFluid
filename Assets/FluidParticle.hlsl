
void ApplyVelocityField(inout VFXAttributes attributes, VFXSampler2D velocityField, in float advection, in float3 planeOffset)
{
    float2 pos = planeOffset.xz - attributes.position.xz;
    pos = (pos + 5.0) * 0.1;
    
    float4 v = velocityField.t.SampleLevel(velocityField.s, pos, 0.0);
        
    attributes.color = abs(v);
    attributes.velocity = float3(advection * -v.r, 0.0, advection * -v.g);
    attributes.position.y = max(0.0, length(normalize(abs(v)).rgb));
}