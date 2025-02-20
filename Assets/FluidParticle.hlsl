
void FillBufferWithParticles(inout VFXAttributes attributes, in RWStructuredBuffer<float4> buffer, in int particleID)
{
    int id = particleID;
    // float4 : position (xyz), float : velocity
    buffer[id] = float4(attributes.position, length(attributes.velocity));
}

void ApplyDensityVelocity(inout VFXAttributes attributes, RWStructuredBuffer<float4> buffer, in int particleCount, in float repelForce, in float densityRadius)
{
    float3 pos = attributes.position;
    
    int maxInfluence = 5;
    
    float3 repelDir = float3(0, 0, 0);
    for (int i = 0; i < particleCount; ++i)
    {
        if (maxInfluence <= 0)
            break;
        
        float3 dir = buffer[i].xyz - pos;
        float len = length(dir);
        if (len >= densityRadius)
            continue;

        repelDir -= dir * buffer[i].w;
        --maxInfluence;
    }

    attributes.position += repelDir * repelForce;
}

void ApplyVelocityField(inout VFXAttributes attributes, VFXSampler2D velocityField, in float advection, in float3 planeOffset)
{
    float2 pos = planeOffset.xz - attributes.position.xz;
    // TODO : use plane scale value from Fluid.cs
    pos = (pos + 5.0) * 0.1;
    
    float4 v = velocityField.t.SampleLevel(velocityField.s, pos, 0.0);
        
    attributes.color = abs(v);
    attributes.velocity = float3(advection * -v.r, 0.0, advection * -v.g);
    attributes.position.y = max(0.0, length(normalize(abs(v)).rgb));
}