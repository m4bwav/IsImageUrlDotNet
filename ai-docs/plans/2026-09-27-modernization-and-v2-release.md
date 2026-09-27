---
title: Modernization and v2 release
kind: plan
status: active
date: 2026-09-27
verified: 2026-09-27
stale_after: never
tags: [v2, plan, nuget, fsharp, github-actions, tests, release]
summary: "the living plan for IsImageUrlDotNet 2.0.0: survey, what 1.0.2 gets wrong, decisions D1-D17 (IsImageUrl kept exact per runtime, a new ImageUrl type for the fixes, netstandard2.0 and net10.0, FSharp.Core pinned at 6.0.7, Fantomas, NUnit), the v2 API, build and test strategy, phases 0-7 with checkboxes, security, verification checklist"
---

# Modernization and v2.0.0 release plan: IsImageUrlDotNet

The plan for taking the NuGet package IsImageUrlDotNet from 1.0.2 (2016, F#, `lib/net45`) to 2.0.0 with current tooling, released through Trusted Publishing after the maintainer's approval. It follows the package-modernize skill (m4bwav/package-modernize: SKILL.md, references/nuget.md) and the TrailerClipper run (m4bwav/TrailerClipperLib) as the NuGet reference. Evidence goes to `ai-docs/log.md`; the survey reading is `ai-docs/notes/2026-09-27-phase-0-survey-baseline-and-capture.md`.

## Status

Active. 2026-09-27: Mark ruled that every recommendation stands and added the Trusted Publishing policy (D13). Phase 0 finished on Windows (00c5355), Phase 2 on `v2` (fb93673 and later), Phase 3 review under way; stopped at the pull request.

## Goal

- One F# library, `IsImageUrlDotNet` 2.0.0, on `netstandard2.0` and `net10.0`, that declares its FSharp.Core dependency and installs cleanly into C# and F# projects.
- `IsImageUrl` gives 1.0.2's exact answers on each runtime (the golden recordings prove it); the fixes live under new names.
- Built, formatted, tested and packed by current tools; tests never touch the internet.
- Released as 2.0.0-beta.1 and then 2.0.0 through nuget.org Trusted Publishing, each after Mark's approval of the `nuget` environment, and verified from nuget.org.

## Where it stands (survey 2026-09-27)

| Fact | Value | Evidence |
|---|---|---|
| Published version, date, downloads, dependents | 1.0.2 (2016-06-05) and 1.0.0 (2016-05-30), both listed; 4 484 downloads (about 1 a day); no dependents on nuget.org or popular GitHub repositories | survey note |
| Source, build, tests, language level | F#, old-style fsproj (VS 2015, FSharp.Core 4.4.0.0, v4.5.2); C# MSTest project with 5 tests that call google.com, placehold.it and sanface.com | the clone |
| Entry points and how the README calls it | `IsImageUrlDotNetLib.IsImageUrl(opt)` as a C# extension (`url.IsImageUrl()`) or static call; public F# lists `ImageFileExtensions`, `NonImageFileExtensions` | README, capture |
| Runtime dependencies | FSharp.Core 4.4.0.0 referenced but **not declared** in the nuspec | monodis, nuspec |
| Issues, pull requests, forks | none ever; 1 fork, 1 star | GitHub tools |
| Alerts, webhooks, secrets, security features | not visible from this session (needs admin API and gh); to check at Phase 4 | survey note |
| Dead services | Travis: `.travis.yml` copied from the F# compiler repository, runs scripts that do not exist; possible Travis webhook | clone |
| README images and badges | 0 images (`check-readme-images.mjs --registry nuget`, exit 0) | log |
| Leaked credentials | none in files or history | log |
| Baseline | does not build: MSB4020 and MSB3644 on SDK 10.0.112; xbuild on Mono 6.8 fails the same way; old tests not runnable (internet) | log |
| Golden capture | 117 cases per runtime, local fixture server as proxy; .NET 10 on Linux and Mono 6.8 recorded; .NET Framework 4.8 and .NET 10 on Windows and macOS pending a runner | tests/Golden, log |

## What 1.0.2 gets wrong, confirmed, and what v2 does

Every item below stays exactly as it is in `IsImageUrl` (D1, D3); the new `ImageUrl` type (D6) does it right.

1. Decides by the text after the last dot of the whole string, query and fragment included: `http://fixture.test/a.png?size=1` sends a GET. v2 new API: the extension of the URL's path.
2. Throws for most undecidable input: relative strings without a known extension (`"a"`, `"x.webp"`) throw `UriFormatException`; unknown schemes throw `NotSupportedException`; any 4xx or 5xx throws `WebException`. New API: returns false; only transport failures and cancellation throw.
3. Content-Type matched case-sensitively as a substring: `IMAGE/PNG` false, `application/x-imagefile` and `text/plain; name=image.png` true. New API: the media type, case-insensitive, must start with `image/`.
4. Reads the local disk for `file:` URLs, and on Linux for rooted paths and UNC strings. New API: http and https only.
5. Follows redirects to any host (up to 50) with a 100-second default timeout and no cancellation. New API: async, cancellable, a caller-supplied `HttpClient` or a shared one with a 10-second timeout.
6. Culture-sensitive: `ToLower()` under tr-TR turns `x.GIF` into an exception and `A.GIF` into a request. New API: ordinal, case-insensitive.
7. The lists miss today's image formats (webp, avif, ico, tiff, heic): `x.webp` throws, `http://.../photo.webp` sends a request. New API: a new read-only list with those added; the old lists keep their eight values each.
8. Packaging: FSharp.Core undeclared, deprecated `licenseUrl`, the description misspells the call (`.IsImageUl()`), an empty XML doc file, AssemblyInfo says 1.0.1. v2: all fixed (no behaviour involved).

## Decisions (recommendation first; the maintainer rules in the plan review, silence means the recommendation stands)

| # | Question | Recommendation | Why | Alternative |
|---|---|---|---|---|
| D1 | Compatibility promise | `IsImageUrl`, `ImageFileExtensions` and `NonImageFileExtensions` give 1.0.2's exact answers on each runtime: every case of `tests/Golden/1.0.2.<runtime>-<os>.json`, results and the requests sent, with **no exceptions**. v2 keeps 1.0.2's algorithm and its transport (`WebRequest`), so this holds by construction and the golden test proves it. Fixes go under new names (D6); the old names stay exact for the 2.x line | The recordings are the only behaviour record (the old tests need the internet); no dependents means no one needs the old bugs, but the skill's rule costs little here | Fix the old function in place (changes the answer for items 1 to 7; a caller could not tell a bug fix from a regression) |
| D2 | Package shape | One assembly `IsImageUrlDotNet.dll`, namespace `IsImageUrlDotNet`, module `IsImageUrlDotNetLib` (compiled as a static class with the extension attributes), `lib/netstandard2.0` and `lib/net10.0`, README, `PackageLicenseExpression MIT`, Source Link, snupkg, XML docs | C# `using IsImageUrlDotNet;` and `url.IsImageUrl()` keep compiling; F# `open IsImageUrlDotNet` too | Rename (breaks every caller for nothing) |
| D3 | Behaviour at the edges | Keep all of 1.0.2's edges in `IsImageUrl` (items 1 to 7 above) | D1 | Refuse the local-file reads in `IsImageUrl` as a security fix (a named exception E1; say so if you want it) |
| D4 | Major? | Yes, 2.0.0 | `lib/net45` becomes `netstandard2.0` (.NET Framework 4.5 to 4.6.1 can no longer install it) and the declared FSharp.Core 6.0.7 floor moves F# callers off 4.x | 1.0.3 on net45 that only declares FSharp.Core and fixes metadata (leaves the old tooling in place) |
| D5 | Runtime dependencies | Only FSharp.Core, **pinned at 6.0.7** (`DisableImplicitFSharpCoreReference`, explicit `PackageReference`, so the nuspec says `>= 6.0.7`); `System.Net.Http` is in netstandard2.0 | The F# team's guidance: libraries pin the lowest FSharp.Core they need and never bundle it. 6.0.7 (2022-11) is the last 6.0 patch and the first line with the `task { }` builder the async API uses; no FSharp.Core version has an advisory. The SDK's implicit reference would set the floor to whatever the build machine's SDK band ships (10.0.112 here, 10.1.401 on CI) | 4.7.2 (net45 and netstandard2.0; widest for old F# code, but no `task { }`, so hand-written continuations); 10.1.401 (latest listed; forces every F# caller up) |
| D6 | Names | Keep all three old names, the parameter name `opt`, and the extension attributes; mark `IsImageUrl` `[<Obsolete>]` (a warning pointing at the new API). Add a type `ImageUrl` (static class, C#-friendly): `HasImageExtension(url)` (offline, never throws, path extension, ordinal case-insensitive) and `IsImageUrlAsync(url, ?httpClient, ?cancellationToken)` (http and https only; extension first, then a GET that reads only the headers; `image/*` media type on a 2xx). Add `ImageUrl.ImageExtensions` (read-only: the old eight plus webp, avif, apng, ico, tif, tiff, heic, heif) | Items 1 to 7 need a new name; an async, cancellable network call is what .NET callers expect in 2026; a type (not the module) because F# module functions cannot have optional or overloaded parameters | No `[<Obsolete>]` (keeps warning-as-error builds quiet; the README says which to use); or only `HasImageExtension` (no network in v2's new API at all) |
| D7 | Errors | Old: unchanged (D1). New: false for invalid input, non-http schemes, non-2xx and non-image answers; `HttpRequestException` for transport failures, `TaskCanceledException` for timeouts and cancellation; `ArgumentNullException` only for a null `HttpClient` passed explicitly | "Is it an image?" has a false answer for everything the server answered; a caller must be able to tell "not an image" from "could not ask" | Never throw (hides outages as "not an image") |
| D8 | Frameworks and CI matrix | Library `netstandard2.0;net10.0`; tests `net10.0` on Ubuntu, Windows and macOS and `net48` on Windows (runs the netstandard2.0 build); the golden test reads the recording for its runtime and OS | Overlay standard; netstandard2.0 serves .NET Framework 4.6.2+ and every modern .NET. **Dropped: net45 to net461**, named reason: all are out of Microsoft support (4.5.2, 4.6 and 4.6.1 since 2022-04-26, per the .NET Blog; 4.5 and 4.5.1 earlier), and FSharp.Core 5+ has no net45 build | Add `net8.0` (leaves support 2026-11-10; adds nothing here); keep a `net45` target with FSharp.Core 4.7.2 (for runtimes out of support) |
| D9 | Language, build, format, tests | SDK-style fsproj; `LangVersion latest`; `Nullable enable` (F# 9+ nullness); `TreatWarningsAsErrors` with `--warnon:1182,3390`; `GenerateDocumentationFile`; **Fantomas 8.0.4** as a local tool (`dotnet fantomas --check` in CI; 8.0.5 is inside the three-day cooldown); **not `dotnet format`** (on F# it prints "supports only C# and Visual Basic projects" and exits 0, a silent green check); F# analyzers (G-Research.FSharp.Analyzers 0.25.0, Ionide.Analyzers 0.19.0 through the `fsharp-analyzers` tool) tried in Phase 2 and kept only if they run clean against SDK 10's compiler; tests in F# with **NUnit 4.6.1**, NUnit3TestAdapter 6.3.0, Microsoft.NET.Test.Sdk 18.10.1 (NUnit 5.0.0 was published today, inside the cooldown), plus a small C# test project for the extension form; `global.json` 10.0.100 with `latestFeature`; `.slnx` | Current, maintained tools for F#; NUnit runs on net48 and net10.0 alike and matches the TrailerClipper run | xunit.v3 (runs only on Microsoft.Testing.Platform from 4.x) or Expecto (F#-idiomatic, but a second style for the C# tests) |
| D10 | Lock files and bot pull requests | `packages.lock.json` per project, `--locked-mode` in CI; no bot pull requests exist | Defaults | none |
| D11 | Dead services, badges | Remove `.travis.yml`; Mark checks for a Travis webhook and app (D16); README gains three badges: NuGet version, CI, downloads; no other images | Defaults | none |
| D12 | Old files to remove | `IsImageUrlDotNet/IsImageUrlDotNet.fsproj` (replaced by an SDK-style one), `AssemblyInfo.fs`, `Script.fsx`, `IsImageUrlDotNet.nuspec`, `IsImageUrlDotNet.sln` (by `IsImageUrlDotNet.slnx`), `IsImageUrlDotNet.Test/` (MSTest against the internet; replaced by the golden, unit and C# tests), `.travis.yml` | Formats SDK 10 cannot build | Keep the old test project in the solution (cannot build) |
| D13 | Release and version | 2.0.0-beta.1 as the rehearsal, then 2.0.0, from `release.yml` (TrailerClipper's, adapted) gated by the `nuget` environment. **Mark adds the Trusted Publishing policy on nuget.org**: owner `m4bwav`, repository `IsImageUrlDotNet`, workflow `release.yml`, environment `nuget`, scope "push only new package versions", glob `IsImageUrlDotNet` | The human gate; no API key anywhere | Real versions only (the brief asks for a beta first) |
| D14 | Branch, default branch, extras | ~~Work on `claude/cool-ptolemy-acnzeg`~~ Superseded 2026-09-27: the work moved to `v2` (the skill's default) when a Windows session took it over; keep `master`; no benchmark project (no hot path) | Session rule | none |
| D15 | Dependents and 1.x | None to move. After 2.0.0 Mark deprecates 1.0.0 and 1.0.2 on nuget.org (UI only): reason Legacy, alternate package IsImageUrlDotNet 2.0.0, message "1.x targets .NET Framework 4.5 and does not declare its FSharp.Core dependency; use 2.x" | Points the few real downloads at the fix | Unlist (hides history) |
| D16 | Repository settings (Phase 4) | Ruleset on `master` (no deletion, no force push, required check `ci`, admin bypass), tag ruleset admins only, secret scanning, push protection, private vulnerability reporting, workflow permissions read, homepage the nuget.org page; environment `nuget` with Mark as required reviewer, tag rule `v*`, secret `NUGET_USER` = `rogersm0`. This session has no gh and no admin API: either Mark grants push access and a later session with gh applies them, or Mark applies them from the commands in HANDOFF | Skill defaults | none |
| D17 | The golden recordings | One file per runtime and OS, all from the published 1.0.2 by the same program: `net10.0-linux` (done), `net48-windows`, `net10.0-windows`, `net10.0-macos` (throwaway workflow, needs push access). Mono 6.8 kept as a reference only, not a test input: Mono is not .NET Framework, and it disagrees with .NET 10 in 12 results | A caller's answer depends on the runtime (`Path`, `Uri` and `WebRequest` differ), so each runtime is compared with itself | One file from .NET Framework only (the new net10.0 build would then need dozens of runtime exceptions) |

## Proposed public API (v2)

```fsharp
namespace IsImageUrlDotNet

[<Extension>]
module IsImageUrlDotNetLib =
    val ImageFileExtensions: string list            // unchanged, 8 values
    val NonImageFileExtensions: string list         // unchanged, 8 values
    [<Extension; Obsolete("...")>]
    val IsImageUrl: opt: string -> bool             // 1.0.2's answers, exactly

[<AbstractClass; Sealed; Extension>]
type ImageUrl =
    static member ImageExtensions: IReadOnlyList<string>
    [<Extension>] static member HasImageExtension: url: string -> bool
    [<Extension>] static member IsImageUrlAsync:
        url: string * [<Optional>] httpClient: HttpClient * [<Optional>] cancellationToken: CancellationToken -> Task<bool>
```

From C#: `url.IsImageUrl()` (unchanged), `url.HasImageExtension()`, `await url.IsImageUrlAsync()`, `await ImageUrl.IsImageUrlAsync(url, client, token)`. From F#: `IsImageUrlDotNetLib.IsImageUrl url`, `ImageUrl.HasImageExtension url`, `ImageUrl.IsImageUrlAsync(url)`. Trim candidates for the maintainer: `ImageExtensions` as a public list, and the `[<Obsolete>]`.

## Build and package specifics

- `src/IsImageUrlDotNet/IsImageUrlDotNet.fsproj` (SDK-style; files `IsImageUrl.fs` with the 1.0.2 module as it was, `ImageUrl.fs`), `tests/IsImageUrlDotNet.Tests/` (F#, NUnit: golden, unit, public API), `tests/IsImageUrlDotNet.CSharpTests/` (C#, NUnit: extension form, a consumer without its own FSharp.Core reference), `tests/Golden/` unchanged.
- The golden test compiles `tests/Golden/Capture/Json.fs`, `FixtureServer.fs` and `Cases.fs` by link, unchanged, against the new library: the replay asks exactly the capture's questions, with nothing generated (the TrailerClipper run generated its runner from a C# program; here the case list is its own file). The fixture server is the proxy for `WebRequest` and, through the test's `HttpClient`, for the new API.
- `PublicApi-1.0.2.txt`: 1.0.2's public members with parameter names and attributes by reflection from the published DLL; a test that 2.x keeps every line (package validation has no usable baseline: 1.0.2 is net45 only).
- `EnablePackageValidation` on, no baseline for 2.0.0; after the release, baseline 2.0.0.
- `.fantomasignore` excludes `tests/Golden/Capture/` (frozen since Phase 0, not reformatted).
- Directory.Build.props, global.json, .editorconfig, .gitattributes, .gitignore, workflows and Dependabot from `templates/nuget/` and TrailerClipperLib, with the F# changes above.

## Phases

### Phase 0: survey and baseline (2026-09-27, no package code changed)
- [x] Cloned; survey-nuget.sh output in `ai-docs/notes/survey-2026-09-27.md` (GitHub half by the GitHub tools; no gh here)
- [x] Old build run as it is: fails on SDK 10.0.112 and on Mono 6.8's xbuild; old tests need the internet (not run)
- [x] Golden capture from the published 1.0.2: program 4160a65; recordings `1.0.2.net10.0-linux.json` and the Mono reference in 57cf1aa (from here on the capture program and each recording never change)
- [x] Recordings `net48-windows` and `net10.0-windows` captured locally (twice each, identical), `net10.0-macos` from run 36342661062 on `scratch/golden-capture-1.0.2` (adbf267); commit 00c5355, only added files
- [x] ai-docs (by hand; no everlast.py here), AGENTS.md, CLAUDE.md with the `@AGENTS.md` import, Copilot pointer
### Phase 1: plan
- [x] This plan and the decision record. **Stop**: Mark rules on the table; questions: push access, the Trusted Publishing policy, the `nuget` environment and repository settings (D16), deleting the scratch branch afterwards.
### Phase 2: rewrite
- [x] Remove the D12 files; add the templates (F# changes in D9); icon added at Mark's request (2026-09-27)
- [x] Golden test first, green on the first build on every recording; canary: a planted line in the source turns it red, reverted, green (both logged); `git diff --exit-code 57cf1aa -- tests/Golden` empty (and against the later recording commit for its files); then the new API, unit tests, C# tests, README, CHANGELOG, SECURITY.md, AGENTS.md
- [x] Verified on net10.0 and net48 on Windows, and from a fresh clone on drive C: (CI on three OSes: pull request)
- [x] Workflows and Dependabot, actionlint 1.7.12, zizmor 1.30.1 and check-workflow-shell.py clean
- [x] Pushed; pull request with a "For review" list. **Stop.**
### Phase 3: review
- [x] Independent read-only review (prompts/review-subagent.md, NuGet substitutions): 10 findings, all fixed or answered (a524f02, CI 36345678841 green); summary on pull request #1. D8 amended in the PR: a `net462` build joins `netstandard2.0;net10.0` (System.Net.Http for .NET Framework consumers)
### Phase 4: CI, settings, merge, cleanup
- [x] CI green (36345678841); merged by Mark as merge commit e9e1eb7 (2026-09-27T20:08:58Z; the rulesets came after the merge, not before); D16 settings applied; Travis webhook kept (Mark: ignore it); scratch branch still waiting for an OK
### Phase 5: release rehearsal
- [ ] Mark has added the Trusted Publishing policy (D13, done). `v2.0.0-beta.1` tagged on 06b4aa4 after ci 36349282496; release run 36349426955 waits for the approval (**stop**); verify-published run id
### Phase 6: release
- [ ] Changelog dated; `v2.0.0` tagged after green; **stop** for the approval; verify-published; GitHub Release; Mark deprecates 1.x (D15)
### Phase 7: wrap-up
- [ ] HANDOFF around standing work; inventory row; lessons into the skill; what the kickoff got wrong

## Test strategy

| Layer | What it proves | How | Runs where |
|---|---|---|---|
| Golden | 1.0.2's answers and requests, per runtime | `Cases.fs` against the new build, compared case by case as JSON text with the recording for this runtime and OS; one exception table (empty) | net10.0 on 3 OSes, net48 on Windows |
| Canary | the golden test can fail | a planted change in `IsImageUrl`, red, reverted, green | local, logged |
| Unit | the new API: extension rules, media types, status codes, redirects, schemes, cancellation, timeout, null client | NUnit against the fixture server | everywhere |
| Public API | every 1.0.2 member and parameter name remains | `PublicApi-1.0.2.txt` by reflection | everywhere |
| C# consumer | `url.IsImageUrl()` and the new extension forms compile and run; FSharp.Core arrives transitively | C# NUnit project referencing the library | everywhere |
| Package | contents, dependency groups (FSharp.Core >= 6.0.7 in both), README, Source Link | `dotnet pack`, package validation, an unzip check | CI Linux |
| Registry | the published package restores and answers | verify-published: fresh C# and F# consumers on net10.0 (and net48 on Windows) against a local fixture | after each approval |

## Pull requests, issues and forks: disposition

| Item | What it is | Disposition | Comment to post |
|---|---|---|---|
| (none) | no issues or pull requests ever | none | none |
| 1 fork | not inspected (no API for it here) | none unless it carries a pull request | none |
| `scratch/golden-capture-1.0.2` | throwaway capture workflow | delete after the recordings are committed (Mark's OK) | none |

## Security

- The library fetches the caller's URL. `IsImageUrl` (kept exact) follows redirects to any host, reads local files for `file:` URLs (and rooted paths or UNC strings on Linux), and has a 100-second default timeout. The README says so plainly, marks it obsolete, and says `ImageUrl.IsImageUrlAsync` fetches only http and https. Neither is SSRF protection: a server that checks untrusted URLs passes its own `HttpClient` with its own restrictions (a handler that refuses private addresses), and the README says that too.
- No secrets, no leaked credentials, no alerts known; the admin-side checks (webhooks, alerts, scanning) wait for D16.
- Publishing: Trusted Publishing bound to `release.yml` and environment `nuget`, Mark's approval before anything goes live, no stored API key; workflows `permissions: contents: read` by default, `id-token: write` only in the push job (no checkout, no restore), actions pinned to SHAs, `persist-credentials: false`, actionlint clean.
- SECURITY.md with private vulnerability reporting and supported versions (2.x).

## Badges and images: disposition

| Image or badge | What it shows now | Decision | New URL or reason |
|---|---|---|---|
| (none in the old README) | | add three | NuGet version and downloads from img.shields.io, CI from github.com/m4bwav/IsImageUrlDotNet/actions/workflows/ci.yml/badge.svg (both hosts on nuget.org's allow-list) |

## Verification checklist (what "done" means)

| Claim | Command or place | Expected |
|---|---|---|
| Restores clean, locked | `dotnet restore --locked-mode` on Linux, Windows, macOS | no NU1004, no audit warnings |
| Formatted | `dotnet fantomas --check .` (Capture excluded) | exit 0 |
| Builds without warnings | `dotnet build -c Release` | 0 warnings |
| Old behaviour kept | the golden test | every case of every recording |
| Golden untouched | `git diff --exit-code 57cf1aa -- tests/Golden` | empty |
| API kept | PublicApi test; package validation at pack | pass |
| Package contents | unzip the nupkg; nuspec dependency groups | lib/netstandard2.0, lib/net10.0, README; FSharp.Core >= 6.0.7 |
| On nuget.org, listed, signed | flat container and registration index; `dotnet nuget verify` | the version; repository signature |
| Consumable | verify-published fresh consumers | build and answer |
| Release exists | GitHub Release v2.0.0 | notes from the changelog, packages attached |

## Risks and open points

- Push access to this repository is missing (403 from GitHub on `git push`); nothing can be pushed, and the Windows recordings cannot be made, until Mark installs the Claude GitHub App on the repository or reconnects GitHub.
- If `net48-windows` shows that the netstandard2.0 build answers differently from 1.0.2 on .NET Framework in some case, that difference comes back to Mark as a named exception before it is accepted.
- F# analyzers: resolved 2026-09-27, both packs run with SDK 10.0.401 and are in CI (log, Phase 2).
- WebRequest is obsolete on .NET 6+ (SYSLIB0014, a warning, suppressed with a comment in the one file that keeps it); Microsoft has not announced its removal.

## Appendix: cleanup commands

~~~
branch	scratch/golden-capture-1.0.2
~~~

## Next single action

Mark reviews pull request #1 and gives the OK to delete the Travis webhook (83050297) and the branch `scratch/golden-capture-1.0.2`; then Phase 4.
