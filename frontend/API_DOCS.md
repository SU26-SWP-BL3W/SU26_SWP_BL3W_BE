# SEAL Platform — Backend API Documentation

> **Branch**: `dev` @ `903b8f9`
> **Stack**: ASP.NET Core (Clean Architecture + MediatR/CQRS + FluentValidation + JWT)
> **Generated for**: Frontend integration

---

## 1. Global Conventions

### 1.1 Base URL

```
{HOST}/api
```

All routes below are relative to `/api`.

### 1.2 Standard Response Envelope

Every successful or handled-error response is wrapped in `BaseResponse<T>`:

```ts
interface BaseResponse<T> {
  data: T | null;
  message: string;
  statusCode: number;   // mirrors HTTP status
  success: boolean;     // true iff statusCode === 200
}
```

### 1.3 Pagination

List endpoints accept a `BasePaginationQuery` via `[FromQuery]`:

| Param         | Type   | Default | Notes                                |
|---------------|--------|---------|--------------------------------------|
| `PageNumber`  | int    | 1       |                                      |
| `PageSize`    | int    | 10      |                                      |
| `SortBy`      | string | —       | Whitelisted per endpoint (see below) |
| `IsAscending` | bool   | false   |                                      |

Paginated response shape:

```ts
interface PagedResult<T> {
  data: T[];
  currentPage: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
```

### 1.4 Auth

- **Scheme**: JWT Bearer
- **Header**: `Authorization: Bearer <accessToken>`
- **Login**: `POST /api/Auth/login` returns `accessToken` + `refreshToken`
- **Refresh**: `POST /api/Auth/refresh-token`
- **Authorization model**:
  - `[Authorize(Roles = "Admin")]` — global admin (used on `UsersController`)
  - `[EventRoleAuthorize(EventRoleType...)]` — custom filter that checks the caller's `EventRole` for the event referenced in the request

### 1.5 Enum: `EventRoleType`

Sent/received as **integer** in payloads, **string** in some response models.

| Value | Name               |
|-------|--------------------|
| 0     | `EventCoordinator` |
| 1     | `Judge`            |
| 2     | `Mentor`           |
| 3     | `TeamLeader`       |
| 4     | `TeamMember`       |

### 1.6 Error Handling

All exceptions flow through `GlobalExceptionMiddleware`:

| Exception              | HTTP |
|------------------------|------|
| `UnauthorizedException`| 401  |
| `ForbiddenException`   | 403  |
| `ErrorException`       | (custom statusCode in payload) |
| FluentValidation fail  | 400  |
| Unhandled              | 500  |

Error body keeps the same envelope — `data: null`, `success: false`, `message` describing the failure.

### 1.7 Date/time

- **Requests**: `DateTime` (ISO-8601, no offset)
- **Responses** (`CreatedTime`, `LastUpdatedTime`): `DateTimeOffset` (ISO-8601 with offset)

---

## 2. Endpoints

### 2.1 Auth — `/api/Auth`

| Verb | Route              | Auth          | Purpose                          |
|------|--------------------|---------------|----------------------------------|
| POST | `/register`        | Anonymous     | Register a user                  |
| POST | `/login`           | Anonymous     | Login, returns JWT pair          |
| POST | `/refresh-token`   | Anonymous     | Rotate access token              |
| POST | `/logout`          | JWT           | Invalidate refresh token         |
| GET  | `/verify-email`    | Anonymous     | Email verification via `?token=` |

**`POST /register`** — body `RegisterUserRequestModel`:
```ts
{ SchoolId: string; StudentCode?: string; Email: string; Password: string;
  FullName: string; IsStudent: boolean }
```
Returns `UserModel` (see §3.1).

**`POST /login`** — body `LoginUserRequestModel`:
```ts
{ Email: string; Password: string }
```
Returns:
```ts
{ accessToken: string; refreshToken: string; userId: string;
  email: string; fullName: string; isAdmin: boolean; isStudent: boolean }
```

**`POST /refresh-token`** — body `{ RefreshToken: string }` → `{ accessToken, refreshToken }`.

