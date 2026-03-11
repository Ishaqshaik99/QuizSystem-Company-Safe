using QuizSystem.Core.Common;
using QuizSystem.Core.Entities;
using QuizSystem.Core.Enums;

namespace QuizSystem.Core.Rules;

public static class AttemptTimingGuard
{
    public static void EnsureAttemptCanBeModified(Attempt attempt, DateTime nowUtc)
    {
        if (attempt.Status != AttemptStatus.InProgress)
        {
            throw new AppException("Attempt is already finalized.");
        }

        if (nowUtc > attempt.EndsAtUtc)
        {
            throw new AppException("Attempt time has expired. Submit is auto-enforced by server.");
        }
    }

    public static int RemainingSeconds(Attempt attempt, DateTime nowUtc)
    {
        var remaining = (int)Math.Floor((attempt.EndsAtUtc - nowUtc).TotalSeconds);
        return Math.Max(0, remaining);
    }
}
