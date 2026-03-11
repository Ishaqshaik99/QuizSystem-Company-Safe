namespace QuizSystem.Core.Enums;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Instructor = "Instructor";
    public const string Student = "Student";

    public static readonly IReadOnlyList<string> All = new[] { Admin, Instructor, Student };
}
