using System;
using UnityEngine;

public enum DayState
{
    Ready,
    Countdown,
    Harvesting,
    Result,
    Settlement
}

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("하루 설정")]
    [SerializeField, Min(1)] private int countdownSeconds = 3;
    [SerializeField, Min(0.1f)] private float harvestDuration = 10f;

    [Header("시스템 참조")]
    [SerializeField] private FarmManager farmManager;

    public int DayNumber { get; private set; } = 1;
    public DayState CurrentState { get; private set; } = DayState.Ready;

    // 결과창 표시용 숫자일 뿐, 별도로 묶어 두는 돈이 아니다.
    public long EarnedGoldToday { get; private set; }

    // UI를 직접 호출하지 않고 상태가 변경됐다는 사실만 알린다.
    public event Action<DayState> StateChanged;

    private float countdownRemaining;
    private float harvestEndTime;
    private bool harvestRewardGranted;
    private HarvestManager harvestManager;
    private EconomyManager economyManager;

    public float CountdownRemaining => Mathf.Max(0f, countdownRemaining);

    public float RemainingHarvestTime =>
        CurrentState == DayState.Harvesting
            ? Mathf.Max(0f, harvestEndTime - Time.time)
            : 0f;

    // Update 실행 순서와 관계없이 종료 시각 이후 공격을 차단한다.
    public bool IsHarvesting =>
        CurrentState == DayState.Harvesting &&
        !harvestRewardGranted && Time.time < harvestEndTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        // Domain Reload를 꺼도 이전 실행의 정적 참조를 남기지 않는다.
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("DayManager가 중복 생성되었습니다.", this);
            enabled = false;

            // 같은 GameObject의 다른 매니저까지 삭제하지 않는다.
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // 씬에 배치된 활성 매니저들이 Awake에서 Instance를 등록한 뒤 캐싱한다.
        harvestManager = HarvestManager.Instance;
        economyManager = EconomyManager.Instance;

        if (farmManager == null || harvestManager == null || economyManager == null)
        {
            Debug.LogError("DayManager의 Farm / Harvest / Economy Manager를 확인해주세요.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case DayState.Countdown:
                countdownRemaining -= Time.deltaTime;

                if (countdownRemaining <= 0f)
                    StartHarvesting();
                break;

            case DayState.Harvesting:
                if (RemainingHarvestTime <= 0f)
                    FinishHarvesting();
                break;
        }
    }

    public void StartFirstDay()
    {
        if (!enabled || CurrentState != DayState.Ready)
            return;

        BeginCountdown();
    }

    private void BeginCountdown()
    {
        // 새 하루에만 지급 여부와 결과 표시값을 초기화한다. 보유 골드는 유지한다.
        harvestRewardGranted = false;
        EarnedGoldToday = 0;
        countdownRemaining = countdownSeconds;
        SetState(DayState.Countdown);
    }

    private void StartHarvesting()
    {
        countdownRemaining = 0f;
        harvestEndTime = Time.time + harvestDuration;

        // 시간을 먼저 설정한 뒤 알린다. UI는 완성된 상태를 읽는다.
        SetState(DayState.Harvesting);
    }

    private void FinishHarvesting()
    {
        if (CurrentState != DayState.Harvesting || harvestRewardGranted)
            return;

        EarnedGoldToday = economyManager.CalculateHarvestGold(
            harvestManager.DailyHarvestedCrops);

        // GoldChanged 알림 전에 지급 여부를 잠근다. 같은 종료 처리가 다시 호출돼도
        // 이 하루의 보상은 한 번만 지급하며, 수확 애니메이션 종료를 기다리지 않는다.
        harvestRewardGranted = true;
        economyManager.AddGold(EarnedGoldToday);

        // 표시할 획득액과 실제 잔액이 모두 확정된 후 결과창에 알린다.
        SetState(DayState.Result);
    }

    public void OpenSettlement()
    {
        if (CurrentState != DayState.Result)
            return;

        SetState(DayState.Settlement);
    }

    public void GoToNextDay()
    {
        if (!enabled || CurrentState != DayState.Settlement)
            return;

        if (farmManager == null || harvestManager == null)
        {
            Debug.LogError("다음 날 준비에 필요한 매니저가 없습니다.", this);
            return;
        }

        // 진행 순서는 UI가 아니라 DayManager가 책임진다.
        farmManager.PrepareForNewDay();
        harvestManager.BeginNewDay();
        DayNumber++;

        BeginCountdown();
    }

    private void SetState(DayState state)
    {
        if (CurrentState == state)
            return;

        CurrentState = state;
        StateChanged?.Invoke(state);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
