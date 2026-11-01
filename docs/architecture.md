# Reelix — Architecture

> **Product:** Reelix  
> **Purpose:** Mobile app for tracking movies and TV shows, managing watchlists, recording progress, rating titles, discovering content, and sharing activity with friends.  
> **Architecture principle:** Clean Architecture, SOLID, pragmatic DDD, KISS, security by design, and a modular monolith with managed services. Keep the design simple until real usage justifies additional infrastructure.
>
> **Visual reference:** [`visual_identity.png`](./visual_identity.png) — visual direction for Reelix. The editable assets in `mobile/assets/brand/` and `mobile/assets/brand/docs/design-tokens.json` are the implementation source of truth. When the reference board and the brand kit differ, follow the brand kit; do not embed the reference board in the app UI.

## 1. Engineering principles and non-negotiable standards

These principles apply to all new code and should guide code reviews, testing, and architectural decisions.

### Clean Architecture

- Keep the **Domain** independent of frameworks, databases, HTTP, and external providers.
- Keep application use cases independent of delivery mechanisms and infrastructure details.
- Put EF Core, external API clients, email/push providers, file storage, and other implementation details in Infrastructure.
- The API/presentation layer translates HTTP requests and responses; it should not contain business rules.
- Dependencies must point inward: Infrastructure may implement application/domain abstractions, but Domain must not depend on Infrastructure.
- Do not add layers or projects mechanically. Preserve the dependency rule while choosing the simplest structure that is understandable and testable.

### SOLID

- **S — Single Responsibility:** each class/module should have one clear reason to change.
- **O — Open/Closed:** extend behavior through focused abstractions and composition when useful, not by repeatedly modifying unrelated code.
- **L — Liskov Substitution:** implementations must honor the contracts of their abstractions.
- **I — Interface Segregation:** prefer small, purpose-specific interfaces over broad interfaces.
- **D — Dependency Inversion:** high-level business rules must not depend directly on low-level implementation details.
- Apply SOLID pragmatically. Do not create an interface for every class or introduce abstractions without a real seam, variation, or testing need.

### Domain-Driven Design (DDD)

- Model business concepts using the language of the product and domain: watched status, episode progress, rewatch history, watchlist, rating, review, and visibility.
- Keep business invariants in the domain/application layer, not only in controllers or database constraints.
- Use entities and value objects where they clarify identity, equality, and rules.
- Define aggregate boundaries around consistency requirements; avoid one large aggregate for the entire catalog or user profile.
- Use repositories where they protect aggregate persistence or domain boundaries; do not wrap every database table in a generic repository by default.
- Keep modules cohesive and define explicit contracts between modules.
- Introduce domain events only when they support a concrete need, such as decoupled follow-up actions. Do not add event-driven complexity by default.

### KISS and maintainability

- Prefer the simplest implementation that meets the current requirements.
- Avoid speculative abstractions, premature microservices, unnecessary design patterns, and excessive indirection.
- Prefer readable names, small cohesive methods, explicit control flow, and consistent conventions.
- Keep business rules discoverable and avoid duplicating them across API endpoints, jobs, and clients.
- Refactor duplicated or confusing code when touching it, while keeping changes focused.
- Add comments to explain *why* a non-obvious decision exists, not to restate what the code already says.
- Every significant architectural choice should have a clear reason and, when useful, a short Architecture Decision Record (ADR).

### Security by design

