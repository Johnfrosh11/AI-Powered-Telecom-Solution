# Contributing to NaijaShield AI

Thank you for your interest in contributing to NaijaShield AI — Nigeria's AI-powered telecoms fraud detection platform. This guide will get you from zero to first pull request quickly.

---

## Table of Contents

1. [Development Setup](#1-development-setup)
2. [Branch Naming](#2-branch-naming)
3. [Commit Style (Conventional Commits)](#3-commit-style-conventional-commits)
4. [Pull Request Process](#4-pull-request-process)
5. [Code Style](#5-code-style)
6. [Test Requirements](#6-test-requirements)
7. [Architecture Overview](#7-architecture-overview)

---

## 1. Development Setup

### Prerequisites

| Tool | Minimum version |
|------|----------------|
| .NET SDK | 9.0 |
| Docker Desktop | 4.x |
| Git | 2.40 |
| PowerShell | 7.4 |

### Quick Start

```powershell
# Clone the repo
git clone https://github.com/Johnfrosh11/AI-Powered-Telecom-Solution.git
cd AI-Powered-Telecom-Solution

# Start dependencies (SQL Server, Redis)
docker compose up -d

# Wait for SQL to be healthy, then seed the dev database
./scripts/seed.ps1

# Run the API
cd backend
dotnet run --project src/NaijaShield.Api
```

The API will be available at `https://localhost:5001` with Swagger UI at the root.

### Environment Variables

Copy `backend/appsettings.Development.json.example` to `backend/appsettings.Development.json` and fill in:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=NaijaShieldDev;User Id=sa;Password=NaijaShield@Dev1;"
  },
  "Redis": { "ConnectionString": "localhost:6379" },
  "AzureOpenAI": {
    "Endpoint": "https://<your-aoai>.openai.azure.com/",
    "ApiKey": "<your-key>",
    "Gpt4oDeployment": "gpt-4o",
    "Gpt4oMiniDeployment": "gpt-4o-mini"
  },
  "Jwt": {
    "Key": "<32-char-secret>",
    "Issuer": "https://localhost:5001",
    "Audience": "naijashield-dev"
  }
}
```

---

## 2. Branch Naming

All branches must follow this convention:

```
<type>/<short-description>
```

| Prefix | Use for |
|--------|---------|
| `feature/` | New features, screens, endpoints |
| `fix/` | Bug fixes |
| `chore/` | Tooling, config, dependency updates |
| `refactor/` | Internal restructuring without behaviour change |
| `docs/` | Documentation only |
| `test/` | Adding or fixing tests |
| `perf/` | Performance improvements |

**Examples:**

```
feature/scam-call-pagination
fix/jwt-refresh-token-expiry
chore/upgrade-ef-core-9.1
docs/api-authentication-guide
```

Branch names must be **kebab-case** and **lowercase only**. No spaces or uppercase.

---

## 3. Commit Style (Conventional Commits)

All commits must follow [Conventional Commits 1.0](https://www.conventionalcommits.org/).

### Format

```
<type>(<optional scope>): <description>

[optional body]

[optional footer]
```

### Types

| Type | When to use |
|------|------------|
| `feat` | Adds a new feature |
| `fix` | Fixes a bug |
| `docs` | Documentation changes |
| `style` | Formatting only (no logic change) |
| `refactor` | Code restructure without feature/fix |
| `perf` | Performance improvement |
| `test` | Adding or updating tests |
| `chore` | Build, CI, dependency updates |
| `ci` | GitHub Actions / pipeline changes |
| `revert` | Reverts a previous commit |

### Examples

```
feat(fraud): add scam pattern bulk import endpoint
fix(auth): prevent token reuse after logout
docs(api): add Postman collection for fraud endpoints
chore(deps): upgrade Semantic Kernel to 1.50.0
test(integration): add ScamCall confirmation flow test
```

### Rules

- **Description** is lowercase, no period at end
- **Body** explains *why*, not *what* (the diff shows the what)
- **Breaking changes** must include `BREAKING CHANGE:` in the footer
- Commits must be **atomic** — one logical change per commit

---

## 4. Pull Request Process

### Before Opening a PR

- [ ] All tests pass: `dotnet test backend/NaijaShield.sln`
- [ ] No build warnings: `dotnet build -c Release`
- [ ] Code formatted: `dotnet format`
- [ ] New public APIs have XML doc comments
- [ ] Database changes include a migration: `dotnet ef migrations add <Name>`
- [ ] `appsettings.*.json` secrets are **not** committed

### PR Title

Must match Conventional Commit format:

```
feat(fraud): add EFCC report bulk submission
```

### PR Description Template

```markdown
## Summary
<!-- One paragraph: what does this PR do and why? -->

## Changes
- 
- 

## Test Plan
- [ ] Unit tests added/updated
- [ ] Integration test added/updated
- [ ] Manual smoke test performed

## Screenshots (if UI changes)

## Checklist
- [ ] Follows branch naming convention
- [ ] Commits follow Conventional Commits
- [ ] No secrets or credentials in code
- [ ] Migration added for DB schema changes
```

### Review Process

1. At least **1 approving review** required for `main`
2. **2 approvals** required for security-sensitive changes (auth, permissions, PII)
3. All CI checks must pass (build, unit tests, integration tests)
4. Reviewer resolves their own comments after addressing
5. Squash-merge into `main`

---

## 5. Code Style

### General

- Follow **Clean Architecture** — domain logic never depends on infrastructure
- **CQRS**: every use case is a `Command` or `Query` + `Handler` pair in `Features/<domain>/`
- Return `Result<T>` / `Result` from handlers — **no exceptions for business logic**
- Use **`sealed`** on all concrete classes that are not designed for inheritance
- Prefer **primary constructors** (.NET 8+) for dependency injection

### C# Conventions

```csharp
// ✅ Good — sealed, primary ctor, async/await
internal sealed class GetScamCallsHandler(IAppDbContext db, ICurrentUserService user)
    : IRequestHandler<GetScamCallsQuery, Result<PagedList<ScamCallDto>>>
{
    public async Task<Result<PagedList<ScamCallDto>>> Handle(
        GetScamCallsQuery request, CancellationToken ct)
    {
        // ...
    }
}

// ❌ Bad — not sealed, manual DI field assignment
public class GetScamCallsHandler : IRequestHandler<...>
{
    private readonly IAppDbContext _db;
    public GetScamCallsHandler(IAppDbContext db) => _db = db;
}
```

### Naming

| Symbol | Convention | Example |
|--------|-----------|---------|
| Class / Interface | PascalCase | `ScamCallHandler` |
| Private field | `_camelCase` | `_context` |
| Local variable | camelCase | `scamCall` |
| Async method | Suffix `Async` | `GetCallsAsync` |
| Constant | UPPER_SNAKE | `MAX_RETRY_COUNT` |

### No Magic Strings

Use constants from `NaijaShield.Domain.Constants.*`:

```csharp
// ✅
if (user.HasPermission(Permissions.FraudCallsBlock)) { ... }

// ❌
if (user.HasPermission("fraud.calls.block")) { ... }
```

---

## 6. Test Requirements

### Unit Tests (`tests/NaijaShield.UnitTests/`)

- Every **Command** and **Query handler** must have unit tests
- Use **xUnit** + **FluentAssertions**
- Mock dependencies with **NSubstitute**
- Target: **≥ 80% coverage** on Application layer

```csharp
[Fact]
public async Task Handle_WhenCallExists_ConfirmsSuccessfully()
{
    // Arrange
    var call = ScamCall.Create(tenantId, ...);
    // ...

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
}
```

### Integration Tests (`tests/NaijaShield.IntegrationTests/`)

- Use **Testcontainers.MsSql** for a real SQL Server instance
- Use **WebApplicationFactory** for full HTTP stack tests
- Test happy path **and** auth/authorisation boundaries
- Must pass without any external Azure services (mock or skip AI calls)

### What NOT to Test

- Trivial property getters/setters
- AutoMapper configurations (covered by `AssertConfigurationIsValid()` in CI)
- EF Core migrations (covered by integration tests)

---

## 7. Architecture Overview

```
backend/
├── src/
│   ├── NaijaShield.Domain/          # Entities, value objects, domain events
│   ├── NaijaShield.Application/     # CQRS handlers, interfaces, DTOs
│   ├── NaijaShield.Infrastructure/  # EF Core, Redis, Azure services, repos
│   ├── NaijaShield.Api/             # ASP.NET Core controllers, SignalR hubs
│   └── NaijaShield.BackgroundJobs/  # Hangfire job registration
├── tests/
│   ├── NaijaShield.UnitTests/
│   └── NaijaShield.IntegrationTests/
infra/                               # Bicep infrastructure-as-code
.github/workflows/                   # CI/CD pipelines
```

### Feature Folder Structure

```
Application/Features/<Domain>/
├── <EntityName>Dtos.cs         # All DTOs for the domain
├── Commands/
│   ├── CreateXxx/
│   │   ├── CreateXxxCommand.cs
│   │   ├── CreateXxxCommandHandler.cs
│   │   └── CreateXxxCommandValidator.cs
└── Queries/
    └── GetXxx/
        ├── GetXxxQuery.cs
        └── GetXxxQueryHandler.cs
```

---

## Questions?

Open a [GitHub Discussion](https://github.com/Johnfrosh11/AI-Powered-Telecom-Solution/discussions) or ping **@Johnfrosh11** in an issue.
