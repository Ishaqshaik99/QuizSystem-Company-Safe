# QuizSystem (.NET 10 + SQL Server)

Production-ready **Online Quiz & Examination System** using:

- Backend: ASP.NET Core Web API (.NET 10)
- Frontend: Blazor Web App (.NET 10, Interactive Server)
- Database: SQL Server + EF Core (Code-First)
- Auth: ASP.NET Core Identity + JWT + role-based authorization
- Roles: Admin, Instructor, Student
- Architecture: Clean Architecture (`Core`, `Infrastructure`, `Api`, `UI`)

## Why Blazor Web App (Server Interactive)

This implementation uses **Blazor Web App with Interactive Server** for low-latency UI updates and stable real-time exam UX (countdown + autosave status) while still consuming the API via JWT.

See `/docs` for endpoint lists, workflow, schema notes, and seed users.

## Share-safe package note

This package excludes executable script files and redacts credential-like literals for safer corporate sharing.
Use manual `dotnet ef` and `dotnet run` commands from docs.
