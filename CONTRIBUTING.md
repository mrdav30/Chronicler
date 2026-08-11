# Contributing to Chronicler

Thanks for helping improve Chronicler. Focused bug fixes, tests, documentation,
and proposals that strengthen explicit deterministic state transfer are welcome.
For a large or breaking change, open an issue first so the serialization
contract and migration impact can be discussed before implementation.

By participating, you agree to follow the code of conduct below.

## Development setup

Chronicler uses the .NET 10 SDK for solution tooling and executes its test suite
on .NET 8. From the repository root:

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

1. Keep the change focused and preserve the public `Chronicler` namespace.
2. Add or update tests for meaningful behavior changes. Exercise JSON and
   MemoryPack where transport parity is part of the contract.
3. Update XML comments, the root README, and the matching guide when public
   behavior, package shape, or developer workflow changes.
4. Run the relevant Release and ReleaseLean validation commands and describe the
   results in the pull request.
5. Do not manually bump package versions. The release workflow derives versions
   through GitVersion.
6. Call out serialized field-name, ordering, default, link-resolution, or
   compatibility changes explicitly; these can affect persisted data and
   deterministic replay.

## Code of Conduct

### Our Pledge

In the interest of fostering an open and welcoming environment, we as
contributors and maintainers pledge to making participation in our project and
our community a harassment-free experience for everyone, regardless of age, body
size, disability, ethnicity, gender identity and expression, level of
experience, nationality, personal appearance, race, religion, or sexual identity
and orientation.

### Our Standards

Examples of behavior that contributes to creating a positive environment
include:

- Using welcoming and inclusive language
- Being respectful of differing viewpoints and experiences
- Gracefully accepting constructive criticism
- Focusing on what is best for the community
- Showing empathy towards other community members

Examples of unacceptable behavior by participants include:

- The use of sexualized language or imagery and unwelcome sexual attention or
  advances
- Trolling, insulting/derogatory comments, and personal or political attacks
- Public or private harassment
- Publishing others' private information, such as a physical or electronic
  address, without explicit permission
- Other conduct which could reasonably be considered inappropriate in a
  professional setting

### Our Responsibilities

Project maintainers are responsible for clarifying the standards of acceptable
behavior and are expected to take appropriate and fair corrective action in
response to any instances of unacceptable behavior.

Project maintainers have the right and responsibility to remove, edit, or reject
comments, commits, code, wiki edits, issues, and other contributions that are
not aligned to this Code of Conduct, or to ban temporarily or permanently any
contributor for other behaviors that they deem inappropriate, threatening,
offensive, or harmful.

### Scope

This Code of Conduct applies both within project spaces and in public spaces
when an individual is representing the project or its community. Examples of
representing a project or community include using an official project e-mail
address, posting via an official social media account, or acting as an appointed
representative at an online or offline event. Representation of a project may be
further defined and clarified by project maintainers.

### Enforcement

Instances of abusive, harassing, or otherwise unacceptable behavior may be
reported by contacting the project team at `david.oravsky@gmail.com`. All
complaints will be reviewed and investigated and will result in a response that
is deemed necessary and appropriate to the circumstances. The project team is
obligated to maintain confidentiality with regard to the reporter of an
incident. Further details of specific enforcement policies may be posted
separately.

Project maintainers who do not follow or enforce the Code of Conduct in good
faith may face temporary or permanent repercussions as determined by other
members of the project's leadership.

### Attribution

This Code of Conduct is adapted from the [Contributor Covenant][homepage],
version 1.4.

[homepage]: https://www.contributor-covenant.org/
