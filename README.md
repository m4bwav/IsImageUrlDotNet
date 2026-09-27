# IsImageUrlDotNet

[![NuGet](https://img.shields.io/nuget/v/IsImageUrlDotNet)](https://www.nuget.org/packages/IsImageUrlDotNet)
[![ci](https://github.com/m4bwav/IsImageUrlDotNet/actions/workflows/ci.yml/badge.svg)](https://github.com/m4bwav/IsImageUrlDotNet/actions/workflows/ci.yml)
[![Downloads](https://img.shields.io/nuget/dt/IsImageUrlDotNet)](https://www.nuget.org/packages/IsImageUrlDotNet)

Tells whether a URL points at an image. Ask offline, from the extension of the URL's path, or ask the server for the media type it answers with. Written in F#; works the same from C# and F#. Builds for netstandard2.0 and net10.0, so it runs on .NET Framework 4.6.2 and later and on every current .NET, on Windows, macOS and Linux.

```
dotnet add package IsImageUrlDotNet
```

## Usage

C#:

```csharp
using IsImageUrlDotNet;

"https://example.com/photos/cat.PNG?w=200".HasImageExtension();   // true, offline
"https://example.com/report.pdf".HasImageExtension();              // false

bool isImage = await "https://example.com/avatar".IsImageUrlAsync(); // asks the server
bool viaMine = await ImageUrl.IsImageUrlAsync(url, myHttpClient, cancellationToken);
```

F#:

```fsharp
open IsImageUrlDotNet

ImageUrl.HasImageExtension "https://example.com/photos/cat.PNG?w=200"   // true
let! isImage = ImageUrl.IsImageUrlAsync("https://example.com/avatar")    // inside task { }
```

## API

| Member | What it does |
|---|---|
| `ImageUrl.HasImageExtension(url)` | True when the path of the URL ends in an image extension (`ImageUrl.ImageExtensions`), without regard to case or culture. The query and fragment are ignored. Offline; never throws; false for null, blank, and absolute URLs that are not http or https. A relative string such as `photos/cat.png?w=1` is read as a path. |
| `ImageUrl.IsImageUrlAsync(url)` and overloads with an `HttpClient` and a `CancellationToken` | For http and https URLs only: true when the extension is an image one; otherwise sends a GET, reads only the response headers, and answers true for a 2xx status with an `image/*` media type. False for other schemes, malformed URLs, other statuses and other media types. Throws `HttpRequestException` when the server cannot be reached, `TaskCanceledException` on timeout or cancellation, and `ArgumentNullException` for a null `HttpClient`. Without a client of yours it uses one shared `HttpClient` with a 10-second timeout. |
| `ImageUrl.ImageExtensions` | The 16 extensions `HasImageExtension` accepts: png, jpg, jpeg, gif, raw, bmp, svg, psd, webp, avif, apng, ico, tif, tiff, heic, heif. |
| `IsImageUrlDotNetLib.IsImageUrl(url)`, `url.IsImageUrl()` | **Obsolete; kept exactly as 1.0.2 had it.** See below. |
| `IsImageUrlDotNetLib.ImageFileExtensions`, `NonImageFileExtensions` | 1.0.2's two F# lists, eight values each, unchanged. |

## Moving from 1.x

`url.IsImageUrl()` still compiles and gives 1.0.2's answers on each runtime, bugs included. The build warns that it is obsolete. Every case recorded from the published 1.0.2 (`tests/Golden/`) is checked on every build. Those answers include:

- It decides by the text after the last dot of the whole string, so `a.png?size=1` sends a request.
- It throws for many inputs it cannot decide (`"a"`, `"x.webp"`, unknown schemes, every 4xx and 5xx answer).
- It matches the Content-Type as a case-sensitive substring (`IMAGE/PNG` is false, `application/x-imagefile` is true).
- It reads local files for `file:` URLs, and on Linux and macOS for rooted paths.
- It follows redirects to any host and waits up to 100 seconds.
- It depends on the current culture: under Turkish, `x.GIF` throws.

Use `HasImageExtension` where 1.x was used as an offline check, and `IsImageUrlAsync` where it made a request. The package now declares its FSharp.Core dependency (6.0.7 or later); 1.x needed FSharp.Core without saying so.

## Limits and what this package is not

- The extension check is a guess from the name. The server check trusts the server's `Content-Type` header and never looks at the bytes.
- `IsImageUrlAsync` fetches whatever http or https URL it is given, following redirects. That is not protection against server-side request forgery. A service that checks URLs from untrusted users should pass its own `HttpClient`, whose handler refuses private and loopback addresses. Never pass such URLs to the obsolete `IsImageUrl`, which also reads local files.
- It does not download images, validate them, or read their dimensions.

## Contributing and support

Issues and pull requests are welcome. Security problems: see [SECURITY.md](SECURITY.md). Changes are listed in [CHANGELOG.md](CHANGELOG.md). MIT licence.
