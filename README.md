# SU26_SWP_BL3W — SEAL DevOps

CI/CD environment setup for the SEAL project (SWP391 SU26 – Group BL3W).

This repository is the **DevOps skeleton**: it contains no application code yet, only the CI pipeline. Push Backend / Frontend / Database code here and the pipeline will pick it up automatically.

## Scope (per assignment)

| Layer     | Status              |
| --------- | ------------------- |
| Backend   | To be added         |
| Frontend  | To be added         |
| Database  | To be added         |

## CI pipeline

GitHub Actions workflow lives at [.github/workflows/ci.yml](.github/workflows/ci.yml).

Triggers:
- `push` to `main` or `dev`
- `pull_request` targeting `main` or `dev`
- Manual dispatch from the Actions tab

Behavior:
- Auto-detects .NET projects (any `*.csproj`) and runs `restore → build → test` on .NET 10.
- Skips the .NET job cleanly when no .csproj is present (so the initial empty repo does not show a red check).

## How to add code

1. **Backend (.NET)** — copy your solution/csproj files to the repo root. The CI will run on the next push.
2. **Frontend** — add under a `frontend/` folder (or wherever fits). Extend the workflow with a `frontend` job when ready.
3. **Database** — add migrations / SQL scripts under a `database/` folder. Add a schema-lint or migration-check job as needed.

## Branch model

- `main` — production-ready
- `dev` — integration branch for daily work

Open PRs into `dev`; promote `dev → main` on release.