- Treat security as a requirement for every feature, not a final hardening step.
- Apply least privilege, deny-by-default authorization, secure defaults, and defense in depth.
- Authenticate the user and authorize access to each resource on the server. Never rely on hidden UI controls or client-supplied user IDs for authorization.
- Validate inputs at trust boundaries and encode output appropriately. Use parameterized database access; never concatenate untrusted input into SQL.
- Store secrets only in secret managers/environment configuration, rotate them when exposed, and never commit them to source control.
- Enforce HTTPS, secure token handling, rate limits, request size limits, and appropriate CORS settings.
- Keep dependencies and base images patched; scan dependencies and secrets in CI.
- Avoid logging credentials, tokens, sensitive personal data, or full request bodies by default.
- Define secure account recovery, session revocation, account deletion, and privacy controls.
- Consider LGPD obligations including data minimization, purpose limitation, retention, export/deletion workflows, and consent where applicable.
- Threat-model sensitive features such as authentication, social interactions, reviews, imports, and administrative actions.
- Record security-relevant events without logging secrets; alert on suspicious patterns where appropriate.

### Quality gates for pull requests

A change should not be considered complete until relevant checks pass:

1. Build, formatting, linting, and type checks.
2. Unit tests for business rules and application use cases.
3. Integration tests for database behavior, authorization, and external adapters where relevant.
4. Authorization and validation checks for newly exposed endpoints.
5. Dependency/security scanning and secret scanning in CI.
6. Review for readability, duplicated logic, accidental data exposure, and unnecessary complexity.
7. Documentation updates for changed contracts, configuration, or architectural decisions.

## 2. Architecture decisions

- **Mobile application:** React Native + Expo + TypeScript.
- **Backend API:** ASP.NET Core Web API with C#.
- **Database:** PostgreSQL, initially hosted by Supabase or another managed PostgreSQL provider.
- **Authentication:** Supabase Auth or ASP.NET Core Identity with an established identity provider. Choose one approach; do not build password handling from scratch.
- **Content metadata:** Use a licensed/provider-approved movie and TV metadata API. Keep provider integration behind an adapter so it can be replaced.
- **API style:** REST + OpenAPI. Generate or maintain a typed TypeScript client from the OpenAPI contract.
- **Hosting:** Deploy the .NET API to a container-friendly platform. Host the mobile app through Apple App Store and Google Play; Expo EAS can support builds and releases. Use Vercel only for optional web surfaces such as a landing page or admin web app.
- **Automation:** n8n for non-critical workflows and integrations, not as the core request-processing backend.
- **Architecture style:** Modular monolith first, with clear module boundaries. Avoid microservices until scale, team ownership, or independent deployment needs justify them.

## 3. Mobile front end

### Front End Core technologies

- **TypeScript:** Primary application language, with strict type checking enabled.
- **React Native:** Native iOS and Android application UI using React.
- **Expo:** Development tooling, native builds, device APIs, and release workflow.
- **Expo Router:** File-based navigation and deep-link support.
- **TanStack Query (React Query):** Server-state fetching, caching, invalidation, retries, and mutations.
- **Zod:** Runtime validation for external data and form payloads where useful.
- **React Hook Form:** Form state and validation integration.
- **NativeWind or StyleSheet:** Choose one styling approach. NativeWind provides a Tailwind-like workflow; React Native's StyleSheet is a lower-dependency option.
- **Biome:** Formatting and linting for supported TypeScript/JavaScript files. Confirm compatibility with the chosen React Native/Expo toolchain and use ESLint if a required rule or plugin is not supported.
- **date-fns:** Date formatting and manipulation; store timestamps in UTC and display them in the user's local timezone.
- **React Native Reanimated:** Use for animations only where they improve the experience.
- **Expo SecureStore:** Store small sensitive values locally when needed; never treat local storage as an authorization boundary.
- **Sentry or an equivalent:** Crash and error reporting, configured with privacy-conscious data filtering.

### UI and design system

