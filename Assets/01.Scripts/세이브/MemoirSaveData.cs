using System;
using System.Collections.Generic;

[Serializable]
public sealed class MemoirSaveData
{
    public int saveVersion;
    public List<string> dialogueIds;
}