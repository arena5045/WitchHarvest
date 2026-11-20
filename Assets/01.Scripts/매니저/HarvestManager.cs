using System;
using System.Collections.Generic;
using UnityEngine;

public class HarvestManager : MonoBehaviour
{
    public static HarvestManager Instance { get; private set; }

    public int TotalHarvestCount { get; private set; }
    public int DailyHarvestCount { get; private set; }

    private readonly Dictionary<CropData, int> harvestedCrops = new();
    private readonly Dictionary<CropData, int> dailyHarvestedCrops = new();

    public IReadOnlyDictionary<CropData, int> HarvestedCrops => harvestedCrops;
    public IReadOnlyDictionary<CropData, int> DailyHarvestedCrops => dailyHarvestedCrops;

    // 수확 또는 하루 초기화가 끝난 뒤 UI 등에 변경 사실을 알린다.
    public event Action HarvestChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("HarvestManager가 중복 생성되었습니다.", this);
            enabled = false;
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void RegisterHarvest(CropData cropData)
    {
        if (cropData == null)
            return;

        TotalHarvestCount++;
        DailyHarvestCount++;

        harvestedCrops.TryGetValue(cropData, out int total);
        harvestedCrops[cropData] = total + 1;

        dailyHarvestedCrops.TryGetValue(cropData, out int daily);
        dailyHarvestedCrops[cropData] = daily + 1;

        // 모든 기록을 갱신한 뒤 알린다.
        HarvestChanged?.Invoke();
    }

    public void BeginNewDay()
    {
        // 전체 누적 기록은 유지한다.
        DailyHarvestCount = 0;
        dailyHarvestedCrops.Clear();

        HarvestChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
