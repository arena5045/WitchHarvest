
using UnityEngine;

public class FarmCell : MonoBehaviour
{
    [Header("격자 좌표")]
    [SerializeField] private Vector2Int gridPosition;

    // 현재 이 칸에 심어져 있는 작물
    private CropHealth currentCrop;

    [Header("작물 생성 위치")]
    [SerializeField] private Transform cropRoot;



    public Vector2Int GridPosition => gridPosition;
    public bool IsOccupied => currentCrop != null;

    public void Initialize(int x, int y)
    {
        gridPosition = new Vector2Int(x, y);
        gameObject.name = $"FarmCell_{x}_{y}";
    }


    public void Plant(CropHealth cropPrefab)
    {
        // 한 칸에는 작물이 하나만 존재할 수 있다.
        if (IsOccupied || cropPrefab == null)
            return;

        if (cropRoot == null)
        {
            Debug.LogError($"{name}: CropRoot가 설정되지 않았습니다.", this);
            return;
        }

        // 밭 그래픽과 분리된 CropRoot 아래에 작물을 생성한다.
        // 이후 하루 초기화 시 작물만 정리하기 위한 구조다.
        CropHealth crop = Instantiate(cropPrefab, cropRoot);

        crop.transform.localPosition = Vector3.zero;
        crop.SetFarmCell(this);

        currentCrop = crop;
    }


    public void ClearCrop(CropHealth crop)
    {
        // 현재 등록된 작물이 수확될 때만 빈칸으로 처리한다.
        if (currentCrop == crop)
            currentCrop = null;
    }


    // 작물만 정리한다. 수확이나 판매로 처리하지 않는다.
    public void ClearCrops()
    {
        if (cropRoot == null)
        {
            Debug.LogError($"{name}: CropRoot가 설정되지 않았습니다.", this);
            return;
        }

        for (int i = cropRoot.childCount - 1; i >= 0; i--)
        {
            GameObject oldCrop = cropRoot.GetChild(i).gameObject;

            oldCrop.SetActive(false);
            Destroy(oldCrop);
        }

        currentCrop = null;
    }

    public void PrepareForNewDay(CropHealth cropPrefab)
    {
        if (cropRoot == null || cropPrefab == null)
        {
            Debug.LogError($"{name}: 작물 초기화 설정이 없습니다.", this);
            return;
        }

        ClearCrops();

        // 새로운 하루에는 수확 가능한 작물을 준비한다.
        Plant(cropPrefab);
    }
}
