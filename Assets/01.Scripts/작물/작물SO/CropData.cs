
using UnityEngine;

[CreateAssetMenu(fileName = "CropData", menuName = "WitchHarvest/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string cropName;
    [SerializeField] private Sprite sprite;

    [Header("능력치")]
    [SerializeField, Min(1)] private int maxHealth = 3;
    [SerializeField, Min(0)] private int sellPrice = 10;

    // 외부에서는 데이터를 읽기만 가능하도록 제한한다.
    public string CropName => cropName;
    public Sprite Sprite => sprite;
    public int MaxHealth => maxHealth;
    public int SellPrice => sellPrice;
}
