
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
