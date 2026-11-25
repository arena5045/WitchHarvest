using System;
using System.Collections.Generic;

// 세이브 파일에서 노드를 식별하기 위한 정보.
// SO 참조 자체는 저장하지 않는다.
[Serializable]
public sealed class UpgradeLevelSaveData
{
    public string nodeId;
    public int level;
}

// 마지막으로 완료한 낮의 진행 상태.
[Serializable]
public sealed class GameSaveData
{
    public int saveVersion;
    public int day;
    public long gold;
    public List<UpgradeLevelSaveData> upgrades;
}