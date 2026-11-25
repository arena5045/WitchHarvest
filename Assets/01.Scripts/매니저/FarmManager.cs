
using UnityEngine;

public class FarmManager : MonoBehaviour
{
    [Header("농장 크기")]
    [SerializeField, Min(1)] private int width = 1;
    [SerializeField, Min(1)] private int height = 1;

    [Header("배치 설정")]
    [SerializeField, Min(0.1f)] private float cellSpacing = 1.5f;
    [SerializeField] private FarmCell cellPrefab;
    [SerializeField] private Transform cellRoot;

    [Header("작물 설정")]
    [SerializeField] private CropHealth cropPrefab;

    private FarmCell[,] cells;

    // 최초 농장 생성과 필수 참조 준비가 끝났는지 확인한다.
    // 구매 처리에서는 골드를 차감하기 전에 이 상태를 검사한다.
    public bool CanExpand =>
        isActiveAndEnabled &&
        cells != null &&
        cellPrefab != null &&
        cellRoot != null;


    private void Start()
    {
        CreateFarm();
        PrepareForNewDay();
    }

    private void CreateFarm()
    {
        if (cellPrefab == null || cellRoot == null)
        {
            Debug.LogError("농장 셀 설정이 누락되었습니다.", this);
            return;
        }

        cells = new FarmCell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 농장 크기가 달라져도 중심을 유지한다.
                Vector3 position = new Vector3(
                    (x - (width - 1) * 0.5f) * cellSpacing,
                    (y - (height - 1) * 0.5f) * cellSpacing,
                    0f
                );

                FarmCell cell = Instantiate(cellPrefab, cellRoot);
                cell.transform.localPosition = position;
                cell.Initialize(x, y);

                cells[x, y] = cell;
            }
        }
    }
    public void ExpandTo(Vector2Int targetSize)
    {
        if (!CanExpand || targetSize.x < 1 || targetSize.y < 1)
        {
            Debug.LogError("농장 확장에 필요한 참조 또는 목표 크기를 확인해주세요.", this);
            return;
        }

        // 목표 크기까지 확장한다. 현재 크기에 더하거나 농장을 축소하지 않는다.
        int newWidth = Mathf.Max(width, targetSize.x);
        int newHeight = Mathf.Max(height, targetSize.y);

        // 같은 효과가 다시 적용되어도 셀을 중복 생성하지 않는다.
        if (newWidth == width && newHeight == height)
            return;

        FarmCell[,] expandedCells = new FarmCell[newWidth, newHeight];

        for (int x = 0; x < newWidth; x++)
        {
            for (int y = 0; y < newHeight; y++)
            {
                FarmCell cell;

                if (x < width && y < height)
                {
                    // 기존 셀과 그 안의 작물은 그대로 보존한다.
                    cell = cells[x, y];
                }
                else
                {
                    // 새로 늘어난 위치에만 빈 셀을 만든다.
                    cell = Instantiate(cellPrefab, cellRoot);
                    cell.Initialize(x, y);
                }

                // 기존 셀까지 함께 재배치해서 농장 중심을 유지한다.
                cell.transform.localPosition = new Vector3(
                    (x - (newWidth - 1) * 0.5f) * cellSpacing,
                    (y - (newHeight - 1) * 0.5f) * cellSpacing,
                    0f
                );

                expandedCells[x, y] = cell;
            }
        }

        cells = expandedCells;
        width = newWidth;
        height = newHeight;

        // 여기서는 작물을 심지 않는다.
        // 다음 날의 PrepareForNewDay()가 확장된 모든 칸에 작물을 준비한다.
    }

    // 저장된 밤 상태로 복귀할 때 사용한다.
    // 농장 칸은 보존하고 작물만 제거한다.
    public void ClearAllCrops()
    {
        if (cells == null)
            return;

        foreach (FarmCell cell in cells)
        {
            cell.ClearCrops();
        }
    }

    [ContextMenu("Test/Prepare New Day")]
    public void PrepareForNewDay()
    {
        if (cells == null)
            return;

        if (cropPrefab == null)
        {
            Debug.LogError("작물 프리팹이 설정되지 않았습니다.", this);
            return;
        }

        // 농장 칸은 그대로 유지하고 작물만 새로 준비한다.
        foreach (FarmCell cell in cells)
        {
            cell.PrepareForNewDay(cropPrefab);
        }
    }



    private void PlantInitialCrops()
    {
        if (cells == null || cropPrefab == null)
        {
            Debug.LogError("농장 또는 작물 프리팹이 없습니다.", this);
            return;
        }

        // 하루 시작 시 모든 칸에 수확 가능한 작물을 준비한다.
        // 일반적인 재성장 기능과는 별도로 동작한다.
        foreach (FarmCell cell in cells)
        {
            cell.Plant(cropPrefab);
        }
    }

    public FarmCell GetCell(int x, int y)
    {
        if (cells == null ||
            x < 0 || x >= width ||
            y < 0 || y >= height)
        {
            return null;
        }

        return cells[x, y];
    }
}
