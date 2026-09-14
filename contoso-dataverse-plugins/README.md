# Contoso Dataverse Plugins

Reference architecture for enterprise C# plugin development on Microsoft Dataverse / Power Platform.

This repository is a **template**. Clone it, rename `Contoso` to your organisation, delete the sample
`Account` slice once you have your own, and keep the `Infrastructure` layer as-is.

---

## 1. Repository layout

```
.
├─ Contoso.Crm.Plugins.sln
├─ Directory.Build.props          # TFM, signing, analyzers - one place, all projects
├─ Directory.Packages.props       # Central Package Management - one place, all versions
├─ .editorconfig                  # Style + plugin-specific analyzer rules
├─ nuget.config                   # Locked feed + package source mapping
│
├─ build/
│  └─ README.md                   # Signing key guidance (the .snk is NEVER committed)
│
├─ src/
│  ├─ Contoso.Crm.Domain/                     # netstandard2.0 - PURE business rules, zero SDK refs
│  │  └─ Accounts/
│  │     ├─ AccountRiskCalculator.cs
│  │     └─ AccountNumberPolicy.cs
│  │
│  └─ Contoso.Crm.Plugins/                    # net462 - the deployable, signed plugin assembly
│     ├─ Infrastructure/                      # Framework. Written once, rarely touched.
│     │  ├─ ILocalPluginContext.cs
│     │  ├─ LocalPluginContext.cs
│     │  ├─ PluginBase.cs                     # The ONLY class implementing IPlugin
│     │  ├─ ExecutionGuard.cs
│     │  ├─ ParameterNames.cs
│     │  └─ PluginRegistrationAttribute.cs
│     │
│     ├─ Tables/                              # One folder per table (vertical slice)
│     │  ├─ Account/
│     │  │  ├─ AccountColumns.cs              # Schema constants
│     │  │  ├─ Services/                      # Table-scoped behaviour, COMPOSED not inherited
│     │  │  │  ├─ AccountValidationService.cs
│     │  │  │  └─ AccountRiskService.cs
│     │  │  └─ Handlers/                      # One class per registered STEP
│     │  │     ├─ PreValidateAccountDelete.cs
│     │  │     ├─ PreOperationAccountCreate.cs
│     │  │     ├─ PreOperationAccountUpdate.cs
│     │  │     ├─ PreOperationAccountAssociate.cs
│     │  │     ├─ PostOperationAccountCreate.cs
│     │  │     ├─ PostOperationAccountUpdate.cs
│     │  │     ├─ PostOperationAccountDelete.cs
│     │  │     ├─ PostOperationAccountStateChanged.cs
│     │  │     └─ PostOperationAccountRetrieveMultiple.cs
│     │  └─ Contact/
│     │
│     ├─ CustomApis/                          # Custom API main-operation plugins
│     │  └─ ContosoRecalculateAccountRisk.cs
│     │
│     └─ Shared/                              # Cross-table only. Guard against god-classes.
│        ├─ Guard.cs
│        ├─ Extensions/EntityExtensions.cs
│        └─ Repositories/AccountRepository.cs
│
├─ tests/
│  ├─ Contoso.Crm.Domain.Tests/               # net8.0 - fast, pure, high coverage
│  └─ Contoso.Crm.Plugins.Tests/              # net472 - FakeXrmEasy pipeline tests
│
├─ solution/                                  # Unpacked Dataverse solution (pac solution sync)
│
├─ docs/
│  ├─ architecture.md
│  ├─ plugin-registration.md
│  └─ adr/0001-composition-over-per-table-base-classes.md
│
└─ .github/workflows/{ci.yml,cd.yml}
```

### Why this shape

| Decision | Reason |
|---|---|
| Folder per table | Vertical slice. A change request about Accounts touches exactly one folder. |
| One class per **step**, not per operation name | The class name tells you the registration: message + stage. Nothing to look up. |
| `PluginBase` is the only `IPlugin` | Cross-cutting concerns are implemented once. Handlers contain business logic only. |
| Table logic in a **Service**, not a base class | Composition. A handler depends only on the behaviour it uses; no fragile base class. |
| Separate `Domain` project | Business rules are testable in milliseconds with no Dataverse types anywhere near them. |
| Repository per aggregate | Every query is in one reviewable place. Handlers become trivially fakeable. |
| Central Package Management | One version bump, whole repo. No mismatched SDK versions across projects. |

---

## 2. Getting started

### Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 8.0+ |
| .NET Framework 4.6.2 targeting pack | required (Visual Studio installer → Individual components) |
| Power Platform CLI (`pac`) | latest |
| Visual Studio 2022 / Rider / VS Code + C# Dev Kit | any |

### First build

```bash
git clone <your-fork>
cd contoso-dataverse-plugins

# Generate a LOCAL dev signing key (never commit it)
sn -k build/Contoso.Crm.snk

dotnet restore Contoso.Crm.Plugins.sln
dotnet build   Contoso.Crm.Plugins.sln -c Release
dotnet test    Contoso.Crm.Plugins.sln
```

### Deploy to your dev environment

```bash
pac auth create --environment https://contoso-dev.crm.dynamics.com
dotnet pack src/Contoso.Crm.Plugins/Contoso.Crm.Plugins.csproj -c Release -o artifacts
pac plugin push --pluginFile artifacts/Contoso.Crm.Plugins.1.0.0.nupkg
```

Plugin **packages** (`.nupkg`) are used instead of a bare `.dll` so that dependent assemblies
deploy with the plugin. This removes the historical need for ILMerge, which is unsupported
and a frequent source of assembly-load failures.

---

## 3. How to add a new table

1. `src/Contoso.Crm.Plugins/Tables/<TableName>/`
2. Add `<TableName>Columns.cs` with logical-name constants and the standard image column set.
3. Put reusable table behaviour in `Services/`. Each service does **one** thing.
4. Add one handler per registered step in `Handlers/`, named `<Stage><Message><Table>.cs`.
5. Decorate each handler with `[PluginRegistration(...)]`.
6. Put any decision logic worth testing into `src/Contoso.Crm.Domain/<TableName>/`.
7. Add tests. A step with no test does not ship.

**Do not** create a `<TableName>PluginBase`. See
[ADR 0001](docs/adr/0001-composition-over-per-table-base-classes.md).

---

## 4. Non-negotiable rules

1. **Never call the Dataverse Web API over HTTP from inside a plugin.** Use the injected
   `IOrganizationService`. HTTP calls leave the transaction, add latency against the 2-minute
   sandbox limit, and require secrets the sandbox should not hold.
2. **No outbound HTTP in a synchronous step.** Use an asynchronous step, a Service Endpoint
   (Azure Service Bus / Event Grid), or Power Automate.
3. **Every `Update` step has filtering attributes.** No exceptions.
4. **Register images, don't `Retrieve`.** If the data is on the PreImage, reading it costs nothing.
5. **Images use explicit column lists.** `ColumnSet(true)` on an image is a performance defect.
6. **Plugins are stateless.** No static mutable state, no instance fields holding per-execution
   data — the platform caches and reuses plugin instances across concurrent executions.
7. **Secrets come from Environment Variables or Azure Key Vault**, never from unsecure
   configuration, never from a constant, never traced.
8. **`InvalidPluginExecutionException` messages are for end users.** Correlation ID in, stack trace out.
9. **One plugin assembly per Dataverse solution.** 16 MB hard limit; a shared mega-assembly
   couples release trains across teams.

---

## 5. Observability

`ILocalPluginContext` exposes both `ITracingService` (visible in the Plugin Trace Log) and
`ILogger` (flows to Application Insights when the environment has telemetry export configured).
`PluginBase` automatically emits message / entity / stage / depth / correlation ID on entry and
an elapsed-milliseconds line on exit, so slow steps surface without extra instrumentation.

Enable trace logging per environment: **Power Platform admin centre → Environment → Settings →
Product → Privacy + Security → Plug-in trace log**. Leave it on `Exception` in production and
switch to `All` only while investigating.

---

## 6. Testing strategy

| Layer | Framework | What it proves | Target |
|---|---|---|---|
| `Contoso.Crm.Domain` | xUnit | Business rules are correct | ~100% branch |
| Handlers | FakeXrmEasy v3 | Right thing happens for the right message/stage/image | every step |
| Solution | `pac` + integration env | Registration matches code | nightly |

`PluginTestBase` builds a realistic `XrmFakedPluginExecutionContext` so individual tests stay short.

---

## 7. ALM

```
feature branch → PR (CI: build + test + size check) → main
main → pack .nupkg artifact
manual dispatch → CD → dev → test → uat → prod (environment approvals)
```

Solution files live unpacked under `solution/` via `pac solution sync`, so solution changes
are reviewable diffs rather than opaque zips.
