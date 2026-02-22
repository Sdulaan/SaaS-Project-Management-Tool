# SaaS Project Management Tool

Enterprise-oriented multi-tenant project management platform with:
- ASP.NET Core Web API backend (Clean Architecture style)
- React + Vite frontend dashboard

## Implemented Backend Modules

- JWT authentication (`/api/auth/register`, `/api/auth/login`)
- Multi-tenant isolation by `organizationId` claim
- Project management (`/api/projects`)
- Work item management and status flow (`/api/work-items`)
- Dashboard analytics summary (`/api/dashboard/summary`)
- Centralized exception handling middleware
- EF Core SQL Server persistence with startup schema bootstrap

## Implemented Frontend

- Modern auth page (login/register workspace)
- Dashboard with KPI cards
- Project and task creation panels
- Kanban-style status columns with move action
- API integration for auth/projects/work items/dashboard

## Tech Layout

- `Backend/` .NET 10 solution with projects:
  - `SaaS.ProjectManagement.Domain`
  - `SaaS.ProjectManagement.Application`
  - `SaaS.ProjectManagement.Infrastructure`
  - `SaaS.ProjectManagement.API`
- `Frontend/client/` React + TypeScript + Vite app

## Run Backend

1. `dotnet build Backend/SaaS.ProjectManagement.slnx -nologo`
2. `dotnet run --project "Backend/src/SaaS.ProjectManagement.API/SaaS.ProjectManagement.API.csproj"`

Default API URL from launch profile is typically `https://localhost:7068`.

## Run Frontend

1. `cd Frontend/client`
2. `npm.cmd install`
3. `npm.cmd run dev`

Set API base URL using env file:
- `Frontend/client/.env` with `VITE_API_URL=https://localhost:7068`

## Notes

- In development startup, API recreates the DB schema (`EnsureDeleted` + `EnsureCreated`) for a clean local bootstrap.
- Update `Backend/src/SaaS.ProjectManagement.API/appsettings.json` JWT key and SQL connection for production.
