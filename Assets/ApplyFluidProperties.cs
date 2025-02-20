using StableFluids;
using Unity.VisualScripting;
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

    [SerializeField] private GameObject player;

    [HideInInspector] public RenderTexture velocityField;
    [SerializeField] private RawImage velocityFieldVisualizer;

    private void OnEnable()
    {
        effect = GetComponent<VisualEffect>();
    }

    private void Start()
    {
        velocityField = new RenderTexture(fluidSimulator.ResolutionX, fluidSimulator.ResolutionY, 1);

        effect.Reinit();
        effect.Play();
    }

    private void OnDisable()
    {
        Destroy(velocityField);
    }

    private void Update()
    {
        if (velocityFieldVisualizer)
            velocityFieldVisualizer.texture = velocityField;
        effect.SetTexture(velocityFieldProperty, velocityField);
        effect.SetVector3(playerRelativePositionProperty, player.transform.position - transform.position);
    }
}