**`POST /logout`** — empty body → `boolean`.

**`GET /verify-email?token=...`** → `boolean`.

---

### 2.2 Users — `/api/Users` 🔒 `Roles = "Admin"` (controller-wide)

| Verb   | Route   | Purpose            |
|--------|---------|--------------------|
| GET    | `/`     | List users (paged) |
| GET    | `/{id}` | Get user           |
| POST   | `/`     | Create user        |
| PUT    | `/{id}` | Update user        |
| DELETE | `/{id}` | Soft-delete user   |

Pagination `SortBy` ∈ { `FullName`, `Email`, `CreatedTime` }.

**Create body** (`CreateUserRequestModel`):
```ts
{ SchoolId: string; StudentCode?: string; Email: string; Password: string;
  FullName: string; IsStudent: boolean; IsAdmin: boolean }
```

**Update body** (`UpdateUserRequestModel`):
```ts
{ SchoolId: string; StudentCode?: string; FullName: string;
  IsStudent: boolean; IsAdmin: boolean; IsApproved: boolean }
```

Response shape — see `UserModel` (§3.1).

---

### 2.3 Events — `/api/Events`

| Verb   | Route          | Auth                         | Purpose                  |
|--------|----------------|------------------------------|--------------------------|
| GET    | `/`            | —                            | List events (paged)      |
| GET    | `/upcoming`    | —                            | Upcoming events (paged)  |
| GET    | `/{id}`        | —                            | Get event                |
| POST   | `/`            | —                            | Create event             |
| PUT    | `/{eventId}`   | `EventCoordinator`           | Update event             |
| DELETE | `/{eventId}`   | `EventCoordinator`           | Soft-delete event        |

List query: `SearchName?`, `SortBy` ∈ { `EventName`, `StartDate`, `EndDate`, `Year` }.

**Create / Update body**:
```ts
{ EventName: string; Season?: string; Year: number;
  StartDate: string; EndDate: string; Description?: string }
```

Response — `EventModel` (§3.2).

---

### 2.4 Criterias — `/api/Criterias`

| Verb   | Route                 | Purpose                  |
|--------|-----------------------|--------------------------|
| GET    | `/`                   | List (paged)             |
| GET    | `/{id}`               | Get one                  |
| POST   | `/`                   | Create                   |
| PUT    | `/{id}`               | Update                   |
| DELETE | `/{id}`               | Soft-delete              |
| PATCH  | `/{id}/toggle-status` | Toggle `IsActive`        |

**Create**: `{ CriteriaName: string; Description?: string; IsActive: boolean }`
**Update**: same fields, `IsActive` required.

Response — `CriteriaModel` (§3.3).

---

### 2.5 Schools — `/api/Schools`

| Verb   | Route               | Purpose                          |
|--------|---------------------|----------------------------------|
| GET    | `/`                 | List (paged)                     |
| GET    | `/{id}`             | Get one                          |
| GET    | `/with-user-count`  | Schools + member counts (no pag) |
| POST   | `/`                 | Create                           |
| PUT    | `/{id}`             | Update                           |
| DELETE | `/{id}`             | Soft-delete                      |

**Create / Update**: `{ SchoolName: string; Address?: string }`
Response — `SchoolModel` (§3.4). `/with-user-count` adds `userCount: number`.

---

### 2.6 EventRoles — `/api/EventRoles`

| Verb   | Route                                    | Auth                  | Purpose                          |
|--------|------------------------------------------|-----------------------|----------------------------------|
| POST   | `/assign`                                | `EventCoordinator`    | Assign role to user in event     |
| PUT    | `/{id}`                                  | `EventCoordinator`    | Update role assignment           |
| DELETE | `/{id}`                                  | `EventCoordinator`    | Remove role                      |
| GET    | `/event/{eventId}`                       | —                     | Roles in an event (paged)        |
| GET    | `/user/{userId}`                         | —                     | Roles of a user (paged)          |
| GET    | `/event/{eventId}/role/{roleName}`       | —                     | Users with given role (paged)    |
| GET    | `/check?UserId=&EventId=&RoleName=`      | —                     | Boolean check                    |

