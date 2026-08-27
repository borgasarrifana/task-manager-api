# Task Manager API

![CI](https://github.com/borgasarrifana/task-manager-api/actions/workflows/ci.yml/badge.svg)

A RESTful task management API built with ASP.NET Core, demonstrating a layered architecture, JWT authentication, and a relational data model with EF Core and PostgreSQL.

**Live API:** https://task-manager-api-1-iusg.onrender.com
**Frontend:** https://task-manager-ui-ruby.vercel.app

## Overview

This project models Projects and Tasks, where each Project can contain many Tasks. It was built as a hands-on learning project to practice core backend patterns expected in professional ASP.NET Core development: clean separation of concerns, secure authentication, role-based authorization, and proper REST API design.

## Tech Stack

- **ASP.NET Core (.NET 10)** — Controller-based Web API
- **Entity Framework Core** — ORM, code-first migrations
- **PostgreSQL** (hosted on Supabase) — relational database, accessed via Session Pooler
- **JWT Bearer Authentication** — token-based auth with role claims
- **BCrypt.Net** — secure password hashing
- **Swagger / OpenAPI** — interactive API documentation
- **Render** — hosting (Frankfurt region, co-located with the database for low latency)

## Architecture

The project follows a layered architecture to separate concerns:

```
Controllers  →  Services  →  DbContext (EF Core)  →  PostgreSQL
```

- **Controllers** handle HTTP requests/responses and route to the appropriate service.
- **Services** contain business logic and orchestrate data access.
- **DbContext** (EF Core) maps domain models to the PostgreSQL database.
- **DTOs** are used at the API boundary to decouple internal models from request/response shapes and to validate input.
- **Global exception handling middleware** centralizes error responses.

## Features

- Full CRUD for **Projects** and **Tasks**, with tasks nested under their parent project (`/api/projects/{projectId}/tasks`)
- **JWT authentication**: register/login endpoints issue signed tokens; passwords are hashed with BCrypt
- **User ownership scoping**: all project/task data is scoped to the authenticated user via `ClaimTypes.NameIdentifier`
- **Role-based authorization**: `UserRole` enum (`Member`, `Admin`) stored on the `User` model, embedded in the JWT as a role claim and returned in the auth response
- **Input validation** on all write endpoints via DTOs and model validation
- **Global exception handling** middleware for consistent error responses
- Correct HTTPS scheme detection behind Render's reverse proxy via forwarded headers

## Data Model

- **User** — id, credentials (hashed), role
- **Project** — id, name, owner (User), collection of Tasks
- **TaskItem** — id, title, description, priority, due date, completion status, parent Project

## Database Configuration

The API connects to Supabase PostgreSQL using the **Session Pooler** (port `5432`), which is required for compatibility with EF Core's prepared statements on Render's free tier. The Transaction Pooler (port 6543) and the direct connection string are not used — the former breaks prepared statements via PgBouncer, and the latter fails over IPv6 on Render.

## Getting Started

### Prerequisites

- .NET 10 SDK
- A PostgreSQL database (e.g. a free Supabase project)

### Setup

1. Clone the repository
2. Configure your connection string and JWT settings via environment variables or `appsettings.Development.json`:
   - `ConnectionStrings__DefaultConnection`
   - `JwtSettings__Secret`
3. Apply EF Core migrations:
   ```bash
   dotnet ef database update
   ```
4. Run the API:
   ```bash
   dotnet run
   ```
5. Swagger UI is available at `/swagger` in development.

## Roadmap

- Refresh tokens
- Admin-gated endpoints and role-aware authorization (Stage 2+)
- Unit tests
- Pagination
- Structured logging
- Background jobs / SignalR
- Docker & CI/CD

## Related Repositories

- Frontend: [`task-manager-ui`](https://github.com/borgasarrifana/task-manager-ui) — React + Vite + Tailwind CSS, JARVIS/HUD-themed UI