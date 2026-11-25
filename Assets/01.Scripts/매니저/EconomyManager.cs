using System;
using System.Collections.Generic;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    // 현재 사용할 수 있는 골드. UI나 다른 시스템에서 직접 대입하지 않는다.
    public long Gold { get; private set; }
    public event Action GoldChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("EconomyManager가 중복 생성되었습니다.", this);
            enabled = false;
            Destroy(this);
            return;
        }

        // 씬 단위 싱글턴. 씬 전환 유지나 저장 기능은 아직 추가하지 않는다.
        Instance = this;
    }

    public long CalculateHarvestGold(IReadOnlyDictionary<CropData, int> harvestedCrops)
    {
        if (harvestedCrops == null)
            throw new ArgumentNullException(nameof(harvestedCrops));

        long total = 0;

        foreach (KeyValuePair<CropData, int> crop in harvestedCrops)
        {
            // 가격 SO는 읽기만 한다. 곱셈 전에 long으로 변환해 int 범위를 넘는
            // 판매금도 계산하며, long 범위 초과는 조용히 잘못된 값으로 바꾸지 않는다.
            total = checked(total + (long)crop.Key.SellPrice * crop.Value);
        }

        return total;
    }

    public void AddGold(long amount)
    {
        if (amount <= 0)
            return;

        Gold = checked(Gold + amount);

        // 잔액을 먼저 확정한 다음 UI에 알린다. UI는 지급을 수행하지 않는다.
        GoldChanged?.Invoke();
    }

    public bool TrySpendGold(long cost)
    {
        if (!TrySpendGoldWithoutNotification(cost))
            return false;

        if (cost > 0)
            NotifyGoldChanged();

        return true;
    }

    // 노드 구매에서 골드와 구매 레벨을 함께 확정한 뒤 알리기 위한 내부 처리다.
    internal bool TrySpendGoldWithoutNotification(long cost)
    {
        if (cost < 0 || Gold < cost)
            return false;

        Gold -= cost;
        return true;
    }

    internal void NotifyGoldChanged()
    {
        GoldChanged?.Invoke();
    }

    // 로드 시 저장된 잔액으로 교체한다.
    // 신규 골드 획득이나 판매금 지급으로 취급하지 않는다.
    internal void RestoreGoldWithoutNotification(long savedGold)
    {
        Gold = savedGold;
    }


    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
