# AGENTS.md

Rules for any AI agent (Claude Code, Copilot, Cursor, Codex) working in this repository. `CLAUDE.md` and `.github/copilot-instructions.md` only point here.

## What this is

The NuGet package `IsImageUrlDotNet` (namespace `IsImageUrlDotNet`, module `IsImageUrlDotNetLib`, written in F#): tells whether a string is a URL of an image, by its file extension first and otherwise by the Content-Type of a GET. 1.0.2 (2016-06-05, an F# `lib/net45` DLL with an undeclared FSharp.Core 4.4.0.0 dependency) is the published version until 2.0.0 ships. The run follows the package-modernize skill (m4bwav/package-modernize); the plan is `ai-docs/plans/2026-09-27-modernization-and-v2-release.md`; start with `ai-docs/HANDOFF.md`.

State on 2026-09-27: 2.0.0 written on branch `v2` (Phase 2 and 3; pull request open), not released. The library keeps 1.0.2's `IsImageUrl` statement for statement and adds the `ImageUrl` type.

## Rules

- **The promise (proposed in the plan, D1).** 2.x gives 1.0.2's answers, on the same runtime, for every case recorded in `tests/Golden/1.0.2.*.json` except the exceptions the plan names; the exceptions live in one table in the golden test. A fix that would change an old answer goes under a new name, with a decision entry and a changelog line.
- **The golden files never change.** `tests/Golden/Capture/` and the `tests/Golden/1.0.2.*.json` recordings were captured from the published 1.0.2 (never from this repository's code). When the golden test fails, fix the source; never re-record. A recording from another runtime may be added as a new file, captured by the same program from the published package.
- **Tests never touch the network.** Everything that makes a request goes to the local fixture server in `tests/Golden/Capture/FixtureServer.fs` (it is also the proxy; hosts are `.test` names, which never resolve).
- **Nothing reaches nuget.org without the maintainer.** No API key is stored anywhere; `release.yml` (to come) publishes through Trusted Publishing from a job that waits at the `nuget` environment for the maintainer's approval. Never push a package from a machine.
- **Releases follow one ritual.** Update `CHANGELOG.md` (a release heading carries its date), set `<Version>` in the library's project file, merge, wait for `ci` to be green on `master`, then tag `v<version>` and push the tag. Tag only after green.
- **Dependencies.** Lock files committed, `--locked-mode` in CI, Dependabot weekly with a three-day cooldown, actions pinned to commit SHAs. FSharp.Core is pinned explicitly, never the SDK's implicit version.
- **Research beats recall.** SDK, package and action versions change; check each on nuget.org before using it, and wait three days after a release.
- **Document for handoff.** Anything learned, decided or built goes into `ai-docs/` before you finish; rewrite `ai-docs/HANDOFF.md` when work is left unfinished.
- **No AI attribution anywhere.**
- **Line endings.** Files are LF.

## Commands

```
dotnet restore --locked-mode
dotnet tool restore
dotnet fantomas --check .                  # F# format; dotnet format skips F# and exits 0
dotnet format tests/IsImageUrlDotNet.CSharpTests --no-restore --verify-no-changes
dotnet build -c Release                    # warnings are errors
# F# analyzers (G-Research 0.25.0, Ionide 0.19.0), as ci.yml runs them; IsImageUrl.fs excluded (1.0.2's code):
dotnet fsharp-analyzers --project src/IsImageUrlDotNet/IsImageUrlDotNet.fsproj --analyzers-path <global-packages>/g-research.fsharp.analyzers/0.25.0/analyzers/dotnet/fs --analyzers-path <global-packages>/ionide.analyzers/0.19.0/analyzers/dotnet/fs --treat-as-error '*' --exclude-files '**/IsImageUrl.fs'
dotnet test --no-build -c Release -f net10.0
dotnet test --no-build -c Release -f net48 # Windows only; runs the netstandard2.0 build
dotnet pack src/IsImageUrlDotNet --no-build -c Release -o artifacts
```

The capture (run only to add a recording for a new runtime, never over a committed one): in `tests/Golden/Capture`, `dotnet build -c Release`, then `dotnet run -c Release -f net10.0 --no-build > <scratch>/x.json` twice, compared.

## Layout and traps

- `src/IsImageUrlDotNet/`: `IsImageUrl.fs` (1.0.2's module, kept exactly; `#nowarn "44"` for WebRequest) and `ImageUrl.fs` (the new API). FSharp.Core pinned at 6.0.7, the declared floor: never raise it for tidiness, and Dependabot ignores it.
- `tests/IsImageUrlDotNet.Tests/` (F#, NUnit): `GoldenTests.fs` compiles the capture's `Json.fs`, `FixtureServer.fs` and `Cases.fs` by link, runs them against the new build and compares each case as JSON text with `tests/Golden/1.0.2.<runtime>-<os>.json`; the one exception table is in that file. `PublicApiTests.fs` checks every line of `PublicApi-1.0.2.txt` (written by `PublicApi.fs` from the published DLL). `ImageUrlTests.fs` tests the new API against the fixture server.
- `tests/IsImageUrlDotNet.CSharpTests/` (C#): the extension forms, and FSharp.Core arriving only through the package's dependency.
- `tests/Golden/Capture/`: the frozen capture program (in `.fantomasignore`). Its empty `Directory.Build.*` and `Directory.Packages.props` stop it from inheriting the repository's MSBuild files.
- The golden test sets the ambient culture from the recording (en-US on Windows, invariant on Linux and macOS) and English UI messages, as the capture did.
- Windows recordings name drive `D:` in one case (`file:///nonexistent-isimageurl/file` resolves against the current drive); the exception table swaps in the current drive, so a clone on another drive passes.
- A canary (a planted wrong line in `src/`) must be reverted with an edit, not `git checkout -- src/`, until the source is committed.
- F# callers cannot use the C# extension form `url.IsImageUrl()`; F# reads the module signature. Test the extension form from C#.
- F# nullness is on in the library (`string | null` parameters). FSharp.Core 6.0.7 has no `NonNull` pattern: narrow with `match x with | null -> ... | x -> ...`.
- The Bash tool's heredocs halve backslashes: write F# with `\n` or `\\` through the editor tools, or build the character from `char 92`.

## ai-docs

`ai-docs/` holds the record in the everlast layout (INDEX, HANDOFF, log, decisions, plans, notes); keep it current at every stop.