- Use `mobile/assets/brand/` as the source for Reelix logos, app icons, navigation and genre icons, badges, and UI illustrations. Use the matching variant from `logos/` for the background and context; do not redraw, recolor, or approximate the logo with styled text.
- Treat `docs/visual_identity.png` as a mood board, not a production asset or a pixel-perfect screen specification. If its displayed colors, typography, or examples conflict with the editable brand kit, follow `mobile/assets/brand/docs/design-tokens.json` and the supplied brand assets.
- Keep reusable colors, spacing, typography, and radii in design tokens, mapped from `mobile/assets/brand/docs/design-tokens.json` to `mobile/src/theme/tokens.ts`. Avoid hard-coded brand values in individual screens and components.
- Brand direction: cinematic near-black surfaces, a vivid red/pink accent, restrained supporting colors, and high-contrast white typography. Use red for primary actions, active navigation, and emphasis rather than as a full-screen fill or decorative gradient.
- Typography: Sora, with a system sans-serif fallback. Load the provided weights once at the app root and refer to them through the shared font tokens.
- Palette from the brand kit:
  - `primary`: `#FF2D55`
  - `secondary`: `#FF6688`
  - `background`: `#080B0F`
  - `surface`: `#10141B`
  - `surface-raised`: `#202D36`
  - `text`: `#FFFFFF`
  - `text-muted`: `#A1A1AA`
  - `success`: `#B7F34A`
  - `warning`: `#FBBF24`
  - `info`: `#38BDF8`
- Use the supplied SVG assets through the existing Expo SVG integration or convert them to typed React Native components where needed. Keep platform-required raster exports, such as the Expo app icon and splash image, in sync with the supplied logo artwork.
- Use the provided navigation, genre, and badge icons where appropriate; preserve their intended stroke/fill behavior and avoid substituting unrelated emoji or platform glyphs for brand assets.
- Build reusable components that follow these tokens and assets: poster card, show/movie row, progress indicator, rating control, watchlist button, empty state, loading skeleton, error state, and confirmation dialog.
- Use accessibility labels, adequate contrast, dynamic text sizing, and touch targets appropriate for mobile.
- **shadcn/ui is designed for web React, not React Native.** Do not assume its components can be reused directly in the mobile app. If a web admin is added, shadcn/ui is a good option there.

### Client state boundaries

- **TanStack Query:** API/server state.
- **React state or a small state store:** temporary UI state only.
- **SecureStore:** small secrets/tokens only when required by the chosen authentication flow.
- Do not duplicate server data into a global client store without a clear reason.

## 4. Backend

### Backend Core technologies

- **C#:** Primary backend language.
- **ASP.NET Core Web API:** HTTP API, dependency injection, middleware, authentication, authorization, and OpenAPI.
- **Entity Framework Core:** Default ORM for normal aggregate persistence and migrations.
- **Npgsql:** PostgreSQL provider for EF Core.
- **Dapper:** Optional for specific read-heavy or reporting queries where measured performance or SQL control warrants it. Avoid using two data-access approaches indiscriminately.
- **FluentValidation or equivalent:** Request validation where validation rules are complex; keep simple invariants close to the domain model.
- **Serilog or built-in structured logging:** Structured logs with correlation IDs and sensitive-data redaction.
- **OpenTelemetry:** Traces and metrics when observability needs justify it.
- **xUnit:** Unit and integration testing.
- **Testcontainers for .NET:** Optional real PostgreSQL integration tests in CI.
- **Scalar or Swagger UI:** API exploration from the generated OpenAPI specification.

### API conventions

- Version the public API when necessary, for example `/api/v1`.
- Use resource-oriented REST endpoints and appropriate HTTP status codes.
- Return consistent problem details for errors (`ProblemDetails`).
- Add pagination, filtering, sorting, and limits to collection endpoints.
- Use idempotency where retryable operations could otherwise create duplicates.
- Use cancellation tokens for I/O-bound operations.
- Keep external provider calls behind interfaces/adapters.
- Generate a TypeScript client from OpenAPI where practical to reduce contract drift.

### Suggested backend modules