`roleName` is an `EventRoleType` integer.

**Assign body**:
```ts
{ UserId: string; EventId: string; TeamId?: string;
  RoleName: number; ExpiredAt?: string; Notes?: string }
```

**Update body**:
```ts
{ TeamId?: string; RoleName: number; ExpiredAt?: string; Notes?: string }
```

Response — `EventRoleModel` (§3.5).

---

### 2.7 Rounds — `/api/Rounds`

| Verb   | Route                | Auth                  | Purpose            |
|--------|----------------------|-----------------------|--------------------|
| GET    | `/{id}`              | —                     | Get one            |
| GET    | `/event/{eventId}`   | —                     | Rounds of an event |
| POST   | `/`                  | `EventCoordinator`    | Create             |
| PUT    | `/{id}`              | `EventCoordinator`    | Update             |
| DELETE | `/{id}`              | `EventCoordinator`    | Soft-delete        |

**Create / Update body**:
```ts
{ EventId: string; RoundName: string; RoundNumber: number;
  StartDate: string; EndDate: string; AdvancementRule?: string }
```
Response — `RoundModel` (§3.6).

---

### 2.8 Tracks — `/api/Tracks`

| Verb   | Route                       | Auth                  | Purpose                |
|--------|-----------------------------|-----------------------|------------------------|
| GET    | `/{id}`                     | —                     | Get one                |
| GET    | `/event/{eventId}`          | —                     | Tracks of an event     |
| POST   | `/`                         | `EventCoordinator`    | Create                 |
| PUT    | `/{id}`                     | `EventCoordinator`    | Update                 |
| DELETE | `/{id}`                     | `EventCoordinator`    | Soft-delete            |
| PATCH  | `/{id}/assign-template`     | `EventCoordinator`    | Assign template to track |

**Create / Update**: `{ RoundId: string; TrackName: string; TemplateId?: string; Description?: string }`
**Assign template**: `{ TemplateId: string }`
Response — `TrackModel` (§3.7).

---

### 2.9 Templates — `/api/Templates`

| Verb   | Route                                | Purpose                              |
|--------|--------------------------------------|--------------------------------------|
| GET    | `/`                                  | List (paged)                         |
| GET    | `/{id}`                              | Get + embedded criteria              |
| POST   | `/`                                  | Create template                      |
| PUT    | `/{id}`                              | Update template                      |
| DELETE | `/{id}`                              | Soft-delete                          |
| POST   | `/{id}/criteria`                     | Add criteria to template             |
| PUT    | `/{id}/criteria/{criteriaId}`        | Update criteria weight/maxScore      |
| DELETE | `/{id}/criteria/{criteriaId}`        | Remove criteria from template        |

**Create / Update**: `{ TemplateName: string; Description?: string }`
**Add / Update criteria**: `{ CriteriaId: string; Weight: number; MaxScore: number }` (add) / `{ Weight: number; MaxScore: number }` (update)

Response — `TemplateModel` (§3.8) with embedded `criterias[]`.

---

### 2.10 Teams — `/api/Teams`

| Verb   | Route   | Auth                                       | Purpose          |
|--------|---------|--------------------------------------------|------------------|
| GET    | `/`     | —                                          | List (paged)     |
| GET    | `/{id}` | —                                          | Get one          |
| POST   | `/`     | —                                          | Create team      |
| PUT    | `/{id}` | `EventCoordinator` OR `TeamLeader`         | Update team      |
| DELETE | `/{id}` | `EventCoordinator` OR `TeamLeader`         | Delete team      |

**Create body**: `{ Name: string; Description: string; EventId: string; LeaderId: string }`
**Update body**: `{ Id: string; Name: string; Description: string; IsActive: boolean }`

Response — `TeamModel` (§3.9).

---

### 2.11 SubmitResults — `/api/SubmitResults`

