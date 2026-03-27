# Core Family

### A Family Reformation to the World

> Global Family & Marriage Counseling Platform — strengthening marriages, empowering parents, reducing divorce rates worldwide.

---

## Architecture

| Layer               | Technology                     | Responsibility                                                                    |
| ------------------- | ------------------------------ | --------------------------------------------------------------------------------- |
| **Frontend**        | Angular 19 (TypeScript)        | Public website, client dashboard, counselor discovery, lessons, booking, payments |
| **Admin Dashboard** | Blazor Web App (.NET 10)       | Moderation, counselor verification, analytics, payout review, support tools       |
| **Backend API**     | ASP.NET Core 10 Web API        | Auth, users, content, sessions, payments, reviews, notifications                  |
| **Database**        | PostgreSQL 16                  | Shared primary data store                                                         |
| **Cache**           | Redis 7                        | Session cache, rate limiting, background job queue                                |
| **Storage**         | Azure Blob Storage             | Videos, documents, certificates, avatars                                          |
| **CDN**             | Azure CDN                      | HLS video delivery, static assets                                                 |
| **Payments**        | Stripe + Paystack + Google Pay | Global + Africa payment processing                                                |

---

## Repository Structure

```
Core-Family-v1/
├── backend/
│   └── CoreFamily.API/          ← ASP.NET Core Web API
├── frontend/                    ← Angular 19 Web App
├── admin-dashboard/
│   └── CoreFamily.Admin/        ← Blazor Admin Dashboard
├── database/
│   ├── migrations/              ← EF Core migration SQL scripts
│   └── init/                   ← Docker init scripts
├── devops/
│   ├── github-actions/          ← CI/CD workflows
│   └── azure/                  ← Bicep/ARM templates
├── docs/
│   ├── architecture.md
│   ├── api-reference.md
│   └── db-schema.md
├── docker-compose.yml           ← Local dev environment
├── CoreFamily.sln               ← .NET Solution file
└── .gitignore
```

---

## Local Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20 LTS](https://nodejs.org/) + Angular CLI (`npm install -g @angular/cli`)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [PostgreSQL client](https://www.pgadmin.org/) (optional, for DB inspection)

### 1. Start infrastructure (PostgreSQL + Redis)

```bash
docker-compose up postgres redis -d
```

### 2. Run the API

```bash
cd backend/CoreFamily.API
dotnet run
# API available at: http://localhost:5000
# Swagger UI: http://localhost:5000/swagger
```

### 3. Run the Angular frontend

```bash
cd frontend
npm install
ng serve
# App available at: http://localhost:4200
```

### 4. Run the Blazor Admin

```bash
cd admin-dashboard/CoreFamily.Admin
dotnet run
# Admin available at: http://localhost:5001
```

---

## Environment Variables

Copy `.env.example` to `.env` and fill in values. **Never commit `.env` files.**

See `/docs/environment-setup.md` for full configuration guide.

---

## Deployment

| Environment | URL                      | Branch      |
| ----------- | ------------------------ | ----------- |
| Development | Local Docker             | `feature/*` |
| Staging     | `staging.corefamily.com` | `develop`   |
| Production  | `corefamily.com`         | `main`      |

Deployments are handled via GitHub Actions CI/CD pipelines in `/devops/github-actions/`.

---

## Mission

> Strengthen marriages, empower parents, and build stable families worldwide through structured education, verified counseling, and principle-based relationship development.

**Target Impact**: Reduce divorce rates in participating communities by 60%.

---

## License

Proprietary — © Core Family. All rights reserved.
