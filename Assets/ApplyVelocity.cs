using StableFluids;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class ApplyVelocity : MonoBehaviour
{
    [SerializeField] private Fluid fluidSimulator;

    private VisualEffect effect;
    private static readonly int vfxProperty = Shader.PropertyToID("VelocityField");

    private void OnEnable()
    {
        effect = GetComponent<VisualEffect>();
    }

    private void Start()
    {
        effect.Reinit();
        effect.Play();
    }

    private void Update()
    {
        Texture velocityField = fluidSimulator.GetVelocityField();
        effect.SetTexture(vfxProperty, velocityField);
    }
}
