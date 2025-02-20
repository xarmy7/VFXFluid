using StableFluids;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class ApplyFluidProperties : MonoBehaviour
{
    [SerializeField] private Fluid fluidSimulator;

    [Range(16, 65536)]
    public int particleCount = 512;

    private VisualEffect effect;
    private static readonly int velocityFieldProperty = Shader.PropertyToID("VelocityField");
    private static readonly int playerRelativePositionProperty = Shader.PropertyToID("PlayerRelativePosition");
    private static readonly int planeOffsetProperty = Shader.PropertyToID("PlaneOffset");
    private static readonly int planeScaleProperty = Shader.PropertyToID("PlaneScale");
    private static readonly int bufferProperty = Shader.PropertyToID("ParticleBuffer");
    private static readonly int particleCountProperty = Shader.PropertyToID("ParticleCount");
    private GraphicsBuffer buffer;

    [SerializeField] private GameObject player;

    [SerializeField] private RawImage velocityFieldVisualizer;

    private void OnEnable()
    {
        effect = GetComponent<VisualEffect>();
        UpdateBuffer(particleCount);
    }

    void UpdateBuffer(int size)
    {
        buffer?.Dispose();
        buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, 4 * sizeof(float));
        effect.SetGraphicsBuffer(bufferProperty, buffer);
        effect.SetInt(particleCountProperty, size);
    }

    private void OnValidate()
    {
        if (!effect)
            effect = GetComponent<VisualEffect>();
        UpdateBuffer(particleCount);
        effect.Reinit();
        effect.Play();
    }

    private void Start()
    {
        effect.Reinit();
        effect.Play();
    }

    private void Update()
    {
        Texture velocityField = fluidSimulator.GetVelocityField();
        if (velocityFieldVisualizer)
            velocityFieldVisualizer.texture = velocityField;
        effect.SetTexture(velocityFieldProperty, velocityField);

        effect.SetVector3(playerRelativePositionProperty, player.transform.position - transform.position);
        effect.SetVector3(planeOffsetProperty, fluidSimulator.transform.position);
        effect.SetFloat(planeScaleProperty, fluidSimulator.transform.localScale.x * 10);
    }
}
