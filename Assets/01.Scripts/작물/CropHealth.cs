
using UnityEngine;

public class CropHealth : MonoBehaviour
{
    [Header("작물 데이터")]
    [SerializeField] private CropData cropData;

    private int currentHealth;
    private bool isHarvested;

    private void Awake()
    {
        if (cropData == null)
        {
            Debug.LogError($"{name}: CropData가 설정되지 않았습니다.", this);
            enabled = false;
            return;
        }

        // SO는 원본 데이터이므로 수정하지 않고, 현재 HP만 개별 관리한다.
        currentHealth = cropData.MaxHealth;

        // 스프라이트가 지정되어 있다면 해당 작물 이미지로 교체한다.
        if (cropData.Sprite != null &&
            TryGetComponent(out SpriteRenderer renderer))
        {
            renderer.sprite = cropData.Sprite;
        }
    }

    public void TakeDamage(int damage)
    {
        // Destroy는 프레임 끝에 처리되므로 중복 수확을 별도로 방지한다.
        if (!enabled || isHarvested || damage <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log($"{name} HP: {currentHealth}");

        if (currentHealth <= 0)
            Harvest();
    }

    private void Harvest()
    {
        isHarvested = true;

        // 개별 작물은 수확 사실만 전달하고, 기록은 매니저가 담당한다.
        if (HarvestManager.Instance != null)
        {
            HarvestManager.Instance.RegisterHarvest(cropData);
        }
        else
        {
            Debug.LogError("HarvestManager를 찾을 수 없습니다.");
        }

        Debug.Log($"{name} harvested!");
        Destroy(gameObject);
    }
}
