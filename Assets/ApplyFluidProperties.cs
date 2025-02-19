using StableFluids;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class ApplyFluidProperties : MonoBehaviour
{
    [SerializeField] private Fluid fluidSimulator;

    private VisualEffect effect;
    private static readonly int velocityFieldProperty = Shader.PropertyToID("VelocityField");
    private static readonly int playerRelativePositionProperty = Shader.PropertyToID("PlayerRelativePosition");

    [SerializeField] private GameObject player;

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
        effect.SetTexture(velocityFieldProperty, velocityField);
        effect.SetVector3(playerRelativePositionProperty, player.transform.position - transform.position);
    }
}
