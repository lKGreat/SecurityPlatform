# Atlas Security Platform - GEMINI.md

> **MANDATORY INSTRUCTION: All AI responses must be in Chinese (回复必须是中文).**

This project is a multi-tenant security support platform designed for **MLPS 2.0 (GB/T 22239-2019 / 等保2.0)** compliance. It follows a **Clean Architecture** and supports complex features like multi-tenancy, workflow/approval engines, and low-code capabilities.

## Project Overview

- **Name:** Atlas Security Platform
- **Architecture:** Clean Architecture (Domain, Application, Infrastructure, WebApi)
- **Backend:** .NET 10.0, ASP.NET Core Web API, SqlSugar ORM, SQLite (with optional encryption).
- **Frontend:** Vue 3.5 (Composition API), Vite, TypeScript, Ant Design Vue, Pinia, Vue Router.
- **Multi-tenancy:** Logical isolation using `X-Tenant-Id` header and automatic filtering in SqlSugar.
- **Key Features:**
    - **Identity & Access:** JWT authentication, MFA (TOTP), RBAC (Roles, Permissions, Menus, Departments).
    - **Security:** CSRF protection, AES encryption, Audit logging, Input validation (FluentValidation).
    - **Workflow:** Approval flows (Definitions, Tasks, Copy-to), General workflows (WorkflowCore).
    - **Low-Code:** Dynamic tables, Form definitions, AMIS-based page runtime.
    - **System Management:** Scheduled jobs (Hangfire), Monitor (CPU/Memory/Health), Dictionary/Config management.

## Core Development Principles (Mandatory)

1. **Iterative Requirement Alignment:** Requirements must be discussed and refined throughout the development lifecycle. Never assume specifications are final without regular clarification.
2. **Implementation-Validation Loop:** A feature is only complete when its implementation, unit testing, and validation are in a closed loop. No code should exist without a verification path.
3. **Zero-Tolerance for E2E Failures:** All issues identified during E2E testing **must be fixed**. Failing E2E tests are blockers, not warnings.
4. **Empirical Verification:** Before applying a fix, reproduce the failure with a test case (Unit or E2E). A fix is only confirmed when the test passes consistently.

## Project Structure

```text
src/
├── backend/
│   ├── Atlas.Core/                 # Base abstractions, models, and shared utilities.
│   ├── Atlas.Domain/               # Core domain logic and shared entities.
│   ├── Atlas.Domain.{Module}/      # Module-specific entities (e.g., Alert, Approval).
│   ├── Atlas.Application/          # Application interfaces, DTOs, and base services.
│   ├── Atlas.Application.{Module}/ # Module-specific DTOs, Validation, and Services.
│   ├── Atlas.Infrastructure/       # Service implementations, Repositories, SqlSugar context.
│   └── Atlas.WebApi/               # Controllers, Middlewares, and API host.
└── frontend/
    └── Atlas.WebApp/               # Vue 3 Vite application.
        ├── src/                    # Main source code (layouts, pages, router, services).
        ├── vite.config.ts          # Vite configuration.
        └── package.json            # Frontend dependencies and scripts.
```

## Building and Running

### Backend

Restore and run the WebApi project:

```powershell
# Restore dependencies
dotnet restore Atlas.SecurityPlatform.slnx

# Run the WebApi
dotnet run --project src/backend/Atlas.WebApi
```

**Note:** Ensure you configure the **Bootstrap Admin** environment variables before running, as mentioned in `README.md`.

### Frontend

Navigate to the frontend directory and start the dev server:

```bash
cd src/frontend/Atlas.WebApp
npm install
npm run dev
```

The frontend defaults to `http://localhost:5173` and proxies `/api/*` to `http://localhost:5000`.

### Automated Testing & Environment Setup

#### 1. Environment Preparation
Before running the backend or tests, you must configure the Bootstrap Admin account via environment variables. This ensures a clean and authorized state for automated suites.

**PowerShell (Recommended for Windows):**
```powershell
$env:Security__BootstrapAdmin__Enabled="true"
$env:Security__BootstrapAdmin__TenantId="00000000-0000-0000-0000-000000000001"
$env:Security__BootstrapAdmin__Username="admin"
$env:Security__BootstrapAdmin__Password="P@ssw0rd!"
$env:Security__BootstrapAdmin__Roles="Admin"
```

