using StableFluids;
using UnityEngine;
using UnityEngine.VFX;

public class TileGrid : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject tiledObject;
    [SerializeField] private Fluid fluidSimulator;
    [SerializeField] private ApplyFluidProperties properties;

    [SerializeField] private VisualEffect effect;

    static readonly int velocityFieldProperty = Shader.PropertyToID("_VelocityField");

    private Grid grid;

    private void Start()
    {
        grid = GetComponent<Grid>();
    }

    public Vector3 GetCellSize()
    {
        return grid.cellSize;
    }

    private void Update()
    {
        /*var selfPos = grid.WorldToCell(tiledObject.transform.position);
        var playerPos = grid.WorldToCell(player.transform.position);

        if (selfPos.x == playerPos.x && selfPos.z == playerPos.z)
            return;
        
        playerPos.y = (int)(transform.position.y);
        tiledObject.transform.position = playerPos;

        var newSelfPos = grid.WorldToCell(tiledObject.transform.position);
        Vector3Int diff = newSelfPos - selfPos;

        fluidSimulator.ResetVelocityField(new Vector2(diff.x * -0.1f, diff.z * -0.1f));*/
    }
}
