# Core Family Progress Report

Date: 2026-03-27

## 1. Foundation and Architecture

- Set up the monorepo structure for API, Angular app, and Blazor admin.
- Confirmed architecture split:
  - Angular public app
  - ASP.NET Core API
  - Blazor admin dashboard
  - Shared PostgreSQL + Redis
- Added local orchestration with Docker Compose.

## 2. Backend (ASP.NET Core)

- Implemented core domain entities and enums for users, content, programs, counseling, sessions, reviews, transactions, and certificates.
- Added EF Core DbContext and mappings.
- Built auth stack:
  - Registration, login, refresh, logout
  - Forgot/reset password
  - Email verification flow
  - JWT access/refresh token handling
  - PBKDF2-SHA512 password hashing
- Added controllers and middleware:
  - Auth endpoints
  - User profile endpoints
  - Global exception handling middleware
- Wired services, auth, CORS, Swagger, and validation in Program setup.

## 3. Database

- Generated initial EF migration: `InitialCreate`.
- Applied schema successfully to PostgreSQL (including Railway setup validation).

## 4. Frontend (Angular)

- Replaced Angular starter with a real app structure:
  - Public app shell
  - Auth shell
  - Home page
  - Protected dashboard route
- Implemented auth screens:
  - Login
  - Register
- Added frontend auth infrastructure:
  - Auth service
  - Token storage
  - HTTP auth interceptor
  - Route guards
- Configured routes and HTTP providers.
- Added responsive branded styling.

## 5. Local Environment and Startup Automation

- Added local env support in `.env.local`.
- Added DB URL variable (`DATABASE_URL`) to local env.
- Added startup automation scripts:
  - `scripts/common-local-env.ps1`
  - `scripts/start-api-local.ps1`
  - `scripts/start-frontend-local.ps1`
- Updated `.gitignore` to ignore local env variants.
- Updated README with local startup instructions.

## 6. Verification Completed

- Backend starts and serves Swagger on localhost.
- Frontend starts on localhost.
- Angular production build succeeds.
- API and frontend endpoint checks returned HTTP 200.

## 7. Current Status

- Phase 1 foundation is operational end-to-end:
  - Auth
  - Database schema
  - Angular auth UX
  - Local env workflow
- Program enrollment foundation is now operational:
  - Published programs API (`GET /api/v1/programs`)
  - Program details API (`GET /api/v1/programs/{id}`)
  - Enrollment API (`POST /api/v1/programs/{id}/enroll`)
  - My enrollments API (`GET /api/v1/programs/me/enrollments`)
  - Angular Programs page (`/programs`)
  - Angular My Learning page (`/my-learning`)
- Payment and monetization foundation is now operational:
  - Payment checkout APIs for program and session payments
    - `POST /api/v1/payments/checkout/program/{programId}`
    - `POST /api/v1/payments/checkout/session/{sessionId}`
  - Transactions API for signed-in users
    - `GET /api/v1/payments/me`
  - Webhook ingestion endpoint
    - `POST /api/v1/payments/webhooks/{provider}`
  - Program enrollment now enforces payment completion for paid programs
  - Angular program enrollment now routes paid enrollments through checkout flow first
- Instructor publishing workflow foundation is now operational:
  - Instructor-only API set for program management:
    - List my programs
    - Create draft program
    - Update program
    - Publish program
  - Instructor-only API set for lesson management:
    - Add lesson with linked content payload
    - Update lesson and linked content
  - Angular Instructor Studio page at `/instructor/programs` with:
    - Draft program creation
    - Program publish action
    - Instructor-only navigation visibility
- Lesson player and progress tracking foundation is now operational:
  - Learning program overview API
    - `GET /api/v1/programs/{programId}/learn`
  - Lesson player API
    - `GET /api/v1/programs/{programId}/lessons/{lessonId}`
  - Lesson progress update API
    - `POST /api/v1/programs/{programId}/lessons/{lessonId}/progress`
  - Enrollment completion is automatically set when all lessons are completed
  - Angular learning pages:
    - Program learning overview page
    - Lesson player page
    - Continue-learning links from My Learning
- Counselor booking lifecycle hardening is now operational:
  - Session lifecycle API additions:
    - Get session by ID
    - Confirm session
    - Cancel session
    - Reschedule session
  - Counselor conflict checks now support booking and rescheduling safely
  - Session summaries now include payment state
  - Session payment completion now auto-confirms the session when appropriate
  - Angular Sessions page at `/sessions` with:
    - Payment action for unpaid sessions
    - Cancel action
    - Reschedule form
    - Session and payment status visibility
- Admin moderation workflow foundation is now operational:
  - Backend admin API endpoints for:
    - List users
    - Suspend/reactivate users
    - List flagged reviews
    - Clear or restore review flags
  - Blazor admin dashboard pages for:
    - User moderation
    - Flagged review moderation
  - Admin dashboard now supports pasting an admin/moderator JWT to call protected backend moderation APIs
- Refund and admin finance workflow foundation is now operational:
  - User refund request API for signed-in users
    - `POST /api/v1/payments/{transactionId}/refund-request`
  - Backend admin finance API endpoints for:
    - List transactions
    - Refund a transaction
  - Refunding a counseling transaction now cancels the linked session when it is still pending or confirmed
  - Blazor admin dashboard finance page at `/finance` with:
    - Transaction list loading
    - Refund action submission with reason capture
- User billing and refund request UI is now operational in Angular:
  - Auth-protected billing route at `/billing`
  - Transaction history list for signed-in users
  - Per-transaction refund/dispute submission for completed payments
  - Refund-request state visibility when a request is already submitted
- AI-assisted counselor matching user flow is now operational:
  - Backend personalized match endpoint:
    - `POST /api/v1/counselors/match`
  - Matching score combines profile signals such as:
    - verification status
    - accepts-new-clients
    - language and country fit
    - specialization alignment to challenge text
    - rating quality and budget fit
  - Angular auth-protected "I Need Help" page at `/help` with:
    - challenge and preference capture
    - ranked counselor recommendations
    - direct booking handoff into sessions workflow
- Remaining major work:
  - Real provider SDK/API wiring for Stripe, Paystack, and Google Pay checkout + webhook signature standards
  - Expanded integration and E2E testing
