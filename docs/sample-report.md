# Dependency Explorer Report

Generated from `LayeredSample.slnx`.

## Summary

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
- Project cycles: 0
- Namespace cycles: 0
- Type cycles: 0

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

## Cycle Summary

- Project cycles: 0 (largest: 0)
- Namespace cycles: 0 (largest: 0)
- Type cycles: 0 (largest: 0)

## Key Findings

- None

## Inventory

| Project | Classification | Documents | Package refs | Project refs | Notes |
| --- | --- | ---: | ---: | ---: | --- |
| LayeredSample.Api | Presentation (High) | 5 | 0 | 2 | presentation-oriented project name/path; runnable entrypoint project |
| LayeredSample.Application | Application (High) | 6 | 0 | 1 | application-oriented project name/path; LayeredSample.Application.Abstractions.IOrderRepository classified as Application |
| LayeredSample.Domain | Domain (High) | 7 | 0 | 0 | domain-oriented project name/path; LayeredSample.Domain.Orders.Order classified as Domain |
| LayeredSample.Infrastructure | Infrastructure (High) | 5 | 0 | 2 | infrastructure-oriented project name/path; LayeredSample.Infrastructure.Data.SqlOrderRepository classified as Infrastructure |

## Findings

No violations or warnings were produced for this run.

## Project Graph

```mermaid
graph TD
    project__LayeredSample_Api[LayeredSample.Api]
    project__LayeredSample_Application[LayeredSample.Application]
    project__LayeredSample_Domain[LayeredSample.Domain]
    project__LayeredSample_Infrastructure[LayeredSample.Infrastructure]
    project__LayeredSample_Api --> project__LayeredSample_Application
    project__LayeredSample_Api --> project__LayeredSample_Infrastructure
    project__LayeredSample_Application --> project__LayeredSample_Domain
    project__LayeredSample_Infrastructure --> project__LayeredSample_Application
    project__LayeredSample_Infrastructure --> project__LayeredSample_Domain
```

## Namespace Graph

```mermaid
graph TD
    project__LayeredSample_Api__namespace__LayeredSample_Api_Endpoints[LayeredSample.Api.Endpoints]
    project__LayeredSample_Application__namespace__LayeredSample_Application_Abstractions[LayeredSample.Application.Abstractions]
    project__LayeredSample_Application__namespace__LayeredSample_Application_Invoices[LayeredSample.Application.Invoices]
    project__LayeredSample_Domain__namespace__LayeredSample_Domain_Orders[LayeredSample.Domain.Orders]
    project__LayeredSample_Domain__namespace__LayeredSample_Domain_Policies[LayeredSample.Domain.Policies]
    project__LayeredSample_Infrastructure__namespace__LayeredSample_Infrastructure_Data[LayeredSample.Infrastructure.Data]
    project__LayeredSample_Api__namespace__LayeredSample_Api_Endpoints --> project__LayeredSample_Application__namespace__LayeredSample_Application_Invoices
    project__LayeredSample_Application__namespace__LayeredSample_Application_Abstractions --> project__LayeredSample_Domain__namespace__LayeredSample_Domain_Orders
    project__LayeredSample_Application__namespace__LayeredSample_Application_Invoices --> project__LayeredSample_Application__namespace__LayeredSample_Application_Abstractions
    project__LayeredSample_Application__namespace__LayeredSample_Application_Invoices --> project__LayeredSample_Domain__namespace__LayeredSample_Domain_Policies
    project__LayeredSample_Domain__namespace__LayeredSample_Domain_Orders --> project__LayeredSample_Domain__namespace__LayeredSample_Domain_Policies
    project__LayeredSample_Infrastructure__namespace__LayeredSample_Infrastructure_Data --> project__LayeredSample_Application__namespace__LayeredSample_Application_Abstractions
    project__LayeredSample_Infrastructure__namespace__LayeredSample_Infrastructure_Data --> project__LayeredSample_Domain__namespace__LayeredSample_Domain_Orders
```

## Global Class Graph

```mermaid
graph TD
    global__LayeredSample_Api_Endpoints_OrdersController[LayeredSample.Api.Endpoints.OrdersController]
    global__LayeredSample_Application_Abstractions_IOrderRepository[LayeredSample.Application.Abstractions.IOrderRepository]
    global__LayeredSample_Application_Invoices_InvoiceQueryHandler[LayeredSample.Application.Invoices.InvoiceQueryHandler]
    global__LayeredSample_Application_Invoices_InvoiceService[LayeredSample.Application.Invoices.InvoiceService]
    global__LayeredSample_Domain_Orders_Order[LayeredSample.Domain.Orders.Order]
    global__LayeredSample_Domain_Policies_DiscountPolicy[LayeredSample.Domain.Policies.DiscountPolicy]
    global__LayeredSample_Infrastructure_Data_SqlOrderRepository[LayeredSample.Infrastructure.Data.SqlOrderRepository]
    global__LayeredSample_Api_Endpoints_OrdersController --> global__LayeredSample_Application_Invoices_InvoiceQueryHandler
    global__LayeredSample_Api_Endpoints_OrdersController --> global__LayeredSample_Application_Invoices_InvoiceService
    global__LayeredSample_Application_Abstractions_IOrderRepository --> global__LayeredSample_Domain_Orders_Order
    global__LayeredSample_Application_Invoices_InvoiceQueryHandler --> global__LayeredSample_Application_Invoices_InvoiceService
    global__LayeredSample_Application_Invoices_InvoiceService --> global__LayeredSample_Application_Abstractions_IOrderRepository
    global__LayeredSample_Application_Invoices_InvoiceService --> global__LayeredSample_Domain_Policies_DiscountPolicy
    global__LayeredSample_Domain_Orders_Order --> global__LayeredSample_Domain_Policies_DiscountPolicy
    global__LayeredSample_Infrastructure_Data_SqlOrderRepository --> global__LayeredSample_Application_Abstractions_IOrderRepository
    global__LayeredSample_Infrastructure_Data_SqlOrderRepository --> global__LayeredSample_Domain_Orders_Order
```

## Global DI Graph

```mermaid
graph TD
    global__LayeredSample_Api_Endpoints_OrdersController[LayeredSample.Api.Endpoints.OrdersController]
    global__LayeredSample_Application_Abstractions_IOrderRepository[LayeredSample.Application.Abstractions.IOrderRepository]
    global__LayeredSample_Application_Invoices_InvoiceQueryHandler[LayeredSample.Application.Invoices.InvoiceQueryHandler]
    global__LayeredSample_Application_Invoices_InvoiceService[LayeredSample.Application.Invoices.InvoiceService]
    global__LayeredSample_Api_Endpoints_OrdersController --> global__LayeredSample_Application_Invoices_InvoiceService
    global__LayeredSample_Application_Invoices_InvoiceQueryHandler --> global__LayeredSample_Application_Invoices_InvoiceService
    global__LayeredSample_Application_Invoices_InvoiceService --> global__LayeredSample_Application_Abstractions_IOrderRepository
```
