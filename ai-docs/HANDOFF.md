# Handoff

<!-- Keep under 50 lines. Replace, never append. Written at the end of a work session so the next one starts without re-deriving state. -->

## Current state (2026-09-27)
- Phase 5 stop: `v2.0.0-beta.1` tagged on 06b4aa4 (master, ci run 36349282496 green). Release run 36349426955: build, Windows tests and attestation passed; the push job waits at the `nuget` environment for Mark's approval.
- Pull request #1 merged 2026-09-27T20:08:58Z as merge commit e9e1eb7 (not a squash). Dependabot #2 (SDK 10.0.401) and #3 (version 2.0.0-beta.1) squash-merged.
- Phase 4 done: rulesets `master` (id 24084842: no deletion, no force push, required check `ci`, admin bypass) and `Tags only by admins` (24084845); secret scanning and push protection on; private vulnerability reporting on; Dependabot alerts on (0 open); workflow permissions read; homepage the nuget.org page; delete-branch-on-merge on.
- Mark: ignore the Travis webhook (83050297 stays). The branch `scratch/golden-capture-1.0.2` still exists (no OK to delete yet).

## Waiting on Mark
- Approve the deployment: https://github.com/m4bwav/IsImageUrlDotNet/actions/runs/36349426955 (Review deployments).
- OK or not to delete `scratch/golden-capture-1.0.2`.

## Next steps
1. After the approval: wait for the push job and the GitHub Release job; run `gh workflow run verify-published.yml -R m4bwav/IsImageUrlDotNet -f version=2.0.0-beta.1` and watch it (both nuget.org indexes, signature, C# and F# consumers on three OSes, net48 on Windows).
2. Phase 6: PR setting `<Version>2.0.0</Version>` and dating `## [2.0.0] - YYYY-MM-DD`; merge; ci green on master; tag `v2.0.0`; stop for approval; verify-published 2.0.0; check the GitHub Release.
3. Mark deprecates 1.0.0 and 1.0.2 on nuget.org (D15: Legacy, alternate IsImageUrlDotNet 2.0.0, message in the plan).
4. Phase 7: `PackageValidationBaselineVersion` 2.0.0; HANDOFF around standing work; inventory row; lessons; kickoff corrections.

## Traps
- Windows Git Bash: `dotnet nuget add source` refuses `/d/...` and `D:/...`; pass `cygpath -w`. Use a nuget.config, not two `--source` flags.
- The golden test must stay alone in its project (fresh process on .NET Framework).
- Commit before a canary; revert with `git checkout -- <file>` only then.

## Next single action
Mark: approve release run 36349426955 in the browser.