#### 2. Process Management
To ensure a clean testing environment, stop any existing instances of the WebApi or frontend dev server:

- **Stop Backend:** `Get-Process Atlas.WebApi -ErrorAction SilentlyContinue | Stop-Process`
- **Stop Frontend:** `Get-Process node -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*vite*" } | Stop-Process`

#### 3. Execution Commands

- **Backend Unit/Integration Tests:**
    ```powershell
    dotnet test --configuration Release --no-restore
    ```
- **Frontend Unit Tests (Vitest):**
    ```bash
    cd src/frontend/Atlas.WebApp
    npm run test -- --run # Run once and exit
    ```
- **E2E Tests (Playwright):**
    ```bash
    # Ensure backend is running at http://localhost:5000 first
    cd src/frontend/Atlas.WebApp
    npm run e2e -- --project=chromium # Run specific browser project
    ```

## Product Development & E2E Debugging Workflow

### 1. The Synchronous Loop
Since the frontend uses generated API clients, follow this sequence when changing backend APIs:
1. **Backend:** Modify Controller/DTO -> `dotnet build` -> Ensure backend is running.
2. **Frontend Sync:** `cd src/frontend/Atlas.WebApp && npm run generate-api` to update Typescript types and services.
3. **Frontend Implementation:** Use the updated `AtlasApiService` in your Vue components.

### 2. E2E Fix & Debug Cycle (Playwright)
When an E2E test fails, use the following tools to diagnose:
- **UI Mode:** `npm run e2e -- --ui` (Allows stepping through tests and inspecting the DOM).
- **Trace Viewer:** If a test fails in CI, download the trace and open it with `npx playwright show-trace path/to/trace.zip`.
- **Headless Debugging:** Use `PWDEBUG=1 npm run e2e` to open the Playwright Inspector.

### 3. Database & State Management for Tests
- **Reset State:** The platform uses SQLite. For a clean test run, you can delete the `.db` files (usually in the `WebApi` bin or project root) and restart the app to let `BootstrapAdmin` re-seed.
- **Environment Consistency:** Always ensure `X-Tenant-Id` in your test headers matches the tenant ID configured in your `BootstrapAdmin` environment variables.

### 4. Task-Based Development (Recommended)
1. **Research:** Check `docs/架构与产品能力总览.md` and `docs/contracts.md` for existing patterns.
2. **Implement:** Start with the Backend (Domain -> Application -> Infrastructure -> WebApi).
3. **Validate:** Run `dotnet test` before moving to the frontend.
4. **E2E:** Add/update a Playwright test in `tests/e2e` to verify the feature end-to-end.

## Development Conventions

### Backend
- **Layered Dependencies:** Dependency flows inward: WebApi -> Infrastructure -> Application -> Domain -> Core.
- **Naming:** Follow `Atlas.{Layer}.{Module}` naming convention.
- **Query/Command Separation:** Use `I{Context}QueryService` for read operations and `I{Context}CommandService` for write operations.
- **Validation:** Use **FluentValidation** for all incoming DTOs in the Application layer.
- **Mapping:** Use **AutoMapper** for DTO <-> Entity conversions.
- **Response Handling:** Wrap all API responses in `ApiResponse<T>` or `PagedResult<T>`.
- **Error Handling:** Use `BusinessException` for expected domain errors; they are handled by `ExceptionHandlingMiddleware`.

### Frontend
- **Composition API:** Use Vue 3 `<script setup>` with TypeScript.
- **UI Framework:** **Ant Design Vue** is the primary UI library.
- **AMIS:** Used for low-code and dynamic components.
- **API Clients:** Generated via NSwag. Run `npm run generate-api` to sync with backend changes.

## Key Documentation

- `README.md`: Quick start and deployment.
- `docs/架构与产品能力总览.md`: Comprehensive architectural and feature overview.
- `docs/contracts.md`: API request/response contracts and standard headers.
- `docs/审批流功能说明.md`: Detailed logic for the approval engine.
- `等保2.0要求清单.md`: Compliance requirements mapping.
- `CLAUDE.md`: Technical stack and coding guidelines for AI assistants.
