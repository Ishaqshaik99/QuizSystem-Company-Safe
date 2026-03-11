namespace QuizSystem.Core.DTOs;

public class ScoreDistributionBinDto
{
    public string RangeLabel { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class QuestionPerformanceDto
{
    public Guid QuestionId { get; set; }
    public string Stem { get; set; } = string.Empty;
    public decimal CorrectRate { get; set; }
    public IReadOnlyDictionary<string, int> CommonWrongOptions { get; set; } = new Dictionary<string, int>();
}

public class TopicPerformanceDto
{
    public string TopicName { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public decimal Accuracy { get; set; }
}

public class TrendPointDto
{
    public DateTime AttemptDateUtc { get; set; }
    public decimal Percentage { get; set; }
}

public class StudentDashboardDto
{
    public decimal OverallAccuracy { get; set; }
    public IReadOnlyCollection<TopicPerformanceDto> TopicWise { get; set; } = Array.Empty<TopicPerformanceDto>();
    public IReadOnlyCollection<TrendPointDto> Trend { get; set; } = Array.Empty<TrendPointDto>();
}

public class InstructorQuizAnalyticsDto
{
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public decimal AverageScore { get; set; }
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public IReadOnlyCollection<ScoreDistributionBinDto> ScoreDistribution { get; set; } = Array.Empty<ScoreDistributionBinDto>();
    public IReadOnlyCollection<QuestionPerformanceDto> QuestionPerformance { get; set; } = Array.Empty<QuestionPerformanceDto>();
    public IReadOnlyCollection<TopicPerformanceDto> TopicPerformance { get; set; } = Array.Empty<TopicPerformanceDto>();
}

public class AdminOverviewDto
{
    public int TotalUsers { get; set; }
    public int TotalQuizzes { get; set; }
    public int TotalAttempts { get; set; }
    public decimal AverageScorePercentage { get; set; }
}
