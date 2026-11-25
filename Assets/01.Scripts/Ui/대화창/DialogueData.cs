using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public enum DialogueSide
{
    Left = 0,
    Right = 1
}

[Serializable]
public sealed class DialogueLine
{
    [SerializeField] private DialogueCharacterData character;

    [SerializeField, ValueDropdown(nameof(GetExpressionIds))]
    private string expressionId = "기본";

    [SerializeField] private DialogueSide side;

    [Tooltip("체크하면 스탠딩 일러스트를 좌우 반전합니다.")]
    [SerializeField] private bool flipHorizontal;

    public bool FlipHorizontal => flipHorizontal;

    [SerializeField, TextArea(2, 6)]
    private string text;

    public DialogueCharacterData Character => character;
    public string ExpressionId => expressionId;
    public DialogueSide Side => side;
    public string Text => text;

    private IEnumerable<string> GetExpressionIds()
    {
        return character != null
            ? character.GetExpressionIds()
            : Array.Empty<string>();
    }
}

[CreateAssetMenu(
    fileName = "Dialogue_",
    menuName = "Witch Harvest/Dialogue/Dialogue")]
public sealed class DialogueData : ScriptableObject
{
    [Header("회고록 정보")]
    [Tooltip("대화마다 다른 고유 ID. 저장 파일에서 사용합니다.")]
    [SerializeField] private string dialogueId;

    [SerializeField] private string memoirTitle;

    public string DialogueId => dialogueId;
    public string MemoirTitle => memoirTitle;

    [SerializeField]
    private DialogueLine[] lines =
        { new DialogueLine() };

    public IReadOnlyList<DialogueLine> Lines => lines;
}