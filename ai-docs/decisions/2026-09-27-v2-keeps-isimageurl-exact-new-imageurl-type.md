---
title: "v2 keeps IsImageUrl exact per runtime and puts every fix in a new ImageUrl type, on netstandard2.0 and net10.0 with FSharp.Core pinned at 6.0.7"
kind: decision
status: accepted
date: 2026-09-27
verified: 2026-09-27
stale_after: never
tags: [v2, compatibility, golden, fsharp, fsharp-core, api]
summary: "read before changing what 2.x answers or depends on: IsImageUrl and the two lists keep 1.0.2's answers on each runtime (golden recordings per runtime, no exceptions); query strings, throwing, case-sensitive media types, local-file reads and culture bugs are fixed only in the new ImageUrl type; netstandard2.0 plus net10.0 (net45 to net461 out of support); FSharp.Core pinned at 6.0.7, never the SDK's implicit version. Accepted 2026-09-27: every recommendation stands"
---

# v2 keeps IsImageUrl exact; fixes go in ImageUrl

## Context

1.0.2 has one function and two lists. The capture (117 cases per runtime against a local fixture server) shows seven kinds of surprising behaviour, all listed in the plan: whole-string extension checks, exceptions for undecidable input, case-sensitive substring media types, local-file reads for `file:` URLs, unbounded redirects with a 100-second timeout, culture-sensitive lower-casing, and no modern image formats. It has no dependents and about one download a day. Its answers differ by runtime (.NET Framework, .NET 10, Mono), because `Path`, `Uri` and `WebRequest` differ.

## Decision (accepted 2026-09-27)

- `IsImageUrl`, `ImageFileExtensions` and `NonImageFileExtensions` keep 1.0.2's code path, transport (`WebRequest`) and answers. The golden test compares each runtime with the recording made on that runtime from the published package. No named exceptions.
- The fixes live in a new static class `ImageUrl`: `HasImageExtension`, `IsImageUrlAsync` (http and https only, async, cancellable, a caller's `HttpClient`), `ImageExtensions`. `IsImageUrl` gets `[<Obsolete>]` pointing at it.
- Targets `netstandard2.0;net10.0`; net45 to net461 are dropped (out of Microsoft support; FSharp.Core 5+ has no net45 build).
- FSharp.Core is pinned at 6.0.7 with `DisableImplicitFSharpCoreReference`: the lowest line with `task { }`, as the F# team asks library authors to do; the SDK's implicit reference would tie the floor to the build machine's SDK band.

## Consequences

- 2.0.0 is a major (targets, FSharp.Core floor), with no behaviour change for code that keeps calling `IsImageUrl`.
- The obsolete function keeps its security caveats; the README says so and points at `ImageUrl`.
- A later 3.0.0 may remove `IsImageUrl` or change it to call the new code.

## Alternatives considered

- Fix `IsImageUrl` in place: simpler API, but callers could not tell a fix from a regression, and nothing would prove the rewrite.
- FSharp.Core 4.7.2 (widest for old F# code) or 10.1.401 (latest): see the plan's D5.