| Verb   | Route   | Auth                                       | Purpose                |
|--------|---------|--------------------------------------------|------------------------|
| GET    | `/`     | —                                          | List, filterable       |
| GET    | `/{id}` | —                                          | Get one (with names)   |
| POST   | `/`     | `EventCoordinator` OR `TeamLeader`         | Submit a solution      |
| PUT    | `/{id}` | `EventCoordinator` OR `TeamLeader`         | Update submission      |
| DELETE | `/{id}` | `EventCoordinator` OR `TeamLeader`         | Delete submission      |

List query filters: `TeamId?`, `RoundId?`, `TrackId?`, `SortBy = CreatedTime`.

**Create**: `{ TeamId: string; RoundId: string; TrackId: string; SubmissionUrl: string; Description: string }`
**Update**: `{ Id: string; SubmissionUrl: string; Description: string; IsActive: boolean }`

Response — `SubmitResultModel` (§3.10). Detail response also includes `teamName`, `trackName`.

---

### 2.12 Scores — `/api/Scores`

| Verb   | Route                       | Auth                                | Purpose              |
|--------|-----------------------------|-------------------------------------|----------------------|
| GET    | `/{id}`                     | —                                   | Get one              |
| GET    | `/event-role/{eventRoleId}` | —                                   | Scores by judge      |
| POST   | `/`                         | `EventCoordinator` OR `Judge`       | Create scoresheet    |
| PUT    | `/{id}`                     | `EventCoordinator` OR `Judge`       | Update               |
| DELETE | `/{id}`                     | `EventCoordinator` OR `Judge`       | Delete (cascades)    |

**Create / Update**: `{ EventRoleId: string; SubmitResultId: string; Comment?: string }`
Response — `ScoreModel` (§3.11). `totalScore` is computed from `ScoreDetails`.

---

### 2.13 ScoreDetails — `/api/ScoreDetails`

| Verb   | Route                 | Auth                                | Purpose                 |
|--------|-----------------------|-------------------------------------|-------------------------|
| GET    | `/{id}`               | —                                   | Get one                 |
| GET    | `/score/{scoreId}`    | —                                   | Details for a score     |
| POST   | `/`                   | `EventCoordinator` OR `Judge`       | Create criterion score  |
| PUT    | `/{id}`               | `EventCoordinator` OR `Judge`       | Update value            |
| DELETE | `/{id}`               | `EventCoordinator` OR `Judge`       | Delete                  |

**Create**: `{ ScoreId: string; TemplateId: string; CriteriaId: string; Value: number }`
**Update**: `{ Value: number }`
Response — `ScoreDetailModel` (§3.12).

---

### 2.14 FinalResults — `/api/FinalResults`

| Verb   | Route                | Purpose                         |
|--------|----------------------|---------------------------------|
| GET    | `/{id}`              | Get one                         |
| GET    | `/round/{roundId}`   | Leaderboard per round (paged)   |
| GET    | `/team/{teamId}`     | Team history (paged)            |
| POST   | `/`                  | Create                          |
| PUT    | `/{id}`              | Update                          |
| DELETE | `/{id}`              | Delete                          |

**Create / Update**: `{ TeamId: string; RoundId: string; FinalScore: number; Rank: number; IsAdvanced: boolean }`
Response — `FinalResultModel` (§3.13).

---

### 2.15 UserRejections — `/api/UserRejections`

| Verb   | Route             | Purpose                       |
|--------|-------------------|-------------------------------|
| GET    | `/`               | List (paged + filters)        |
| GET    | `/user/{userId}`  | Rejections for a user         |
| POST   | `/`               | Record a rejection            |
| PUT    | `/{id}`           | Update reason                 |
| DELETE | `/{id}`           | Soft-delete                   |

List filters: `FromDate?`, `ToDate?`, `RejectedBy?`, `UserId?`, `SortBy` ∈ { `CreatedTime`, `UserId`, `RejectedBy` }.

**Create**: `{ UserId: string; RejectedBy: string; Reason?: string }`
**Update**: `{ Reason?: string }`
Response — `UserRejectionModel` (§3.14).

---

### 2.16 FPT Mock — `/api/fpt-mock`

