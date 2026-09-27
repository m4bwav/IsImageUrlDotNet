---
title: "Phase 0 survey: registry, repository, baseline, capture and security of IsImageUrlDotNet 1.0.2"
kind: note
status: active
date: 2026-09-27
verified: 2026-09-27
stale_after: 2027-03-27
tags: [survey, baseline, v2, golden, fsharp, webrequest, security]
aliases: [survey, baseline, capture, FSharp.Core, WebRequest, fixture server, Mono]
summary: "read before the plan or the rewrite: what 1.0.2 is (an F# net45 DLL with an undeclared FSharp.Core 4.4.0.0 dependency, one function that may GET the URL), why the old solution cannot build, the golden capture (117 cases per runtime against a local fixture server, .NET 10 and Mono recorded, .NET Framework pending a Windows runner) and the behaviour it confirmed, no dependents, no security debt in the repository"
---

# Phase 0 survey: IsImageUrlDotNet 1.0.2

## Summary

1.0.2 is one F# function, `IsImageUrlDotNetLib.IsImageUrl(opt: string) : bool`, plus two public F# lists of extensions. It decides by the last dot-separated piece of the lower-cased string, and when that is in neither list it sends a GET through `WebRequest` and looks for "image" in the Content-Type. The capture shows it throws for most inputs it cannot decide, sends requests for URLs with query strings, follows redirects to other hosts, reads local files for `file:` URLs and treats `IMAGE/PNG` as not an image. It has no dependents. Raw survey output: [survey-2026-09-27.md](survey-2026-09-27.md).

## Registry (nuget.org, 2026-09-27)

- Package `IsImageUrlDotNet`, owner `rogersm0`. Two versions, both listed: 1.0.0 (2016-05-30, `lib/net452`, 2 129 downloads) and 1.0.2 (2016-06-05, `lib/net45`, 2 355 downloads); 1.0.1 was never published. 4 484 downloads in total, "per day average 1": mirrors and crawlers, not users.
- The nuspec declares **no dependencies**, but the DLL references `FSharp.Core 4.4.0.0` (monodis `--assemblyref`: mscorlib 4.0.0.0, FSharp.Core 4.4.0.0, System 4.0.0.0). A C# project that does not already have FSharp.Core fails on first use; the new package must declare it.
- Deprecated `licenseUrl`; no readme, icon or repository metadata; the description misspells the call (`.IsImageUl()`). `lib/net45/IsImageUrlDotNet.XML` is empty (no doc comments). SHA-256 of 1.0.2's DLL: `8cf446a8…0454` (the capture's header proves it loaded this file).
- "Used by": no NuGet packages, no popular GitHub repositories (nuget.org page). The search API host (azuresearch-*.nuget.org) is denied by this session's egress policy, so the survey script's search section is empty; the numbers above come from the package page.

## Repository

