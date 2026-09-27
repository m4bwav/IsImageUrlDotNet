# Handoff

<!-- Keep under 50 lines. Replace, never append. Written at the end of a work session so the next one starts without re-deriving state. -->

## Current state (2026-09-27)
- **IsImageUrlDotNet 2.0.0 is released and verified.** nuget.org lists 1.0.0, 1.0.2, 2.0.0-beta.1 and 2.0.0 (with icon). Release run 36350568172 (approved by Mark), GitHub Release v2.0.0 with nupkg and snupkg, verify-published run 36352902029 green on Ubuntu, Windows (net48 too) and macOS; the published README's three badges check clean.
- The modernization plan (`ai-docs/plans/2026-09-27-modernization-and-v2-release.md`) is complete. Package validation now compares every pack with the published 2.0.0.
- Repository: rulesets `master` (24084842, required check `ci`, admin bypass) and `Tags only by admins` (24084845); secret scanning, push protection, private vulnerability reporting on; workflow permissions read.

## Owed by Mark (UI or decisions only)
- Deprecate 1.0.0 and 1.0.2 on nuget.org (D15): reason Legacy, alternate package IsImageUrlDotNet 2.0.0, message "1.x targets .NET Framework 4.5 and does not declare its FSharp.Core dependency; use 2.x". Not yet confirmed done.
- Whether to delete the branch `scratch/golden-capture-1.0.2` (throwaway capture workflow; unanswered). The Travis webhook 83050297 stays by his decision.

## Standing work
- Dependabot (weekly, Mondays, 3-day cooldown): merge when `ci` is green; read release notes for majors. FSharp.Core is ignored on purpose (6.0.7 is the declared floor).
- Fantomas and the F# analyzers are pinned in `dotnet-tools.json` and the library's `PackageDownload` items; bump the analyzers with the SDK (the tool must match the compiler).
- .NET 11 GA (expected November 2026): consider adding `net11.0` when it is current; .NET 8 and 9 leave support 2026-11-10 (not targeted).
- A 2.x release: PR with `<Version>` and a dated `## [x.y.z] - date` changelog heading; merge; `ci` green on master; tag `vx.y.z` on that commit; approve the `nuget` environment; run verify-published.
- `tests/Golden/` never changes; the golden test stays alone in `tests/IsImageUrlDotNet.GoldenTests`.

## Traps
- Windows Git Bash: `dotnet nuget add source` refuses `/d/...` and `D:/...`; pass `cygpath -w`. Use a nuget.config, not two `--source` flags.
- Commit before a canary; revert with `git checkout -- <file>` only then.

## Next single action
None for agents; Mark's deprecation of 1.x on nuget.org.
