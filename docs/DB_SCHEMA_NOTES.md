# DB Schema Notes

Primary schema is generated from EF Core code-first models.

## Identity

- `AspNetUsers` (extended as `ApplicationUser` with `FullName`)
- `AspNetRoles`, `AspNetUserRoles`, and related Identity tables

## Domain Tables

- `Topics`
- `GroupClasses`
- `GroupMemberships`
- `Questions`
- `QuestionOptions`
- `Quizzes`
- `QuizQuestions`
- `QuizAssignments`
- `Attempts`
- `AttemptAnswers`
- `RefreshTokens`

## Important Constraints

- Unique topic name
- Unique group name per instructor
- Unique quiz-question mapping
- Unique attempt answer per (attempt, question)
- Unique refresh token value

## Timing & Integrity

- `Attempts.StartedAtUtc` and `Attempts.EndsAtUtc` are server-authoritative.
- In-progress attempts are auto-submitted when expired.
- Attempts cannot be modified after submission or after end time.
