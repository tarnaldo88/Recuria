# \# Recuria

# 

# \## Overview

# 

# Recuria is a \*\*multi-tenant SaaS subscription and billing platform\*\* designed for \*\*B2B software products\*\*. It provides the core billing capabilities that modern SaaS applications require, including subscription lifecycle management, recurring invoicing, plan enforcement, and role-based access.

# 

# Recuria is intentionally scoped to reflect \*\*real-world production systems\*\*, focusing on correctness, separation of concerns, and maintainability rather than surface-level features.

# 

# This project is built as a \*\*full-stack ASP.NET Core application\*\* with a Blazor WebAssembly frontend and a cleanly layered backend architecture.

# 

# ---

# 

# \## SaaS Scenario: What Recuria Is For

# 

# Recuria represents the \*\*billing subsystem for a B2B SaaS product\*\* such as:

# 

# \* Internal analytics dashboards

# \* Developer tools

# \* Collaboration or productivity platforms

# \* Admin or back-office systems

# 

# \### Conceptual Example

# 

# A fictional company, \*\*Acme Analytics\*\*, offers a web-based analytics dashboard for teams.

# 

# \* Companies sign up and create an \*\*Organization\*\*

# \* Each organization chooses a \*\*subscription plan\*\*

# \* Users are invited to the organization with specific roles

# \* Billing is handled automatically on a recurring cycle

# 

# Recuria is the system responsible for:

# 

# \* Managing subscriptions and plans

# \* Enforcing plan limits

# \* Generating invoices

# \* Tracking subscription state transitions

# 

# It does \*\*not\*\* process real payments; instead, it simulates billing behavior to focus on backend correctness and architecture.

# 

# ---

# 

# \## Core Features

# 

# \### Subscription Management

# 

# \* Free trial and paid plans

# \* One active subscription per organization

# \* Explicit subscription states:

# 

# &nbsp; \* Trialing

# &nbsp; \* Active

# &nbsp; \* PastDue

# &nbsp; \* Canceled

# \* Plan upgrades and downgrades

# 

# \### Billing \& Invoicing

# 

# \* Recurring billing cycles

# \* Invoice generation per period

# \* Immutable invoices once finalized

# \* Grace periods for overdue subscriptions

# 

# \### Multi-Tenancy

# 

# \* Organizations own subscriptions

# \* Users can belong to multiple organizations

# \* Role-based access per organization

# 

# \### Authentication \& Authorization

# 

# \* Secure authentication using ASP.NET Core Identity

# \* JWT-based API authentication

# \* Role-based authorization for protected operations

# 

# ---

# 

# \## Architecture

# 

# Recuria follows a \*\*Clean Architecture\*\* approach, separating business logic from infrastructure and presentation layers.

# 

# ```text

# Recuria.sln

# │

# ├── Recuria.Api            // ASP.NET Core Web API

# ├── Recuria.Blazor         // Blazor WebAssembly frontend

# ├── Recuria.Domain         // Domain entities and enums

# ├── Recuria.Application    // Business logic and use cases

# ├── Recuria.Infrastructure // EF Core, Identity, persistence

# ```

# 

# \### Key Design Principles

# 

# \* Business rules live in the Application layer

# \* Domain models are persistence-agnostic

# \* API layer exposes DTOs, not entities

# \* Infrastructure concerns are isolated

# 

# ---

# 

# \## Technology Stack

# 

# \* \*\*Backend:\*\* ASP.NET Core (.NET 8)

# \* \*\*Frontend:\*\* Blazor WebAssembly

# \* \*\*Database:\*\* SQL Server (EF Core)

# \* \*\*Authentication:\*\* ASP.NET Core Identity + JWT

# \* \*\*API Documentation:\*\* Swagger / OpenAPI

# 

# ---

# 

# \## Goals of This Project

# 

# This project is designed to:

# 

# \* Demonstrate professional ASP.NET Core architecture

# \* Model non-trivial business domains (billing, subscriptions)

# \* Show full-stack capability using modern .NET technologies

# \* Serve as a portfolio project aligned with real industry practices

# 

# ---

# 

# \## Non-Goals

# 

# \* Real payment processing (Stripe, PayPal, etc.)

# \* Production-grade UI/UX polish

# \* Microservices or distributed systems

# 

# These are intentionally excluded to keep the focus on correctness and design.

# 

# ---

# 

# \## Future Enhancements

# 

# Potential extensions include:

# 

# \* Usage-based billing

# \* Webhooks for external systems

# \* Audit logs and billing history

# \* Dockerized deployment

# 

# ---

# 

# \## Author

# 

# Built as a professional portfolio project to demonstrate full-stack ASP.NET Core development and SaaS billing concepts.



