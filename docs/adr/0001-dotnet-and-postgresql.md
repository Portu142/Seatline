# ADR 0001: ASP.NET Core (.NET 10) and PostgreSQL for the backend

- **Status:** Accepted
- **Date:** 2026-11-27

## Context

Seatline sells cinema tickets for a 200-seat hall. Two requirements dominate the backend choice:

1. **No seat can be sold twice**, even when many people click the same seat at the same moment.
   This must be enforced by the data layer, not by "check then insert" application code that loses races.
2. **Tickets cannot be forged or reused**, and validating one at the door must be atomic.

Secondary goals: the project is a portfolio piece aimed at backend roles in Spain, so the stack should be one
that companies here actually run in production, with first-class testing and container support.

## Decision

- **ASP.NET Core Web API on .NET 10 (LTS)** with **EF Core** for data access.
- **PostgreSQL** from day one, in Docker locally and in CI (via Testcontainers), and managed Postgres in production.
- **ASP.NET Core Identity** for accounts and password hashing, with **JWT access tokens + rotating refresh tokens**.

## Rationale

**.NET**
- Widely used by Spanish consultancies and enterprises; strong typing and mature tooling.
- EF Core gives explicit transactions, migrations and raw SQL when needed, which is what the seat-hold logic needs.
- Identity provides vetted password hashing, lockout and role management instead of hand-rolled auth.
- First-class OpenAPI support, `WebApplicationFactory` for in-memory integration tests, and small
  chiseled (distroless) container images.
- .NET 10 is the current LTS (supported until November 2028). .NET 8 support ended in November 2026.

**PostgreSQL**
- Real concurrency semantics: unique constraints, row locks (`SELECT ... FOR UPDATE`), and
  transaction isolation behave the same in tests and production.
- A unique index on `(show_id, seat_id)` is the simplest bulletproof guard against double booking:
  the database rejects the second insert even if two requests arrive in the same millisecond.
- SQLite or the EF in-memory provider would make the concurrency tests meaningless, so they are ruled out even for tests.
- Free managed tiers exist (Neon, Render, Railway), so the demo can stay online at no cost.

## Alternatives considered

| Option | Why not |
|---|---|
| Node.js (NestJS) + Postgres | Equally capable; less aligned with the target job market, and the author wants to deepen .NET. |
| Java (Spring Boot) | Strong in Spain too, but heavier setup for a 4-week MVP. |
| SQLite | No real concurrent writers; would hide exactly the race conditions the project must prove it handles. |
| SQL Server | Fine technically, but no free managed tier and heavier container image. |

## Consequences

- Integration tests need Docker (Testcontainers). GitHub Actions runners have it, and so does local dev via Docker Compose.
- Hosting must run a container (Render, Railway, Fly.io) instead of a serverless platform.
- Concurrency guarantees live in the schema (constraints) and in short transactions. They are covered by
  dedicated tests in block 2 ("50 users, 1 seat → exactly 1 succeeds").
