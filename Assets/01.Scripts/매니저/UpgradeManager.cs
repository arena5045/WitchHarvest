using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }
    public bool IsInitialized { get; private set; }
    public event Action UpgradesChanged;

    [Header("전체 노드 목록 - 아직 숨겨진 노드도 등록")]
    [SerializeField, DisableInPlayMode]
    private UpgradeNodeData[] nodes = Array.Empty<UpgradeNodeData>();

    [Header("효과 적용 대상")]
    [SerializeField] private FarmManager farmManager;

    [SerializeField] private HarvestAttack harvestAttack;


    // SO는 설정만 보관한다. 현재 구매 레벨은 이곳에 따로 기록한다.
    private readonly Dictionary<UpgradeNodeData, int> levels =
        new Dictionary<UpgradeNodeData, int>();

    private EconomyManager economyManager;
    private DayManager dayManager;
    private bool purchaseInProgress;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("UpgradeManager가 중복 생성되었습니다.", this);
            enabled = false;
            Destroy(this);
            return;
        }

        Instance = this;

        // 자신의 데이터만 준비한다. 다른 매니저의 Awake 순서에는 의존하지 않는다.
        if (!ValidateNodes())
        {
            enabled = false;
            return;
        }

        foreach (UpgradeNodeData node in nodes)
            levels.Add(node, 0);
    }

    private void Start()
    {
        economyManager = EconomyManager.Instance;
        dayManager = DayManager.Instance;

        if (economyManager == null || dayManager == null ||
     farmManager == null || harvestAttack == null)
        {
            Debug.LogError(
                "UpgradeManager의 Economy / Day / Farm Manager / harvestAttack를 확인해주세요.",
                this);

            enabled = false;
            return;
        }

        IsInitialized = true;

        // UI가 먼저 Start를 실행했더라도, 준비 완료 후 다시 조회할 기회를 준다.
        UpgradesChanged?.Invoke();
    }

    public int GetLevel(UpgradeNodeData node)
    {
        return node != null && levels.TryGetValue(node, out int level)
            ? level
            : 0;
    }

    // 노드판 표시용 상태다. 로비 밖 구매 차단은 TryPurchase에서 별도로 한다.
    public UpgradeNodeState GetState(UpgradeNodeData node)
    {
        if (!IsInitialized || !isActiveAndEnabled ||
            economyManager == null || !economyManager.isActiveAndEnabled ||
            node == null || !levels.TryGetValue(node, out int level))
            return UpgradeNodeState.Locked;

        if (level >= node.MaxLevel)
            return UpgradeNodeState.Maxed;

        foreach (UpgradeRequirement requirement in node.Requirements)
        {
            if (GetLevel(requirement.Node) < requirement.RequiredLevel)
                return UpgradeNodeState.Locked;
        }

        if (!node.TryGetLevelData(level + 1, out UpgradeLevelData nextLevel))
            return UpgradeNodeState.Locked;

        return economyManager.Gold >= nextLevel.Cost
            ? UpgradeNodeState.Purchasable
            : UpgradeNodeState.Unaffordable;
    }

    public bool TryPurchase(UpgradeNodeData node)
    {
        if (!IsInitialized || !isActiveAndEnabled || purchaseInProgress ||
            dayManager == null || !dayManager.isActiveAndEnabled)
            return false;

        purchaseInProgress = true;

        try
        {
            if (dayManager.CurrentState != DayState.Settlement ||
                GetState(node) != UpgradeNodeState.Purchasable)
                return false;

            int nextLevel = levels[node] + 1;

            if (!node.TryGetLevelData(nextLevel, out UpgradeLevelData levelData))
                return false;

            // 농장 준비가 안 됐다면 돈과 구매 레벨을 변경하지 않는다.
            if (farmManager == null || !farmManager.CanExpand)
            {
                Debug.LogError("농장이 아직 준비되지 않아 노드를 구매할 수 없습니다.", this);
                return false;
            }

            // 골드를 차감하기 전에 구매 후 효과를 계산해 둔다.
            UpgradeStats targetStats = CalculateStats(node, nextLevel);


            // 아직 알리지 않는다. 골드와 구매 레벨을 모두 확정한 뒤 알린다.
            if (!economyManager.TrySpendGoldWithoutNotification(levelData.Cost))
                return false;

            levels[node] = nextLevel;

            // 구매 기록이 확정된 뒤 실제 시스템에 효과를 적용한다.
            ApplyStats(targetStats);

            if (levelData.Cost > 0)
                economyManager.NotifyGoldChanged();

            UpgradesChanged?.Invoke();
            return true;
        }
        finally
        {
            // 이벤트에서 다시 구매를 요청해도 위의 잠금에 걸린다.
            // 중간에 반환되거나 예외가 발생해도 잠금은 남기지 않는다.
            purchaseInProgress = false;
        }
    }

    private bool ValidateNodes()
    {
        if (nodes == null || nodes.Length == 0)
            return DataError("전체 노드 목록이 비어 있습니다.");

        var registered = new HashSet<UpgradeNodeData>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (UpgradeNodeData node in nodes)
        {
            if (node == null || !registered.Add(node))
                return DataError("빈 슬롯 또는 중복 등록된 노드가 있습니다.");

            if (string.IsNullOrWhiteSpace(node.NodeId) || !ids.Add(node.NodeId))
                return DataError($"{node.name}: ID가 비어 있거나 다른 노드와 중복됩니다.");

            if (node.MaxLevel == 0 || node.Requirements == null)
                return DataError($"{node.name}: 레벨 또는 선행 조건 목록을 확인해주세요.");

            for (int level = 1; level <= node.MaxLevel; level++)
            {
                if (!node.TryGetLevelData(level, out UpgradeLevelData data) ||
                    data.Cost < 0)
                    return DataError($"{node.name}: {level}레벨의 데이터 또는 가격이 잘못되었습니다.");

                if (data.Effects == null || data.Effects.Count == 0)
                    return DataError($"{node.name}: {level}레벨에 효과가 없습니다.");

                foreach (UpgradeEffectData effect in data.Effects)
                {
                    if (effect == null)
                        return DataError($"{node.name}: {level}레벨에 비어 있는 효과가 있습니다.");

                    if (effect.Type == UpgradeEffectType.FarmSize &&
                        (effect.FarmSize.x < 1 || effect.FarmSize.y < 1))
                    {
                        return DataError(
                            $"{node.name}: {level}레벨의 농장 크기는 가로·세로 모두 1 이상이어야 합니다.");
                    }
                }
            }
        }

        foreach (UpgradeNodeData node in nodes)
        {
            foreach (UpgradeRequirement requirement in node.Requirements)
            {
                if (requirement == null || requirement.Node == null ||
                    !registered.Contains(requirement.Node))
                    return DataError($"{node.name}: 선행 노드가 없거나 전체 목록에 등록되지 않았습니다.");

                if (requirement.RequiredLevel < 1 ||
                    requirement.RequiredLevel > requirement.Node.MaxLevel)
                    return DataError($"{node.name}: 선행 노드의 요구 레벨이 범위를 벗어났습니다.");
            }
        }

        var visiting = new HashSet<UpgradeNodeData>();
        var visited = new HashSet<UpgradeNodeData>();

        foreach (UpgradeNodeData node in nodes)
        {
            if (HasCycle(node, visiting, visited))
                return DataError($"{node.name}: 선행 조건이 순환합니다. 서로를 먼저 사야 하는 연결입니다.");
        }

        return true;
    }

    // 현재 구매 레벨에서 계산한 강화 결과를 조회한다.
    public UpgradeStats GetCurrentStats()
    {
        return CalculateStats();
    }

    // 실제 보유 레벨 또는 구매 직후 예상 레벨을 기준으로 효과를 집계한다.
    private UpgradeStats CalculateStats(
        UpgradeNodeData purchasingNode = null,
        int purchasingLevel = 0)
    {
        Vector2Int farmSize = Vector2Int.one;
        float speedBonus = 0f;

        foreach (UpgradeNodeData node in nodes)
        {
            // 구매 예정 노드만 다음 레벨로 계산한다.
            int level = node == purchasingNode
                ? purchasingLevel
                : GetLevel(node);

            if (level == 0)
                continue;

            node.TryGetLevelData(level, out UpgradeLevelData levelData);

            foreach (UpgradeEffectData effect in levelData.Effects)
            {
                switch (effect.Type)
                {
                    case UpgradeEffectType.FarmSize:
                        // 농장 크기는 가장 큰 목표 크기를 선택한다.
                        farmSize.x = Mathf.Max(farmSize.x, effect.FarmSize.x);
                        farmSize.y = Mathf.Max(farmSize.y, effect.FarmSize.y);
                        break;

                    case UpgradeEffectType.AttackSpeedPercent:
                        // 서로 다른 노드의 현재 레벨 보너스를 합산한다.
                        speedBonus += effect.PercentBonus;
                        break;
                }
            }
        }

        return new UpgradeStats(farmSize, speedBonus);
    }

    // 계산된 효과를 실제 게임 시스템에 전달한다.
    private void ApplyStats(UpgradeStats stats)
    {
        farmManager.ExpandTo(stats.FarmSize);
        harvestAttack.SetAttackSpeedBonusPercent(
            stats.AttackSpeedBonusPercent);
    }

    // 현재 구매한 노드만 저장 데이터로 변환한다.
    public List<UpgradeLevelSaveData> CaptureLevels()
    {
        var result = new List<UpgradeLevelSaveData>();

        foreach (UpgradeNodeData node in nodes)
        {
            int level = GetLevel(node);

            if (level == 0)
                continue;

            result.Add(new UpgradeLevelSaveData
            {
                nodeId = node.NodeId,
                level = level
            });
        }

        return result;
    }

    // 최초 게임 로드 전용.
    // 골드를 차감하거나 구매 함수를 다시 실행하지 않는다.
    internal bool TryRestoreLevels(
        IReadOnlyList<UpgradeLevelSaveData> savedLevels)
    {
        if (!IsInitialized || !isActiveAndEnabled ||
            purchaseInProgress ||
            dayManager == null ||
            dayManager.CurrentState != DayState.Ready ||
            farmManager == null || !farmManager.CanExpand ||
            harvestAttack == null ||
            savedLevels == null)
        {
            return false;
        }

        var nodesById =
            new Dictionary<string, UpgradeNodeData>(StringComparer.Ordinal);

        var restoredLevels =
            new Dictionary<UpgradeNodeData, int>();

        foreach (UpgradeNodeData node in nodes)
        {
            nodesById.Add(node.NodeId, node);
            restoredLevels.Add(node, 0);
        }

        // 실제 구매 기록을 변경하기 전에 저장값을 모두 검사한다.
        foreach (UpgradeLevelSaveData saved in savedLevels)
        {
            if (saved == null ||
                string.IsNullOrWhiteSpace(saved.nodeId) ||
                !nodesById.TryGetValue(saved.nodeId, out UpgradeNodeData node) ||
                saved.level < 1 ||
                saved.level > node.MaxLevel ||
                restoredLevels[node] != 0)
            {
                Debug.LogError("저장된 노드 ID 또는 레벨이 올바르지 않습니다.", this);
                return false;
            }

            restoredLevels[node] = saved.level;
        }

        // 저장된 구매 레벨들이 선행 조건을 만족하는지도 확인한다.
        foreach (var entry in restoredLevels)
        {
            if (entry.Value == 0)
                continue;

            foreach (UpgradeRequirement requirement in entry.Key.Requirements)
            {
                if (restoredLevels[requirement.Node] < requirement.RequiredLevel)
                {
                    Debug.LogError(
                        $"저장된 노드의 선행 조건 오류: {entry.Key.NodeId}",
                        this);

                    return false;
                }
            }
        }

        // 검사가 모두 끝난 뒤 실제 구매 기록을 복원한다.
        foreach (var entry in restoredLevels)
            levels[entry.Key] = entry.Value;

        ApplyStats(CalculateStats());

        return true;
    }

    internal void NotifyUpgradesChanged()
    {
        UpgradesChanged?.Invoke();
    }


    private bool HasCycle(
        UpgradeNodeData node,
        HashSet<UpgradeNodeData> visiting,
        HashSet<UpgradeNodeData> visited)
    {
        if (visited.Contains(node))
            return false;

        if (!visiting.Add(node))
            return true;

        foreach (UpgradeRequirement requirement in node.Requirements)
        {
            if (HasCycle(requirement.Node, visiting, visited))
                return true;
        }

        visiting.Remove(node);
        visited.Add(node);
        return false;
    }

    private bool DataError(string message)
    {
        Debug.LogError($"[노드 설정 오류] {message}", this);
        return false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

#if UNITY_EDITOR
    // 실제 구매 함수로 검사한다. 이 테스트 도구들은 에디터용이다.
    [Button("테스트: 모든 노드 상태 출력"), DisableInEditorMode]
    private void PrintNodeStates()
    {
        if (!IsInitialized || economyManager == null)
            return;

        Debug.Log($"현재 골드: {economyManager.Gold:N0} G", this);

        foreach (UpgradeNodeData node in nodes)
        {
            Debug.Log(
                $"{node.DisplayName}: {GetLevel(node)}/{node.MaxLevel}, {GetState(node)}",
                this);
        }
    }

    [Button("테스트: 노드 구매"), DisableInEditorMode]
    private void TestPurchase(UpgradeNodeData node)
    {
        bool success = TryPurchase(node);

        Debug.Log(success ? "노드 구매 성공" : "노드 구매 실패", this);
        PrintNodeStates();
    }

    [Button("테스트: 골드 +100"), DisableInEditorMode]
    private void AddTestGold()
    {
        if (!IsInitialized || economyManager == null || purchaseInProgress)
            return;

        economyManager.AddGold(100);
        PrintNodeStates();
    }
#endif
}