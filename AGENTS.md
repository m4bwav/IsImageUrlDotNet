# AGENTS.md

Rules for any AI agent (Claude Code, Copilot, Cursor, Codex) working in this repository. `CLAUDE.md` and `.github/copilot-instructions.md` only point here.

## What this is

The NuGet package `IsImageUrlDotNet` (namespace `IsImageUrlDotNet`, module `IsImageUrlDotNetLib`, written in F#): tells whether a string is a URL of an image, by its file extension first and otherwise by the Content-Type of a GET. 1.0.2 (2016-06-05, an F# `lib/net45` DLL with an undeclared FSharp.Core 4.4.0.0 dependency) is the published version until 2.0.0 ships. The run follows the package-modernize skill (m4bwav/package-modernize); the plan is `ai-docs/plans/2026-09-27-modernization-and-v2-release.md`; start with `ai-docs/HANDOFF.md`.

State on 2026-09-27: Phases 0 and 1 (survey, golden capture, plan). No new code yet; the 2016 projects are still in place and do not build on a current SDK.

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

## Commands (Phase 0)

```
cd tests/Golden/Capture
dotnet build -c Release
dotnet run -c Release -f net10.0 --no-build > /tmp/check.json   # never over the committed recordings
mono bin/Release/net48/Capture.exe                              # the net48 build under Mono (Linux reference only)
```

## Layout and traps

- `IsImageUrlDotNet/` and `IsImageUrlDotNet.Test/`: the 2016 projects (old-style fsproj needing Visual Studio's F# targets; MSTest calling google.com). Replaced in Phase 2.
- `tests/Golden/Capture/`: the capture program (F#). `Cases.fs` holds every case and is meant to be compiled unchanged by the golden test against the new library; `FixtureServer.fs` is the fixture server and proxy; `Json.fs` writes the recording. Its empty `Directory.Build.*` and `Directory.Packages.props` stop it from inheriting the repository's MSBuild files.
- F# callers cannot use the C# extension form `url.IsImageUrl()`; F# reads the module signature. Test the extension form from C#.
- FSharp.Core: the SDK's implicit reference follows the SDK's feature band (SDK 10.0.1xx gives 10.0.1xx, 10.0.4xx gives 10.1.4xx), so a library pins it.

## ai-docs

`ai-docs/` holds the record in the everlast layout (INDEX, HANDOFF, log, decisions, plans, notes); keep it current at every stop.
