---
name: xihan-basicapp-backend
description: Use when modifying, reviewing, documenting, or debugging the XiHan.BasicApp backend under backend/, including modules, Dynamic API application services, domain services, repositories, permissions, multi-tenancy, SqlSugar entities, seeders, health checks, SignalR, or Scalar contracts. Preserve XiHan.Framework, .NET 10, DynamicApi app-service boundaries, SqlSugar repositories, and the existing DDD/CQRS module layout; do not apply dotnet-business-backend scaffolding, Minimal API endpoint rewrites, or EF Core replacement unless the user explicitly asks for a separate migration.
---

# XiHan.BasicApp Backend Development

## Start Here

Before editing backend code:

1. Read `README.md` and the nearest module README, such as `backend/src/modules/XiHan.BasicApp.Saas/README.md` or `backend/src/modules/XiHan.BasicApp.CodeGeneration/README.md`.
2. Inspect neighboring files for the same feature type: contracts, DTOs, mappers, app services, query services, domain services, repositories, permissions, seeders, and module registration.
3. Treat `dotnet-business-backend` as inspiration for small files, explicit boundaries, and verification habits only. Do not import its solution layout, template, Minimal API pattern, EF Core persistence model, Result/UseCase pipeline, or scaffold commands into this repository.

## Project Shape

The backend is a `.NET 10` XiHan.Framework application:

```text
backend/
  src/
    framework/
      XiHan.BasicApp.Core/
      XiHan.BasicApp.Web.Core/
    modules/
      XiHan.BasicApp.Saas/
      XiHan.BasicApp.CodeGeneration/
      XiHan.BasicApp.AI/
    main/
      XiHan.BasicApp.WebHost/
  test/
```

Use the existing module boundary before creating new files:

- `Domain/Entities`: SqlSugar entities, enums, value objects, specifications, permission constants, repository contracts, and domain services.
- `Application/Contracts`: app-service and query-service interfaces exposed through Dynamic API plus request/response contracts when the module uses contract files.
- `Application/Dtos`: DTO shapes consumed by Dynamic API and frontend API modules.
- `Application/Mappers`: explicit application mapping helpers.
- `Application/AppServices`: command-oriented app services with `[DynamicApi]`, permission attributes, and unit-of-work attributes.
- `Application/QueryServices`: read-oriented query services.
- `Infrastructure/Repositories`: SqlSugar repository implementations.
- `Infrastructure/Seeders`: idempotent seed data for permissions, menus, resources, roles, operations, and defaults.
- `Extensions/ServiceCollectionExtensions.cs`: module-owned service registration.
- `XiHanBasicApp*Module.cs`: XiHan module dependency and initialization wiring.

## Non-Negotiable Boundaries

- Keep `Program.cs` small and module-driven through `builder.AddApplicationAsync<XiHanBasicAppWebHostModule>()`.
- Expose ordinary HTTP APIs through `[DynamicApi]` application services and `IApplicationService` contracts. Do not replace them with Minimal API endpoint groups for normal feature work.
- Use `[PermissionAuthorize(...)]` on protected operations and keep permission codes in module-owned permission constant files.
- Use `[UnitOfWork(true)]` on write operations when neighboring command services do so.
- Keep cancellation explicit: accept `CancellationToken cancellationToken = default`, call `cancellationToken.ThrowIfCancellationRequested()` in app/domain/repository methods that follow the local pattern, and pass the token to async calls.
- Use `BasicAppEntity`, `BasicAppFullAuditedEntity`, `BasicAppAggregateRoot`, and related base classes from `XiHan.BasicApp.Core.Entities`.
- Use SqlSugar attributes such as `[SugarTable]`, `[SugarColumn]`, and `[SugarIndex]` for persisted entities. Do not introduce EF Core configuration files for this repository's normal backend.
- Preserve tenant semantics from `BasicAppEntity`: platform/global records use `TenantId = 0`; tenant records use the active tenant context. Do not use nullable tenant ids to represent global data.
- For soft-deleted entities, include `IsDeleted` in unique indexes the same way nearby `BasicAppFullAuditedEntity` models do.
- Keep repositories behind domain contracts and reuse `SaasRepository<T>` with `ISqlSugarClientResolver` when the module depends on Saas repository infrastructure.
- Keep business rules in domain services or domain models, not in controllers, endpoint lambdas, or frontend code.
- Keep DTOs, contracts, mappers, app services, query services, domain services, repositories, and seeders in focused files.
- Follow the existing copyright header style when adding C# files.

## Contribution Conventions

When adding or changing backend code:

- Follow the repository's existing C# copyright header style on new C# files, including the MIT license line that points to `LICENSE` in the project root.
- Use `backend/.editorconfig` and checked-in backend formatting configuration as the formatting source of truth.
- Visual Studio 2022 and CodeMaid can be useful local tools, but they are not required project gates.
- When committing backend-only documentation or code changes, use one of the conventional prefixes listed in `AGENTS.md`.

## Backend Change Workflow

For a new backend capability:

1. Locate the owning module. Use `Saas` for platform identity, RBAC, ABAC, tenancy, configuration, logging, workflow, files, messaging, and monitoring; use `CodeGeneration` for generator metadata/runtime; use `AI` for prompt/provider/knowledge capabilities.
2. Add or extend domain entities, enums, permission constants, repository contracts, and domain services first.
3. Add DTOs and mapper methods that keep frontend-facing shapes stable.
4. Add command methods in `Application/AppServices` and read methods in `Application/QueryServices`.
5. Add repository methods under `Infrastructure/Repositories` when data access cannot be expressed through existing repository methods.
6. Add or update seeders for permissions, menus, resources, role grants, operations, or default configuration when the feature must appear in the UI or authorization catalog.
7. Register new services in the module's `Extensions/ServiceCollectionExtensions.cs` when constructor injection requires it.
8. Check the frontend API module and route/component path if the backend contract changes.

## Verification

Use focused verification first, then broaden when shared behavior changes.

Common commands from the repository root:

```bash
dotnet build backend/XiHan.BasicApp.slnx
dotnet test backend/XiHan.BasicApp.slnx
dotnet run --project backend/src/main/XiHan.BasicApp.WebHost/XiHan.BasicApp.WebHost.csproj --launch-profile Development
```

When running locally, point `dotnet run --project` at `XiHan.BasicApp.WebHost.csproj`; the WebHost directory contains multiple project files, so the directory path is ambiguous.

For runtime HTTP checks, do not hardcode hostnames or ports in guidance or scripts. Discover the current backend base URL from `Properties/launchSettings.json`, `Hosting:Urls`, `appsettings*.json`, environment variables, or the startup log. Then set a temporary shell variable and use it for checks:

```bash
# Set BACKEND_BASE_URL from the current launch/config output before running these.
curl "$BACKEND_BASE_URL/health"
curl -L "$BACKEND_BASE_URL/scalar"
```
