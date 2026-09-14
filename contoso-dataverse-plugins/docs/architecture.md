# Architecture

## Execution flow

```
Dataverse pipeline
   │
   ▼
PluginBase.Execute(IServiceProvider)          ← cross-cutting only
   ├─ build LocalPluginContext (lazy services)
   ├─ trace message / entity / stage / depth / correlationId
   ├─ depth guard              → skip
   ├─ ExecutionGuard           → skip if suppressed
   ├─ ShouldExecute(context)   → skip cheaply
   ├─ Execute(context)         ← YOUR HANDLER. business logic only.
   │     ├─ Services (table-scoped behaviour)
   │     │     └─ Domain (pure rules, no SDK)
   │     └─ Repositories (all data access)
   └─ exception shaping → InvalidPluginExecutionException + correlation ID
```

## Layer rules

| Layer | May reference | Must never |
|---|---|---|
| `Infrastructure` | Xrm SDK | know about any table |
| `Handlers` | Infrastructure, Services, Repositories | contain a query or a business rule |
| `Services` | Infrastructure, Domain, Repositories | implement `IPlugin` |
| `Repositories` | Xrm SDK | contain business rules |
| `Domain` | nothing | reference `Microsoft.Xrm.Sdk` |

If a handler contains a `QueryExpression`, it belongs in a repository.
If a service contains an `if` that a business analyst would recognise, it belongs in Domain.

## Integration boundary

Plugins talk to Dataverse through `IOrganizationService` only.

For anything crossing the org boundary:

| Need | Mechanism |
|---|---|
| Fire-and-forget to Azure | Service Endpoint (Service Bus / Event Grid) via `IServiceEndpointNotificationService` |
| Orchestration, retries, connectors | Power Automate triggered on the Dataverse row |
| Synchronous external read | Virtual table, or reconsider the requirement |
| Scheduled/bulk | Azure Function with the Dataverse ServiceClient, outside the sandbox |

Outbound HTTP from a synchronous plugin couples your user transaction to a third party's uptime.
It is the single most common cause of "Dynamics is slow" incidents.
