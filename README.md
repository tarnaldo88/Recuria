# Recuria

Recuria is a **SaaS subscription and billing platform** built with **ASP.NET Core (.NET 8)**.  
It models real-world SaaS business rules such as organization ownership, subscription lifecycles, role-based access, and invoice generation.

This project is designed as a **portfolio-quality, industry-aligned system** emphasizing domain modeling, correctness, and testability rather than simple CRUD operations.

---

## Purpose

Recuria simulates the backend architecture of a modern SaaS product that supports:

- Organization-based accounts
- Role-based user management
- Subscription lifecycle management (trial → active → canceled)
- Plan upgrades
- Invoice generation
- Enforced business invariants

The goal is to demonstrate **professional backend engineering practices** suitable for full-stack or backend roles.

---

## Architecture

Recuria follows a Clean Architecture / DDD-inspired structure:

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
- Code is validated through unit tests, not just manual testing

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
  - Has lifecycle states: `Trial`, `Active`, `Canceled`

- **Invoice**
  - Generated from subscriptions
  - Represents billable charges

---

## Business Rules & Invariants

Recuria enforces realistic SaaS constraints:

- An organization must always have an **Owner**
- Owners cannot be removed or demoted
- Users cannot belong to multiple organizations
- Subscriptions cannot be upgraded once canceled
- Only one active subscription per organization
- Trial subscriptions have a fixed duration

All rules are enforced **in code**, not just in the database.

---

## Testing

The project includes unit tests focused on **business behavior**, not implementation details.

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

### Testing Stack

- **xUnit**
- **FluentAssertions**

---

## Technology Stack

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Blazor WebAssembly (planned)
- xUnit + FluentAssertions

---

## Getting Started

### Prerequisites

- .NET SDK 8.0+
- SQL Server (local or Docker)
- Visual Studio 2022+

### Setup

```bash
dotnet restore
dotnet ef database update
dotnet run --project Recuria.Api

### Run Tests

- dotnet test
