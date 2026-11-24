using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class DialogueExpression
{
    [SerializeField] private string expressionId = "기본";
    [SerializeField] private Sprite sprite;

    public string ExpressionId => expressionId;
    public Sprite Sprite => sprite;
}

[CreateAssetMenu(
    fileName = "Character_",
    menuName = "Witch Harvest/Dialogue/Character")]
public sealed class DialogueCharacterData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField]
    private DialogueExpression[] expressions =
        { new DialogueExpression() };

    public string DisplayName => displayName;

    public IEnumerable<string> GetExpressionIds()
    {
        if (expressions == null)
            yield break;

        foreach (DialogueExpression expression in expressions)
        {
            if (expression != null &&
                !string.IsNullOrWhiteSpace(expression.ExpressionId))
            {
                yield return expression.ExpressionId;
            }
        }
    }

    public bool TryGetPortrait(string expressionId, out Sprite sprite)
    {
        sprite = null;

        if (expressions == null || string.IsNullOrWhiteSpace(expressionId))
            return false;

        bool found = false;

        foreach (DialogueExpression expression in expressions)
        {
            if (expression == null ||
                expression.ExpressionId != expressionId)
                continue;

            // 같은 표정 이름이 중복되면 임의의 그림을 선택하지 않는다.
            if (found)
            {
                sprite = null;
                return false;
            }

            found = true;
            sprite = expression.Sprite;
        }

        return found && sprite != null;
    }
}