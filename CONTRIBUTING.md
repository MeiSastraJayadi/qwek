# Contributing to Qwek

Thanks for taking the time to contribute! This guide covers local setup, conventions, and how to submit changes.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL (local or [Neon](https://neon.tech))
- EF Core CLI tool

```bash
dotnet tool install --global dotnet-ef --version 10.0.14
```

## Local setup

1. Fork and clone the repository:

   ```bash
   git clone https://github.com/<your-username>/<repo-name>.git
   cd <repo-name>
   ```

2. Restore dependencies:

   ```bash
   dotnet restore
   ```

3. Configure secrets with user secrets (do not commit credentials):

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string>"
   dotnet user-secrets set "Jwt:Issuer" "qwek"
   dotnet user-secrets set "Jwt:Audience" "qwek"
   dotnet user-secrets set "Jwt:Key" "<random-string-at-least-32-chars>"
   dotnet user-secrets set "Jwt:ExpiryMinutes" "60"
   ```

   The connection string must use Npgsql keyword/value format, for example:

   ```text
   Host=ep-xxx-pooler.region.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=xxx;SSL Mode=Require;Trust Server Certificate=true
   ```

4. Apply database migrations:

   ```bash
   dotnet ef database update
   ```

5. Run the API:

   ```bash
   dotnet run
   ```

   The API listens on `http://localhost:5206`. OpenAPI is available in Development at `/openapi/v1.json`.

## Project structure

```text
Domains/         Entities and repository contracts
Applications/    Use cases, ports, and DTOs
Infrastructures/ EF Core, Identity, repositories, and services
Interfaces/      HTTP endpoints, SignalR hub, and web adapters
Migrations/      EF Core migrations
```

Keep dependencies pointing inward: `Interfaces` -> `Applications` -> `Domains`. Infrastructure implements ports defined by the application layer.

## Making changes

1. Create a branch from `main` with a descriptive name:

   ```bash
   git checkout -b feat/channel-threads
   ```

2. Write focused changes. Avoid mixing refactors with features or fixes.
3. Add a migration if the schema changes:

   ```bash
   dotnet ef migrations add <MigrationName>
   ```

4. Verify the build before committing:

   ```bash
   dotnet build
   ```

5. Commit using [Conventional Commits](https://www.conventionalcommits.org/):

   ```text
   feat: add thread replies
   fix: reject duplicate usernames
   docs: document avatar uploads
   chore: update dependencies
   ```

## Pull requests

- Keep PRs small and scoped to a single concern.
- Describe the problem, the approach, and how you tested it.
- Reference related issues (for example, `Closes #12`).
- Make sure the project builds and no secrets, `.env` files, or generated artifacts are committed.
- Update `README.md` if behavior, configuration, or API routes change.

## Reporting issues

Include:

- Steps to reproduce
- Expected vs. actual behavior
- Relevant logs or error messages
- Environment details (.NET version, OS, database)

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