- m4bwav/IsImageUrlDotNet, public, created 2016-05-29, last push 2016-06-01, language F#, MIT, 1 star, 1 fork. One branch `master`; no tags, releases, issues or pull requests (ever), no workflows.
- Not visible from this session (the admin API needs `gh`, which is not installed, and push access is missing): webhooks, Dependabot alerts, secrets, environments, rulesets, security settings. The survey script prints "(none)" and "(no classic branch protection)" for some of these even when `gh` is missing; those lines are not evidence.
- Two projects in `IsImageUrlDotNet.sln` (VS 2015): `IsImageUrlDotNet.fsproj` (old-style, `TargetFrameworkVersion v4.5.2`, `TargetFSharpCoreVersion 4.4.0.0`, imports VS's `Microsoft.FSharp.Targets`), with `AssemblyInfo.fs` (version 1.0.1, while the package is 1.0.2), `MainCode.fs`, `Script.fsx` and a `.nuspec` of `$tokens$`; `IsImageUrlDotNet.Test.csproj` (C#, MSTest from VS's QualityTools, five tests). The csproj targets 4.5.2 while the published DLL says 4.5 (the release notes: "lowered the .net framework requirement to 4.5"), so the committed project is not what built 1.0.2.
- `.travis.yml` runs `travis-autogen.sh`, `make`, `tests/projects/build.sh` and `tests/fsharp/core/run-opt.sh`, none of which exist: it was copied from the F# compiler's repository and could never pass. Dead service: Travis (no badge; the webhook, if any, needs the maintainer to check).
- README: one C# example, no images or badges (`check-readme-images.mjs README.md --registry nuget`: 0 images, exit 0). No README in the package.
- No credentials in files or history (`git log -p --all`, 16 commits: only public key tokens and the VS .gitignore's comment).

## Baseline (old build and tests as they are)

- `dotnet build IsImageUrlDotNet.sln` on SDK 10.0.112 (Ubuntu's package): MSB4020 (the fsproj's `$(FSharpTargetsPath)` import is empty without Visual Studio) and MSB3644 (no .NET Framework 4.5.2 reference assemblies). Mono 6.8's `xbuild`: the same empty import. The old solution cannot build anywhere current.
- The five old tests need VS's QualityTools MSTest and the internet: google.com (two), placehold.it (an extensionless image) and sanface.com (a PDF). They were not run; this session's egress policy blocks those hosts, and the capture replaces them with fixture routes.

## Golden capture

`tests/Golden/Capture/` (F#: `Capture.fsproj`, `Json.fs`, `FixtureServer.fs`, `Cases.fs`, `Program.fs`), multi-targeting net10.0 and net48, referencing `IsImageUrlDotNet [1.0.2]` and FSharp.Core 10.1.401 (pinned). 1.0.2's `lib/net45` also restores into net10.0 (NU1701) and runs there, so there is one recording per runtime a caller can have.

- The fixture server (`FixtureServer.fs`) is a raw TCP HTTP server on 127.0.0.1 that is also set as `WebRequest.DefaultWebProxy`. Every http case uses a `.test` host (RFC 6761: never resolves), so each request arrives in absolute form at the fixture and nothing can reach the internet even if a runtime bypassed the proxy; https arrives as CONNECT and gets 403; non-HTTP schemes use 127.0.0.1. Each case records the request heads the server saw.
- 117 cases: the two lists and their type, the extension attributes (by reflection), 105 inputs under en-US (null, whitespace, relative names by extension, absolute URLs by extension, query and fragment, 30 fixture routes for content types, status codes and redirects, credentials in the URL, explicit ports, https, loopback, ftp, file, mailto, data, urn, javascript, UNC, paths, spaces, control characters, non-ASCII, emoji, 300 and 70 000 characters) and six under tr-TR.
- Files: `1.0.2.net10.0-linux.json` (.NET 10.0.12 on Ubuntu 24.04; used by the tests) and `1.0.2.mono-6.8-linux.reference.json` (Mono 6.8.0.105, the net48 build; a reference only, not used by tests). Each was run twice, byte-identical, under a second each.
- Pending: `1.0.2.net48-windows.json` (the .NET Framework answer, which is what 1.0.2's callers had) and net10.0 on Windows and macOS, from a throwaway workflow on a windows-latest runner (`scratch/golden-capture-1.0.2`, commit 6f0d945, not yet pushed: this session has no push access to the repository). Current Mono (6.14 at WineHQ) and the Microsoft .NET download hosts are denied by the egress policy; Mono 6.8 and .NET SDK 10.0.112 came from Ubuntu's archive.

### What the capture shows (net10.0; Mono agrees unless noted)

1. Decides by the text after the last `.` of the whole lower-cased string, not the URL's path: `http://fixture.test/a.png?size=1`, `...a.png#top`, `...a.pdf?x=1`, a trailing dot, `dir.png/file` and unknown extensions (`photo.webp`) all send a GET; each of those routes is a 404 here, so each throws `WebException`.
2. Relative strings with no known extension throw `UriFormatException` (`"a"`, `"png"`, `"x.webp"`, `"x.zip"`, `"relative/path"`); known extensions answer without a request (`"x.exe.png"` true, `"x.png.exe"` false, `".png"` true).
3. Content-Type is matched case-sensitively as a substring: `IMAGE/PNG` false, `application/x-imagefile` true, `text/plain; name=image.png` true, any `text/html` false even with "image" in it, no header or an empty one false, 204 with `image/png` true.
4. Any 4xx or 5xx throws `WebException` (ProtocolError), even with an image Content-Type. Redirects are followed, to other hosts too (50 at most; the loop throws after 51 requests); a redirect to https fails at the proxy.
5. Credentials in the URL are not sent (no Authorization header). On .NET 10 loopback URLs also go through the proxy.
6. Non-HTTP schemes: `mailto:`, `data:`, `urn:` and `javascript:` throw `NotSupportedException`; `ftp://` tries to connect; `file:` URLs and, on Linux, rooted paths (`/nonexistent/...`) and UNC strings are **read from the local disk** (an existing file answers false, a missing one throws). `C:\temp\x.png` answers false (not well-formed).
7. Whitespace: a leading space is fine (true); a trailing space or newline is trimmed by `Uri` but not by the extension check, so it sends a request. A space inside, `|`, `<`, `"` and emoji: false (not well-formed); non-ASCII letters and percent-escapes: true.
8. Culture: under tr-TR, `"x.GIF"` throws and `http://fixture.test/A.GIF` sends a request (ToLower maps I to dotless ı); en-US answers true without a request.
9. Length: a 70 000-character `.png` URL answers true on .NET 10 and **false on Mono**; the extensionless one sends a request on .NET 10 and answers false on Mono.
10. The type holds the C# extension attributes (class and method), so C# can write `url.IsImageUrl()`; F# cannot (F# reads the module signature), and the capture had to drop that case.
11. Mono differs from .NET 10 in 12 results and 48 request lists (Mono sends no `Connection: Keep-Alive` header, gives up on a dropped connection after one request where .NET 10 sends four, and words exceptions differently: "Max. redirections exceeded", "No Location header found for 302"). Whether Mono stands in for .NET Framework is what the Windows capture will answer.

### Claims checked

- F#: confirmed (and C# callers are the audience the README addresses).
- "1.0.2": confirmed; 1.0.1 does not exist on nuget.org and the repository's AssemblyInfo says 1.0.1.
- 4 484 downloads: confirmed; no dependents.
- "Probably makes HTTP requests": confirmed, and more often than the README suggests (see 1).
- The old README example calls google.com, which the new tests must not.

## F# specifics learned so far

- The SDK's implicit FSharp.Core reference follows the SDK's feature band: SDK 10.0.112 resolves FSharp.Core 10.0.112, SDK 10.0.4xx resolves 10.1.4xx. A library that keeps the implicit reference makes its FSharp.Core floor depend on the build machine; pin it (`DisableImplicitFSharpCoreReference` plus an explicit `PackageReference`).
- FSharp.Core 11.0.100 is in the flat container but unlisted (registration date 1900-01-01); the latest listed is 10.1.401 (2026-09-08).
- F# ignores `[<Extension>]` members of F# assemblies; only C# sees the extension form.
- `monodis` from Mono is a quick way to read an old assembly's references on Linux (it segfaulted on the full disassembly but printed the tables first).
