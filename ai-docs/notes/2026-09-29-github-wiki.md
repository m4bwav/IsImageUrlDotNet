---
title: GitHub wiki written for 2.0.0
kind: note
date: 2026-09-29
verified: 2026-09-29
stale_after: 2027-03-29
tags: [wiki, docs, 2.0.0, github, golden, verification]
summary: "the wiki's pages, where their git working copy is, how every example was verified against the published 2.0.0 (a stand-in proxy for .test hosts, net10.0, net48, net8.0, Linux, F#), the golden replay of 1.0.2, the facts found on the way, the inaccuracies in the shipped docs, and how to update the wiki; read before touching the wiki or the doc sentences listed under inaccuracies"
---

# GitHub wiki for 2.0.0

## Summary

Asked for: the GitHub wiki for IsImageUrlDotNet 2.0.0, written with the wikiwright skill (0.6.0), every example verified against the published package with no request leaving the machine, committed in the wiki clone but not pushed. Written: 9 pages plus sidebar and footer, from the README, CHANGELOG, AGENTS.md, ai-docs, the F# source, the tests, the golden capture, the CI workflows, nuget.org and the published nupkg. Wiki commit `737a3d2` (not pushed, so no live check yet). `wikiwright.py check`: 9 pages, 0 errors. `outputs`: 38 checked, 0 missing. everwrite `tells.py`: 0 strong, 2 weak (repeated openings, judged fine). `2026-09-29-snippets-match.py`: 18 of 18 C# and F# blocks on the pages are byte for byte snippets of the program.

Pages: Home, Getting-Started, API-Reference, How-Urls-Are-Checked (the behaviour page), Edge-Cases-and-Errors, Recipes, Versions-and-Upgrading, FAQ, Development, _Sidebar, _Footer.

## Where the pages are

`D:\m4bwa\Claude\Projects\Ai\labs\IsImageUrlDotNet.wiki` (a sibling of this clone, outside this repository), branch `master`, remote `origin` = `https://github.com/m4bwav/IsImageUrlDotNet.wiki.git`. Plain markdown links between pages (`[Recipes](Recipes)`), no wikilinks, LF line endings.

## How it was published

