# Core Family - Trello Task Plan

## Product Identity

- App Name: Core Family
- Mission: A Family Reformation to the World
- Primary Goal: Strengthen marriages, reduce stress/divorce rates, improve parenting, and provide accessible global counseling/training.
- Strategic Public Positioning: Principle-based, faith-friendly global platform.

## Recommended Trello Board Setup

Board Name: Core Family - Product Build & Launch

Lists:

1. Inbox / Ideas
2. Strategy & Compliance
3. Product Requirements
4. UX/UI & Brand
5. Architecture & DevOps
6. Frontend (Angular)
7. Backend (.NET / ASP.NET Core)
8. Data (PostgreSQL)
9. Payments & Billing
10. AI Matching & Recommendations
11. Admin Dashboard (Blazor/MVC)
12. QA, Security & Performance
13. Launch Readiness
14. Live / Post-Launch
15. Blocked

Suggested Labels:

- Priority: P0 Critical, P1 High, P2 Medium, P3 Low
- Area: Frontend, Backend, Data, DevOps, AI, Admin, Compliance, Content, Growth
- Type: Feature, Bug, Tech Debt, Security, Research, Legal

---

## Trello Cards (Ready to Add)

### Strategy & Compliance

Card: Define mission, vision, and measurable impact KPIs [P0]

- Checklist:
  - Finalize mission and vision statements for app and website
  - Confirm KPI framework: divorce reduction proxy, stress reduction, satisfaction, completion rates
  - Define baseline and quarterly target metrics
  - Create KPI ownership matrix
- Acceptance Criteria:
  - KPI document approved and versioned
  - Dashboard metric definitions signed off by product and data owners

Card: Legal and policy framework by region [P0]

- Checklist:
  - Terms of Service, Privacy Policy, Consent policy
  - Counselor/instructor liability disclaimer
  - Child safety and abuse resource policy
  - Data retention and deletion policy
  - Country-based legal review for counseling/telehealth restrictions
- Acceptance Criteria:
  - Legal documents published
  - Compliance checklist completed for phase-1 launch countries

Card: Trust & safety escalation model [P0]

- Checklist:
  - Define flagged message workflow
  - Create abuse-risk escalation runbook
  - Emergency referral message templates by country
  - Moderator role permissions and SLA
- Acceptance Criteria:
  - High-risk cases can be flagged, reviewed, escalated, and audited

---

### Product Requirements

Card: Final PRD for multi-role platform [P0]

- Checklist:
  - Define user roles: Single, Married, Parent, Family, Youth
  - Define provider roles: Instructor, Counselor/Therapist
  - Define admin roles and permissions
  - Document free vs paid capability matrix
  - Confirm web/mobile parity scope
- Acceptance Criteria:
  - PRD approved with scope boundaries for MVP and post-MVP

Card: Information architecture and navigation map [P1]

- Checklist:
  - Homepage structure and sections
  - Category pages: Married, Singles, Parenting, Conflict, Family Finance
  - Program detail, lesson, quiz, session booking, profile pages
  - Account, billing, progress dashboard, certificates
- Acceptance Criteria:
  - Site map approved and linked to implementation tickets

---

### UX/UI & Brand

Card: Brand system (logo, favicon, color palette) [P1]

- Checklist:
  - Create logo concepts and finalize one mark
  - Design favicon set for web/mobile/PWA
  - Define color palette with accessibility contrast checks
  - Define typography and component design tokens
- Acceptance Criteria:
  - Brand guide published with downloadable assets

Card: Responsive design system and reusable components [P1]

- Checklist:
  - Build component library: buttons, cards, forms, tabs, badges, alerts
  - States: loading, empty, error, success
  - Light/dark mode decision and implementation
  - WCAG AA accessibility checks
- Acceptance Criteria:
  - Design system components used across all core pages

Card: Homepage + trust pages implementation [P1]

- Checklist:
  - Mission, vision, impact statistics, testimonials
  - Team page
  - Privacy statement and safety notice
- Acceptance Criteria:
  - Public pages are complete and SEO-ready

---

### Architecture & DevOps

Card: Cloud architecture on Azure [P0]

- Checklist:
  - Define services: App Service/Container Apps, PostgreSQL, Storage, Key Vault, CDN
  - Environment strategy: Dev, Staging, Prod
  - Cost estimate and scaling policy
