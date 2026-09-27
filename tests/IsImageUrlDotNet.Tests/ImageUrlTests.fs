/// The new API. Every request goes to the golden fixture server, which is the client's proxy; hosts are .test names,
/// which never resolve, so nothing reaches the internet.
module IsImageUrlDotNet.Tests.ImageUrlTests

open System
open System.Globalization
open System.Net
open System.Net.Http
open System.Net.Sockets
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open IsImageUrlDotNet

[<TestCase("http://fixture.test/a.png", true)>]
[<TestCase("https://fixture.test/dir/A.JPEG", true)>]
[<TestCase("http://fixture.test/a.png?size=1", true)>]
[<TestCase("http://fixture.test/a.png#top", true)>]
[<TestCase("http://fixture.test/a.pdf?x=a.png", false)>]
[<TestCase("http://fixture.test/photo.webp", true)>]
[<TestCase("http://fixture.test/x.avif", true)>]
[<TestCase("http://fixture.test/x.heic", true)>]
[<TestCase("http://fixture.test/%E2%82%AC.png", true)>]
[<TestCase("http://fixture.test/a.png.", false)>]
[<TestCase("http://fixture.test/dir.png/file", false)>]
[<TestCase("http://fixture.test/", false)>]
[<TestCase("http://fixture.test", false)>]
[<TestCase("  http://fixture.test/a.png  ", true)>]
[<TestCase("image.png", true)>]
[<TestCase("photos/cat.PNG?w=1", true)>]
[<TestCase("x.webp", true)>]
[<TestCase("x.exe", false)>]
[<TestCase("x.png.exe", false)>]
[<TestCase("x.exe.png", true)>]
[<TestCase("png", false)>]
[<TestCase(".png", true)>]
[<TestCase("a", false)>]
[<TestCase("", false)>]
[<TestCase(" ", false)>]
[<TestCase(null, false)>]
[<TestCase("file:///tmp/a.png", false)>]
[<TestCase("ftp://fixture.test/a.png", false)>]
[<TestCase("data:image/png;base64,iVBORw0KGgo=", false)>]
[<TestCase("mailto:a@fixture.test", false)>]
[<TestCase("a b.png", true)>]
[<TestCase("http://fixture.test/😀.png", true)>]
let ``HasImageExtension reads the extension of the path`` (url: string, expected: bool) =
    Assert.That(ImageUrl.HasImageExtension url, Is.EqualTo expected)

[<Test>]
let ``HasImageExtension ignores the culture`` () =
    let thread = Thread.CurrentThread
    let old = thread.CurrentCulture
    thread.CurrentCulture <- CultureInfo("tr-TR")

    try
        Assert.That(ImageUrl.HasImageExtension "x.GIF", Is.True)
        Assert.That(ImageUrl.HasImageExtension "http://fixture.test/A.GIF", Is.True)
        Assert.That(ImageUrl.HasImageExtension "x.TIF", Is.True)
    finally
        thread.CurrentCulture <- old

[<Test>]
let ``HasImageExtension never throws on long or odd input`` () =
    Assert.That(ImageUrl.HasImageExtension("http://fixture.test/" + String('a', 70000) + ".png"), Is.True)
    Assert.That(ImageUrl.HasImageExtension(String('.', 70000)), Is.False)
    Assert.That(ImageUrl.HasImageExtension "http://[::1", Is.False)
    Assert.That(ImageUrl.HasImageExtension "\n\t", Is.False)

[<Test>]
let ``ImageExtensions holds the old eight and the new eight, read-only`` () =
    let list = ImageUrl.ImageExtensions
    Assert.That(list.Count, Is.EqualTo 16)

    for old in IsImageUrlDotNetLib.ImageFileExtensions do
        Assert.That(list, Does.Contain old)

    for added in [ "webp"; "avif"; "apng"; "ico"; "tif"; "tiff"; "heic"; "heif" ] do
        Assert.That(list, Does.Contain added)

    match box list with
    | :? System.Collections.Generic.IList<string> as l -> Assert.That(l.IsReadOnly, Is.True)
    | _ -> ()

/// A fixture server plus a client whose proxy is that server.
type private Fixture() =
    let server = new Golden.FixtureServer.Server()

    let handler =
        new HttpClientHandler(Proxy = WebProxy(sprintf "http://127.0.0.1:%d" server.Port), UseProxy = true)

    let client = new HttpClient(handler, Timeout = TimeSpan.FromSeconds 10.0)
    member _.Client = client
    member _.Requests() = server.Take()

    interface IDisposable with
        member _.Dispose() =
            client.Dispose()
            (server :> IDisposable).Dispose()

let private ask (fixture: Fixture) (url: string) =
    ImageUrl.IsImageUrlAsync(url, fixture.Client).GetAwaiter().GetResult()

