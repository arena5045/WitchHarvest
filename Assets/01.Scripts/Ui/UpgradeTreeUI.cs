using UnityEngine;

public class UpgradeTreeUI : MonoBehaviour
{
    private UpgradeNodeUI[] nodeViews;

    private UpgradeManager upgradeManager;
    private EconomyManager economyManager;
    private DayManager dayManager;

    private bool initialized;
    private bool bound;

    private void Start()
    {
        upgradeManager = UpgradeManager.Instance;
        economyManager = EconomyManager.Instance;
        dayManager = DayManager.Instance;

        if (upgradeManager == null ||
            economyManager == null ||
            dayManager == null)
        {
            Debug.LogError(
                "노드 UI의 Upgrade / Economy / Day Manager를 확인해주세요.",
                this);

            enabled = false;
            return;
        }

        // 처음부터 숨겨 둔 노드도 포함한다. 매 프레임 검색하지 않는다.
        nodeViews = GetComponentsInChildren<UpgradeNodeUI>(true);

        if (nodeViews.Length == 0)
        {
            Debug.LogError("노드판 아래에 UpgradeNodeUI가 없습니다.", this);
            enabled = false;
            return;
        }

        foreach (UpgradeNodeUI nodeView in nodeViews)
        {
            if (nodeView == null || !nodeView.IsConfigured)
            {
                Debug.LogError(
                    "노드 UI의 데이터 / 버튼 / 이미지 / 텍스트 슬롯을 확인해주세요.",
                    nodeView);

                enabled = false;
                return;
            }
        }

        initialized = true;
        BindUI();
    }

    private void OnEnable()
    {
        // 최초 연결은 Start에서 하고, 이후 재활성화할 때만 다시 연결한다.
        if (initialized)
            BindUI();
    }

    private void BindUI()
    {
        if (bound)
            return;

        bound = true;

        foreach (UpgradeNodeUI nodeView in nodeViews)
            nodeView.Bind(upgradeManager);

        upgradeManager.UpgradesChanged += RefreshNodes;
        economyManager.GoldChanged += RefreshNodes;
        dayManager.StateChanged += HandleDayStateChanged;

        // 닫혀 있는 동안 놓친 알림이 있어도 현재 상태로 복구한다.
        RefreshNodes();
    }

    private void OnDisable()
    {
        if (!bound)
            return;

        bound = false;

        if (upgradeManager != null)
            upgradeManager.UpgradesChanged -= RefreshNodes;

        if (economyManager != null)
            economyManager.GoldChanged -= RefreshNodes;

        if (dayManager != null)
            dayManager.StateChanged -= HandleDayStateChanged;

        foreach (UpgradeNodeUI nodeView in nodeViews)
        {
            if (nodeView != null)
                nodeView.Unbind();
        }
    }

    private void HandleDayStateChanged(DayState state)
    {
        RefreshNodes();
    }

    private void RefreshNodes()
    {
        // 상태 변경 알림 도중 패널이 꺼진 경우에도 갱신하지 않는다.
        if (!bound || !isActiveAndEnabled)
            return;

        bool canPurchase =
            dayManager.isActiveAndEnabled &&
            dayManager.CurrentState == DayState.Settlement;

        foreach (UpgradeNodeUI nodeView in nodeViews)
            nodeView.Refresh(canPurchase);
    }
}