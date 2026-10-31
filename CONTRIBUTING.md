# Contributing to Chronicler

Thanks for helping improve Chronicler. Focused bug fixes, tests, documentation,
and proposals that strengthen explicit deterministic state transfer and
simulation timing are welcome. For a large or breaking change, open an issue
first so the serialization contract and migration impact can be discussed before
implementation.

By participating, you agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md).
For help and private vulnerability reports, see [Support](.github/SUPPORT.md)
and [Security](SECURITY.md).

## Development setup

Chronicler uses the .NET 10 SDK for solution tooling and executes its test suite
on .NET 10. C# 14 is the default language version; library projects override it
to C# 11 for `netstandard2.1`. From the repository root:

```bash
dotnet restore Chronicler.slnx
dotnet build Chronicler.slnx --configuration Release --no-restore
dotnet test Chronicler.slnx --configuration Release --no-build
```

Validate both package profiles when a change touches public APIs, serialization,
dependencies, or packaging:

```bash
dotnet build Chronicler.slnx --configuration ReleaseLean
dotnet test Chronicler.slnx --configuration ReleaseLean --no-build
```

To build the documentation locally after a Release build:

```bash
dotnet tool restore
dotnet tool run docfx docs/api/docfx.json --warningsAsErrors
```

## Pull Request Process

For coordinated sibling builds, pass `-p:UseLocalLsfStack=true`. Chronicler and
its Lean shim use local development identities to match the sibling dependency
graph. These do not select release versions or make generated local packages
publishable. Leave package mode as the default for real release validation.

The independent `Chronicler.Diagnostics` package has no runtime package
dependencies. Its existing .NET 10 tests cover string overloads, caller
information, explicit formatting guards, and lazy handler selection. When
changing the public diagnostic API, perform a focused C# 9 consumer compilation
smoke check against the built netstandard assembly. That release check is
separate from the ordinary unit-test build; neither check establishes
compatibility with every host runtime or AOT pipeline.

1. Keep the change focused: shared recording APIs and host lifecycle hooks use
   `Chronicler`, transports and converters use `Chronicler.Serialization`,
   record-hash APIs use `Chronicler.Hashing`, and time values and the clock use
   `Chronicler.Timing`. Transport subfolders do not add public namespaces. See
   the [migration guide](docs/MIGRATION.md) for consumer changes and
   recorded-type identity implications.
2. Add or update tests for meaningful behavior changes. Exercise JSON and
   MemoryPack where transport parity is part of the contract. For time values,
   cover signed endpoints, fractional carry/borrow, exact ordering and
   allocation-free successful arithmetic. Wide-number test oracles belong in
   tests, not the runtime library. Compile guide examples and assert their
   results in the existing suites; do not add a separate sample project just to
   verify documentation. Preserve the distinction between standalone clock
   population and host-owned world restore.
3. Update XML comments, the root README, and the matching guide when public
   behavior, package shape, or developer workflow changes.
4. Run the relevant Release and ReleaseLean validation commands and describe the
   results in the pull request.
5. Do not manually bump package versions. The release workflow derives versions
   through GitVersion.
6. Call out serialized field-name, ordering, default, link-resolution, or
   compatibility changes explicitly; these can affect persisted data and
   deterministic replay.

## Local release archives

With the .NET SDK and the global `GitVersion.Tool` tool available on `PATH`, run:

```powershell
.\.assets\scripts\set-version-and-build.ps1
```

The script builds Release and ReleaseLean, then writes ZIP archives for
Chronicler, Chronicler.Diagnostics, and Chronicler.MemoryPackShim to
`artifacts/releases/`. Each archive includes the project's output for one
declared target framework. Names include the project, GitVersion version,
framework, and configuration, for example
`Chronicler-v1.0.0-netstandard2.1-releaselean.zip`. The current projects produce
12 archives; obsolete framework directories from older builds are excluded.

## Formatting and file layout

Run cleanup from the repository root with the .NET SDK selected by `global.json`
and Node.js/npm for Prettier. The Prettier version is explicit so each checkout
uses the same formatter; `npx` downloads it on first use if it is not cached.

```bash
dotnet format Chronicler.slnx --severity error
npx --yes prettier@3.9.5 --write .
```

`dotnet format` applies whitespace rules plus code-style and analyzer fixes at
the requested severity. It follows `.editorconfig`; it does not reproduce a
Visual Studio Code Cleanup profile. Prettier uses `.prettierrc` and
`.prettierignore` to leave generated output alone.

To check formatting without rewriting files:

```bash
dotnet format Chronicler.slnx --severity error --verify-no-changes
npx --yes prettier@3.9.5 --check .
```

Keep cleanup explicit before review. No commit hook rewrites or stages files.

- Put each namespace-level type or delegate in its own file. Move public or
  internal nested types to a separate file while preserving any required CLR
  identity; private nested implementation details may stay with their owner.
- Keep the existing filename and MIT license header on authored C# files under
  `src`. Update the filename line when moving a file. Test files have no license
  header.
- Keep current guides evergreen. Put version-specific upgrade steps in
  [Migration](docs/MIGRATION.md) and preserve completed design records as
  history. Generated documentation and coverage reports describe a particular
  checkout; they are not permanent release guarantees.
