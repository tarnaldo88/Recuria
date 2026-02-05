# Recuria

Recuria is a **SaaS subscription and billing platform** built with **ASP.NET Core (.NET 8)**.

It models real-world SaaS business rules including organization ownership, subscription lifecycles, role-based access, billing, invoicing, and observability.

This project is designed as a **industry-aligned system** emphasizing domain modeling, correctness, and testability rather than simple CRUD operations.

---

## Purpose

Recuria simulates the backend architecture of a modern SaaS product that supports:

- Organization-based accounts
- Role-based user management
- Subscription lifecycle management  
  *(trial → active → past-due → canceled → expired)*
- Plan upgrades with enforced business invariants
- Automated billing cycles with retry and grace periods
- Invoice generation
- Event-driven workflows using domain events and the outbox pattern
- Built-in observability (logging, metrics, tracing)

The goal is to demonstrate **professional backend engineering practices** suitable for backend or full-stack roles.

---

## Architecture

Recuria follows a **Clean Architecture / DDD-inspired structure**, with strict separation of concerns.

```text
Recuria
│
├── Recuria.Domain          // Core domain entities & business rules
├── Recuria.Application     // Use cases, services, interfaces
├── Recuria.Infrastructure  // EF Core, persistence, background services
├── Recuria.Api             // ASP.NET Core Web API
├── Recuria.Blazor          // Blazor WebAssembly frontend (in progress)
└── Recuria.Tests           // Unit & integration tests
```
---

## Architectural Principles

- Domain logic is isolated from frameworks and persistence
- Business rules are enforced in domain entities and services
- EF Core is configured explicitly (no convention-only modeling)
- Domain events drive side effects and workflows
- Outbox pattern ensures reliable, eventually consistent processing
- Background services handle billing, retries, and outbox dispatch
- Observability is treated as a first-class concern

---

## Domain Model

### Core Entities

#### Organization
- Owns users and subscriptions
- Enforces ownership rules
- Determines the currently active subscription

#### User
- Belongs to exactly one organization
- Has a role: `Owner`, `Admin`, or `Member`

#### Subscription
- Belongs to an organization
- Tracks plan and billing period
- Lifecycle states:
  - `Trial`
  - `Active`
  - `PastDue`
  - `Canceled`
  - `Expired`
- Emits domain events on state transitions  
  *(e.g., `SubscriptionActivatedDomainEvent`)*

#### Invoice
- Generated from subscriptions
- Represents billable charges

#### BillingAttempt & OutboxMessage
- Persist billing attempts and domain events
- Enable retries, idempotency, and resilience

---

## Business Rules & Invariants

Recuria enforces realistic SaaS constraints **in code**, not just the database:

- An organization must always have **one Owner**
- Owners cannot be removed or demoted
- Users cannot belong to multiple organizations
- Only one active subscription per organization
- Subscriptions cannot be upgraded once canceled or expired
- Trial subscriptions have a fixed duration
- Billing only occurs for active subscriptions
- Grace periods apply before cancellation
- Domain events are persisted reliably via the outbox pattern

---

## Billing & Subscription Lifecycle

- Automated billing cycles per subscription
- Grace periods before cancellation for past-due subscriptions
- Retry policies for failed billing attempts
- Centralized lifecycle orchestration:
  - Trial expiration
  - Active billing and renewal
  - Past-due handling
  - Cancellation after grace period
- Domain events emitted for activation, expiration, and cancellation

---

## Observability

### Logging
- Structured logs across domain logic, billing, and background jobs
- Serilog JSON output with correlation IDs (`X-Correlation-Id`)

### CORS & Security Headers
- Configure allowed origins in `Recuria.Api/appsettings.json`
- Security headers are enabled by default (CSP, Permissions-Policy, etc.)

### Metrics
- Prometheus-compatible metrics for:
  - Subscription counts
  - Billing success/failure
  - Retry attempts

### Tracing
- OpenTelemetry tracing for:
  - HTTP requests
  - EF Core queries
  - Background services

### Runtime Instrumentation
- CPU, memory, GC, thread, and process metrics

---

## Testing

Testing focuses on **business behavior**, not implementation details.

### Covered Areas

- **OrganizationService**
  - Organization creation
  - User management
  - Role enforcement

- **SubscriptionService**
  - Trial creation
  - Plan upgrades
  - Cancellation logic
  - Invoice generation

- **SubscriptionLifecycleOrchestrator**
  - Trial expiration
  - Billing period advancement
  - Past-due transitions
  - Cancellation after grace period

- **BillingService**
  - Retry behavior
  - Failure handling
  - Invoice creation

### Testing Stack

- xUnit
- FluentAssertions
- Moq (where appropriate)
- Integration tests using real EF Core contexts

---

## Technology Stack

- **.NET 8 / C# 12**
- **ASP.NET Core Web API**
- **Entity Framework Core 10**
- **SQL Server**
- **Blazor WebAssembly** (planned)
- **OpenTelemetry** (Prometheus / OTLP)
- **xUnit + FluentAssertions**

---

## Getting Started

### Prerequisites

- .NET SDK 8+
- SQL Server (local instance or Docker)
- Visual Studio 2022+ or VS Code

### Setup

```bash
dotnet restore
dotnet ef database update --project Recuria.Infrastructure
dotnet run --project Recuria.Api
```

---

## Secrets management (free + easy)
Local development:
```bash
dotnet user-secrets init --project Recuria.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your-connection-string>" --project Recuria.Api
dotnet user-secrets set "Jwt:Issuer" "Recuria" --project Recuria.Api
dotnet user-secrets set "Jwt:Audience" "Recuria.Api" --project Recuria.Api
dotnet user-secrets set "Jwt:SigningKey" "CHANGE_ME_DEV_KEY" --project Recuria.Api
```

Production (environment variables):
```
ConnectionStrings__DefaultConnection
Jwt__Issuer
Jwt__Audience
Jwt__SigningKey
Cors__AllowedOrigins__0
SecurityHeaders__ContentSecurityPolicy
SecurityHeaders__PermissionsPolicy
```

---

## Disaster recovery (DR)
Targets (example):
- RPO: 15 minutes
- RTO: 1 hour

Automated backup scripts:
- `scripts\backup-db.ps1`
- `scripts\restore-db.ps1`

Env vars required:
```
RECURIA_SQL_SERVER
RECURIA_SQL_DATABASE
RECURIA_SQL_BACKUP_PATH
RECURIA_SQL_BACKUP_FILE
```

Run a restore drill quarterly (staging or isolated environment).
