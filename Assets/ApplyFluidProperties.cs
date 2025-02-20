using StableFluids;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class ApplyFluidProperties : MonoBehaviour
{
    [SerializeField] private Fluid fluidSimulator;

    private VisualEffect effect;
    private static readonly int velocityFieldProperty = Shader.PropertyToID("VelocityField");
    private static readonly int playerRelativePositionProperty = Shader.PropertyToID("PlayerRelativePosition");
    private static readonly int planeOffsetProperty = Shader.PropertyToID("PlaneOffset");
    private static readonly int planeScaleProperty = Shader.PropertyToID("PlaneScale");

    [SerializeField] private GameObject player;

    [SerializeField] private RawImage velocityFieldVisualizer;

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
        if (velocityFieldVisualizer)
            velocityFieldVisualizer.texture = velocityField;
        effect.SetTexture(velocityFieldProperty, velocityField);

        effect.SetVector3(playerRelativePositionProperty, player.transform.position - transform.position);
        effect.SetVector3(planeOffsetProperty, fluidSimulator.transform.position);
        effect.SetFloat(planeScaleProperty, fluidSimulator.transform.localScale.x * 10);
    }
}