1. **Identity and Profiles** — user identity, profile, preferences, privacy settings.
2. **Catalog** — movies, series, seasons, episodes, people, genres, and provider identifiers.
3. **Tracking** — watch status, episode progress, watched dates, and rewatch history.
4. **Watchlists and Collections** — personal lists, custom lists, and ordering.
5. **Ratings and Reviews** — ratings, reviews, spoiler flags, and moderation hooks.
6. **Social** — follows, activity feed, reactions, and visibility controls.
7. **Discovery** — search, trending content, and recommendation interfaces.
8. **Notifications** — user preferences and notification delivery.
9. **Integrations** — external metadata provider clients, imports, and exports.
10. **Administration and Moderation** — operational tools and abuse handling.

Start with only the modules required for the MVP. Keep module dependencies explicit and avoid direct access to another module's tables when a module-owned interface is sufficient.

## 5. Architecture and code organization

Use a **modular monolith**, not microservices, for the initial product. Organize the code around Clean Architecture dependency rules, SOLID principles, pragmatic DDD domain boundaries, and KISS. These are engineering constraints, not a reason to create unnecessary layers.

A possible solution layout:

```text
src/
  Reelix.Api/                 # HTTP endpoints, middleware, OpenAPI
  Reelix.Application/          # Use cases, DTOs, orchestration
  Reelix.Domain/               # Domain models, invariants, domain events
  Reelix.Infrastructure/       # EF Core, provider clients, persistence
  Reelix.Modules.Catalog/      # Catalog module
  Reelix.Modules.Tracking/     # Watch progress and history
  Reelix.Modules.Collections/  # Watchlists and custom lists
  Reelix.Modules.Social/       # Add when required
tests/
  Reelix.UnitTests/
  Reelix.IntegrationTests/
  Reelix.ArchitectureTests/
```

This is a conceptual layout, not a requirement to create a separate project for every module on day one. Avoid unnecessary project proliferation; the key requirement is clear boundaries.

Prefer simple, testable application services and domain rules. Apply Clean Architecture ideas where they add value, without adding interfaces and abstractions for every class.

## 6. Database and persistence

### PostgreSQL

- **PostgreSQL:** Primary source of truth for users, catalog references, tracking history, lists, ratings, and social relationships.
- **Supabase:** A managed PostgreSQL option that can also provide authentication, storage, and other services. The backend should still own business rules and authorization.
- **EF Core + Npgsql:** Migrations and normal persistence.
- Add indexes based on real query patterns and inspect query plans before optimizing.
- Use foreign keys, unique constraints, and transactions to protect data integrity.
- Use UTC timestamps consistently.
- Store provider identifiers alongside internal IDs; do not use a third-party provider's ID as the only internal identity.
- Keep secrets out of source control and separate development, staging, and production environments.

### Suggested core entities

- `UserProfile`
- `Title` (movie or series)
- `Season`
- `Episode`
- `WatchProgress`
- `WatchHistoryEntry`
- `Watchlist`
- `WatchlistItem`
- `Rating`
- `Review`
- `Follow`
- `Activity`
- `ExternalCatalogReference`

Model these according to actual use cases. For example, current episode progress and historical watch events may need different records so rewatches and viewing history can be represented accurately.

### Supabase security note

If the mobile client connects directly to Supabase for a deliberately chosen feature, enable and test Row Level Security (RLS) policies on every exposed table. Never ship a service-role key in the mobile app. If the .NET API is the only data access path, keep database credentials server-side and enforce authorization in the API.

## 7. External movie and TV metadata

- Select a metadata provider after reviewing its API terms, attribution requirements, rate limits, commercial-use rules, and image usage rights.
- Do not scrape streaming services or assume that metadata/poster rights are unrestricted.
- Create an adapter such as `IMetadataProvider` so the provider can be changed without rewriting catalog and tracking modules.
- Cache provider responses where permitted, respecting provider cache and attribution requirements.
- Treat provider data as external input: validate it, handle missing fields, and plan for rate limits and outages.
- Keep the Reelix user-generated data independent of the provider so a provider change does not destroy watch history or lists.

## 8. Authentication and security

