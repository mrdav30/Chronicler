# Cyclomatic Complexity Exception Register

This document records methods that intentionally exceed the current cyclomatic
complexity review threshold.

## Policy

- Review threshold: cyclomatic complexity greater than 10.
- Risk threshold: CRAP score greater than 30 requires immediate test hardening
  or refactoring.
- Require exact 100% of reported lines, branch outcomes, and executable methods
  in Release and ReleaseLean. A complexity exception does not waive coverage.
  Fix reported gaps without adding exclusions or disabling checks; rounded
  percentages do not establish this gate.
- Generate coverage and CRAP results for the checkout under review. Local
  reports and published coverage describe the commit tested, not a standing
  guarantee for every package profile or future release.

Complexity exceptions are acceptable when the method is deterministic, narrowly
scoped, fully covered, and simpler to audit in one explicit flow than through
indirection. These exceptions should be revisited when coverage drops, behavior
changes, or the implementation becomes harder to reason about.

## Exception Register

| Module       | Method                                                       | Complexity | Rationale                                                                                                                                                                                                                                                                                                                                                                   | Revisit if                                                                                                                                  |
| ------------ | ------------------------------------------------------------ | ---------: | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| `Chronicler` | `ChronicleHashChronicler.WriteLeafValue<T>(ref T, LeafKind)` |         13 | Generic deterministic leaf hashing dispatches every supported primitive and enum kind to an explicit `Unsafe.As<T, ...>` write without boxing, reflection, or delegate tables. `GetLeafKind(...)` validates the kind before dispatch; after string and primitive cases, only enums remain. Keeping the writes together makes the record-hash byte contract easier to audit. | A new leaf kind is added, coverage drops, or a no-allocation helper design reduces complexity without hiding the primitive-to-byte mapping. |

## Review Notes

- Prefer reducing high-complexity serializer/control-flow methods by extracting
  named private helpers before adding exceptions.
- Cover all reported paths before documenting a complexity exception. A proven
  unreachable branch may be simplified only after verifying its callers and
  invariants; it cannot receive a coverage waiver here.
- For deterministic hash and serialization code, do not reduce complexity by
  introducing reflection, serializer-specific behavior, hidden allocations, or
  unordered lookup flows that affect payload semantics.
- Re-run coverage and CRAP analysis after changes that touch methods listed
  here, then update this register.
