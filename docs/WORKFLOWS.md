# Workflows

## 1) Authentication

1. User logs in via `/api/auth/login`.
2. API returns access token + refresh token.
3. UI stores tokens in browser local storage and adds bearer token for API calls.
4. Refresh flow uses `/api/auth/refresh`.

## 2) Instructor Quiz Lifecycle

1. Instructor creates question bank entries.
2. Instructor creates quiz with configurable rules:
   - marks per question
   - negative marking
   - attempt limits
   - shuffle questions/options
   - autosave interval
3. Instructor publishes quiz.
4. Instructor assigns quiz to students or groups.

## 3) Student Attempt Lifecycle

1. Student starts attempt (`/api/attempts/start`).
2. Server persists start/end timestamps and enforces timer.
3. UI autosaves every N seconds via `/api/attempts/{id}/answers`.
4. Student submits via `/api/attempts/{id}/submit`.
5. If timer expires, server auto-submits (`background service + endpoint`).
6. Late saves/submits are blocked by server-side timing checks.

## 4) Grading & Results

1. Objective questions are auto-graded at submit.
2. Short answers are either auto-checked (if enabled) or left for manual grading.
3. Instructor grades short answers via grade endpoint.
4. Attempt score/percentage recalculates and analytics update.

## 5) Reporting

- Student dashboard:
  - overall accuracy
  - topic-wise performance
  - trend over time
- Instructor/Admin reports:
  - score distribution
  - avg/min/max
  - question-wise correctness and common wrong options
  - topic-wise performance
