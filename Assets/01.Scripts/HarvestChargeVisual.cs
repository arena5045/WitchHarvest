
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HarvestChargeVisual : MonoBehaviour
{
    private HarvestAttack harvestAttack;
    private LineRenderer ring;

    private void Awake()
    {
        harvestAttack = GetComponentInParent<HarvestAttack>();
        ring = GetComponent<LineRenderer>();

        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 64;

        // 부모 HarvestArea의 기본 Circle 스프라이트는
        // 지름이 1이므로 로컬 반지름 0.5를 사용한다.
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / ring.positionCount;

            ring.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * 0.5f,
                Mathf.Sin(angle) * 0.5f,
                0f
            ));
        }
    }

    private void LateUpdate()
    {
        // 공격 타이머를 따로 계산하지 않고
        // 실제 공격 진행도를 시각적으로 표현한다.
        float progress = harvestAttack.AttackProgress;

        transform.localScale = new Vector3(
            progress,
            progress,
            1f
        );
    }
}
