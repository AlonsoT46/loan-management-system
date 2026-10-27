# Control de Dinero

**A personal loan ledger built for a real-world use case.**

Control de Dinero is meant to help one person replace a handwritten lending notebook with a clear, searchable record of clients, loans, installments, payments, and interest earnings.

> **Project status:** backend in progress. The domain model, database, unit tests and the API for **clients, loans and payments** exist (tested manually with Postman). There is **no frontend and no authentication yet**: the features listed below are the agreed scope, not completed functionality.

## Why this project?

Keeping loan records in a notebook makes it difficult to find older transactions, track partial repayments, and separate returned principal from earned interest. This project tackles those problems with a simple interface for the end user and a carefully designed financial data model behind the scenes.

## Planned MVP features

- **Client directory:** create and search clients; select an existing client instead of adding duplicates.
- **Loan records:** register multiple loans per client, each with its own date, principal, fixed interest rate, and repayment plan.
- **Flexible repayments:** record full or partial payments on any date, even when they differ from scheduled installments.
- **Loan and client history:** browse transactions and filter past loans and payments by month.
- **Financial overview:** view disbursed principal, recovered principal, collected interest, and outstanding balances separately.
- **Single-user access:** authenticated access for the owner; no multi-tenant or employee management in the MVP.
- **Mobile-first PWA:** an installable web experience designed for an iPhone.

## Interest and payment rules (MVP)

Interest is **20% of the principal for every quincena** (a 15-day period) between the loan date and the due date. It is simple interest: it does not compound. The number of quincenas is rounded up, with a minimum of 1.

Examples: ₡10,000 for 1 quincena = ₡12,000; ₡50,000 for 1 quincena = ₡60,000; ₡50,000 borrowed on the 1st and repaid on the 30th (2 quincenas) = **₡70,000**.

The rate is editable per loan when it is created. Confirmed with the lender: passing the due date does **not** add more quincenas, but the loan is flagged as **overdue** (derived from the due date and the remaining balance, never stored). Also confirmed: paying early does **not** reduce the agreed interest.

Partial payments are allocated proportionally between principal and interest, using decimal arithmetic; the final payment absorbs any rounding difference. Therefore, money received and interest earnings are not treated as the same figure. These rules live in the `Loan` entity (`Loan.Create`, `Loan.RegisterPayment`) and are covered by unit tests.

> The rate is a configurable project assumption, **not** a statement of legal compliance. Applicable lending and personal-data rules must be checked before using the app with real records.

## Tech stack