- Use a reputable identity solution: Supabase Auth, or ASP.NET Core Identity with a suitable identity provider. Avoid custom password authentication unless there is a clear, reviewed requirement.
- If passwords are managed by the .NET application, use ASP.NET Core Identity's supported password hashing and security mechanisms. Do not use `bcryptjs` in the .NET backend.
- Do not implement password hashing manually or store plaintext passwords.
- Use HTTPS everywhere and keep secrets in the hosting provider's secret manager.
- Apply authorization checks to every user-owned resource, not only at the UI level.
- Validate request bodies, query parameters, file uploads, and external API responses.
- Add rate limiting to login, search, review creation, and other abuse-prone endpoints.
- Protect against insecure direct object references by verifying that the current user may access or modify the requested resource.
- Define data retention, account deletion, export, and consent practices appropriate to LGPD.
- Avoid logging passwords, access tokens, refresh tokens, private user data, or unnecessary personal information.
- Keep dependencies patched and run automated vulnerability/dependency checks in CI.

## 9. Cache, background jobs, and messaging

Start without additional infrastructure unless the product needs it.

- **In-process/background jobs:** Suitable for simple, low-volume work while running a single API instance, provided jobs can safely retry.
- **Managed queue or durable job system:** Introduce when catalog synchronization, imports, notifications, or recommendations require reliable asynchronous processing.
- **Redis:** Add only when shared caching, distributed coordination, or measured performance needs justify it. It is not the primary database.
- Make background jobs idempotent and observable. Record failures and support retries with limits and dead-letter handling where the chosen queue supports it.

## 10. AI and recommendations

AI is optional for the initial MVP. First build reliable catalog, tracking, rating, and list data.

Potential later features:

- Personalized recommendations based on user preferences and history.
- Natural-language search, such as “a short sci-fi series with a satisfying ending.”
- Summaries of a user's own viewing habits.
- Discovery assistants that explain why a title may fit a user's tastes.

Guidelines:

- Keep AI calls behind a backend service/interface.
- Never expose provider API keys in the mobile client.
- Treat model output as untrusted input and validate structured outputs.
- Do not invent catalog facts or availability. Ground factual answers in provider data.
- Respect consent, privacy, deletion requests, and data minimization.
- Use a tool/agent framework only when there is a clear multi-step workflow. A framework such as Mastra is TypeScript-oriented and would normally live in a separate TypeScript service if adopted; it is not required for a .NET-first backend.

## 11. Automation with n8n

Use n8n for workflows such as:

- Internal alerts for failed catalog syncs.
- Admin notifications.
- Scheduled operational reports.
- Low-risk integrations and prototypes.

Do not make n8n a mandatory synchronous dependency for core actions such as marking an episode watched, updating a list, or submitting a rating. Protect webhook endpoints, store credentials securely, add retry/error handling, and avoid sending unnecessary personal data to workflows.

## 12. Hosting and delivery

### Recommended initial setup

- **Mobile:** Expo development workflow and EAS Build/Submit for store builds and releases.
- **Backend:** Containerized ASP.NET Core API on a .NET-compatible managed hosting platform.
- **Database:** Managed PostgreSQL (Supabase is one option).
- **Web presence:** Optional landing page on Vercel; optional web admin can use Next.js and shadcn/ui.
- **CI/CD:** GitHub Actions or an equivalent pipeline for build, tests, security checks, and deployment.
- **Environments:** Development, staging, and production with separate credentials and databases.
- **Infrastructure configuration:** Environment variables and a secret manager; use infrastructure-as-code when the environment becomes complex enough to benefit from it.

Vercel is not the default host for an ASP.NET Core API. Do not assume a Next.js hosting platform will run a .NET backend as a native application; deploy the API to a platform that supports .NET containers or processes.

## 13. Testing and quality

