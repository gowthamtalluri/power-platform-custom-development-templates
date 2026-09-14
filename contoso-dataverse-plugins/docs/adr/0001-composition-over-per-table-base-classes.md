# ADR 0001 — Composition over per-table abstract base classes

**Status:** Accepted
**Date:** 2026-09-14

## Context

A common pattern is: one abstract base class per table, inheriting `IPlugin`, holding the
methods shared across that table's pre- and post-operation plugins. Each step class then
inherits from it.

## Problem

1. **Fragile base class.** The table base accumulates every helper any step ever needed.
   After two years an `AccountPluginBase` with 40 protected methods is normal. Every step
   inherits all 40 whether it uses 1 or 30.
2. **Inheritance is the strongest coupling in C#.** Changing a shared method signature forces
   recompilation and re-review of every step on that table, including unrelated ones.
3. **Single inheritance is spent.** Because the table base is the `IPlugin` base, cross-cutting
   infrastructure (tracing, exception shaping, depth guards) must either be duplicated in every
   table base or pushed into a second base layer — producing a three-deep hierarchy.
4. **Pre and post needs genuinely differ.** Pre-operation mutates the Target; post-operation
   reads images and writes elsewhere. Forcing them to share a parent invites helpers that behave
   differently by stage, which is where stage-dependent bugs live.
5. **Testing gets harder.** You cannot substitute one shared method; you must construct the whole
   base class and satisfy its entire surface.

## Decision

- Exactly **one** class implements `IPlugin`: `Infrastructure.PluginBase`. It owns cross-cutting
  concerns only and has no table knowledge.
- Table-shared behaviour lives in **small, focused service classes** under
  `Tables/<Table>/Services/`, constructed by the handlers that need them.
- Data access lives in **repositories** under `Shared/Repositories/`.
- Decision logic lives in the **`Contoso.Crm.Domain`** project with no SDK dependency.

## Consequences

**Positive:** each handler declares exactly which behaviour it uses; services are independently
testable and independently replaceable; the inheritance chain is two levels and stays that way;
cross-cutting concerns exist in one file.

**Negative:** a handler explicitly constructs its services (2–3 lines of ceremony). This is a
deliberate trade — explicit dependencies are the point.

**Rejected alternative:** a full DI container inside the plugin. Container resolution cost is paid
on every execution, sandbox assembly-load constraints make it fragile, and the dependency graphs
in plugins are too shallow to justify it.
