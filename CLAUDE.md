# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build              # Build the project
dotnet run                # Run the dev server (HTTP: http://localhost:5194, Swagger UI at /swagger)
dotnet clean              # Clean build artifacts
```

There are currently no test projects. The file `FlightApp/FlightKS.http` contains sample API requests usable with the VS Code REST Client extension.

## Architecture

This is an early-stage **ASP.NET Core 10.0 Web API** project using the minimal hosting model (no `Startup.cs`). The entire app bootstrap lives in `FlightApp/Program.cs`.

- **Framework:** .NET 10.0, C# 14, nullable reference types enabled
- **API docs:** Swagger UI served at `/swagger` in development via Swashbuckle (6.6.2)
- **Solution file:** `FlightKS.sln` at the repo root; the single project is under `FlightApp/`

CRUD endpoints for Flights and Users are exposed under `/api/flights` and `/api/users` via minimal-API endpoint groups (`FlightApp/Endpoints/`). Services live in `FlightApp/Services/` and are wired in `Program.cs`.
