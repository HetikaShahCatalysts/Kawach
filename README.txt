Kawach assessment service

Database connection:
  Server: localhost
  Database: Kawach
  Authentication: Windows integrated security
  Configuration: Api/appsettings.json

The local connection uses the same Trusted_Connection and
MultipleActiveResultSets format as the MIS application. TrustServerCertificate
is for local development. Production hosting should provide an encrypted SQL
connection string through ConnectionStrings__DefaultConnection.

EF migrations are applied automatically only in Development. Production
migrations should be applied once during deployment, before scaling out to
multiple application instances.

Persistence uses Entity Framework Core with a code-first model.

Database model:
  Api/Data/Entities
  Api/Data/KawachDbContext.cs
  Api/Data/Migrations

Install the matching EF CLI if needed:
  dotnet tool install --global dotnet-ef --version 8.0.8

Create/update the local database:
  dotnet ef database update --project Api --startup-project Api

Create future schema changes:
  dotnet ef migrations add MigrationName --project Api --startup-project Api

Run the application:
  dotnet run --project Api

Application:
  http://localhost:5187

Swagger API documentation:
  http://localhost:5187/swagger

Operational endpoints:
  /health/live  - application process is running
  /health/ready - application can connect to SQL Server

Security and scalability controls:
  - EF Core DbContext pooling
  - asynchronous database and controller operations
  - per-client API rate limiting
  - strict production host allowlist
  - strict production browser-origin allowlist
  - one-megabyte request-body limit
  - compressed responses
  - production HSTS and HTTPS redirection
  - production-safe exception responses
  - security response headers
  - Swagger limited to Development

Production gate:
  The admin dashboard is cookie-authenticated and restricted to the Admin role.
  Open /admin/login. The temporary development credential is:
    Username: admin
    Password: KawachAdmin@2026

  Change this before deployment. Generate a new PBKDF2 salt/hash with:
    powershell -File Tools/Generate-AdminPasswordHash.ps1 -Password "NewPassword"

  Put the generated values in hosting environment variables rather than
  committing production credentials:
    AdminAuthentication__Username
    AdminAuthentication__PasswordSalt
    AdminAuthentication__PasswordHash
    AdminAuthentication__PasswordIterations

  Cookie encryption keys are persisted under Api/DataProtection-Keys. For
  multiple application instances, use a shared protected key store so every
  instance can validate the same authentication cookie.

Package security:
  Keep NuGet auditing enabled, regularly run `dotnet list package --vulnerable`,
  patch dependencies, protect production secrets with hosting environment
  variables, and never commit production database credentials.

Production domain lock-down:
  The browser UI and API are designed to be hosted together on your own domain.
  Set these hosting environment variables before go-live so only your domain is
  accepted:
    RequestSecurity__AllowedOrigins__0=https://your-domain.example
    RequestSecurity__AllowedHosts__0=your-domain.example
    AllowedHosts=your-domain.example

  What this enforces:
  - browsers may call the API only from the allowed origin
  - unsafe API requests without the approved Origin header are rejected
  - unexpected Host headers are rejected in production
  - admin authentication cookies are HTTPS-only and SameSite=Strict

Module 1: SQL schema
Module 2: ASP.NET API skeleton
Module 3: Assessment workflow and scoring

Module 3 provides:
- participant detail collection with a generated UserId
- a separate assessment ID for each form attempt
- dynamic step timing by stable data-assessment-step codes
- dynamic answer capture by question and answer codes
- Hindi-to-English normalization through Api/Content/content-lookup.json
- idempotent assessment completion
- score-to-risk/pathway evaluation
- assessment retrieval and admin lookup by UserId

Run:
  dotnet run --project Api

Endpoints:
  POST /api/assessment/start
  POST /api/assessment/track-step
  POST /api/assessment/answer
  POST /api/assessment/complete
  GET  /api/assessment/{assessmentId}
  GET  /api/assessment/users/{userId}

Open the application root URL to use the participant-details first step.
Open /admin.html and enter a UserId to view answers grouped by:
Step -> Module -> Question -> Selected answer.

AssessmentAnswer stores StepName, ModuleName, Question and AnswerText as its
business data. AnswerId, UserId, AssessmentId and AnsweredOn are retained only
for linking and audit purposes. Codes are used during submission and language
lookup but are not stored as answer columns.

AssessmentAnswer also stores Score. The backend obtains this score from the
approved content lookup file for Hindi content. The supplied English framework
submits the score defined in its question data. Assessment completion uses
SUM(AssessmentAnswer.Score) to calculate the selected total.

The supplied framework is now Api/wwwroot/index.html. When the user first
starts Risk Assessment, a participant-details dialog creates the UserId and
AssessmentId. Leaving Risk Assessment or Observation Scoring synchronizes that
step to the database: previous rows for the step are deleted and only currently
selected factors are inserted. Unselected factors are never stored.

Observation Scoring example:
  StepName: Observation Scoring
  ModuleName: Education & School Continuity
  Question: Child's education badly affected...
  AnswerText: Selected
  Score: 4

Each form page should have data-assessment-step and data-step-name.
Each module should have data-module-code and data-module-name.
Each question should have data-question-code and data-question-text.
Each selectable answer should have data-answer-code and may have
data-score-delta. assessment-form.js reads these attributes automatically.
Checkbox questions should also define data-unselected-answer-code and
data-unselected-answer-text so "No" is distinguishable from missing data.
Questions can be added, removed, or reordered without changing backend code.

See Api/wwwroot/assessment.html for a complete coded example.

For Hindi pages, the question and answer codes and submitted Hindi text must
match an approved entry in content-lookup.json. The API rejects unmapped Hindi
content instead of accidentally storing Hindi text in the English DB columns.

The Module 3 store is in memory. It is isolated behind IAssessmentService so
SQL persistence can be added without changing the controller contract.