- Acceptance Criteria:
  - Architecture document approved and deployed for Dev

Card: CI/CD + branching + release strategy [P0]

- Checklist:
  - GitHub Actions/Azure Pipelines setup
  - Build/test/deploy pipeline for frontend and backend
  - DB migration pipeline
  - Versioning and rollback process
- Acceptance Criteria:
  - Automatic deployment to Dev and manual promotion to Staging/Prod

Card: Observability and incident operations [P1]

- Checklist:
  - Centralized logs, tracing, metrics, alerts
  - Uptime checks and synthetic monitoring
  - Incident runbook and on-call rotation
- Acceptance Criteria:
  - Alerting works and test incident is resolved via runbook

---

### Frontend (Angular)

Card: Authentication and account flows [P0]

- Checklist:
  - Register/login/logout/reset password
  - Category onboarding selection
  - Profile completion wizard
- Acceptance Criteria:
  - Auth flow stable across mobile/web responsive breakpoints

Card: Category-tailored content experience [P0]

- Checklist:
  - Married page modules
  - Singles page modules with RAM-based readiness tools
  - Parenting page modules and style assessments
  - Family finance resources page
  - Conflict resolution and abuse support page
- Acceptance Criteria:
  - Tailored feed and page access reflect selected user category

Card: Program enrollment and lesson player [P0]

- Checklist:
  - Program list and detail pages
  - Enroll flow with payment
  - Lesson viewer with video/document resources
  - Quizzes and surveys integration
- Acceptance Criteria:
  - Users can buy, enroll, and complete a lesson with saved progress

Card: Progress dashboard + certificates [P1]

- Checklist:
  - Milestone tracker
  - Quiz score history
  - Completion certificates download/share
- Acceptance Criteria:
  - Dashboard data is correct and certificate generation works

Card: Multi-language UX (phase 1) [P1]

- Checklist:
  - English, French, Spanish, Portuguese
  - i18n framework setup and locale switching
  - Translation quality review
- Acceptance Criteria:
  - All core user flows available in phase-1 languages

---

### Backend (.NET / ASP.NET Core)

Card: Identity, RBAC, and role onboarding APIs [P0]

- Checklist:
  - User account model and role assignment
  - Provider verification states
  - JWT/session security and refresh strategy
- Acceptance Criteria:
  - Secured API access by role with audit logs

Card: Instructor verification and program management APIs [P0]

- Checklist:
  - Certificate/qualification upload
  - Instructor profile, lessons, videos, duration, schedule
  - Live session hosting metadata
- Acceptance Criteria:
  - Verified instructor can publish and manage programs

Card: Counselor profile and booking APIs [P0]

- Checklist:
  - License/qualification upload
  - Session fee setup by counselor
  - Booking + availability engine
  - “I need help” intake endpoint
- Acceptance Criteria:
  - Users can book counselor and complete payment-ready workflow

Card: Monetization and commission engine [P0]

- Checklist:
  - Lesson pricing ($3-$5)
  - Program fee model (lifetime/monthly)
  - Counselor annual registration fee ($5)
  - Video upload annual fee ($5)
  - 5% session commission calculation
  - Payout ledger and settlement report
- Acceptance Criteria:
  - Finance calculations are consistent, test-covered, and auditable

Card: Notifications and communication service [P1]

- Checklist:
  - Email/SMS/push abstraction
  - Enrollment reminders
  - Session reminders and missed-session follow-up
- Acceptance Criteria:
  - Notifications trigger from key lifecycle events

---

### Data (PostgreSQL)

Card: Data model and schema design [P0]

- Checklist:
  - Core entities: users, profiles, roles, programs, lessons, sessions, payments, reviews
  - Assessment and quiz schema
  - Certificates and progress tracking
  - Audit and moderation entities
- Acceptance Criteria:
  - ERD approved and migration scripts versioned

Card: Data governance and privacy controls [P0]

- Checklist:
  - Encryption at rest and in transit
  - Sensitive field protection and masking
  - Backup/restore policy with RPO/RTO
  - Data export/deletion endpoints
- Acceptance Criteria:
  - Backup restore drill completed successfully

---

### Payments & Billing

Card: Payment gateway integration (Stripe, Paystack, Google Pay) [P0]

- Checklist:
  - Unified billing abstraction layer
  - Product types: lesson, program, counseling, provider annual fees
  - Country/currency handling
  - Payment failure recovery and retry
