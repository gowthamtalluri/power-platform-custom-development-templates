# Contributing

## Branching
`feature/<work-item>-<slug>` → PR into `main`. Squash merge. No direct pushes to `main`.

## Naming conventions

| Artefact | Pattern | Example |
|---|---|---|
| Handler | `<Stage><Message><Table>` | `PreOperationAccountUpdate` |
| Custom API plugin | `<Publisher><ApiName>` | `ContosoRecalculateAccountRisk` |
| Service | `<Table><Concern>Service` | `AccountValidationService` |
| Repository | `<Aggregate>Repository` | `AccountRepository` |
| Schema constants | `<Table>Columns` | `AccountColumns` |

## Definition of done
- Handler decorated with `[PluginRegistration]` including the work item reference
- Domain rules unit tested
- Handler tested with FakeXrmEasy for at least the happy path and one rejection path
- Filtering attributes and image columns specified and minimal
- `docs/plugin-registration.md` still accurate
- PR checklist complete

## Code review focus
Reviewers check pipeline correctness before style: stage, sync/async, filtering attributes,
image columns, query count, recursion risk, and whether the error message helps a user.
