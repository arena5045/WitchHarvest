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

        if (economyManager == null || dayManager == null)
        {
            Debug.LogError("UpgradeManager의 Economy / Day Manager를 확인해주세요.", this);
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

            // 아직 알리지 않는다. 골드와 구매 레벨을 모두 확정한 뒤 알린다.
            if (!economyManager.TrySpendGoldWithoutNotification(levelData.Cost))
                return false;

            levels[node] = nextLevel;

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