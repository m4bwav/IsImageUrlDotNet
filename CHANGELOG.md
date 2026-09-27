# Changelog

All notable changes to IsImageUrlDotNet. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [2.0.0]

**Compatibility promise.** `IsImageUrlDotNetLib.IsImageUrl` and the lists `ImageFileExtensions` and `NonImageFileExtensions` give 1.0.2's answers, with the same requests, on each runtime a caller can have. The check covers every case recorded from the published 1.0.2 on .NET Framework 4.8 (Windows) and on .NET 10 (Windows, Linux and macOS), 117 cases per runtime in `tests/Golden/`. There are no exceptions. The namespace, module, parameter name `opt` and the C# extension form `url.IsImageUrl()` stay as they were. Every fix is under a new name, in the new `ImageUrl` type.

### Added

- `ImageUrl.HasImageExtension(url)`: an offline check of the extension of the URL's path (query and fragment ignored), ordinal and case-insensitive, that never throws and never reads local files.
- `ImageUrl.IsImageUrlAsync(url)`, with overloads taking an `HttpClient` and a `CancellationToken`: http and https only; a GET that reads only the headers; true for a 2xx answer with an `image/*` media type; false for anything else the server answers; `HttpRequestException` and `TaskCanceledException` when it cannot ask. The shared client times out after 10 seconds.
- `ImageUrl.ImageExtensions`: 1.0.2's eight image extensions plus webp, avif, apng, ico, tif, tiff, heic and heif.
- Both new methods are C# extension methods too: `url.HasImageExtension()`, `await url.IsImageUrlAsync()`.
- netstandard2.0 and net10.0 builds, Source Link, symbols (snupkg), XML documentation, a README and an icon in the package.

### Changed

- `IsImageUrl` is marked `[Obsolete]` (a warning). Its answers are unchanged.
- The package declares its dependency on FSharp.Core (6.0.7 or later). 1.0.2 referenced FSharp.Core 4.4.0.0 without declaring it, so a C# project without FSharp.Core failed on first use.
- Requires .NET Framework 4.6.2 or later, or .NET Core 2.0 or later (1.0.2 targeted .NET Framework 4.5). .NET Framework 4.5 to 4.6.1 have been out of Microsoft support since 2022, and FSharp.Core 5 and later have no net45 build.
- Licence metadata is an SPDX expression (MIT); the package description no longer misspells the call.

### Removed

- The 2016 Visual Studio projects, the nuspec and the Travis configuration (it could never pass).
- The old MSTest tests, which called google.com and other live sites. The golden, unit, public-API and C# tests replace them and never touch the network.

## [1.0.2] - 2016-06-05

- Lowered the .NET Framework requirement to 4.5.

## [1.0.0] - 2016-05-30

- First release.
