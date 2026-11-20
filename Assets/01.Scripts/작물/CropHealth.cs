
using UnityEngine;

public class CropHealth : MonoBehaviour
{
    [Header("작물 데이터")]
    [SerializeField] private CropData cropData;

    private int currentHealth;
    private bool isHarvested;

    private FarmCell farmCell;
    private CropVisual cropVisual;

    private void Awake()
    {
        if (cropData == null)
        {
            Debug.LogError($"{name}: CropData가 없습니다.", this);
            enabled = false;
            return;
        }

        // SO의 최대 HP를 이용해 작물마다 현재 HP를 초기화한다.
        currentHealth = cropData.MaxHealth;
        cropVisual = GetComponentInChildren<CropVisual>();

        if (cropData.Sprite != null)
        {
            SpriteRenderer renderer =
                GetComponentInChildren<SpriteRenderer>();

            if (renderer != null)
                renderer.sprite = cropData.Sprite;
        }
    }

    public void SetFarmCell(FarmCell cell)
    {
        farmCell = cell;
    }

    public void TakeDamage(int damage)
    {
        if (!enabled || isHarvested || damage <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log($"{name} HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            Harvest();
        }
        else
        {
            cropVisual?.PlayHit();
        }
    }

    private void Harvest()
    {
        isHarvested = true;

        // 수확이 확정되는 순간 밭을 논리적으로 빈칸 처리한다.
        // 실제 그래픽은 수확 애니메이션 동안 잠시 남아 있을 수 있다.
        farmCell?.ClearCrop(this);

        if (HarvestManager.Instance != null)
        {
            HarvestManager.Instance.RegisterHarvest(cropData);
        }

        // 애니메이션 중 중복 공격을 방지한다.
        if (TryGetComponent(out Collider2D hitCollider))
            hitCollider.enabled = false;

        if (cropVisual != null)
        {
            cropVisual.PlayHarvest(() => Destroy(gameObject));
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
