# Backend

Placeholder branch for the SEAL Backend (.NET) code.

## What goes here

Copy the backend solution into this folder (or repo root, matching whatever
layout the CI workflow expects) — e.g. `SEAL_Backend/`, `SEAL.Application/`,
`SEAL.Domain/`, `SEAL.Infrastructure/`, `SEAL.Tests.Application/` and the
`.slnx`/`.sln` file.

## CI

`.github/workflows/ci.yml` auto-detects any `*.csproj` in the repo and runs
`dotnet restore/build/test` on push/PR. No changes needed to the workflow
once backend code is added here — it activates automatically.

## Workflow

1. Branch off `feature/backend` for your task.
2. Open a PR back into `feature/backend` (or directly into `dev`, per team
   convention) once CI is green.
3. `feature/backend` merges into `dev` when the backend milestone is stable.
