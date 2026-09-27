# Handoff

<!-- Keep under 50 lines. Replace, never append. Written at the end of a work session so the next one starts without re-deriving state. -->

## Current state (2026-09-27)
- Phase 6 stop: `v2.0.0` tagged on 9e51d05 (master, ci run 36350423737 green). Release run 36350568172: build, Windows tests and attestation passed; the push job waits at the `nuget` environment for Mark's approval.
- 2.0.0-beta.1 is on nuget.org, verified (verify-published run 36349972948, three OSes), with a GitHub prerelease.
- Phase 4 done (rulesets 24084842 and 24084845, scanning, push protection, private reporting, workflow permissions read, homepage). Travis webhook kept (Mark). `scratch/golden-capture-1.0.2` still exists (no OK to delete).

## Waiting on Mark
- Approve https://github.com/m4bwav/IsImageUrlDotNet/actions/runs/36350568172 (Review deployments).
- Then deprecate 1.0.0 and 1.0.2 on nuget.org (UI): reason Legacy, alternate package IsImageUrlDotNet 2.0.0, message "1.x targets .NET Framework 4.5 and does not declare its FSharp.Core dependency; use 2.x".
- OK or not to delete `scratch/golden-capture-1.0.2`.

## Next steps
1. After the approval: `gh workflow run verify-published.yml -R m4bwav/IsImageUrlDotNet -f version=2.0.0`, watch it; check `gh release view v2.0.0`; run `node <skill>/scripts/check-readme-images.mjs` on the README from the published nupkg (`--registry nuget`).
2. Phase 7: set `<PackageValidationBaselineVersion>2.0.0</PackageValidationBaselineVersion>` in the library project (PR); HANDOFF around standing work (Dependabot merges; target floor when .NET 8 and 9 leave support 2026-11-10 and .NET 11 ships); inventory row and kickoff corrections in package-modernization; lessons in package-modernize (L-077+: e.g. rulesets came after the maintainer's merge).

## Traps
- Windows Git Bash: `dotnet nuget add source` refuses `/d/...` and `D:/...`; pass `cygpath -w`. Use a nuget.config, not two `--source` flags.
- The golden test must stay alone in its project (fresh process on .NET Framework).
- Commit before a canary; revert with `git checkout -- <file>` only then.

## Next single action
Mark: approve release run 36350568172 in the browser.