- Acceptance Criteria:
  - End-to-end payment success and failure flows validated

Card: Invoicing, receipts, refunds, disputes [P1]

- Checklist:
  - Receipt generation
  - Refund workflow
  - Dispute handling logs
  - Admin override with audit trail
- Acceptance Criteria:
  - Finance/admin can manage billing lifecycle reliably

---

### AI Matching & Recommendations

Card: Counselor recommendation engine MVP [P1]

- Checklist:
  - Input variables: country, language, issue type, availability
  - Ranking logic and explainability fields
  - Manual override and fallback routing
- Acceptance Criteria:
  - “I need help” returns ranked counselor recommendations

Card: Content recommendation engine MVP [P2]

- Checklist:
  - Recommend lessons/programs by user category and behavior
  - Cold-start strategy for new users
- Acceptance Criteria:
  - Recommendation endpoint available and integrated in dashboard

---

### Admin Dashboard (Blazor/MVC)

Card: Admin control center foundation [P0]

- Checklist:
  - Admin auth, RBAC, and activity logs
  - User/provider management views
  - Suspension/ban/reinstate workflow
- Acceptance Criteria:
  - Admin can moderate users and providers with full audit trail

Card: Flagged content/message review [P0]

- Checklist:
  - Queue for flagged messages and reports
  - Severity classification and action controls
  - Escalation status tracking
- Acceptance Criteria:
  - Moderators can resolve flagged items inside SLA windows

Card: Operations analytics dashboard [P1]

- Checklist:
  - Revenue, conversion, churn, completion metrics
  - Counselor utilization and response times
  - Geographic and language adoption insights
- Acceptance Criteria:
  - Leadership KPI dashboard is available with daily refresh

---

### QA, Security & Performance

Card: Test strategy and automation [P0]

- Checklist:
  - Unit, integration, and E2E test plans
  - Critical path automation (auth, payment, booking, enrollment)
  - Regression suite in CI
- Acceptance Criteria:
  - Release gate requires passing critical test suite

Card: Security hardening and abuse prevention [P0]

- Checklist:
  - OWASP baseline controls
  - Rate limiting and bot prevention
  - Secure file upload scanning (certificates/videos)
  - Secrets management via Azure Key Vault
- Acceptance Criteria:
  - Security review completed with no critical open issues

Card: Performance and scalability readiness [P1]

- Checklist:
  - Load test for concurrent sessions/lesson streaming
  - API latency budgets and optimization
  - CDN caching strategy for static/video assets
- Acceptance Criteria:
  - Meets defined performance SLOs in staging

---

### Launch Readiness

Card: Pilot launch plan (country/language rollout) [P1]

- Checklist:
  - Choose initial markets
  - Localize legal/support content
  - Define success criteria and rollback triggers
- Acceptance Criteria:
  - Pilot launch checklist complete and signed off

Card: Support model and knowledge base [P1]

- Checklist:
  - Help center articles
  - Support ticket workflow and SLA
  - Escalation process for safety-critical reports
- Acceptance Criteria:
  - Support team can resolve top 20 issues using KB + workflow

Card: App store and web release packaging [P2]

- Checklist:
  - PWA readiness for web
  - Android/iOS packaging strategy and metadata
  - Policy-compliant wording for global distribution
- Acceptance Criteria:
  - Release candidates prepared for store submission and web deploy

---

## Card Template (Use for every new Trello card)

- Title:
- Owner:
- Priority: P0/P1/P2/P3
- Labels:
- Description:
- Checklist:
- Dependencies:
- Acceptance Criteria:
- Target Sprint:
- Due Date:
- Risks/Notes:

## MVP Scope (Phase 1)

Include:

- Role-based onboarding and tailored category content
- Verified instructor/counselor workflows
- Lesson/program purchase + counseling booking + 5% commission
- Progress dashboard, quizzes, certificates
- Admin moderation and suspension tools
- English + at least one additional language in pilot

Exclude (Phase 2+):

- University/corporate/government partnership integrations
- Advanced AI predictive analytics
- Deep research database features

## Suggested First 3 Sprints

Sprint 1:

- PRD, architecture, auth, role onboarding, base schema, CI/CD

Sprint 2:

- Category pages, instructor/counselor onboarding, payments core, booking core

Sprint 3:

- Progress dashboard, certificates, admin moderation, QA hardening, pilot launch prep
