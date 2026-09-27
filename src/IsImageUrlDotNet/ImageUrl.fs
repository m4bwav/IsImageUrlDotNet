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

    static let maxRedirects = 10

    // One shared client for callers who pass none: HttpClient is meant to be reused, and the 10-second timeout keeps
    // a slow server from holding a caller for HttpClient's default 100 seconds. It follows no redirect by itself:
    // ask follows them, http and https only, so a Location such as file: or ftp: is never fetched.
    static let shared =
        lazy
            (let handler =
#if NET
                new SocketsHttpHandler(AllowAutoRedirect = false, PooledConnectionLifetime = TimeSpan.FromMinutes 5.0)
#else
                new HttpClientHandler(AllowAutoRedirect = false)
#endif
             let client = new HttpClient(handler, Timeout = TimeSpan.FromSeconds 10.0)

             client.DefaultRequestHeaders.UserAgent.ParseAdd(
                 "IsImageUrlDotNet/2.0 (+https://github.com/m4bwav/IsImageUrlDotNet)"
             )

             client)

    static let tryAbsolute (url: string) =
        match Uri.TryCreate(url, UriKind.Absolute) with
        | true, uri ->
            match uri with
            | null -> None
            | uri -> Some uri
        | false, _ -> None

    static let isHttpScheme (uri: Uri) =
        uri.Scheme = Uri.UriSchemeHttp || uri.Scheme = Uri.UriSchemeHttps

    /// The scheme a string starts with ("http" in "http://x"), by RFC 3986's syntax, or None.
    static let schemeOf (url: string) =
        let colon = url.IndexOf ':'

        let isSchemeChar (c: char) =
            (c >= 'a' && c <= 'z')
            || (c >= 'A' && c <= 'Z')
            || (c >= '0' && c <= '9')
            || c = '+'
            || c = '-'
            || c = '.'

        if
            colon > 0
            && Char.IsLetter url.[0]
            && url.[0] < char 128
            && Seq.forall isSchemeChar (url.Substring(0, colon))
        then
            Some(url.Substring(0, colon))
        else
            None

    static let beforeQuery (url: string) =
        let cut = url.IndexOfAny [| '?'; '#' |]
        if cut < 0 then url else url.Substring(0, cut)

    /// The path to take the extension from: an http or https URL's path, a protocol-relative URL's path ("//host/a.png"
    /// read as http), or a relative string before any query or fragment. None for any other scheme.
    static let pathOf (url: string) =
        if url.StartsWith("//", StringComparison.Ordinal) then
            match tryAbsolute ("http:" + url) with
            | Some uri -> Some uri.AbsolutePath
            | None -> Some(beforeQuery url)
        else
            match schemeOf url with
            | Some scheme when
                scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                ->
                match tryAbsolute url with
                | Some uri -> Some uri.AbsolutePath
                | None -> Some(beforeQuery url)
            | Some _ -> None
            | None -> Some(beforeQuery url)

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

    /// The next URL of a redirect answer, when it is one ask may follow: http or https, and never https to http.
    static let nextHop (current: Uri) (response: HttpResponseMessage) =
        let code = int response.StatusCode

        match response.Headers.Location with
        | null -> None
        | location when code >= 300 && code < 400 ->
            match Uri.TryCreate(current, location) with
            | true, next ->
                match next with
                | null -> None
                | next when not (isHttpScheme next) -> None
                | next when current.Scheme = Uri.UriSchemeHttps && next.Scheme = Uri.UriSchemeHttp -> None
                | next -> Some next
            | false, _ -> None
        | _ -> None

    static let isImageAnswer (response: HttpResponseMessage) =
        if not response.IsSuccessStatusCode then
            false
        else
            match response.Content.Headers.ContentType with
            | null -> false
            | contentType ->
                match contentType.MediaType with
                | null -> false
                | mediaType -> mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)

    static let ask (client: HttpClient) (url: string | null) (cancellationToken: CancellationToken) =
        match isHttp url with
        | None -> Task.FromResult false
        | Some uri when hasImageExtension uri.AbsoluteUri -> Task.FromResult true
        | Some uri ->
            task {
                let mutable current = uri
                let mutable hops = 0
                let mutable answer = None

                while answer.IsNone do
                    use request = new HttpRequestMessage(HttpMethod.Get, current)

                    // A caller's client may follow a redirect to a scheme it cannot fetch (a file: or ftp: Location on
                    // .NET 5+ throws UriFormatException or NotSupportedException); that is a "no", not a failure.
                    let! response =
                        task {
                            try
                                let! r =
                                    client.SendAsync(
                                        request,
                                        HttpCompletionOption.ResponseHeadersRead,
                                        cancellationToken
                                    )

                                return Some r
                            with
                            | :? UriFormatException
                            | :? NotSupportedException -> return None
                        }

                    match response with
                    | None -> answer <- Some false
                    | Some response ->
                        use response = response

                        match nextHop current response with
                        | Some next when hops < maxRedirects ->
                            hops <- hops + 1
                            current <- next
                        | Some _ -> answer <- Some false
                        | None -> answer <- Some(isImageAnswer response)

                return Option.defaultValue false answer
            }

    /// The extensions HasImageExtension accepts, lower case: 1.0.2's eight plus webp, avif, apng, ico, tif, tiff,
    /// heic and heif.
    static member ImageExtensions = extensions

    /// True when the path of the URL (not its query or fragment) ends in an image extension, compared without regard
    /// to case or culture. Offline; never throws. A string with a scheme counts only for http and https; a
    /// protocol-relative "//host/a.png" is read as http; any other string, such as "photos/cat.PNG?w=1" or
    /// "/images/a.png", is read as a path, the same on every OS.
    [<Extension>]
    static member HasImageExtension(url: string | null) : bool = hasImageExtension url

    /// True when the http or https URL has an image extension, or else when a GET answers with a 2xx status and an
    /// image/* media type. Reads only the response headers and follows up to 10 redirects, http and https only (never
    /// https to http). False for other schemes, malformed URLs and every other answer; throws HttpRequestException
    /// when the server cannot be reached and TaskCanceledException on a timeout (10 seconds) or cancellation. Uses a
    /// shared HttpClient that sends a User-Agent naming this package.
    [<Extension>]
    static member IsImageUrlAsync(url: string | null) : Task<bool> =
        ask shared.Value url CancellationToken.None

    /// As IsImageUrlAsync(url), cancellable.
    [<Extension>]
    static member IsImageUrlAsync(url: string | null, cancellationToken: CancellationToken) : Task<bool> =
        ask shared.Value url cancellationToken

    /// As IsImageUrlAsync(url), through the caller's HttpClient: its handler, proxy, timeout and headers. When its
    /// handler follows redirects itself, it applies its own rules; one that does not (AllowAutoRedirect false) gets
    /// the rules above. A server that checks untrusted URLs should pass a client whose handler refuses private
    /// addresses. Throws ArgumentNullException for a null client.
    [<Extension>]
    static member IsImageUrlAsync(url: string | null, httpClient: HttpClient) : Task<bool> =
        if obj.ReferenceEquals(httpClient, null) then
            nullArg (nameof httpClient)

        ask httpClient url CancellationToken.None

    /// As IsImageUrlAsync(url, httpClient), cancellable.
    [<Extension>]
    static member IsImageUrlAsync
        (url: string | null, httpClient: HttpClient, cancellationToken: CancellationToken)
        : Task<bool> =
        if obj.ReferenceEquals(httpClient, null) then
            nullArg (nameof httpClient)

        ask httpClient url cancellationToken