| Verb | Route                      | Purpose                          |
|------|----------------------------|----------------------------------|
| GET  | `/students/{studentCode}`  | Validate FPT student code (mock) |

Response:
```ts
{ isValid: boolean; studentCode: string; fullName: string;
  major: string; enrollYear: number }
```

---

## 3. Response Models

### 3.1 `UserModel`
```ts
{ id: string; schoolId: string; studentCode: string | null;
  email: string; fullName: string; isStudent: boolean;
  isAdmin: boolean; isApproved: boolean }
```

### 3.2 `EventModel`
```ts
{ id: string; eventName: string; season: string | null; year: number;
  startDate: string; endDate: string; description: string | null;
  createdTime: string; lastUpdatedTime: string }
```

### 3.3 `CriteriaModel`
```ts
{ id: string; criteriaName: string; description: string | null;
  isActive: boolean; createdTime: string; lastUpdatedTime: string }
```

### 3.4 `SchoolModel`
```ts
{ id: string; schoolName: string; address: string | null }
```

### 3.5 `EventRoleModel`
```ts
{ id: string; userId: string; eventId: string; teamId: string | null;
  roleName: string; assignedAt: string | null; expiredAt: string | null;
  notes: string | null; createdTime: string; lastUpdatedTime: string }
```

### 3.6 `RoundModel`
```ts
{ id: string; eventId: string; roundName: string; roundNumber: number;
  startDate: string; endDate: string; advancementRule: string | null;
  createdTime: string; lastUpdatedTime: string }
```

### 3.7 `TrackModel`
```ts
{ id: string; roundId: string; trackName: string;
  templateId: string | null; description: string | null;
  createdTime: string; lastUpdatedTime: string }
```

### 3.8 `TemplateModel`
```ts
{ id: string; templateName: string; description: string | null;
  createdTime: string; lastUpdatedTime: string;
  criterias: Array<{
    criteriaId: string; criteriaName: string; description: string | null;
    weight: number; maxScore: number;
  }> }
```

### 3.9 `TeamModel`
```ts
{ id: string; name: string; description: string;
  isActive: boolean; createdTime: string; lastUpdatedTime?: string }
```

### 3.10 `SubmitResultModel`
```ts
{ id: string; teamId: string; trackId: string;
  submissionUrl: string; description: string;
  isActive: boolean; createdTime: string; lastUpdatedTime?: string;
  // detail endpoint adds:
  teamName?: string; trackName?: string }
```

### 3.11 `ScoreModel`
```ts
{ id: string; eventRoleId: string; submitResultId: string;
  totalScore: number; comment: string | null;
  createdTime: string; lastUpdatedTime: string }
```

### 3.12 `ScoreDetailModel`
```ts
{ id: string; scoreId: string; templateId: string; criteriaId: string;
  value: number; createdTime: string; lastUpdatedTime: string }
```

### 3.13 `FinalResultModel`
```ts
{ id: string; teamId: string; roundId: string;
  finalScore: number; rank: number; isAdvanced: boolean;
  createdTime: string; lastUpdatedTime: string }
```

### 3.14 `UserRejectionModel`
```ts
{ id: string; userId: string; rejectedBy: string; reason: string | null;
  createdTime: string; lastUpdatedTime: string }
```

---

## 4. Suggested Frontend Setup

```ts
// axios instance
const api = axios.create({ baseURL: `${import.meta.env.VITE_API_URL}/api` });

api.interceptors.request.use((cfg) => {
  const token = localStorage.getItem('accessToken');
  if (token) cfg.headers.Authorization = `Bearer ${token}`;
  return cfg;
});

// unwrap envelope
api.interceptors.response.use((res) => {
  const env = res.data as BaseResponse<unknown>;
  if (!env.success) return Promise.reject(env);
  return { ...res, data: env.data };
});
```

Refresh-token flow: on `401`, call `POST /api/Auth/refresh-token` with the stored refresh token, replay the original request.

---

_Generated 2026-06-15 from branch `dev` @ `903b8f9`._
