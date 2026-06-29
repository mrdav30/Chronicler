# Cyclomatic Complexity Exception Register

This document records methods that intentionally exceed the current cyclomatic complexity review threshold.

## Policy

- Review threshold: cyclomatic complexity greater than 10.
- Risk threshold: CRAP score greater than 30 requires immediate test hardening or refactoring.
- Current status: the fresh coverage/CRAP report generated on 2026-06-29 has no methods above CRAP 30.
- Source report: `tests/Chronicler.Tests/TestResults/complexity-analysis/reports/Summary.txt`.

Complexity exceptions are acceptable when the method is deterministic, narrowly scoped, well covered, and simpler to audit in one explicit flow than through indirection. These exceptions should be revisited when coverage drops, behavior changes, or the implementation becomes harder to reason about.

## Exception Register

| Module | Method | Complexity | Coverage | CRAP Score | Rationale | Revisit if |
| --- | --- | ---: | --- | ---: | --- | --- |
| `Chronicler` | `ChronicleHashChronicler.WriteLeafValue<T>(ref T, string)` | 15 | 90.9% line / 86.7% branch | 15.17 | Generic deterministic leaf hashing must dispatch every supported primitive and enum kind to an explicit `Unsafe.As<T, ...>` write without boxing, reflection, or delegate tables. The remaining uncovered switch default is defensive and unreachable through the validated `GetLeafKind(...)` path. Keeping the write cases local makes the record-hash byte contract easier to audit. | A new leaf kind is added, coverage drops, or a no-allocation helper design reduces complexity without hiding the primitive-to-byte mapping. |

## Review Notes

- Prefer reducing high-complexity serializer/control-flow methods by extracting named private helpers before adding exceptions.
- Prefer adding focused tests for reachable branches before documenting an exception.
- For deterministic hash and serialization code, do not reduce complexity by introducing reflection, serializer-specific behavior, hidden allocations, or unordered lookup flows that affect payload semantics.
- Re-run coverage and CRAP analysis after changes that touch methods listed here, then update this register.
