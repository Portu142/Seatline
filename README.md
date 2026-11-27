# Seatline

[![CI](https://github.com/Portu142/seatline/actions/workflows/ci.yml/badge.svg)](https://github.com/Portu142/seatline/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**Cinema ticketing for a 200-seat hall.** Pick your seat on a live seat map, pay, and walk in with a QR ticket
that can't be forged or used twice.

> 🚧 **Work in progress.** Target: deployed MVP on **23 Dec 2026**. This README grows with every release.

## The two hard problems

1. **Two people can never buy the same seat**, even if they click at the same millisecond. A unique constraint on
   `(show, seat)` in PostgreSQL rejects the loser, seat holds are all-or-nothing transactions, and holds expire
   after 10 minutes. A test fires 50 concurrent buyers at one seat and expects exactly one winner.
2. **Tickets can't be faked or reused.** Each QR carries an HMAC signature, and validation at the door marks the
   ticket as used in a single atomic update.

## Try it

| Role | Email | Can |
|---|---|---|
| Customer | `customer@seatline.dev` | buy tickets |
| Box office (admin) | `admin@seatline.dev` | manage shows, see sales |
| Door staff (validator) | `validator@seatline.dev` | scan tickets |

Password for all demo accounts: `Seatline2026!` (local default; set with `DEMO_PASSWORD`).

Live demo: *coming with the first deployment.*

## Run it locally

Requirements: Docker with Compose v2.

```bash
git clone https://github.com/Portu142/seatline.git
cd seatline
docker compose up --build
```

| What | URL |
|---|---|
| Web app | http://localhost:4173 |
| API docs (Swagger UI) | http://localhost:8080/swagger |
| Health | http://localhost:8080/health |
| Mailpit (test inbox) | http://localhost:8025 |

The API applies EF Core migrations and seeds the demo accounts on startup. Copy `.env.example` to `.env` to
change ports, passwords or the JWT key.

### Developing without containers

```bash
docker compose up -d db                              # PostgreSQL on localhost:5433
(cd api && dotnet run --project src/Seatline.Api)    # API on http://localhost:8080, in one terminal
(cd web && npm install && npm run dev)               # web on http://localhost:5173, in another
```

Run the tests (they start their own PostgreSQL with Testcontainers, so Docker must be running):

```bash
cd api && dotnet test
```

## Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core (.NET 10), EF Core, OpenAPI + Swagger UI |
| Database | PostgreSQL 17 |
| Auth | ASP.NET Core Identity, JWT access tokens + rotating refresh tokens, roles `customer` / `admin` / `validator` |
| Web | React 19 + TypeScript, Vite, React Router, TanStack Query, Tailwind CSS, React Hook Form + Zod |
| Tests | xUnit v3, `WebApplicationFactory`, Testcontainers (real PostgreSQL) |
| Delivery | Docker (chiseled .NET image), Docker Compose, GitHub Actions, Dependabot |
| Coming next | SignalR (live seat map), Stripe Checkout (test mode) + webhooks, HMAC-signed QR tickets, Playwright E2E |

## How auth works

- `POST /api/auth/register` creates a **customer**, `POST /api/auth/login` returns a 15-minute access token (JWT) and
  a 7-day refresh token.
- Refresh tokens are stored **hashed** (SHA-256) and are **single-use**: `POST /api/auth/refresh` rotates them.
  Re-using an already rotated token is treated as theft and revokes the user's whole token family.
- Login, register and refresh are rate-limited per IP, and 5 wrong passwords lock the account for 5 minutes.
- Role checks are policies: `/api/admin/*` needs `admin`; `/api/door/*` accepts `validator` or `admin`.

## Repository layout

```
api/                 ASP.NET Core API (src/) and integration tests (tests/)
web/                 React + TypeScript single-page app
docs/adr/            Architecture decision records
docs/deployment.md   How to deploy (Render + Neon + Vercel, Railway, Fly.io)
docker-compose.yml   Local stack: API + PostgreSQL + web + Mailpit
```

## Decisions

- [ADR 0001: ASP.NET Core (.NET 10) and PostgreSQL](docs/adr/0001-dotnet-and-postgresql.md)

## Roadmap

- [x] **1. Foundation**: monorepo, Docker Compose, CI, accounts with roles
- [ ] **2. Core**: movies, hall, shows, seat holds + the 50-buyers concurrency test
- [ ] **3. Checkout**: programme, real-time seat map (SignalR), Stripe test payments with idempotent webhooks
- [ ] **4. Tickets**: signed QR codes, "My tickets", email and PDF
- [ ] **5. Door & box office**: mobile ticket scanning, occupancy dashboard
- [ ] **6. Polish**: accessibility, Lighthouse ≥ 90, demo GIF

## License

[MIT](LICENSE)
