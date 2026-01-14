# Recuria

Recuria is a **SaaS subscription and billing platform** built with **ASP.NET Core (.NET 10)**.  
It models real-world SaaS business rules including organization ownership, subscription lifecycles, role-based access, billing, invoicing, and observability.

This project is designed as a **portfolio-quality, industry-aligned system** emphasizing domain modeling, correctness, and testability rather than simple CRUD operations.

---

## Purpose

Recuria simulates the backend architecture of a modern SaaS product that supports:

- Organization-based accounts
- Role-based user management
- Subscription lifecycle management (trial → active → past-due → canceled → expired)
- Plan upgrades and enforced business invariants
- Automated billing cycles with retry and grace periods
- Invoice generation
- Event-driven workflows with domain events and outbox pattern
- Observability with logging, metrics, and distributed tracing

The goal is to demonstrate **professional backend engineering practices** suitable for full-stack or backend roles.

---

## Architecture

Recuria follows a **Clean Architecture / DDD-inspired structure**:

Recuria
│
├── Recuria.Domain // Core domain entities & business rules
├── Recuria.Application // Services & interfaces
├── Recuria.Infrastructure // EF Core persistence & configurations
├── Recuria.Api // ASP.NET Core Web API
├── Recuria.Blazor // Blazor WebAssembly frontend (in progress)
└── Recuria.Tests // Unit tests


### Architectural Principles

- Domain logic is isolated from frameworks and persistence
- Business rules are enforced in domain entities and services
- EF Core is configured explicitly (no convention-only modeling)
- Outbox pattern ensures reliable, eventually consistent event dispatch
- Background services handle billing, retries, and outbox processing
- Code is validated through **unit tests**, not just manual testing
- Observability built-in via OpenTelemetry metrics and tracing

---

## Domain Model

### Core Entities

- **Organization**
  - Owns users and subscriptions
  - Enforces ownership and subscription rules
  - Determines the currently active subscription

- **User**
  - Belongs to a single organization
  - Has a role: `Owner`, `Admin`, or `Member`

- **Subscription**
  - Belongs to an organization
  - Tracks billing period and plan
  - Has lifecycle states: `Trial`, `Active`, `PastDue`, `Canceled`, `Expired`
  - Emits domain events (e.g., `SubscriptionActivatedDomainEvent`) on state changes

- **Invoice**
  - Generated from subscriptions
  - Represents billable charges

- **BillingAttempt & OutboxMessage**
  - Persist billing attempts and events in the same transactional context
  - Supports retries with exponential backoff and idempotency

---

## Business Rules & Invariants

Recuria enforces realistic SaaS constraints:

- An organization must always have an **Owner**
- Owners cannot be removed or demoted
- Users cannot belong to multiple organizations
- Only one active subscription per organization
- Subscriptions cannot be upgraded once canceled or expired
- Trial subscriptions have a fixed duration
- Billing only occurs on active subscriptions and respects grace periods
- Domain events are persisted reliably via outbox pattern

All rules are enforced **in code**, not just in the database.

---

## Billing & Subscription Lifecycle

- Automated billing cycles for each subscription
- Grace periods before cancellation for past-due subscriptions
- Retry policy for failed billing attempts
- Subscription lifecycle orchestrator centralizes trial, active, and past-due processing
- Domain events emitted for subscription activations, cancellations, and expirations

---

## Observability

- **Logging:** Structured logging throughout subscription lifecycle, billing, and domain events
- **Metrics:** Prometheus-compatible metrics for subscription counts, billing success/failure, and retries
- **Tracing:** OpenTelemetry tracing for HTTP requests, EF Core queries, and background services
- **Runtime & Process Instrumentation:** CPU, memory, thread, and GC metrics

---

## Testing

The project includes unit and integration tests focused on **business behavior**, not implementation details.

### Test Coverage

- `OrganizationService`
  - Organization creation
  - User management
  - Role enforcement

- `SubscriptionService`
  - Trial creation
  - Plan upgrades
  - Cancellation logic
  - Invoice generation

- `SubscriptionLifecycleOrchestrator`
  - Trial expiration
  - Active subscription billing and period advancement
  - Past-due handling
  - Cancellation after grace period

- `BillingService`
  - Billing retries and failure handling
  - Invoice generation
  - Past-due status enforcement

### Testing Stack

- **xUnit**
- **FluentAssertions**
- Moq for mocking services

---

## Technology Stack

- **.NET 10 / C# 12**
- **ASP.NET Core Web API**
- **Entity Framework Core 10**
- **SQL Server**
- **Blazor WebAssembly** (planned)
- **OpenTelemetry** (Prometheus/OTLP)
- **xUnit + FluentAssertions**

---

## Getting Started

### Prerequisites

- .NET SDK 10+
- SQL Server (local or Docker)
- Visual Studio 2022+ or VS Code

### Setup

```bash
dotnet restore
dotnet ef database update --project Recuria.Infrastructure
dotnet run --project Recuria.Api