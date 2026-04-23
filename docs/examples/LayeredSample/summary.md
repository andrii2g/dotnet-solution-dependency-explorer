# Dependency Explorer Summary

Input path: `LayeredSample.slnx`

## Scope

- Level: All
- Focus project: none
- Focus namespace: none
- Focus class: none

## Counts

- Projects: 4
- Package references: 0
- Documents: 23
- Named types: 8
- Project dependency edges: 5
- Namespace dependency edges: 19
- Type dependency edges: 25
- Internal type dependency edges: 11
- External type dependency edges: 14
- Constructor DI edges: 5

## Analysis Options

- Classification: enabled
- Constructor DI graph: enabled

## Workspace Diagnostics

- None

## Projects

- `LayeredSample.Api`
  Path: `LayeredSample.Api/LayeredSample.Api.csproj`
  Frameworks: net10.0
  Documents: 5
  Project references: LayeredSample.Application, LayeredSample.Infrastructure
  Package references: none

- `LayeredSample.Application`
  Path: `LayeredSample.Application/LayeredSample.Application.csproj`
  Frameworks: net10.0
  Documents: 6
  Project references: LayeredSample.Domain
  Package references: none

- `LayeredSample.Domain`
  Path: `LayeredSample.Domain/LayeredSample.Domain.csproj`
  Frameworks: net10.0
  Documents: 7
  Project references: none
  Package references: none

- `LayeredSample.Infrastructure`
  Path: `LayeredSample.Infrastructure/LayeredSample.Infrastructure.csproj`
  Frameworks: net10.0
  Documents: 5
  Project references: LayeredSample.Application, LayeredSample.Domain
  Package references: none

## Top Type Fan-Out

- `LayeredSample.Api.Endpoints.OrdersController`: 2
- `LayeredSample.Application.Invoices.InvoiceService`: 2
- `LayeredSample.Infrastructure.Data.SqlOrderRepository`: 2
- `LayeredSample.Application.Abstractions.IOrderRepository`: 1
- `LayeredSample.Application.Invoices.InvoiceQueryHandler`: 1
- `LayeredSample.Domain.Orders.Order`: 1

## Top Type Fan-In

- `LayeredSample.Application.Abstractions.IOrderRepository`: 2
- `LayeredSample.Application.Invoices.InvoiceService`: 2
- `LayeredSample.Domain.Orders.Order`: 2
- `LayeredSample.Domain.Policies.DiscountPolicy`: 2
- `LayeredSample.Application.Invoices.InvoiceQueryHandler`: 1

## Key Findings

- [warning] Project 'LayeredSample.Api' looks mixed: LayeredSample.Api.Endpoints.OrdersController classified as Mixed
- [warning] Project 'LayeredSample.Application' looks mixed: LayeredSample.Application.Abstractions.IOrderRepository classified as Mixed; LayeredSample.Application.Invoices.InvoiceQueryHandler classified as Mixed; LayeredSample.Application.Invoices.InvoiceService classified as Mixed
- [warning] Project 'LayeredSample.Infrastructure' looks mixed: LayeredSample.Infrastructure.Data.SqlOrderRepository classified as Mixed; LayeredSample.Infrastructure.Files.FileInvoiceGateway classified as Mixed