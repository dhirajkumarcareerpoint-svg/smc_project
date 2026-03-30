# SMC Street Light Installation Request System

SMC Street Light is a full-stack application for submitting, reviewing, and tracking street light installation requests. The solution contains an ASP.NET Core 8 backend, an Angular 17 frontend, and a SQL Server database script for initial setup.

## Tech stack

- Backend: ASP.NET Core 8 Web API + MVC + Entity Framework Core
- Frontend: Angular 17 standalone application
- Database: SQL Server

## Repository structure

- `backend/SmcStreetlight.Api` - ASP.NET Core backend
- `frontend` - Angular frontend
- `database/schema.sql` - SQL schema and seed data
- `Smc_streetlight.sln` - Visual Studio solution file

## Prerequisites

Before starting, install the following:

- .NET 8 SDK
- Node.js 20 or later
- SQL Server
- SQL Server Management Studio or `sqlcmd` for running the schema script
- Git

## Initial setup

### 1. Configure the database

Create the database and seed core tables by running:

```sql
database/schema.sql
```

The default database name used by the project is `SmcStreetlightDb`.

### 2. Configure the backend

Review and update:

- `backend/SmcStreetlight.Api/appsettings.json`

Important values:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Key`
- `Auth` demo password settings

The current sample configuration points to:

- SQL Server: `localhost`
- Database: `SmcStreetlightDb`
- SQL login: `sa`

Replace the JWT key with a strong secret before shared or production use.

### 3. Restore backend dependencies

From the repository root:

```powershell
cd backend/SmcStreetlight.Api
dotnet restore
```

### 4. Install frontend dependencies

From the repository root:

```powershell
cd frontend
npm install
```

## Run the application

### Start the backend

```powershell
cd backend/SmcStreetlight.Api
dotnet run
```

Default local URLs commonly include:

- `http://localhost:5072`
- Swagger UI at `/swagger`
- Health endpoint at `/health`

### Start the frontend

Open a second terminal:

```powershell
cd frontend
npm start
```

The Angular dev server runs on:

- `http://localhost:4200`

## Build commands

### Backend build

```powershell
cd backend/SmcStreetlight.Api
dotnet build
```

### Frontend build

```powershell
cd frontend
npm run build
```

## Demo internal users

These sample mobile numbers are seeded for local testing:

- JE: `9000000001`
- AE: `9000000002`
- Deputy Engineer: `9000000003`
- Executive Engineer: `9000000004`
- Admin: `9000000005`

For local demo flows, the OTP is returned by the API login response.

## Git workflow

Use a simple two-branch strategy:

- `main` - production-ready, stable code only
- `dev` - active integration branch for ongoing work

Recommended workflow:

1. Create feature branches from `dev`
2. Open pull requests into `dev`
3. Test and validate in `dev`
4. Merge `dev` into `main` for releases

Suggested branch naming:

- `feature/<short-description>`
- `bugfix/<short-description>`
- `hotfix/<short-description>`

## Team access

Repository member access must be configured in the remote hosting platform, such as GitHub, GitLab, or Azure DevOps. Recommended access model:

- Admins/Maintainers: repository settings and branch protection
- Developers: push to feature branches and create pull requests
- Reviewers/QA: read access plus PR review permissions

## Recommended next repository steps

After Git is available and a remote repository is created:

1. Initialize git in the project root
2. Commit the current codebase with `.gitignore` in place
3. Create `main` and `dev` branches
4. Push both branches to the remote
5. Add team members in the hosting platform
6. Protect `main` and optionally `dev` with pull request rules