Preflight on 2026-09-29: `placeholder` (Mark had saved the first page; the clone held GitHub's `Home.md`). The pages were committed on top of it as `737a3d2`. Not pushed, by request: `git -C <wiki dir> push` is a plain fast-forward, then `python <wikiwright>/scripts/wikiwright.py live m4bwav/IsImageUrlDotNet <wiki dir>`.

## Updating the wiki later

1. `git -C <wiki dir> pull --ff-only`, then edit the pages.
2. Re-verify: copy `2026-09-29-wiki-verify.cs` to a scratch folder outside the repository (the repository's `Directory.Build.props` would apply inside it), bump `2.0.0` everywhere in it, and run `dotnet run wiki-verify.cs > out.txt` on Windows (it needs the .NET 8 runtime for its net8.0 child and runs net48 on Windows only). Save with LF (`tr -d '\r'`). Then `python <wikiwright>/scripts/wikiwright.py diffout 2026-09-29-wiki-verify.out.txt <new output>`: every difference is a page to fix. For Linux, `2026-09-29-linux-run.sh` (WSL, user-level SDK and ICU in scratch) writes the Linux output and the Linux golden replay.
3. `wikiwright.py outputs <wiki dir> <new Windows output> <new Linux output> <golden outputs> --address ''`, `wikiwright.py check <wiki dir> --version <new>`, `python 2026-09-29-snippets-match.py <wiki dir> <new wiki-verify.cs>`, and the everwrite checker.
4. Commit, `git push`, then `wikiwright.py live`. The pages that name the version: every page's footer (`_Footer.md`), Home (last line and the checking paragraph), Getting-Started (install lines, F# script), API-Reference ("Since" columns), Recipes and FAQ (F# `#r` lines), Versions-and-Upgrading (releases table, side by side, golden replay), Development (none by number).

## How the examples were verified

`2026-09-29-wiki-verify.cs` is a .NET 10 file-based app (`#:package IsImageUrlDotNet@2.0.0`, `#:property PublishAot=false`). It holds every C# and F# snippet on the pages as a string, exactly as shown, and:

- starts a stand-in proxy on 127.0.0.1 that answers `images.test`, `other.test` (plain http) and `secure.test` (CONNECT, then TLS with a throwaway self-signed certificate) from fixed routes, refuses every other host (a reply that is not HTTP, or 403 to CONNECT) and never opens a socket;
- generates one file-based app per target from the C# snippets: net10.0, net48 (`#:property TargetFramework=net48` works in a file-based app and loads the net462 build) and net8.0 (loads the netstandard2.0 build), built without proxy variables so restores reach nuget.org, then runs each snippet in its own process with `HTTP_PROXY`/`HTTPS_PROXY` pointing at the stand-in; .NET Framework ignores those variables, so the generated harness sets `WebRequest.DefaultWebProxy` from `WIKI_NETFX_PROXY` before the snippet runs;
- runs the F# snippets with `dotnet fsi --quiet` under the same variables, after one warm-up restore without them;
- builds a 1.0.2 app (`IsImageUrlDotNet@1.0.2` with FSharp.Core 10.1.401) and one without FSharp.Core, for Versions and upgrading;
- **gate, before any case:** on each route (net10.0, net48, net8.0, 1.0.2, fsi) the shared client, and a caller's `new HttpClient()`, ask `http://gate.invalid/...` and `https://gate.invalid/...`; each must throw and each request must have reached the stand-in, else the program exits 1 with no case run. All passed. A first version that refused by closing the connection showed HttpClient retrying (4 GETs and 16 CONNECTs on .NET 10), so the stand-in now answers with a non-HTTP line or 403.
- prints the harness tables the pages show as text (answers with the request lines the stand-in received, marked per call over a raw socket), the compiler messages of a file that uses `IsImageUrl()` and `IsImageUrlAsync(default)`, the public surface by reflection, and each build's metadata;
- prints the hosts the stand-in was asked for: gate.invalid, images.test, other.test, secure.test. `example.com` appears only in offline calls.

Outputs: `2026-09-29-wiki-verify.out.txt` (Windows) and `2026-09-29-wiki-verify.linux.out.txt` (WSL 2, Ubuntu 26.04.1, .NET 10.0.12, net10.0 only). Two Windows runs were byte-identical after the concurrent recipe's requests were sorted. Windows against Linux (`diffout`): only the socket error text inside the timeout's inner exceptions and fsi's warning path differ. On net48 every C# page example printed the same as net10.0 (with CRLF line endings, normalised); the differences are in the tables and are on the pages: exception messages, timeout messages, the `/close` retry count (2 against 4), header order plus `Proxy-Connection`, and two redirect rows.

Golden replay: `2026-09-29-golden-replay.py` copies `tests/Golden/Capture` into scratch twice, changes only the package version in the 2.0.0 copy (the capture pins `[1.0.2]` and has no property for it), builds each, and runs each target as a child process one after the other, comparing answers and requests apart. Windows net10.0 and net48, 1.0.2 today and 2.0.0 today: 117 of 117 answers and 117 of 117 request lists each (one answer needs the drive letter swap the golden test does, since scratch is on C:). Linux net10.0: 117 of 117 for both. macOS and the Mono reference were not replayed.

The repository's own tests (`dotnet test IsImageUrlDotNet.slnx -c Release`, Windows, 2026-09-29, 16 s): net10.0 and net48 both ran; IsImageUrlDotNet.Tests 103 + 103, CSharpTests 12 + 12, GoldenTests 1 + 1; 232 passed, 0 failed.

Not tested: macOS; Mono; Unity; Xamarin; .NET Framework 4.6.2 to 4.7.2; `dotnet add package` and `PackageReference` as install routes (the directives were used); a handler that refuses private addresses (the README's advice, described but not run); 1.0.2's file reads and 100-second wait (from the README and the recordings).

## Facts verified while writing (not in the README)

- Which build each framework loads: net10.0 gets `lib/net10.0`, net48 gets `lib/net462`, net8.0 gets `lib/netstandard2.0`; the three have the same 9 public methods.
- A C# app with no other F# package gets FSharp.Core 6.0.7 (assembly 6.0.0.0).
- The shared client's User-Agent is `IsImageUrlDotNet/2.0 (+https://github.com/m4bwav/IsImageUrlDotNet)`; a caller's client sends its own headers and none from the package.
- The shared client uses the default proxy: `HTTP_PROXY`/`HTTPS_PROXY` on .NET 10 and 8, `WebRequest.DefaultWebProxy` on .NET Framework.
- `IsImageUrlAsync` is false with no request for relative (`avatar.png`), scheme-less (`images.test/avatar`) and protocol-relative (`//images.test/avatar.png`) input, even where `HasImageExtension` is true.
- An already-cancelled token is ignored when the extension decides: true, no throw.
- A 204 answer with `Content-Type: image/png` is true.
- User name and password in the URL are dropped from the request; no `Authorization` header; the query is kept; the URL is trimmed.
- `HasImageExtension("localhost:8080/a.png")` is false (`localhost:` reads as a scheme).
- `ArgumentNullException` for a null client is thrown by the call, before a task exists; its message differs by runtime.
- On .NET Framework every `HttpRequestException` says only "An error occurred while sending the request." with the detail in an inner `WebException`; timeouts say "A task was canceled." with no inner exception. .NET 10 names the 10-second `HttpClient.Timeout`.
- Through a caller's client that follows redirects itself, the handler's redirects and the package's add up: a redirect loop took 561 requests (51 x 11) on .NET 10 and on .NET Framework.
- .NET Framework's handler with automatic redirects follows https to http (the answer then comes from the http page) and does not follow `ftp:`; .NET 10's handler follows `ftp:` as http and refuses https to http. The package's own rules hold on both when `AllowAutoRedirect = false`.
- A server that closes without answering was asked 4 times on .NET 10 and 8, twice on .NET Framework (handler retries, seen through the proxy).
- F# can call the new methods in extension form (`"x.png".HasImageExtension()`) but not the old `url.IsImageUrl()` (FS0039); the old call warns FS0044 from F# and CS0618 from C#.
- 1.0.2 still restores on .NET 10 (NU1701); without FSharp.Core it fails with `FileNotFoundException` for FSharp.Core 4.4.0.0.

## Inaccuracies found in the shipped docs

None fixed here (README and CHANGELOG ship in the package; AGENTS.md and ai-docs can change any time, on request).

1. AGENTS.md, "What this is": "1.0.2 ... is the published version until 2.0.0 ships." Stale: 2.0.0 was published on 2026-09-27, as the next paragraph says.
2. AGENTS.md, Rules, "The promise (proposed in the plan, D1)... except the exceptions the plan names; the exceptions live in one table". The promise was accepted, and the CHANGELOG says "There are no exceptions": the golden test's one table entry is about the drive letter, not the library.
3. AGENTS.md, Rules: "`release.yml` (to come)". It exists and published 2.0.0.
4. AGENTS.md, Layout and traps: "the ambient culture from the recording (en-US on Windows, invariant on Linux and macOS)". The macOS recording's ambient culture is en-US; only Linux is invariant (the first three cases of `1.0.2.net10.0-macos.json` and `-linux.json`).
5. README, "Moving from 1.x": "Every case recorded from the published 1.0.2 (`tests/Golden/`) is checked on every build." Each test run checks only the recording for its own runtime and OS, and the Mono reference is never checked.
6. README (API table and Limits) and the XML docs of `IsImageUrlAsync(url, httpClient)`: "When its handler follows redirects itself, it applies its own rules." Incomplete: the package then follows any 3xx the handler still returns under its own rules, so the two add up (561 requests for a loop).
7. README (API table) and the XML docs of `IsImageUrlAsync(url)`: `HttpRequestException` "when the server cannot be reached". It is also thrown when a reachable server closes without answering, answers something that is not HTTP, or fails TLS.
8. ai-docs/INDEX.md and the decision `2026-09-27-v2-keeps-isimageurl-exact-new-imageurl-type.md`, title: "on netstandard2.0 and net10.0". The package also has a net462 build (CHANGELOG, nupkg).

## Gotchas

- A stand-in that refuses by closing the connection makes HttpClient retry; answer with a non-HTTP line or a 403 instead.
- The shared client cannot trust a throwaway certificate, so https answers through it are TLS failures; the https redirect rows use a harness client that trusts only the stand-in's certificate, labelled on the page.
- `dotnet fsi` restores `#r "nuget:"` at run time: warm it once without proxy variables, or the restore goes to the stand-in.
- WSL's Ubuntu 26.04 has no libicu, and invariant mode breaks the tr-TR cases: `apt-get download libicu78` plus `dpkg -x` into scratch and `LD_LIBRARY_PATH` work without root.
- Mask a Linux temp path only when it is distinctive: `/tmp` would also hit `file:///tmp/a.png`; `linux-run.sh` sets `TMPDIR` into scratch.

Related: see also [../HANDOFF.md](../HANDOFF.md), [../log.md](../log.md).
