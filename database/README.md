# Database

Placeholder branch for database schema, migrations, and seed data.

## What goes here

- EF Core migrations (if reused from the backend project), or raw SQL
  scripts under e.g. `database/migrations/`.
- A `docker-compose.yml` service definition for local Postgres/SQL Server,
  so the whole team spins up an identical DB.
- Seed/sample data scripts, if any.

## CI/CD ideas

Once real migration files land here:

- Add a CI step that spins up a throwaway DB container and runs migrations
  against it, to catch broken migrations before merge.
- On deploy (CD), run `dotnet ef database update` (or the SQL equivalent)
  against the target environment as a pipeline step, before/alongside the
  backend deploy.

## Workflow

1. Branch off `feature/database` for your task.
2. Open a PR back into `feature/database` (or directly into `dev`, per team
   convention) once checks are green.
3. `feature/database` merges into `dev` when the schema/migration work is
   stable.