| Layer | Technology | Status |
| --- | --- | --- |
| Backend | ASP.NET Core Web API (.NET 10, C#) | Clients, loans and payments endpoints |
| Data access | Entity Framework Core 10 (Code First) | Implemented |
| Database | MySQL 8.4 (`MySql.EntityFrameworkCore`) | Implemented |
| Testing | xUnit | Domain and service tests implemented; integration tests planned |
| Frontend | React, TypeScript, Tailwind CSS | Planned |
| Delivery | HTTPS web deployment and mobile PWA | Planned |
| Version control | Git and GitHub | In use |

## Architecture

```text
Mobile browser / installed PWA                (planned)
          |
          | HTTPS / JSON
          v
React + TypeScript frontend                   (planned)
          |
          | authenticated API requests
          v
ASP.NET Core Web API (C#)
  - clients, loans, payments
  - validation and financial rules
  - transactional payment posting
          |
          | Entity Framework Core
          v
MySQL
```

Backend layers (dependencies point inwards):

- `Domain`: entities and business rules. Depends on nothing.
- `Application`: use cases (`ClientService`, `LoanService`), DTOs, request models and repository interfaces. Depends on `Domain`.
- `Infrastructure`: EF Core, `AppDbContext`, migrations. Depends on `Domain` and `Application`.
- `Api`: HTTP entry point and dependency injection. Depends on `Application` and `Infrastructure`.

**Current data model:** `clients`, `loans`, `installments`, `payments`, `payment_allocations`, and `audit_events`. A user table (`app_users`, possibly ASP.NET Core Identity) is planned for authentication.

## Repository layout

```text
loan-management-system/
├── backend/
│   ├── ControlDeDinero.slnx
│   ├── ControlDeDinero.Api/
│   ├── ControlDeDinero.Application/
│   ├── ControlDeDinero.Domain/
│   ├── ControlDeDinero.Infrastructure/
│   └── ControlDeDinero.Tests/
├── frontend/            (not created yet)
├── docs/
│   ├── requirements/    (specification will go here)
│   ├── instalacion-desde-github.md
│   └── configuracion-entity-framework.md
└── README.md
```

## API (current)

| Method and path | What it does |
| --- | --- |
| `POST /api/clients` | Create a client (returns possible duplicates by name) |
| `GET /api/clients?search=&includeInactive=` | List / search clients |
| `GET /api/clients/{id}` · `PUT /api/clients/{id}` | Read / edit a client |
| `POST /api/clients/{id}/deactivate` · `/activate` | Hide / restore a client (no hard delete) |
| `POST /api/loans` | Create a loan (interest and quincenas are calculated) |
| `GET /api/loans?clientId=&status=&onlyOverdue=` | List loans; `onlyOverdue=true` shows who is late |
| `GET /api/loans/{id}` | Loan with its payments |
| `POST /api/loans/{id}/payments` | Register a full or partial payment |

"Today" for overdue checks is the Costa Rica date, regardless of the server's time zone.

## Getting started

Requirements: .NET SDK 10 and MySQL 8.4. Step-by-step instructions (Spanish) are in [docs/instalacion-desde-github.md](docs/instalacion-desde-github.md).

```powershell
cd backend
dotnet build
dotnet test
```

## Development roadmap

- [x] Identify the user's real workflow and business rules.
- [x] Approve the initial UI concept and architecture.
- [x] Prepare functional and technical specification v3.0.
- [x] Set up the solution, layers, database connection and initial migration.
- [x] Model loans, interest and payment allocation in the domain, with unit tests.
- [x] Wire dependency injection per layer and expose the first endpoints.
- [x] Implement the client directory and duplicate-warning search.
- [x] Implement loan creation (quincena interest) and payment registration through the API.
- [ ] Implement scheduled installments (only if the lender needs a payment calendar).
- [ ] Harden payment registration (idempotency keys, traceable corrections).
- [ ] Add financial dashboard and monthly history filters.
- [ ] Add authentication, logging, backup procedure, and integration tests.
- [ ] Build and deploy the PWA; validate with the real user.

## Key engineering decisions

- `decimal` in C# and `decimal(18,2)` for monetary amounts in MySQL.
- Every loan retains its agreed rate, number of quincenas and amounts; changing the default rate never changes history.
- Derived loan amounts (interest, total, paid, remaining) can only change through domain methods, so they cannot drift from the recorded payments.
- Scheduled installments are **not** proof of payment; only recorded payments affect collected money.
- Payments are not silently edited or deleted; corrections are traceable through `audit_events`.
- Foreign keys use `DeleteBehavior.Restrict`: financial history is never removed by cascade.
- Dates are stored in UTC.
- Statuses are enums stored as readable strings.
- Client names are **not** globally unique: two different people may share a name. Potential duplicates are identified using additional information and confirmation.
- A payment and the loan balance it changes are saved in a single transaction. Idempotency keys against duplicate submissions are planned.

## Documentation

The functional and technical specification (Spanish) covers requirements, business rules, flows, the entity-relationship model, API design, and acceptance scenarios.

Detailed technical documentation is available in Spanish:

- [Installation Guide (ES)](docs/instalacion-desde-github.md)
- [Entity Framework Core Setup (ES)](docs/configuracion-entity-framework.md)

## Privacy and demo data

This repository must contain **fictional clients and financial records only**. Never commit connection strings with passwords, real customer details, production database exports, or secret keys. Use environment variables or a local secrets manager (`dotnet user-secrets`) for development settings.

## About the project

This is both a practical tool for a family member and a portfolio project focused on full-stack engineering, relational data modeling, financial correctness, and thoughtful UX. The user-facing interface will be in Spanish; source code and database identifiers will be in English.

## License

License to be decided before public distribution.
