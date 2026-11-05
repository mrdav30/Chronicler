# Feature Work Issue Tracker

## Purpose

This document tracks unresolved correctness, determinism, API, documentation,
build, and tooling issues that fall outside an active implementation plan.

Measured performance and allocation concerns belong in
[`benchmark-signal-hardening-backlog.md`](benchmark-signal-hardening-backlog.md).
Work that requires staged implementation belongs in a focused feature-work plan.

## Tracker Rules

- Record only unresolved, reproducible concerns.
- Include the affected area, evidence, user or runtime impact, priority, and
  smallest useful next action.
- Use repository-relative paths and portable commands. Do not record
  developer-specific drive letters, home directories, or machine-local
  dependency locations.
- Preserve the reproducer, source revision, observed failure and investigation
  conclusions here. Ignored artifacts are disposable supporting evidence;
  reproducing or continuing an issue must not require their survival.
- Keep an issue here while it needs investigation or is deliberately deferred.
- Link an active implementation plan instead of duplicating its task details.
- Remove an entry after its resolution and verification are complete. Preserve
  durable design decisions in the completed plan, public documentation, or
  release notes rather than retaining a resolved-issue archive here.

## Active Issues

### CHR-Issue-001: Shim VSTest worker intermittently stops making progress

- **Observed:** 2026-11-04 on Windows, SDK `10.0.302`, runtime `.NET 10.0.10`,
  xUnit `3.2.2`, VS adapter `3.1.5`, Test SDK `18.4.0`; Chronicler `70b97a1`
  with the coordinated local-identity configuration changes.
- **Status:** Needs reproduction; two incomplete invocations were observed,
  but subsequent invocations completed. Attribution to the runner, environment,
  or build configuration is not established.
- **Priority:** Low
- **Affected area:** `tests/Chronicler.MemoryPackShim.Tests` under VSTest.
- **Evidence:** During serial solution validation in Release and ReleaseLean,
  the shim worker stopped making progress for more than two minutes, with no
  child build active when inspected. Terminating the owned worker produced
  `Xunit.Sdk.TestPipelineException` and only four reported passing tests; those
  incomplete invocations are not successful suite results. Re-running the
  normal runner with source mode inherited by child builds completed all 17
  tests in both configurations. The direct runner also completed all 17, and
  an isolated numeric-identity control completed all 17 under VSTest.
- **Reproduction:** Set `$env:UseLocalLsfStack='true'`, build with
  `-p:DisableTransitiveProjectReferences=true -m:1`, then run
  `dotnet test tests/Chronicler.MemoryPackShim.Tests/Chronicler.MemoryPackShim.Tests.csproj -c ReleaseLean --no-build --no-restore -p:UseLocalLsfStack=true -p:DisableTransitiveProjectReferences=true`.
  The stall is intermittent. A bounded diagnostic alternative is
  `dotnet tests/Chronicler.MemoryPackShim.Tests/bin/ReleaseLean/net10.0/Chronicler.MemoryPackShim.Tests.dll -parallel none`.
- **Impact:** Local validation can hang despite complete passing executions of
  the shim tests. No runtime-library failure or stable identity regression was
  established, and passing retries do not explain the stalled invocations.
- **Next action:** On recurrence, capture the testhost/worker stacks and IPC
  state before termination, and compare the same binaries through both runners.
  Do not weaken tests or change library versions to mask an unattributed stall.

**Next issue ID:** `CHR-Issue-002`

## Issue Template

```markdown
### CHR-Issue-###: Concise title

- **Status:** Needs reproduction | Confirmed | Planned | Deferred
- **Priority:** Critical | High | Medium | Low
- **Affected area:** Public API, source owner, package, build, or documentation
- **Evidence:** Minimal reproduction, failing test, or portable command
- **Impact:** Observable correctness, determinism, usability, or tooling risk
- **Next action:** The smallest useful investigation or implementation step
```

When adding an issue, use the next ID and increment the field above. Never reuse
an ID that appears in a completed plan or repository history.
