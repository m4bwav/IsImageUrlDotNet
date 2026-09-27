# Handoff

<!-- Keep under 50 lines. Replace, never append. Written at the end of a work session so the next one starts without re-deriving state. -->

## Current state (2026-09-27)
- Stopped at the pull request review (end of Phase 3): https://github.com/m4bwav/IsImageUrlDotNet/pull/1, branch `v2` into `master`, head a524f02 (plus the docs commit after it), CI run 36345678841 green on Ubuntu, Windows (net48 too) and macOS.
- Mark ruled on 2026-09-27: every recommendation D1-D17 stands; the nuget.org Trusted Publishing policy is added (owner m4bwav, repository IsImageUrlDotNet, workflow release.yml, environment nuget, new versions only, glob IsImageUrlDotNet).
- Phase 0 done: golden recordings for net48-windows, net10.0-windows, net10.0-macos (00c5355) and net10.0-linux (57cf1aa). `tests/Golden/` never changes.
- Phase 3 review: 10 findings, all fixed or answered (PR comment); departures from the plan listed in the PR's "For review" (net462 build added to D8's targets, overloads instead of optional parameters, golden test in its own project).
- Done in D16 already: environment `nuget` (required reviewer m4bwav, tag rule `v*`, secret NUGET_USER = rogersm0). Not yet: rulesets, scanning, push protection, private reporting, workflow permissions, homepage.

## Waiting on Mark
- Review of pull request #1 (its "For review" list: net462, redirect behaviour, the golden drive-letter entry, the icon).
- OK to delete the Travis webhook (id 83050297, https://notify.travis-ci.org, active) and the branch `scratch/golden-capture-1.0.2` (throwaway capture workflow, run 36342661062).

## Next steps (Phase 4, after the review)
1. Apply D16 before the merge: ruleset on master (no deletion, no force push, required check `ci`, admin bypass), tag ruleset (admins only), secret scanning and push protection, private vulnerability reporting, workflow permissions read, homepage https://www.nuget.org/packages/IsImageUrlDotNet.
2. Mark squash-merges; read back `gh pr view 1 --json mergeCommit,mergedAt`.
3. With the OK: `gh api -X DELETE repos/m4bwav/IsImageUrlDotNet/hooks/83050297`; `git push origin --delete scratch/golden-capture-1.0.2`.
4. Phase 5: set `<Version>2.0.0-beta.1</Version>` and a `## [2.0.0-beta.1]` changelog section on master, wait for ci green, tag `v2.0.0-beta.1`, stop for Mark's approval, run verify-published.

## Traps
- Windows Git Bash: `dotnet nuget add source` refuses `/d/...` and `D:/...`; pass `cygpath -w`. `dotnet restore --source a --source https://...` read the URL as a local folder on SDK 10.0.401: use a nuget.config.
- The golden test must stay alone in its project (fresh process on .NET Framework).
- A canary before the source is committed: revert with an edit, not `git checkout -- src/`.

## Next single action
Mark: review pull request #1 and answer the two deletions.
