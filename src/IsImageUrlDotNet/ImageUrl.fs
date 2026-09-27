namespace IsImageUrlDotNet

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Net.Http
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks

/// Tells whether a URL points at an image: by the extension of its path, offline, or by asking the server.
/// Never reads local files, never throws for a malformed URL, and ignores the current culture.
[<AbstractClass; Sealed; Extension>]
type ImageUrl =
    static let extensions: IReadOnlyList<string> =
        ReadOnlyCollection<string>(
            [|
                "png"
                "jpg"
                "jpeg"
                "gif"
                "raw"
                "bmp"
                "svg"
                "psd"
                "webp"
                "avif"
                "apng"
                "ico"
                "tif"
                "tiff"
                "heic"
                "heif"
            |]
        )

    static let extensionSet = HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase)

    // One shared client for callers who pass none: HttpClient is meant to be reused, and the 10-second timeout keeps
    // a slow server from holding a caller for HttpClient's default 100 seconds.
    static let shared = lazy (new HttpClient(Timeout = TimeSpan.FromSeconds 10.0))

    /// The path of an absolute http or https URL, or the part of a relative string before any query or fragment.
    static let tryAbsolute (url: string) =
        match Uri.TryCreate(url, UriKind.Absolute) with
        | true, uri ->
            match uri with
            | null -> None
            | uri -> Some uri
        | false, _ -> None

    static let isHttpScheme (uri: Uri) =
        uri.Scheme = Uri.UriSchemeHttp || uri.Scheme = Uri.UriSchemeHttps

    static let pathOf (url: string) =
        match tryAbsolute url with
        | Some uri when isHttpScheme uri -> Some uri.AbsolutePath
        | Some _ -> None
        | None ->
            let cut = url.IndexOfAny [| '?'; '#' |]
            Some(if cut < 0 then url else url.Substring(0, cut))

    static let extensionOf (path: string) =
        let name = path.Substring(path.LastIndexOf '/' + 1)
        let dot = name.LastIndexOf '.'

        if dot < 0 || dot = name.Length - 1 then
            None
        else
            Some(name.Substring(dot + 1))

    static let hasImageExtension (url: string | null) =
        match url with
        | null -> false
        | url when String.IsNullOrWhiteSpace url -> false
        | url ->
            match pathOf (url.Trim()) |> Option.bind extensionOf with
            | Some extension -> extensionSet.Contains extension
            | None -> false

    static let isHttp (url: string | null) =
        match url with
        | null -> None
        | url ->
            match tryAbsolute (url.Trim()) with
            | Some uri when isHttpScheme uri -> Some uri
            | _ -> None

    static let ask (client: HttpClient) (url: string | null) (cancellationToken: CancellationToken) =
        match isHttp url with
        | None -> Task.FromResult false
        | Some uri when hasImageExtension uri.AbsoluteUri -> Task.FromResult true
        | Some uri ->
            task {
                use request = new HttpRequestMessage(HttpMethod.Get, uri)

                use! response =
                    client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)

                if not response.IsSuccessStatusCode then
                    return false
                else
                    match response.Content.Headers.ContentType with
                    | null -> return false
                    | contentType ->
                        match contentType.MediaType with
                        | null -> return false
                        | mediaType -> return mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            }

    /// The extensions HasImageExtension accepts, lower case: 1.0.2's eight plus webp, avif, apng, ico, tif, tiff,
    /// heic and heif.
    static member ImageExtensions = extensions

    /// True when the path of the URL (not its query or fragment) ends in an image extension, compared without regard
    /// to case or culture. Offline; never throws. Absolute URLs count only with the http or https scheme; a relative
    /// string such as "photos/cat.PNG?w=1" is read as a path.
    [<Extension>]
    static member HasImageExtension(url: string | null) : bool = hasImageExtension url

    /// True when the http or https URL has an image extension, or else when a GET answers with a 2xx status and an
    /// image/* media type. Reads only the response headers. False for other schemes, malformed URLs and every other
    /// answer; throws HttpRequestException when the server cannot be reached and TaskCanceledException on a timeout
    /// (10 seconds) or cancellation. Uses a shared HttpClient.
    [<Extension>]
    static member IsImageUrlAsync(url: string | null) : Task<bool> =
        ask shared.Value url CancellationToken.None

    /// As IsImageUrlAsync(url), cancellable.
    [<Extension>]
    static member IsImageUrlAsync(url: string | null, cancellationToken: CancellationToken) : Task<bool> =
        ask shared.Value url cancellationToken

    /// As IsImageUrlAsync(url), through the caller's HttpClient (its handler, proxy, timeout and redirect rules).
    /// A server that checks untrusted URLs should pass a client whose handler refuses private addresses.
    [<Extension>]
    static member IsImageUrlAsync(url: string | null, httpClient: HttpClient | null) : Task<bool> =
        match httpClient with
        | null -> nullArg (nameof httpClient)
        | httpClient -> ask httpClient url CancellationToken.None

    /// As IsImageUrlAsync(url, httpClient), cancellable.
    [<Extension>]
    static member IsImageUrlAsync
        (url: string | null, httpClient: HttpClient | null, cancellationToken: CancellationToken)
        : Task<bool> =
        match httpClient with
        | null -> nullArg (nameof httpClient)
        | httpClient -> ask httpClient url cancellationToken
