# SU26_SWP_BL3W_BE — SEAL Backend

CI/CD environment + backend source for the SEAL project (SWP391 SU26 – Group BL3W).

This repository holds the **backend** (.NET) under [`backend/`](backend/) — see [`backend/README.md`](backend/README.md) for the full architecture, tech stack, and setup guide. The **frontend** lives in a separate repository, [`SU26_SWP_BL3W_FE`](https://github.com/SU26-SWP-BL3W/SU26_SWP_BL3W_FE) (not a subfolder here, unlike the original plan below).

## Scope (per assignment)

| Layer     | Status                                                                                                   |
| --------- | ---------------------------------------------------------------------------------------------------------|
| Backend   | ✅ Shared scaffold (Domain/Infrastructure/API skeleton) done; **Flow 4 — Submissions & Scoring** implemented. Other flows (Auth, Events, Teams, Results & Prizes) are still per-teammate work in progress. See [`backend/README.md`](backend/README.md). |
| Frontend  | Moved to its own repo — [`SU26_SWP_BL3W_FE`](https://github.com/SU26-SWP-BL3W/SU26_SWP_BL3W_FE). Scaffold (4-layer MVVM + Repository) done; feature screens still in progress. |
| Database  | Schema tracked via EF Core migrations in [`backend/SEAL.Infrastructure/Migrations`](backend/SEAL.Infrastructure/Migrations). Open PR #3 (`feature/database → main`) still pending review/merge. |

## CI pipeline

GitHub Actions workflow lives at [.github/workflows/ci.yml](.github/workflows/ci.yml).

Triggers:
- `push` to `main` or `dev`
- `pull_request` targeting `main` or `dev`
- Manual dispatch from the Actions tab

Behavior:
- Runs `dotnet restore/build/test` against [`backend/SEAL_Backend.slnx`](backend/SEAL_Backend.slnx).

## Branch model

- `main` — production-ready
- `dev` — integration branch for daily work

Open PRs into `dev`; promote `dev → main` on release.

> ⚠️ **Known issue**: this repo's GitHub default branch is currently still set to `main` (Settings → Branches), so the "Create pull request" button defaults its base to `main` instead of `dev`. Until an org admin changes the default branch, **always double-check/switch the base branch to `dev` before opening a PR.** (This has already caused one PR to get merged into `main` by mistake — see git history around 2026-08-11.)

## CI verification

This line confirms the branch → PR → CI flow works end to end: a change on
`test/ci-verify` opens a PR into `dev`, and GitHub Actions runs automatically.
