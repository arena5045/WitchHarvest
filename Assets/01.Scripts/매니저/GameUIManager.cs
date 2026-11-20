using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("수확 UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text harvestCountText;

    [Header("결과창")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultCountText;
    [SerializeField] private TMP_Text resultGoldText;
    [SerializeField] private Button settlementButton;

    [Header("정산창")]
    [SerializeField] private GameObject settlementPanel;
    [SerializeField] private Button nextDayButton;

    [Header("골드 UI")]
    [SerializeField] private TMP_Text goldText;

    private DayManager dayManager;
    private HarvestManager harvestManager;
    private EconomyManager economyManager;
    private bool initialized;

    private void Start()
    {
        // 씬에 배치된 활성 매니저들의 Awake 이후에 참조를 가져온다.
        dayManager = DayManager.Instance;
        harvestManager = HarvestManager.Instance;
        economyManager = EconomyManager.Instance;

        if (dayManager == null || harvestManager == null || economyManager == null)
        {
            Debug.LogError("Day / Harvest / Economy Manager를 확인해주세요.", this);
            enabled = false;
            return;
        }

        // Day Text만 선택 사항이며, 나머지는 기존 UI를 연결해야 한다.
        if (startButton == null || phaseText == null || timerText == null ||
            harvestCountText == null || resultPanel == null ||
            resultCountText == null || settlementButton == null ||
            settlementPanel == null || nextDayButton == null ||
            resultGoldText == null || goldText == null)
        {
            Debug.LogError("GameUIManager의 필수 UI 슬롯을 연결해주세요.", this);
            enabled = false;
            return;
        }

        initialized = true;
        BindUI();
    }

    private void OnEnable()
    {
        // 최초 OnEnable은 Start보다 빠르므로 초기 연결은 Start에서 한다.
        // 이후 UI 매니저를 다시 켰을 때만 여기서 재연결한다.
        if (initialized)
            BindUI();
    }

    private void BindUI()
    {
        dayManager.StateChanged += RefreshState;
        harvestManager.HarvestChanged += RefreshHarvestCount;
        economyManager.GoldChanged += RefreshGold;

        startButton.onClick.AddListener(HandleStartClicked);
        settlementButton.onClick.AddListener(HandleSettlementClicked);
        nextDayButton.onClick.AddListener(HandleNextDayClicked);

        // 꺼져 있는 동안 놓친 이벤트가 있어도 현재 상태로 복구한다.
        RefreshState(dayManager.CurrentState);
        RefreshHarvestCount();
    }

    private void OnDisable()
    {
        if (!initialized)
            return;

        // 재활성화할 때 이벤트와 버튼 동작이 중복 등록되지 않도록 해제한다.
        if (dayManager != null)
            dayManager.StateChanged -= RefreshState;

        if (harvestManager != null)
            harvestManager.HarvestChanged -= RefreshHarvestCount;

        if (economyManager != null)
            economyManager.GoldChanged -= RefreshGold;

        if (startButton != null)
            startButton.onClick.RemoveListener(HandleStartClicked);

        if (settlementButton != null)
            settlementButton.onClick.RemoveListener(HandleSettlementClicked);

        if (nextDayButton != null)
            nextDayButton.onClick.RemoveListener(HandleNextDayClicked);
    }

    private void LateUpdate()
    {
        if (initialized)
            RefreshTime();
    }

    private void RefreshState(DayState state)
    {
        startButton.gameObject.SetActive(state == DayState.Ready);
        phaseText.gameObject.SetActive(state == DayState.Countdown);
        timerText.gameObject.SetActive(state == DayState.Harvesting);
        resultPanel.SetActive(state == DayState.Result);
        settlementPanel.SetActive(state == DayState.Settlement);

        // 결과창에서는 판매금과 잔액을 함께 표시하므로 공통 골드 텍스트를 숨긴다.
        goldText.gameObject.SetActive(state != DayState.Result);

        if (dayText != null)
            dayText.text = $"DAY {dayManager.DayNumber}";

        if (state == DayState.Result)
        {
            resultCountText.text = $"오늘 수확: {harvestManager.DailyHarvestCount}개";
        }

        // 수확이 0개라 GoldChanged가 없어도 결과 표시를 갱신한다.
        RefreshGold();
        RefreshTime();
    }

    private void RefreshTime()
    {
        // UI는 시간을 계산하지 않고 DayManager의 값을 표시만 한다.
        if (dayManager.CurrentState == DayState.Countdown)
        {
            phaseText.text =
                Mathf.CeilToInt(dayManager.CountdownRemaining).ToString();
        }
        else if (dayManager.CurrentState == DayState.Harvesting)
        {
            timerText.text = $"남은 시간: {dayManager.RemainingHarvestTime:F1}";
        }
    }

    private void RefreshHarvestCount()
    {
        harvestCountText.text = $"오늘 수확량: {harvestManager.DailyHarvestCount}";
        RefreshGold();
    }

    private void RefreshGold()
    {
        // 상태 / 수확 / 잔액 이벤트와 UI 재연결 시에만 갱신한다.
        switch (dayManager.CurrentState)
        {
            case DayState.Result:
                // 이미 지급된 판매금과 현재 잔액을 표시만 한다. UI에서는 지급하지 않는다.
                resultGoldText.text =
                    $"판매금액 : + {dayManager.EarnedGoldToday:N0} G\n" +
                    $"현재 골드 : {economyManager.Gold:N0} G";
                break;

            case DayState.Settlement:
                goldText.text = $"보유 골드: {economyManager.Gold:N0} G";
                break;

            default:
                // 준비 / 카운트다운 / 낮에는 이번 하루의 예상 판매금만 표시한다.
                // 하루 종료와 같은 계산 함수를 사용하되 실제 잔액은 변경하지 않는다.
                long expectedGold = economyManager.CalculateHarvestGold(
                    harvestManager.DailyHarvestedCrops);
                goldText.text = $"예상 판매금 : {expectedGold:N0} G";
                break;
        }
    }

    // UI는 요청만 전달한다. 실행 가능 여부는 DayManager가 판단한다.
    private void HandleStartClicked() => dayManager.StartFirstDay();
    private void HandleSettlementClicked() => dayManager.OpenSettlement();
    private void HandleNextDayClicked() => dayManager.GoToNextDay();
}
