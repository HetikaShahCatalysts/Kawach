# Kawach Project Overview

## Purpose

Kawach is a web-based assessment platform for collecting participant details, running a guided assessment flow, tracking step progress, recording answers, calculating risk outcomes, and providing an authenticated internal admin view for review and reporting.

## Project Scope

This project currently covers:

- participant onboarding and assessment-session creation
- assessment step tracking and timing capture
- answer submission and answer synchronization
- score aggregation and risk/pathway evaluation
- admin login and protected assessment review screens
- SQL-backed persistence through Entity Framework Core
- same-domain hosting with production-oriented API protection

This project does not currently aim to provide:

- a public multi-tenant platform
- external partner API access
- background job processing
- distributed event-driven architecture
- native mobile applications
- advanced analytics or BI pipelines

## Solution Shape

The solution is intentionally compact and organized as a single ASP.NET Core application:

- `Api/Controllers`: HTTP endpoints and request/response orchestration
- `Api/Services`: business rules, workflow logic, and application services
- `Api/Data`: EF Core DbContext, entities, migrations, and persistence setup
- `Api/Contracts`: API request and response contracts
- `Api/Security`: authentication, credential validation, and request-security policies
- `Api/Content`: approved content lookup and normalization data
- `Api/wwwroot`: participant-facing frontend assets
- `Api/AdminUi`: admin login and dashboard static pages
- `Tools`: operational helper scripts
- `Database`: reserved for database-related artifacts and future expansion

## Architecture Layers

The application follows a pragmatic layered architecture.

### 1. Presentation Layer

Responsible for HTTP handling and browser-delivered UI.

- ASP.NET Core controllers expose API endpoints
- static HTML, CSS, and JavaScript provide the frontend
- admin pages are served separately from the participant UI

### 2. Application Layer

Responsible for use-case execution and workflow behavior.

- assessment start, step tracking, answer sync, completion, and admin listing live in services
- contracts define stable request and response shapes
- content normalization protects the backend from storing unapproved source text

### 3. Domain and Data Layer

Responsible for persistence and core records.

- EF Core entities model participants, sessions, answers, tracking, and results
- SQL Server is the system of record
- migrations provide schema evolution

### 4. Security and Platform Layer

Responsible for cross-cutting operational controls.

- cookie-based admin authentication
- strict host and origin validation for production deployment
- rate limiting, response compression, HTTPS, HSTS, and hardened response headers

## Tech Stack

### Backend

- .NET 9 / ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Cookie Authentication
- Built-in rate limiting, health checks, data protection, and response compression
- Swagger in development environments

### Frontend

- HTML5
- CSS3
- Vanilla JavaScript
- same-origin `fetch` calls to backend endpoints

## Scalability Approach

The current architecture is designed to scale sensibly without premature complexity.

- stateless HTTP request processing for the API layer
- DbContext pooling for efficient database access
- asynchronous controller and service methods
- separated service and controller responsibilities for maintainability
- production-ready reverse-proxy support through forwarded headers
- configurable request-security policies for controlled deployment

For future scale, the cleanest next steps would be:

- move more business rules into dedicated domain-focused service classes
- introduce repository/query separation only when query complexity materially grows
- externalize data protection keys for multi-instance deployment
- add automated integration and security regression tests
- split frontend assets into a dedicated client app only if UI complexity justifies it

## Coding and Governance Direction

The codebase is aligned to these internal standards:

- clear folder ownership by responsibility
- thin controllers and service-led business logic
- configuration-driven security controls
- environment-specific production hardening
- limited public surface area
- readiness for audit through explicit documentation and deterministic behavior

## Hosting Model

Recommended hosting model:

- host the frontend and API on the same domain
- allow only the approved production host and browser origin
- terminate TLS at the trusted edge or reverse proxy
- inject secrets and environment-specific settings from hosting configuration

Required production configuration should include:

- database connection string
- admin authentication secrets
- allowed host and allowed origin entries
- shared data-protection key storage when running multiple instances
