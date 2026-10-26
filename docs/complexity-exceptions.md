# Cyclomatic Complexity Exception Register

This document records methods that intentionally exceed the current cyclomatic complexity review threshold.

## Policy

- Review threshold: cyclomatic complexity greater than 10.
- Risk threshold: CRAP score greater than 30 requires immediate test hardening or refactoring.
- Current status: the coverage/CRAP report generated on 2026-09-24 has no methods above CRAP 30. Release and ReleaseLean both have 100% line, branch, and method coverage across the core and shim assemblies.
- Source report: `tests/Chronicler.Tests/TestResults/coverage-analysis/coverage-analysis.md` (generated locally, not committed).

Complexity exceptions are acceptable when the method is deterministic, narrowly scoped, well covered, and simpler to audit in one explicit flow than through indirection. These exceptions should be revisited when coverage drops, behavior changes, or the implementation becomes harder to reason about.

## Exception Register

| Module | Method | Complexity | Coverage | CRAP Score | Rationale | Revisit if |
| --- | --- | ---: | --- | ---: | --- | --- |
| `Chronicler` | `ChronicleHashChronicler.WriteLeafValue<T>(ref T, LeafKind)` | 13 | 100% line / 100% branch | 13.00 | Generic deterministic leaf hashing dispatches every supported primitive and enum kind to an explicit `Unsafe.As<T, ...>` write without boxing, reflection, or delegate tables. `GetLeafKind(...)` validates the kind before dispatch; after string and primitive cases, only enums remain. Keeping the writes together makes the record-hash byte contract easier to audit. | A new leaf kind is added, coverage drops, or a no-allocation helper design reduces complexity without hiding the primitive-to-byte mapping. |

## Review Notes

- Prefer reducing high-complexity serializer/control-flow methods by extracting named private helpers before adding exceptions.
- Prefer adding focused tests for reachable branches before documenting an exception.
- For deterministic hash and serialization code, do not reduce complexity by introducing reflection, serializer-specific behavior, hidden allocations, or unordered lookup flows that affect payload semantics.
- Re-run coverage and CRAP analysis after changes that touch methods listed here, then update this register.
