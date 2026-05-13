# Backend tasks

## Goal

This file contains the recommended backend implementation plan for the swirl API.

Implement tasks stage by stage.

Do not jump to the next stage unless explicitly requested.

## Backend project structure

Use this simplified ASP.NET Core API structure:

```text
Swirl.Api/
  Properties/
  Controllers/
  Data/
  Hubs/
  Interfaces/
  Migrations/
  Models/
  Services/
  appsettings.json
  Program.cs
  Swirl.Api.http
```

Folder responsibilities:

- `Controllers` — HTTP API controllers.
- `Data` — `AppDbContext`, EF Core configuration, database setup, seed logic.
- `Models` — database entities and request/response DTO models.
- `Interfaces` — service interfaces.
- `Services` — business logic services.
- `Migrations` — EF Core migrations.
- `Hubs` — optional folder for SignalR; do not use it in MVP unless explicitly requested.

Do not create folders `DTOs`, `Repositories`, `Mapping`, `Middleware`, `Extensions`, or `Seed` unless explicitly requested later.

## Stage 1. Project setup

Tasks:

- Create ASP.NET Core Web API project.
- Configure .NET 8.
- Configure Swagger / OpenAPI.
- Configure PostgreSQL connection.
- Configure Entity Framework Core.
- Create `AppDbContext` in `Data`.
- Configure CORS for Flutter development.
- Configure static file serving from `wwwroot/media`.
- Add initial `appsettings.json` structure.
- Ensure the project builds successfully.

Expected result:

- API starts successfully.
- Swagger opens successfully.
- `dotnet build` passes.
- Static files from `/media` can be served later.

## Stage 2. Database models and DbContext

Read before starting:

- `docs/02_DATABASE_SCHEMA.md`

Tasks:

- Create database entities in `Models`.
- Create `AppDbContext` in `Data`.
- Add DbSet properties.
- Configure table names and relationships.
- Configure unique constraints.
- Configure required fields.
- Configure PostgreSQL-compatible mappings.
- Create initial EF Core migration.

Entities to create:

- `User`
- `UserProfile`
- `Avatar`
- `Section`
- `Level`
- `Word`
- `Exercise`
- `ExerciseOption`
- `UserLevelProgress`
- `UserWordProgress`
- `LevelAttempt`
- `UserAnswer`
- `DailyTest`
- `DailyTestAnswer`

Expected result:

- Database schema matches `docs/02_DATABASE_SCHEMA.md`.
- Initial migration is created.
- `dotnet build` passes.

## Stage 3. Auth

Read before starting:

- `docs/03_API_CONTRACT.md`
- `docs/04_AUTH_AND_SECURITY.md`

Tasks:

- Create auth request/response models in `Models`.
- Create `IAuthService` in `Interfaces`.
- Create `AuthService` in `Services`.
- Implement password hashing.
- Implement JWT generation.
- Implement registration.
- Implement login.
- Implement current user endpoint.
- Create `AuthController` in `Controllers`.

