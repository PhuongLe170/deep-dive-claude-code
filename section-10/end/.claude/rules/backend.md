---
paths: app/api/**
---

# Backend rules

## Structure

Features are vertical slices under `Features/`. Each slice owns its entity, contracts, and endpoints. Follow `Features/Expenses` as the reference slice:

- `Expense.cs`: the entity.
- `ExpenseContracts.cs`: request and response `record` types, plus explicit `ToEntity()` / `ToResponse()` mapping. No AutoMapper.
- `ExpenseEndpoints.cs`: a static class with a `MapExpenseEndpoints(this IEndpointRouteBuilder)` extension. Group routes with `MapGroup` and keep handlers as private static methods.

## Handlers

- Async all the way. Every handler takes a `CancellationToken` parameter and passes it down to EF Core.
- Validate in the handler. Return `Results.ValidationProblem(errors)` for a 400.

## Persistence

Register the entity on `ExpenseDbContext` and add a migration.

## Build

Warnings fail the build (`TreatWarningsAsErrors` in `Directory.Build.props`). Fix them. Do not suppress them.
