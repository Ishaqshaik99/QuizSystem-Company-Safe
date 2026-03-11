# Seed Users

Seeding runs on API startup (or `--seed-only`).

## Default Accounts

- Admin
  - Email: `admin@quizsystem.local`
  - Password: `<SET_PASSWORD>`

- Instructor
  - Email: `instructor@quizsystem.local`
  - Password: `<SET_PASSWORD>`

- Student 1
  - Email: `student1@quizsystem.local`
  - Password: `<SET_PASSWORD>`

- Student 2
  - Email: `student2@quizsystem.local`
  - Password: `<SET_PASSWORD>`

Before first run, choose strong passwords and set them in:
- `QuizSystem.Infrastructure/Seed/DatabaseSeeder.cs`

Change credentials immediately in production.
