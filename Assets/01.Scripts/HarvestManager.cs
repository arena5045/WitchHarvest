
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HarvestManager : MonoBehaviour
{
    public static HarvestManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text harvestCountText;

    public int TotalHarvestCount { get; private set; }

    // 작물 종류별 수확 개수를 기록한다.
    // 이후 하루 정산 및 판매 시스템에서 활용할 데이터다.
    private readonly Dictionary<CropData, int> harvestedCrops = new();

    public IReadOnlyDictionary<CropData, int> HarvestedCrops => harvestedCrops;

    private void Awake()
    {
        // 현재 씬에서 HarvestManager가 중복 생성되지 않도록 한다.
        if (Instance != null && Instance != this)
        {
            Debug.LogError("HarvestManager가 중복 생성되었습니다.", this);
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        UpdateDisplay();
    }

    private void OnDestroy()
    {
        // 씬이 종료될 때 정적 참조를 정리한다.
        if (Instance == this)
            Instance = null;
    }

    public void RegisterHarvest(CropData cropData)
    {
        if (cropData == null)
            return;

        TotalHarvestCount++;

        // 동일 종류의 작물은 하나의 항목에 수량을 누적한다.
        harvestedCrops.TryGetValue(cropData, out int count);
        harvestedCrops[cropData] = count + 1;

        UpdateDisplay();

        Debug.Log($"Total Harvested: {TotalHarvestCount}");
    }

    private void UpdateDisplay()
    {
        if (harvestCountText != null)
        {
            harvestCountText.text = $"수확량: {TotalHarvestCount}";
        }
    }
}
