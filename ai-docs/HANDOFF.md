# Handoff

<!-- Keep under 50 lines. Replace, never append. Written at the end of a work session so the next one starts without re-deriving state. -->

## Current state
- Phase 1 reached 2026-09-27: stopped at the plan review. Plan `ai-docs/plans/2026-09-27-modernization-and-v2-release.md` (D1-D17), decision `ai-docs/decisions/2026-09-27-v2-keeps-isimageurl-exact-new-imageurl-type.md` (proposed).
- Phase 0 done except three recordings: capture program 4160a65; `tests/Golden/1.0.2.net10.0-linux.json` and the Mono 6.8 reference in 57cf1aa. From 57cf1aa on, `tests/Golden/` never changes (new recordings are added as new files by the same program).
- Nothing is pushed: `git push` to m4bwav/IsImageUrlDotNet returns 403 ("Claude doesn't have GitHub access"). Local commits on `claude/cool-ptolemy-acnzeg`; `scratch/golden-capture-1.0.2` (adbf267) holds the throwaway workflow for the Windows and macOS captures.

## Waiting on Mark
- Push access: install the Claude GitHub App on m4bwav/IsImageUrlDotNet or reconnect GitHub (claude.ai/connect-github), then attach the repository to the session.
- Rulings on D1-D17 (silence: the recommendations stand).
- The nuget.org Trusted Publishing policy: owner m4bwav, repository IsImageUrlDotNet, workflow release.yml, environment nuget, "push only new package versions", glob IsImageUrlDotNet.
- D16 settings need gh or admin rights (this session has neither): environment `nuget` (required reviewer m4bwav, deployment tag rule `v*`, secret NUGET_USER = rogersm0), rulesets, scanning, private reporting, workflow permissions read. Also: is there a Travis webhook or app on the repository?

## Next steps once access exists
1. `git push -u origin scratch/golden-capture-1.0.2`; wait for the golden-capture run; download the artifacts (or decode the BEGIN/END blocks in the log); check each header's `assemblySha256` is 8cf446a8...0454; commit `1.0.2.net48-windows.json`, `1.0.2.net10.0-windows.json`, `1.0.2.net10.0-macos.json` to `tests/Golden/` on the working branch; compare net10.0-linux with the CI Linux copy (expected identical).
2. Push `claude/cool-ptolemy-acnzeg`; Phase 2 per the plan.

## Environment notes (cloud session)
- .NET SDK 10.0.112 and Mono 6.8 come from Ubuntu's archive (`apt-get install dotnet-sdk-10.0 mono-complete`); Microsoft's and WineHQ's download hosts are blocked.
- No gh, no everlast.py: ai-docs/INDEX.md is hand-written in the generated format.

## Next single action
Mark: grant push access, rule on the plan, add the Trusted Publishing policy.
