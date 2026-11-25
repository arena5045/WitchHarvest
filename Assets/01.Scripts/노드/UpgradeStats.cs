using UnityEngine;

// 현재 구매한 노드들로부터 계산한 강화 결과.
// 세이브 파일에 직접 저장하지 않고 구매 레벨에서 다시 계산한다.
public readonly struct UpgradeStats
{
    public Vector2Int FarmSize { get; }
    public float AttackSpeedBonusPercent { get; }

    public UpgradeStats(
        Vector2Int farmSize,
        float attackSpeedBonusPercent)
    {
        FarmSize = farmSize;
        AttackSpeedBonusPercent = attackSpeedBonusPercent;
    }
}