Endpoints:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me`

Registration must:

- validate request
- create user
- create user profile
- assign avatar
- create initial level progress
- make first level of each section available by default
- return JWT and user DTO

Expected result:

- User can register.
- User can login.
- Authenticated user can call `/api/auth/me`.
- Password hash is never returned.
- `dotnet build` passes.

## Stage 4. Profile and avatars

Read before starting:

- `docs/03_API_CONTRACT.md`
- `docs/04_AUTH_AND_SECURITY.md`

Tasks:

- Create profile and avatar models in `Models`.
- Create `IProfileService` in `Interfaces`.
- Create `ProfileService` in `Services`.
- Implement avatar list endpoint.
- Implement profile endpoint.
- Implement avatar change endpoint.
- Create controllers if needed.

Endpoints:

- `GET /api/avatars`
- `GET /api/profile`
- `PUT /api/profile/avatar`

Expected result:

- User can get available avatars.
- User can get own profile.
- User can change own avatar.
- User cannot access another user's profile.
- `dotnet build` passes.

## Stage 5. Seed content

Read before starting:

- `docs/08_SEED_DATA.md`

Tasks:

- Add seed logic in `Data`.
- Seed avatars.
- Seed sections.
- Seed levels.
- Seed words.
- Seed exercises.
- Seed exercise options.
- Make seed logic idempotent if practical.

Minimum early MVP seed:

- 4 avatars
- 4 sections
- at least 2 normal levels per section
- 1 final test per section
- 5 words per normal level
- several exercises per normal level

The schema and logic must still support:

- 5 normal levels per section
- 1 final test per section
- about 10 words per normal level
- about 20 exercises per normal level

Expected result:

- Clean database can be populated with MVP content.
- Seed can run without breaking the app.
- `dotnet build` passes.

## Stage 6. Sections and levels

Read before starting:

- `docs/03_API_CONTRACT.md`
- `docs/06_LEARNING_LOGIC.md`

Tasks:

- Create section and level response models in `Models`.
- Create `IContentService` or similar service interface in `Interfaces`.
- Create content service in `Services`.
- Implement getting sections with user progress.
- Implement getting section details.
- Implement getting section levels with user statuses.
- Implement getting level details.

Endpoints:

- `GET /api/sections`
- `GET /api/sections/{sectionId}`
- `GET /api/sections/{sectionId}/levels`
- `GET /api/levels/{levelId}`

Expected result:

- Authenticated user can view sections.
- Authenticated user can view level map.
- Level statuses are user-specific.
- Progress percent is calculated correctly.
- Locked, available, and completed statuses work.
- `dotnet build` passes.

## Stage 7. Words and learning

Read before starting:

- `docs/03_API_CONTRACT.md`
- `docs/06_LEARNING_LOGIC.md`

Tasks:

- Create word response models in `Models`.
- Implement getting words for level.
- Implement marking level words as learned.
- Save learned words in `user_word_progress`.
- Set `user_level_progress.words_learned = true`.

Endpoints:

- `GET /api/levels/{levelId}/words`
- `POST /api/levels/{levelId}/words/mark-learned`

Rules:

- Level must be available or completed.
- Word ids must belong to the specified level.
- Do not duplicate already learned words.
- Final test does not introduce new words.

Expected result:

- Flutter can load words for Learn word screen.
- User can mark words as learned.
- Learned words are available for daily test later.
- `dotnet build` passes.

## Stage 8. Level session and completion

Read before starting:

- `docs/03_API_CONTRACT.md`
- `docs/06_LEARNING_LOGIC.md`
- `docs/07_DAILY_TEST_AND_STREAK.md`

Tasks:

- Create level session models in `Models`.
- Create complete level request/response models in `Models`.
- Create `ILearningService` in `Interfaces`.
- Create `LearningService` in `Services`.
- Implement full level session endpoint.
- Implement level completion endpoint.
- Save level attempts.
- Save user answers.
- Count mistakes.
- Increment attempts count.
- Complete level only if mistakes count is 0.
- Unlock next level only if mistakes count is 0.
- Unlock final test after all normal levels are completed.
- Update streak after every completed attempt.

Endpoints:

- `GET /api/levels/{levelId}/session`
- `POST /api/levels/{levelId}/complete`

Expected result:

- Flutter can get full session in one request.
- Flutter can send completed answers in one request.
- Failed attempts are saved.
- Successful attempts complete the level.
- Next level unlocks correctly.
- Streak updates correctly.
- `dotnet build` passes.

## Stage 9. Daily test

Read before starting:

- `docs/03_API_CONTRACT.md`
- `docs/07_DAILY_TEST_AND_STREAK.md`

Tasks:

- Create daily test models in `Models`.
- Create `IDailyTestService` in `Interfaces`.
- Create `DailyTestService` in `Services`.
- Generate daily test from learned words.
- Return unavailable response if learned words count is less than 5.
- Generate 15-30 exercises when possible.
- Generate choice options.
- Complete daily test.
- Save daily test result.
- Save daily test answers.
- Update streak.

Endpoints:

- `GET /api/daily-test`
- `POST /api/daily-test/complete`

Expected result:

- Daily test is unavailable with fewer than 5 learned words.
- Daily test is generated from current user's learned words.
- Daily test completion saves result.
- Daily test updates streak.
- Daily test does not change section or level progress.
- `dotnet build` passes.

## Stage 10. Error handling and polish

Read before starting:

- `docs/09_ERROR_HANDLING.md`
- `docs/10_CODE_STYLE.md`

Tasks:

- Add consistent JSON error responses.
- Validate request DTOs.
- Return correct HTTP status codes.
- Hide internal exception details.
- Check Swagger output.
- Check CORS.
- Check static media paths.
- Check migrations on clean database.
- Check all MVP endpoints manually.

Expected result:

- API returns predictable errors.
- Swagger is usable.
- Flutter can integrate with the API.
- `dotnet build` passes.

## Important implementation rules

- Use current user id from JWT for user-specific operations.
- Do not accept user id from request body.
- Do not expose password hashes.
- Do not expose EF entities directly from controllers.
- Keep controllers thin.
- Put business logic in `Services`.
- Put service interfaces in `Interfaces`.
- Put entities and request/response models in `Models`.
- Put database context and seed logic in `Data`.
- Do not implement non-MVP features unless explicitly requested.

## Verification after each stage

After each stage:

```bash
dotnet build
```

If tests exist:

```bash
dotnet test
```

Also verify:

- Swagger starts.
- No unrelated features were added.
- Code follows the documented API contract.