[<TestCase("image-png", true)>]
[<TestCase("image-jpeg", true)>]
[<TestCase("image-webp", true)>]
[<TestCase("image-svg", true)>]
[<TestCase("image-upper", true)>]
[<TestCase("image-params", true)>]
[<TestCase("html", false)>]
[<TestCase("html-upper", false)>]
[<TestCase("html-and-image", false)>]
[<TestCase("json", false)>]
[<TestCase("octet", false)>]
[<TestCase("pdf", false)>]
[<TestCase("x-imagefile", false)>]
[<TestCase("text-image", false)>]
[<TestCase("no-type", false)>]
[<TestCase("empty-type", false)>]
[<TestCase("not-found-image", false)>]
[<TestCase("server-error", false)>]
[<TestCase("redirect-image", true)>]
[<TestCase("redirect-html", false)>]
[<TestCase("redirect-other-host", true)>]
[<TestCase("redirect-relative", true)>]
[<TestCase("redirect-no-location", false)>]
[<TestCase("no-route", false)>]
let ``IsImageUrlAsync asks the server when the extension does not decide`` (path: string, expected: bool) =
    use fixture = new Fixture()
    Assert.That(ask fixture ("http://fixture.test/" + path), Is.EqualTo expected)
    Assert.That(fixture.Requests(), Is.Not.Empty)

[<Test>]
let ``IsImageUrlAsync sends one GET and no credentials`` () =
    use fixture = new Fixture()
    Assert.That(ask fixture "http://user:secret@fixture.test/image-png", Is.True)

    match fixture.Requests() with
    | [ head ] ->
        Assert.That(head.Head, Is.EqualTo "GET http://fixture.test/image-png HTTP/1.1")

        Assert.That(
            head
            |> List.exists (fun h -> h.StartsWith("Authorization", StringComparison.OrdinalIgnoreCase)),
            Is.False
        )
    | other -> Assert.Fail(sprintf "expected one request, got %A" other)

[<TestCase("http://fixture.test/a.png?size=1")>]
[<TestCase("http://fixture.test/photo.webp")>]
[<TestCase("HTTP://FIXTURE.TEST/A.PNG")>]
let ``IsImageUrlAsync answers image extensions without a request`` (url: string) =
    use fixture = new Fixture()
    Assert.That(ask fixture url, Is.True)
    Assert.That(fixture.Requests(), Is.Empty)

[<TestCase(null)>]
[<TestCase("")>]
[<TestCase("image-png")>]
[<TestCase("relative/path")>]
[<TestCase("ftp://127.0.0.1:1/file")>]
[<TestCase("file:///nonexistent-isimageurl/file")>]
[<TestCase("/nonexistent-isimageurl/file")>]
[<TestCase("//127.0.0.1/share/file")>]
[<TestCase("mailto:someone@fixture.test")>]
[<TestCase("data:image/png;base64,iVBORw0KGgo=")>]
[<TestCase("javascript:alert(1)")>]
let ``IsImageUrlAsync is false for anything but http and https, without a request`` (url: string) =
    use fixture = new Fixture()
    Assert.That(ask fixture url, Is.False)
    Assert.That(fixture.Requests(), Is.Empty)

[<TestCase("https://fixture.test/image-png")>]
[<TestCase("http://fixture.test/close")>]
[<TestCase("http://fixture.test/garbage")>]
let ``IsImageUrlAsync throws HttpRequestException when it cannot ask`` (url: string) =
    use fixture = new Fixture()

    Assert.That(TestDelegate(fun () -> ask fixture url |> ignore), Throws.InstanceOf<HttpRequestException>())

[<Test>]
let ``IsImageUrlAsync throws for an explicit null client`` () =
    Assert.That(
        TestDelegate(fun () ->
            ImageUrl.IsImageUrlAsync("http://fixture.test/image-png", (null: HttpClient))
            |> ignore),
        Throws.TypeOf<ArgumentNullException>()
    )

[<Test>]
let ``IsImageUrlAsync honours cancellation`` () =
    use fixture = new Fixture()
    use cts = new CancellationTokenSource()
    cts.Cancel()

    Assert.That(
        TestDelegate(fun () ->
            ImageUrl
                .IsImageUrlAsync("http://fixture.test/image-png", fixture.Client, cts.Token)
                .GetAwaiter()
                .GetResult()
            |> ignore),
        Throws.InstanceOf<OperationCanceledException>()
    )

[<Test>]
let ``IsImageUrlAsync times out with TaskCanceledException`` () =
    // A proxy that accepts the connection and never answers.
    let silent = new TcpListener(IPAddress.Loopback, 0)
    silent.Start()

    try
        let port = (silent.LocalEndpoint :?> IPEndPoint).Port

        use handler =
            new HttpClientHandler(Proxy = WebProxy(sprintf "http://127.0.0.1:%d" port), UseProxy = true)

        use client = new HttpClient(handler, Timeout = TimeSpan.FromMilliseconds 500.0)

        Assert.That(
            TestDelegate(fun () ->
                ImageUrl.IsImageUrlAsync("http://fixture.test/image-png", client).GetAwaiter().GetResult()
                |> ignore),
            Throws.InstanceOf<TaskCanceledException>()
        )
    finally
        silent.Stop()

[<Test>]
let ``IsImageUrlAsync with the shared client decides by extension without a request`` () =
    Assert.That(ImageUrl.IsImageUrlAsync("http://fixture.test/a.png").GetAwaiter().GetResult(), Is.True)
    Assert.That(ImageUrl.IsImageUrlAsync("mailto:x@fixture.test").GetAwaiter().GetResult(), Is.False)

    Assert.That(ImageUrl.IsImageUrlAsync("file:///x.gif", CancellationToken.None).GetAwaiter().GetResult(), Is.False)
