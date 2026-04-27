# NaijaShield AI — Enterprise Telecom Fraud Detection Platform

[![CI](https://github.com/your-org/naijashield-ai/actions/workflows/ci.yml/badge.svg)](https://github.com/your-org/naijashield-ai/actions)
[![.NET](https://img.shields.io/badge/.NET-9.0-blueviolet)](https://dotnet.microsoft.com)

> AI-powered scam detection for Nigerian telecoms (MTN, Airtel, Glo, 9mobile) across 5 languages — English, Pidgin, Yoruba, Hausa, Igbo.

---

## Architecture

```
NaijaShield/
├── src/
│   ├── NaijaShield.Domain          # Aggregates, Entities, Domain Events, Enums
│   ├── NaijaShield.Application     # CQRS Commands/Queries, MediatR Handlers, Interfaces
│   ├── NaijaShield.Infrastructure  # EF Core, Semantic Kernel, Azure SDK, Repositories
│   ├── NaijaShield.Api             # ASP.NET Core Web API, SignalR Hubs, Middleware
│   └── NaijaShield.BackgroundJobs  # Hangfire Worker (6 recurring jobs)
├── tests/
│   ├── NaijaShield.UnitTests
│   ├── NaijaShield.IntegrationTests
│   └── NaijaShield.ArchitectureTests
├── docker/
│   ├── Dockerfile
│   ├── Dockerfile.jobs
│   └── docker-compose.yml
└── .github/workflows/
    └── ci.yml
```

Clean Architecture with CQRS/MediatR:
- **Domain** → no dependencies
- **Application** → depends on Domain only
- **Infrastructure** → depends on Application + Domain
- **Api** → depends on all layers

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for local infra)
- Azure Subscription (for Azure OpenAI, Azure Storage, Service Bus)

---

## Local Development

### 1. Start local infrastructure

```bash
cd backend/docker
docker-compose up -d sqlserver redis azurite
```

### 2. Set user secrets (never commit secrets to git)

```bash
cd backend/src/NaijaShield.Api

dotnet user-secrets set "Jwt:SecretKey" "your-super-secret-key-at-least-32-chars"
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-key"
dotnet user-secrets set "AfricasTalking:ApiKey" "your-key"
dotnet user-secrets set "AfricasTalking:Username" "sandbox"
```

### 3. Apply database migrations

```bash
cd backend
dotnet ef database update --project src/NaijaShield.Infrastructure --startup-project src/NaijaShield.Api
```

### 4. Run the API

```bash
dotnet run --project src/NaijaShield.Api
```

API runs at: http://localhost:5000  
Swagger UI: http://localhost:5000 (dev only)

### 5. Run background jobs

```bash
dotnet run --project src/NaijaShield.BackgroundJobs
```

---

## Running Tests

```bash
# All tests
cd backend
dotnet test NaijaShield.sln

# Unit tests only
dotnet test tests/NaijaShield.UnitTests

# Architecture tests only
dotnet test tests/NaijaShield.ArchitectureTests

# Integration tests (requires Docker)
dotnet test tests/NaijaShield.IntegrationTests
```

---

## Docker (full stack)

```bash
cd backend/docker

# Build and start all services
docker-compose up --build

# Stop
docker-compose down
```

Set Azure OpenAI credentials via environment variables:
```bash
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
export AZURE_OPENAI_KEY="your-key"
docker-compose up --build
```

---

## API Overview

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/auth/login` | Authenticate (email + password) |
| POST | `/api/v1/auth/refresh` | Rotate refresh token |
| GET | `/api/v1/fraud/calls` | Search scam calls |
| POST | `/api/v1/fraud/calls/{id}/confirm` | Confirm scam |
| POST | `/api/v1/fraud/calls/{id}/warn` | Send SMS/WhatsApp warning |
| GET | `/api/v1/fraud/watchlist` | Blocked numbers list |
| GET | `/api/v1/dashboard/kpis` | Real-time KPI metrics |
| POST | `/api/v1/aistudio/sandbox` | Test transcript classification |
| GET | `/api/v1/reports` | Regulatory reports list |
| POST | `/api/v1/reports/generate` | Generate NCC/CBN/EFCC report |

**SignalR Hubs:**
- `/hubs/fraud` — Real-time scam detection alerts
- `/hubs/dashboard` — Live KPI updates
- `/hubs/conversations` — Customer conversation events

---

## Supported Languages

| Code | Language |
|------|----------|
| `en` | English |
| `pcm` | Nigerian Pidgin |
| `yo` | Yoruba |
| `ha` | Hausa |
| `ig` | Igbo |

---

## Key Environment Variables

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `ConnectionStrings__Redis` | Redis connection string |
| `Jwt__SecretKey` | JWT signing key (≥32 chars) |
| `AzureOpenAI__Endpoint` | Azure OpenAI endpoint URL |
| `AzureOpenAI__ApiKey` | Azure OpenAI API key |
| `AzureOpenAI__DeploymentName` | Model deployment name (default: `gpt-4o`) |
| `AfricasTalking__ApiKey` | Africa's Talking SMS API key |
| `WhatsApp__AccessToken` | WhatsApp Business Cloud API token |

---

## Security

- JWT authentication with refresh token rotation
- HMAC audit chain — tamper-evident log integrity
- Soft delete — no data is hard-deleted without data retention policy
- Non-root Docker containers
- Rate limiting (100 req/min/user)
- Security headers middleware (CSP, X-Frame-Options, etc.)
- Secrets managed via Azure Key Vault in production (never in appsettings)

Report vulnerabilities: see [SECURITY.md](SECURITY.md)

---

## License

Proprietary — All rights reserved © NaijaShield AI
