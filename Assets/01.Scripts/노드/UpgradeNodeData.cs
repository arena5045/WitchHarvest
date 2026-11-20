using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

// 우선 실제로 연결할 두 종류만 정의한다.
// 새로운 메커니즘은 구현할 때 효과 종류와 처리 코드를 함께 추가한다.
public enum UpgradeEffectType
{
    FarmSize = 0,
    AttackSpeedPercent = 1
}

[Serializable]
public sealed class UpgradeRequirement
{
    [SerializeField] private UpgradeNodeData node;
    [SerializeField, Min(1)] private int requiredLevel = 1;

    public UpgradeNodeData Node => node;
    public int RequiredLevel => requiredLevel;
}

[Serializable]
public sealed class UpgradeEffectData
{
    [SerializeField] private UpgradeEffectType type;

    [Tooltip("추가할 칸 수가 아니라, 이 효과가 제공하는 목표 농장 크기다.")]
    [SerializeField, ShowIf(nameof(type), UpgradeEffectType.FarmSize)]
    private Vector2Int farmSize = new Vector2Int(2, 2);

    [Tooltip("10이면 기본 공격속도 대비 +10%다. 0.1을 입력하는 방식이 아니다.")]
    [SerializeField, Min(0f)]
    [ShowIf(nameof(type), UpgradeEffectType.AttackSpeedPercent)]
    private float percentBonus;

    public UpgradeEffectType Type => type;
    public Vector2Int FarmSize => farmSize;
    public float PercentBonus => percentBonus;
}

[Serializable]
public sealed class UpgradeLevelData
{
    [Tooltip("직전 레벨에서 이 레벨로 올라갈 때 지불하는 가격이다.")]
    [SerializeField, MinValue(0)]
    private long cost = 10;

    [Tooltip("이 레벨에서 해당 노드가 제공하는 전체 효과다. 이전 레벨 효과와 더하지 않는다.")]
    [SerializeField]
    private UpgradeEffectData[] effects = { new UpgradeEffectData() };

    public long Cost => cost;
    public IReadOnlyList<UpgradeEffectData> Effects => effects;
}

[CreateAssetMenu(
    fileName = "UpgradeNode_",
    menuName = "Witch Harvest/Upgrade Node")]
public sealed class UpgradeNodeData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("노드마다 다른 고유 ID를 사용한다. 표시 이름과 별도로 관리한다.")]
    [SerializeField] private string nodeId;

    [SerializeField] private string displayName;

    [SerializeField, TextArea(2, 4)]
    private string description;

    [SerializeField] private Sprite icon;

    [Header("선행 조건")]
    [Tooltip("비어 있으면 선행 조건이 없다. 여러 개면 모두 충족해야 한다.")]
    [SerializeField]
    private UpgradeRequirement[] requirements =
        Array.Empty<UpgradeRequirement>();

    [Header("레벨별 설정")]
    [Tooltip("Element 0은 1레벨, Element 1은 2레벨이다. 항목 수가 최대 레벨이다.")]
    [SerializeField]
    private UpgradeLevelData[] levels = { new UpgradeLevelData() };

    public string NodeId => nodeId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;

    public IReadOnlyList<UpgradeRequirement> Requirements => requirements;
    public int MaxLevel => levels?.Length ?? 0;

    // 구매 레벨은 1부터 시작한다. 0레벨은 미구매 상태이므로 데이터가 없다.
    public bool TryGetLevelData(int level, out UpgradeLevelData levelData)
    {
        levelData = null;

        if (level < 1 || level > MaxLevel)
            return false;

        levelData = levels[level - 1];
        return levelData != null;
    }
}