- **Unit tests:** Domain rules, progress calculations, list rules, and application use cases.
- **Integration tests:** API endpoints, database constraints, authentication, and provider adapters.
- **Contract tests:** Verify the API's OpenAPI contract and generated client.
- **Mobile tests:** Component tests for important flows and end-to-end tests for sign-in, search, tracking, and list management.
- **Architecture tests:** Guard module boundaries and prevent unwanted dependencies.
- **CI checks:** Formatting, linting, type checking, build, tests, dependency audit, and secret scanning.

Do not chase a coverage percentage without testing meaningful behavior. Prioritize critical user journeys and regression-prone business rules.

## 14. MVP implementation order

1. Create the mobile app shell, navigation, Reelix brand assets and design tokens, and API client.
2. Implement authentication and user profile/preferences.
3. Integrate a permitted movie/TV metadata provider and catalog search.
4. Implement movie/series detail screens.
5. Implement watch status, episode progress, and watch history.
6. Implement watchlists and ratings.
7. Add pagination, caching, error states, telemetry, and production safeguards.
8. Add social features, recommendations, notifications, and AI only after validating demand.

## 15. Initial decisions summary

| Area | Recommendation |

| Mobile | React Native + Expo + TypeScript |
| Navigation | Expo Router |
| Server-state cache | TanStack Query |
| Validation | Zod on mobile; server-side validation in .NET |
| Backend | ASP.NET Core Web API + C# |
| API contract | REST + OpenAPI |
| ORM | EF Core + Npgsql |
| Database | PostgreSQL, optionally managed by Supabase |
| Authentication | Supabase Auth or ASP.NET Core Identity + identity provider |
| UI styling | React Native StyleSheet or NativeWind, using tokens mapped from the Reelix Brand Kit |
| Web admin (optional) | Next.js + shadcn/ui |
| Lint/format | Biome where supported; ESLint if needed |
| Date handling | date-fns |
| Observability | Structured logs now; evaluate Prometheus after hosting and scrape access are defined |
| Automation | n8n for asynchronous integrations, not core API requests |
| Deployment | Expo EAS for mobile builds; .NET-compatible hosting for API |
| Architecture | Modular monolith first |

## 16. Non-goals for the first release

- No microservices without a demonstrated need.
- No Kubernetes unless deployment scale and operational requirements justify it.
- No mandatory Redis, message broker, or vector database before use cases require them.
- No custom authentication or password cryptography.
- No AI agent framework until a concrete feature benefits from it.
- No assumption that metadata, posters, or streaming availability data can be reused without checking provider terms.

## 17. Evaluation of additional technologies

The [technology stack evaluation](technology-stack-evaluation.md) records why the reference stack is not copied wholesale. PostgreSQL and strict TypeScript are already in use. Bun, Node16 module mode, NestJS, and MikroORM are not being added because they do not solve a current gap or would duplicate the Expo/npm and ASP.NET Core/EF Core stacks. Money primitives remain out of scope until the product has monetary values.

Prometheus is the most plausible additive candidate, but implementation is deferred until API hosting, scrape networking, and endpoint protection are defined. SQS/LocalStack is deferred until a durable asynchronous workflow requires it. Continue using xUnit; consider Testcontainers for PostgreSQL integration tests when Docker is available in local development and CI. No runtime package or infrastructure is introduced by this evaluation.

---

**Guiding principle:** Build the smallest secure and reliable version of Reelix. Apply Clean Architecture, SOLID, pragmatic DDD, and KISS consistently so code remains readable, testable, and maintainable. Preserve security and privacy at every layer, and add infrastructure only when real product requirements justify its cost and complexity.

**Brand reference:** Keep [`visual_identity.png`](./visual_identity.png) in `docs/` as a visual direction board. Use the editable logos and other artwork in `mobile/assets/brand/`, with `mobile/assets/brand/docs/design-tokens.json` as the token source, for the app UI. Keep the implementation accessible and responsive; do not use the board itself as an app screen or asset.
