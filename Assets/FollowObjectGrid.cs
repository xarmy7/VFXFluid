using StableFluids;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(Grid))]
public class FollowObjectGrid : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private GameObject tiledObject;

    [SerializeField] private Fluid fluidSimulator;
    [SerializeField] private ApplyFluidProperties fluidProperties;

    [SerializeField] private VisualEffect effect;

    private Grid grid;

    private void Start()
    {
        grid = GetComponent<Grid>();
    }

    public Vector3 GetCellSize()
    {
        return grid.cellSize;
    }

    public Vector3Int GetCellPosition(Vector3 pos)
    {
        return grid.WorldToCell(pos);
    }

    public Vector2 GetCellPosition2D(Vector3 pos)
    {
        Vector3Int cellPos = grid.WorldToCell(pos);
        return new Vector2(cellPos.x, cellPos.z);
    }

    private void Update()
    {
        var selfPos = grid.WorldToCell(tiledObject.transform.position);
        var playerPos = grid.WorldToCell(targetObject.transform.position);

        fluidSimulator.RetrieveVelocityField(ref fluidProperties.velocityField, Vector2.zero);

        if (selfPos.x == playerPos.x && selfPos.z == playerPos.z)
            return;
        
        playerPos.y = (int)(transform.position.y);
        tiledObject.transform.position = playerPos;

        var newSelfPos = grid.WorldToCell(tiledObject.transform.position);
        Vector3Int diff = newSelfPos - selfPos;

        fluidSimulator.SnapVelocityField(new Vector2(diff.x * -0.1f, diff.z * -0.1f));
    }
}
