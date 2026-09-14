# Plugin registration guide

The `[PluginRegistration]` attribute on each handler is the **source of truth**. Registration in
an environment must match it. CI can be extended to reflect over the assembly and diff the
attributes against the deployed step configuration to catch drift.

## Stage selection

| Stage | Number | In transaction | Use it for |
|---|---|---|---|
| Pre-validation | 10 | **No** (for the root operation) | Rejecting an operation; validation that must run before cascades begin; logic that must run as the calling user before the transaction opens |
| Pre-operation | 20 | Yes | Defaulting and deriving values on the Target — free, no extra Update, no recursion |
| Main operation | 30 | Yes | **Custom APIs / Custom Actions only.** Never register on out-of-box messages |
| Post-operation | 40 | Yes (sync) / No (async) | Reading the committed result; creating related records; integration events |

## Sync vs async

Register **synchronous** only when the user must see the effect immediately or the work must roll
back with the transaction. Everything else is **asynchronous**: it keeps the form responsive,
gets platform retry, and does not consume the 2-minute sandbox budget of the user's transaction.

Set `DeleteAsyncOperationIfSuccessful = true` on async steps or the System Job table grows without bound.

## Images

| Message | PreImage | PostImage |
|---|---|---|
| Create | not available | available (post-op only) |
| Update | available | available (post-op only) |
| Delete | available | not available |

Always specify explicit columns. An image with all columns serialises the whole row into the
execution context on every single operation.

## Message-specific payloads

| Message | InputParameters |
|---|---|
| Create / Update | `Target` (Entity) |
| Delete | `Target` (EntityReference) |
| Retrieve | `Target` (EntityReference), `ColumnSet` |
| RetrieveMultiple | `Query` (QueryBase); result in `OutputParameters["BusinessEntityCollection"]` |
| Associate / Disassociate | `Target` (EntityReference), `Relationship`, `RelatedEntities` |
| Assign | `Target`, `Assignee` |

**Associate/Disassociate cannot be registered against a specific table** — register against all
entities and filter on the relationship schema name in `ShouldExecute`, as
`PreOperationAccountAssociate` demonstrates.

**Deactivation is an `Update` to `statecode`.** `SetStateDynamicEntity` is deprecated and is not
raised by current clients. Register on Update with a `statecode` filter.

## Performance budget

| Concern | Limit / guidance |
|---|---|
| Sandbox execution timeout | 2 minutes |
| Assembly size | 16 MB |
| Queries per step | aim for 0; justify anything above 2 |
| `RetrieveMultiple` plugins | last resort — they run on every grid, lookup and view |
| Depth | platform limit is 8; this codebase self-limits to 4 |
