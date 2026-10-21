# Control de Dinero

**A personal loan ledger built for a real-world use case.**

Control de Dinero is a mobile-first application designed to help one person replace a handwritten lending notebook with a clear, searchable record of clients, loans, installments, payments, and interest earnings.

> **Project status:** Planning and initial backend setup. The features below are the agreed scope, not completed functionality.

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

The initial interest rate is **15% per loan**, editable when a loan is created. It is a **one-time fixed rate**, not a recurring monthly charge. Early repayment does **not** reduce the agreed interest.

Example: principal **₡50,000** + fixed interest **₡7,500** = **₡57,500** to repay.

Partial payments are allocated proportionally between principal and interest, using decimal arithmetic and a final-payment rounding adjustment. Therefore, money received and interest earnings are not treated as the same figure.

> The 15% rate is a configurable project assumption, **not** a statement of legal compliance. Applicable lending and personal-data rules must be checked before using the app with real records.

## Tech stack

| Layer | Technology |
| --- | --- |
| Frontend | React, TypeScript, Tailwind CSS |
| Backend | ASP.NET Core Web API (.NET 10, C#) |
| Data access | Entity Framework Core |
| Database | PostgreSQL |
| Testing | xUnit / integration tests (planned) |
| Delivery | HTTPS web deployment and mobile PWA |
| Version control | Git and GitHub |

## Architecture

```text
Mobile browser / installed PWA
          |
          | HTTPS / JSON
          v
React + TypeScript frontend
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
PostgreSQL
```

**MVP data model:** `app_users`, `clients`, `loans`, `installments`, `payments`, `payment_allocations`, and `audit_events`. ASP.NET Core Identity may add its own supporting authentication tables.

## Intended repository layout

```text
loan-management-system/
├── backend/
│   ├── ControlDeDinero.Api/
│   ├── ControlDeDinero.Application/
│   ├── ControlDeDinero.Domain/
│   ├── ControlDeDinero.Infrastructure/
│   └── ControlDeDinero.Tests/
├── frontend/
├── docs/
│   ├── requirements/
│   ├── diagrams/
│   └── screenshots/
└── README.md
```

The structure above is the **target organization** and will be built out incrementally.

## Development roadmap

- [x] Identify the user's real workflow and business rules.
- [x] Approve the initial UI concept and architecture.
- [x] Prepare functional and technical specification v3.0.
- [ ] Set up the solution, layers, dependency injection, and database connection.
- [ ] Implement the client directory and duplicate-warning search.
- [ ] Implement loan creation and scheduled installments.
- [ ] Implement atomic payment registration and allocation.
- [ ] Add financial dashboard and monthly history filters.
- [ ] Add authentication, logging, backup procedure, and automated tests.
- [ ] Build and deploy the PWA; validate with the real user.

## Key engineering decisions

- `decimal` in C# and `numeric(18,2)` for monetary amounts in PostgreSQL.
- Every loan retains its agreed rate and amounts; changing the default rate never changes history.
- Scheduled installments are **not** proof of payment; only recorded payments affect collected money.
- Payments are not silently edited or deleted; corrections are traceable.
- Transactional posting and idempotency keys help prevent inconsistent or duplicate repayments.
- Client names are **not** globally unique: two different people may share a name. Potential duplicates are identified using additional information and confirmation.

## Documentation

The functional and technical specification (Spanish) covers requirements, business rules, flows, the entity-relationship model, API design, and acceptance scenarios. Add the approved Word document to `docs/requirements/` when publishing the repository.

## Privacy and demo data

This repository must contain **fictional clients and financial records only**. Never commit connection strings with passwords, real customer details, production database exports, or secret keys. Use environment variables or a local secrets manager for development settings.

## About the project

This is both a practical tool for a family member and a portfolio project focused on full-stack engineering, relational data modeling, financial correctness, and thoughtful UX. The user-facing interface will be in Spanish; source code and database identifiers will be in English.

## License

License to be decided before public distribution.
