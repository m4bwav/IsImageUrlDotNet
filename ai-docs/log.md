# Log

Append-only. One line per operation: `## [YYYY-MM-DD] op | title` where op is one of add, update, supersede, prune, handoff, index. Newest at the bottom. Never edited, only appended; this is the history the entries themselves do not carry.

## [2026-09-27] init | scaffolded by hand (cloud session; everlast.py is not installed here, so INDEX.md is written by hand in its format)

## [2026-09-27] add | Phase 0: survey, baseline, golden capture
- Environment: Claude Code cloud session on Ubuntu 24.04. .NET SDK 10.0.112 and Mono 6.8.0.105 installed from Ubuntu's archive; builds.dotnet.microsoft.com, download.mono-project.com, dl.winehq.org, gitlab.winehq.org and azuresearch-*.nuget.org are denied by the egress policy; api.nuget.org and www.nuget.org are open. No gh CLI.
- survey-nuget.sh output: ai-docs/notes/survey-2026-09-27.md (registry half fine except the search section; GitHub half failed without gh). Repository facts from the GitHub tools instead: one branch, no tags, issues or pull requests ever, 1 star, 1 fork. Reading: ai-docs/notes/2026-09-27-phase-0-survey-baseline-and-capture.md.
- Registry: 1.0.0 (lib/net452) and 1.0.2 (lib/net45), both listed; 4 484 downloads; no dependents. The nuspec declares no dependency; the DLL references FSharp.Core 4.4.0.0 (monodis --assemblyref).
- Baseline: dotnet build IsImageUrlDotNet.sln fails with MSB4020 (empty FSharpTargetsPath import) and MSB3644 (no v4.5.2 reference assemblies); xbuild on Mono 6.8 fails on the same import. The old MSTest tests call google.com, placehold.it and sanface.com and were not run.
- Golden capture (F#, tests/Golden/Capture, commit 4160a65 for the program): 117 cases against a local fixture server that is also the proxy. net10.0 on Linux and the net48 build under Mono 6.8: each run twice, byte-identical, under a second. 1.0.2's DLL SHA-256 in the header matches the nupkg's. Mono differs from .NET 10 in 12 results and 48 request lists.
- The Windows captures (net48, net10.0) and macOS net10.0 need a GitHub runner: throwaway workflow on scratch/golden-capture-1.0.2 (6f0d945), not pushed: git push returns 403 "Claude doesn't have GitHub access to m4bwav/IsImageUrlDotNet" (package-modernize and package-modernization accept pushes).
- check-readme-images.mjs README.md --registry nuget: 0 images, exit 0. History grep for credentials: none.
- AGENTS.md (from templates/nuget/AGENTS.md), CLAUDE.md and .github/copilot-instructions.md copied from the templates.

## [2026-09-27] add | Phase 1: plan and proposed decision
- ai-docs/plans/2026-09-27-modernization-and-v2-release.md (D1-D17, no exceptions proposed); ai-docs/decisions/2026-09-27-v2-keeps-isimageurl-exact-new-imageurl-type.md (proposed).
- Checked on nuget.org for the plan: FSharp.Core 10.1.401 latest listed (2026-09-08; 11.0.100 is in the flat container but unlisted), 6.0.7 (2022-11-07), 4.7.2 (2020-05-21), no FSharp.Core advisories; NUnit 5.0.0 published 2026-09-27 (cooldown; 4.6.1 used), NUnit3TestAdapter 6.3.0 (2026-08-24), Microsoft.NET.Test.Sdk 18.10.1 (2026-09-15), Fantomas 8.0.5 (2026-09-25, cooldown; 8.0.4 used), xunit.v3 4.0.1, G-Research.FSharp.Analyzers 0.25.0, Ionide.Analyzers 0.19.0, Microsoft.NETFramework.ReferenceAssemblies 1.0.3.
- Probes in a scratch copy: `dotnet format --verify-no-changes` on an fsproj prints "Format currently supports only C# and Visual Basic projects" and exits 0; `dotnet fantomas --check` 8.0.4 exits 99 on unformatted files.
- F# library guidance (github.com/dotnet/fsharp docs/fsharp-core-notes.md): pin the lowest FSharp.Core you need; never bundle it. learn.microsoft.com is blocked here.
- Stop for Mark's plan review.
