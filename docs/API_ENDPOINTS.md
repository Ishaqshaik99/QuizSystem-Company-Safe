# API Endpoints

Base URL: `https://localhost:7001`

All secured endpoints use JWT bearer token.

## Auth

- `POST /api/auth/register` - AllowAnonymous
- `POST /api/auth/login` - AllowAnonymous
- `POST /api/auth/refresh` - AllowAnonymous
- `POST /api/auth/logout` - Authenticated

## Admin - Users & Roles

- `GET /api/admin/users` - Admin
- `GET /api/admin/users/roles` - Admin
- `POST /api/admin/users` - Admin
- `PUT /api/admin/users/{userId}/lock` - Admin
- `POST /api/admin/users/assign-role` - Admin

## Questions

- `GET /api/questions` - Instructor/Admin
- `GET /api/questions/{questionId}` - Instructor/Admin
- `POST /api/questions` - Instructor/Admin
- `PUT /api/questions/{questionId}` - Instructor/Admin
- `DELETE /api/questions/{questionId}` - Instructor/Admin

## Quizzes

- `GET /api/quizzes/mine` - Instructor
- `GET /api/quizzes/all` - Admin
- `GET /api/quizzes/assigned` - Student
- `GET /api/quizzes/{quizId}` - Instructor/Admin
- `POST /api/quizzes` - Instructor/Admin
- `PUT /api/quizzes/{quizId}` - Instructor/Admin
- `DELETE /api/quizzes/{quizId}` - Instructor/Admin
- `POST /api/quizzes/{quizId}/publish` - Instructor/Admin
- `POST /api/quizzes/assign` - Instructor/Admin
- `POST /api/quizzes/groups` - Instructor/Admin
- `GET /api/quizzes/groups` - Instructor/Admin

## Attempts

- `POST /api/attempts/start` - Student
- `GET /api/attempts/{attemptId}/session` - Student
- `POST /api/attempts/{attemptId}/answers` - Student
- `POST /api/attempts/{attemptId}/submit` - Student
- `POST /api/attempts/{attemptId}/auto-submit` - Student
- `POST /api/attempts/auto-submit-expired` - Instructor/Admin
- `GET /api/attempts/mine` - Student
- `GET /api/attempts/all` - Admin
- `GET /api/attempts/quiz/{quizId}` - Instructor/Admin
- `GET /api/attempts/{attemptId}` - Student
- `POST /api/attempts/{attemptId}/grade-short-answer` - Instructor/Admin

## Results & Reports

- `GET /api/results/student/dashboard` - Student
- `GET /api/results/quiz/{quizId}/analytics` - Instructor/Admin
- `GET /api/results/admin/overview` - Admin

## Infra

- `GET /health` - health check
- `GET /swagger` - Swagger UI (supports JWT Authorize)
