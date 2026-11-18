
using UnityEngine;

public class HarvestAttack : MonoBehaviour
{
    [Header("공격 능력치")]
    [SerializeField, Min(0.1f)] private float attackSpeed = 1f;
    [SerializeField, Min(1)] private int damage = 1;

    private float attackTimer;

    // 실제 공격 타이머를 시각 효과에서도 사용한다.
    // 0 = 충전 시작, 1 = 공격 직전
    public float AttackProgress =>
        Mathf.Clamp01(attackTimer * attackSpeed);

    private void Update()
    {
        attackTimer += Time.deltaTime;

        // 공격속도는 초당 공격 횟수를 의미한다.
        // 예: 2라면 0.5초마다 공격한다.
        float interval = 1f / attackSpeed;

        if (attackTimer < interval)
            return;

        // 남은 시간을 보존해 프레임 오차 누적을 줄인다.
        attackTimer -= interval;

        Attack();
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
