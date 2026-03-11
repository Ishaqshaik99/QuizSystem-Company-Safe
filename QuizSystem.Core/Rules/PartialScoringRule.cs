using QuizSystem.Core.Enums;

namespace QuizSystem.Core.Rules;

public static class PartialScoringRule
{
    public static decimal CalculateObjectiveScore(
        QuestionType questionType,
        IReadOnlyCollection<Guid> correctOptionIds,
        IReadOnlyCollection<Guid> selectedOptionIds,
        decimal marks,
        decimal negativeMarks,
        bool allowPartialScoring)
    {
        if (selectedOptionIds.Count == 0)
        {
            return 0;
        }

        if (questionType is QuestionType.McqSingle or QuestionType.TrueFalse)
        {
            var isCorrect = selectedOptionIds.Count == 1 && selectedOptionIds.All(correctOptionIds.Contains);
            return isCorrect ? marks : -Math.Max(negativeMarks, 0);
        }

        if (questionType != QuestionType.McqMultiple)
        {
            return 0;
        }

        var correctSelected = selectedOptionIds.Count(correctOptionIds.Contains);
        var incorrectSelected = selectedOptionIds.Count(id => !correctOptionIds.Contains(id));

        if (!allowPartialScoring)
        {
            var exactMatch = incorrectSelected == 0 && correctSelected == correctOptionIds.Count;
            return exactMatch ? marks : -Math.Max(negativeMarks, 0);
        }

        if (correctOptionIds.Count == 0)
        {
            return 0;
        }

        var positivePart = marks * (correctSelected / (decimal)correctOptionIds.Count);
        var negativePart = Math.Max(negativeMarks, 0) * incorrectSelected;
        var net = positivePart - negativePart;

        return Math.Clamp(net, -Math.Max(negativeMarks, 0), marks);
    }

    public static bool IsShortAnswerCorrect(string? expected, string? actual, bool caseSensitive)
    {
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
        {
            return false;
        }

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return string.Equals(expected.Trim(), actual.Trim(), comparison);
    }
}
