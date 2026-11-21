using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeNodeUI : MonoBehaviour
{
    [Header("노드 데이터")]
    [SerializeField] private UpgradeNodeData nodeData;

    [Header("UI 참조")]
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Image borderImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text statusText;

    [Header("상태별 테두리 색상")]
    [SerializeField] private Color unaffordableColor = Color.red;
    [SerializeField] private Color purchasableColor = Color.green;
    [SerializeField] private Color maxedColor = new Color(1f, 0.8f, 0.2f);

    private UpgradeManager upgradeManager;

    public bool IsConfigured =>
        nodeData != null &&
        purchaseButton != null &&
        borderImage != null &&
        nameText != null &&
        levelText != null &&
        statusText != null;

    // 숨겨진 노드도 노드판에서 직접 연결한다.
    // 이 컴포넌트의 Awake / Start 실행 여부에 의존하지 않는다.
    public void Bind(UpgradeManager manager)
    {
        upgradeManager = manager;
        purchaseButton.onClick.AddListener(HandleClicked);
    }

    public void Unbind()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveListener(HandleClicked);
            purchaseButton.interactable = false;
        }

        upgradeManager = null;
    }

    public void Refresh(bool canPurchase)
    {
        if (upgradeManager == null)
            return;

        UpgradeNodeState state = upgradeManager.GetState(nodeData);

        purchaseButton.interactable =
            enabled &&
            canPurchase &&
            state == UpgradeNodeState.Purchasable;

        if (state == UpgradeNodeState.Locked)
        {
            gameObject.SetActive(false);
            return;
        }

        int currentLevel = upgradeManager.GetLevel(nodeData);

        nameText.text = nodeData.DisplayName;
        levelText.text = $"{currentLevel}/{nodeData.MaxLevel}";

        if (state == UpgradeNodeState.Maxed)
        {
            borderImage.color = maxedColor;
            statusText.text = "강화 완료";
        }
        else
        {
            // GetState에서 다음 레벨 데이터가 존재하는지 이미 확인했다.
            nodeData.TryGetLevelData(
                currentLevel + 1,
                out UpgradeLevelData nextLevel);

            bool affordable = state == UpgradeNodeState.Purchasable;

            borderImage.color = affordable
                ? purchasableColor
                : unaffordableColor;

            string stateLabel = affordable ? "구매 가능" : "골드 부족";
            statusText.text = $"{nextLevel.Cost:N0} G\n{stateLabel}";
        }

        // 표시 내용을 먼저 준비한 다음 노드를 보여준다.
        gameObject.SetActive(true);
    }

    private void HandleClicked()
    {
        if (upgradeManager == null ||
            !isActiveAndEnabled ||
            !purchaseButton.interactable)
            return;

        // UI는 요청만 전달한다.
        // 실제 조건 검사, 차감, 레벨 증가, 효과 적용은 매니저가 담당한다.
        upgradeManager.TryPurchase(nodeData);
    }
}