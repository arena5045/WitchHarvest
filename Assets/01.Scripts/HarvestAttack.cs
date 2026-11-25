
using UnityEngine;

public class HarvestAttack : MonoBehaviour
{
    [Header("공격 능력치")]
    [SerializeField, Min(0.1f)] private float attackSpeed = 1f;
    [SerializeField, Min(1)] private int damage = 1;

    // 노드 강화로 얻은 보너스. 기본 공격속도와 분리해서 관리한다.
    private float attackSpeedBonusPercent;

    // 실제 공격에 사용하는 초당 공격 횟수.
    public float CurrentAttackSpeed =>
        attackSpeed * (1f + attackSpeedBonusPercent / 100f);


    private float attackTimer;

    // 실제 공격 타이머를 시각 효과에서도 사용한다.
    // 0 = 충전 시작, 1 = 공격 직전
    public float AttackProgress =>
        Mathf.Clamp01(attackTimer * CurrentAttackSpeed);

    private DayManager dayManager;

    private void Start()
    {
        // 씬에 배치된 활성 DayManager가 Awake에서 Instance를 등록한 뒤 가져온다.
        dayManager = DayManager.Instance;
    }
    private void Update()
    {

        // 수확 시간이 아니면 공격과 충전을 모두 중단한다.
        if (dayManager == null || !dayManager.IsHarvesting)
        {
            attackTimer = 0f;
            return;
        }

        attackTimer += Time.deltaTime;

        float interval = 1f / CurrentAttackSpeed;

        if (attackTimer < interval)
            return;

        // 기존처럼 남은 시간을 보존한다.
        attackTimer -= interval;

        Attack();
    }
    public void SetAttackSpeedBonusPercent(float percent)
    {
        attackSpeedBonusPercent = Mathf.Max(0f, percent);
    }

    private void Attack()
    {
        // 현재 임시 원형 스프라이트의 크기를 공격 범위로 사용한다.
        float radius = transform.lossyScale.x * 0.5f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            radius
        );

        // 범위에 포함된 모든 작물에 동일한 공격을 적용한다.
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out CropHealth crop))
            {
                crop.TakeDamage(damage);
            }
        }
    }
}
