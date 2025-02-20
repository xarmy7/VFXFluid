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

    [SerializeField] private bool FollowX = true;
    [SerializeField] private bool FollowY = true;
    [SerializeField] private bool FollowZ = true;

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

        Vector3 newTargetPos = new Vector3(
            FollowX ? playerPos.x : tiledObject.transform.position.x,
            FollowY ? playerPos.y : tiledObject.transform.position.y,
            FollowZ ? playerPos.z : tiledObject.transform.position.z
            );
        tiledObject.transform.position = newTargetPos;

        var newSelfPos = grid.WorldToCell(tiledObject.transform.position);
        Vector3Int diff = newSelfPos - selfPos;

        fluidSimulator.SnapVelocityField(new Vector2(diff.x * -fluidSimulator.planeScale, diff.z * -fluidSimulator.planeScale));
    }
}
