// 노드의 구매 상태다. 실제 테두리 색상은 UI에서 결정한다.
public enum UpgradeNodeState
{
    Locked = 0,        // 선행 조건 미충족
    Unaffordable = 1,  // 선행 조건 충족, 골드 부족
    Purchasable = 2,   // 지금 구매 가능
    Maxed = 3          // 최대 레벨 달성
}