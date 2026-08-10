# Database (SEAL)

Schema + seed data cho DB Postgres cua du an SEAL, dung chung cho ca nhom.

## Cau truc

- `migrations/001_seal_schema.sql` — schema day du (20 bang), export truc
  tiep tu DB dev thuc te bang `pg_dump --schema-only`, khop 100% voi entity
  C# hien tai (repo backend khong dung EF Migrations).
- `seed/001_seed_roles.sql` — tao san 1 tai khoan cho **moi role** trong he
  thong (Admin, EventCoordinator, Judge, Mentor, TeamLeader, TeamMember,
  Student chua vao doi) kem 1 School/Event/Round/Track/Team lam moi truong
  test. Idempotent — chay lai nhieu lan khong loi.
- `docker-compose.yml` — Postgres 18 dung chung, tranh sai khac moi truong
  giua cac may.

## Cach dung nhanh (local, khong Docker)

```bash
createdb -U postgres seal_dev
psql -U postgres -d seal_dev -f database/migrations/001_seal_schema.sql
psql -U postgres -d seal_dev -f database/seed/001_seed_roles.sql
```

## Cach dung voi Docker

```bash
docker compose -f database/docker-compose.yml up -d
psql -h localhost -p 5432 -U postgres -d seal_dev -f database/migrations/001_seal_schema.sql
psql -h localhost -p 5432 -U postgres -d seal_dev -f database/seed/001_seed_roles.sql
```

## Tai khoan test (mat khau chung: `123456`)

| Email             | Role            |
| ----------------- | --------------- |
| admin@seal.com    | Admin           |
| ec@seal.com       | EventCoordinator|
| judge@seal.com    | Judge           |
| mentor@seal.com   | Mentor          |
| leader@seal.com   | TeamLeader      |
| member@seal.com   | TeamMember      |
| student@seal.com  | Student (chua co doi) |

## Workflow

1. Branch off `feature/database` cho tung task.
2. Mo PR ve lai `feature/database` (hoac thang vao `dev`, theo quy uoc nhom)
   khi CI xanh.
3. `feature/database` merge vao `dev` khi phan schema/migration da on dinh.
