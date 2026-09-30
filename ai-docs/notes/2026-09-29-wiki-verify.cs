#:package IsImageUrlDotNet@2.0.0
#:property PublishAot=false
// wiki-verify for IsImageUrlDotNet 2.0.0: prints every output the wiki's pages show, run against the PUBLISHED
// package from nuget.org, never the working tree. Saved in the repository as ai-docs/notes/2026-09-29-wiki-verify.cs,
// its output beside it as 2026-09-29-wiki-verify.out.txt (LF, ports and local paths masked).
//
// Run it from a scratch folder outside any project cone (the repository's Directory.Build.props would apply):
//   dotnet run wiki-verify.cs > out.txt
// Package restores are the only traffic that leaves the machine; they happen in the build steps, which run without
// proxy variables. Every request the library makes goes to StandIn below: a proxy on 127.0.0.1 that answers the host
// names images.test, other.test and secure.test (RFC 6761 names that never resolve) from fixed routes, refuses every
// other host (a reply that is not HTTP, or 403 to CONNECT), and never opens a socket itself. The children find it through HTTP_PROXY and
// HTTPS_PROXY (.NET 10, .NET 8, dotnet fsi); on .NET Framework, which ignores those variables, the child's harness sets
// WebRequest.DefaultWebProxy before any snippet runs. Each child first runs the gate: requests to gate.invalid, from the
// shared client and from a caller's new HttpClient(), must fail AND be seen by the stand-in, else the run stops.
//
// The pages' C# snippets are the strings in `csharp` below, exactly as the pages show them. They are compiled into one
// file-based app per target (net10.0; net48, which loads the package's net462 build; net8.0, which loads the
// netstandard2.0 build), each snippet run in its own process. The F# snippets run with dotnet fsi. Harness cases
// (Harness: true) print the tables the pages show as text; their code is not on the pages.
// Two runs must print the same thing: no times (elapsed seconds are rounded), ports or paths.
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using IsImageUrlDotNet;

Console.OutputEncoding = Encoding.UTF8;
var here = args.Length > 0 ? Path.GetFullPath(args[0]) : (string)AppContext.GetData("EntryPointFileDirectoryPath")!;
var work = Path.Combine(here, "work");
if (Directory.Exists(work))
{
    Directory.Delete(work, true);
}

Directory.CreateDirectory(work);
var windows = OperatingSystem.IsWindows();
var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var temp = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
var nugetRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? Path.Combine(home, ".nuget", "packages");

using var standIn = new StandIn();
var proxyUrl = $"http://127.0.0.1:{standIn.Port}";

string Mask(string s)
{
    foreach (var (path, token) in new[] { (work, "<scratch>/work"), (here, "<scratch>"), (nugetRoot, "<nuget>"), (temp, "<temp>"), (home, "<home>") })
    {
        s = s.Replace(path, token).Replace(path.Replace('\\', '/'), token);
    }

    return s.Replace(":" + standIn.Port, ":<port>").Replace("\r\n", "\n");
}

void Show(string label, string text)
{
    Console.WriteLine($"## {label}");
    Console.WriteLine(Mask(text).TrimEnd());
    Console.WriteLine();
}

var installed = typeof(ImageUrl).Assembly;
Show("installed", $"{installed.GetName().Name} {installed.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0]} from nuget.org; runner {RuntimeInformation.FrameworkDescription} on {(windows ? "Windows" : RuntimeInformation.OSDescription)}");

// ----- the pages' C# snippets, exactly as shown -----
var csharp = new List<Snip>
{
    new("home", "home and getting started: the first calls", """
    using IsImageUrlDotNet;

    Console.WriteLine("https://example.com/photos/cat.PNG?w=200".HasImageExtension());
    Console.WriteLine("https://example.com/report.pdf".HasImageExtension());
    Console.WriteLine(await "http://images.test/avatar".IsImageUrlAsync());
    """),
    new("static-form", "getting started: the static form", """
    using IsImageUrlDotNet;

    bool offline = ImageUrl.HasImageExtension("photos/cat.PNG?w=1");
    bool online = await ImageUrl.IsImageUrlAsync("http://images.test/about");
    Console.WriteLine($"{offline} {online}");
    """),
    new("own-client", "getting started: your own client and a token", """
    using IsImageUrlDotNet;

    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    Console.WriteLine(await "http://images.test/avatar".IsImageUrlAsync(client, cts.Token));
    """),
    new("lists", "api reference: the extension lists", """
    using IsImageUrlDotNet;

    Console.WriteLine(string.Join(" ", ImageUrl.ImageExtensions));
    Console.WriteLine(string.Join(" ", IsImageUrlDotNetLib.ImageFileExtensions));
    Console.WriteLine(string.Join(" ", IsImageUrlDotNetLib.NonImageFileExtensions));
    """),
    new("obsolete", "api reference: the obsolete 1.0.2 call", """
    using IsImageUrlDotNet;

    #pragma warning disable CS0618 // IsImageUrl is obsolete
    Console.WriteLine("image.png".IsImageUrl());
    Console.WriteLine("http://images.test/avatar".IsImageUrl());
    #pragma warning restore CS0618
    """),
    new("null-client", "edge cases: a null client throws before a task exists", """
    using IsImageUrlDotNet;

    HttpClient? none = null;
    try
    {
        Task<bool> pending = "http://images.test/avatar".IsImageUrlAsync(none!);
        Console.WriteLine("returned a task");
    }
    catch (ArgumentNullException e)
    {
        Console.WriteLine(e.Message);
    }
    """),
    new("recipe-filter", "recipes: keep the URLs that look like images, offline", """
    using IsImageUrlDotNet;

    string[] urls =
    [
        "https://example.com/photos/cat.PNG?w=200",
        "https://example.com/report.pdf",
        "https://example.com/avatar",
        "/static/logo.svg",
    ];

    foreach (var url in urls.Where(u => u.HasImageExtension()))
    {
        Console.WriteLine(url);
    }
    """),
    new("recipe-many", "recipes: ask about several URLs at once", """
    using IsImageUrlDotNet;

    string[] urls = ["http://images.test/avatar", "http://images.test/about", "http://images.test/photo"];
    bool[] answers = await Task.WhenAll(urls.Select(url => url.IsImageUrlAsync()));

    for (var i = 0; i < urls.Length; i++)
    {
        Console.WriteLine($"{urls[i]} {answers[i]}");
    }
    """, Concurrent: true),
    new("recipe-unknown", "recipes: treat an unreachable server as unknown", """
    using IsImageUrlDotNet;

    static async Task<bool?> TryIsImageAsync(string url)
    {
        try
        {
            return await url.IsImageUrlAsync();
        }
        catch (HttpRequestException)
        {
            return null; // the server could not be reached or did not answer HTTP
        }
        catch (TaskCanceledException)
        {
            return null; // timed out
        }
    }

    foreach (var url in new[] { "http://images.test/avatar", "http://images.test/close" })
    {
        bool? answer = await TryIsImageAsync(url);
        Console.WriteLine($"{url} {answer?.ToString() ?? "unknown"}");
    }
    """),
    new("recipe-deadline", "recipes: a shorter deadline for one call", """
    using IsImageUrlDotNet;

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    try
    {
        Console.WriteLine(await "http://images.test/slow".IsImageUrlAsync(cts.Token));
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine("gave up after 2 seconds");
    }
    """),
    new("recipe-headers", "recipes: your own client with its own headers", """
    using IsImageUrlDotNet;

    using var client = new HttpClient();
    client.DefaultRequestHeaders.UserAgent.ParseAdd("my-crawler/1.0 (+https://my.example/bot)");
    client.DefaultRequestHeaders.Accept.ParseAdd("image/*");
    Console.WriteLine(await "http://images.test/avatar".IsImageUrlAsync(client));
    """),
    new("recipe-no-auto-redirect", "recipes: let the package apply its redirect rules", """
    using IsImageUrlDotNet;

    using var handler = new HttpClientHandler { AllowAutoRedirect = false };
    using var client = new HttpClient(handler);
    Console.WriteLine(await "http://images.test/photo".IsImageUrlAsync(client));
    Console.WriteLine(await "http://images.test/to-ftp".IsImageUrlAsync(client));
    """),
    new("recipe-fake-server", "recipes: test your code without a network", """
    using System.Net;
    using System.Net.Http.Headers;
    using IsImageUrlDotNet;

    using var client = new HttpClient(new FakeServer());
    Console.WriteLine(await "https://cdn.test/avatar".IsImageUrlAsync(client));
    Console.WriteLine(await "https://cdn.test/page".IsImageUrlAsync(client));

    sealed class FakeServer : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var type = request.RequestUri!.AbsolutePath == "/avatar" ? "image/png" : "text/html";
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(type);
            return Task.FromResult(response);
        }
    }
    """),
    // ----- harness cases: their printed tables go on the pages as text; the code does not -----
    new("gate", "gate", """
    foreach (var url in new[] { "http://gate.invalid/shared", "https://gate.invalid/shared" })
    {
        await Row("shared client " + url, async () => (await url.IsImageUrlAsync()).ToString());
    }

    using var mine = new HttpClient();
    await Row("new HttpClient() http://gate.invalid/mine", async () => (await "http://gate.invalid/mine".IsImageUrlAsync(mine)).ToString());
    """, Harness: true),
    new("info", "info", """
    var asm = typeof(ImageUrl).Assembly;
    Console.WriteLine("runtime: " + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
    Console.WriteLine("package build loaded: " + asm.GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false).Cast<System.Runtime.Versioning.TargetFrameworkAttribute>().First().FrameworkName);
    var fsharpCore = IsImageUrlDotNetLib.ImageFileExtensions.GetType().Assembly;
    Console.WriteLine("FSharp.Core loaded: " + fsharpCore.GetName().Version + " (" + ((AssemblyInformationalVersionAttribute)fsharpCore.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)[0]).InformationalVersion.Split('+')[0] + ")");
    Console.WriteLine("ImageFileExtensions is a " + IsImageUrlDotNetLib.ImageFileExtensions.GetType().FullName!.Split('`')[0]);
    """, Harness: true),
    new("has-table", "behaviour: HasImageExtension answers", """
    string?[] inputs =
    [
        "https://example.com/photos/cat.PNG?w=200", "https://example.com/report.pdf", "https://example.com/a.png#top",
        "https://example.com/a.pdf?name=a.png", "https://example.com/dir.png/file", "https://example.com/a.png.",
        "https://example.com/", "Http://example.com/a.PNG", "  https://example.com/a.png  ",
        "https://example.com/%E2%82%AC.png", "https://example.com/x.heic", "https://example.com/x.TIF",
        "photos/cat.PNG?w=1", "/images/a.png", "images\\a.png", "//cdn.example.com/a.png", "a b.png",
        "x.exe.png", "x.png.exe", ".png", "png", "", null,
        "file:///tmp/a.png", "ftp://example.com/a.png", "data:image/png;base64,iVBORw0KGgo=", "C:\\temp\\x.png",
        "localhost:8080/a.png",
    ];

    foreach (var u in inputs)
    {
        await Row("HasImageExtension(" + Q(u) + ")", () => Task.FromResult(u.HasImageExtension().ToString()));
    }

    var before = Thread.CurrentThread.CurrentCulture;
    Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
    await Row("HasImageExtension(\"x.GIF\") under tr-TR", () => Task.FromResult("x.GIF".HasImageExtension().ToString()));
    Thread.CurrentThread.CurrentCulture = before;
    """, Harness: true),
    new("ask-table", "behaviour: IsImageUrlAsync answers with the shared client", """
    string?[] urls =
    [
        "http://images.test/avatar", "http://images.test/about", "http://images.test/upper", "http://images.test/svg",
        "http://images.test/x-imagefile", "http://images.test/octet", "http://images.test/no-type",
        "http://images.test/gone", "http://images.test/error", "http://images.test/empty",
        "http://images.test/photo", "http://images.test/to-other", "http://images.test/loop",
        "http://images.test/to-ftp", "http://images.test/to-https", "https://secure.test/avatar",
        "http://images.test/close", "http://images.test/garbage",
        "http://images.test/a.png?size=1", "http://images.test/photo.webp", "  http://images.test/avatar  ",
        "http://user:secret@images.test/avatar?size=large", "images.test/avatar", "//images.test/avatar.png",
        "avatar.png", "ftp://images.test/a.png", "", null,
    ];

    foreach (var u in urls)
    {
        await Row("IsImageUrlAsync(" + Q(u) + ")", async () => (await u.IsImageUrlAsync()).ToString());
    }
    """, Harness: true),
    new("head", "behaviour: the request the shared client sends", """
    await Row("head: shared client", async () => (await "http://user:secret@images.test/avatar?size=large".IsImageUrlAsync()).ToString());
    using var mine = new HttpClient();
    await Row("head: new HttpClient()", async () => (await "http://images.test/avatar?size=large".IsImageUrlAsync(mine)).ToString());
    """, Harness: true),
    new("redirects", "edge cases: redirects by client", """
    using (var noAuto = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
    {
        foreach (var u in new[] { "http://images.test/photo", "http://images.test/loop", "http://images.test/to-ftp", "http://images.test/to-https" })
        {
            await Row("AllowAutoRedirect=false " + u, async () => (await u.IsImageUrlAsync(noAuto)).ToString());
        }
    }

    using (var auto = new HttpClient())
    {
        foreach (var u in new[] { "http://images.test/photo", "http://images.test/loop", "http://images.test/to-ftp", "http://images.test/to-https" })
        {
            await Row("new HttpClient() " + u, async () => (await u.IsImageUrlAsync(auto)).ToString());
        }
    }

    using (var tlsNoAuto = TrustingClient(false))
    {
        foreach (var u in new[] { "https://secure.test/avatar", "https://secure.test/photo", "https://secure.test/to-http" })
        {
            await Row("stand-in TLS, AllowAutoRedirect=false " + u, async () => (await u.IsImageUrlAsync(tlsNoAuto)).ToString());
        }
    }

    using (var tlsAuto = TrustingClient(true))
    {
        await Row("stand-in TLS, AllowAutoRedirect=true https://secure.test/to-http", async () => (await "https://secure.test/to-http".IsImageUrlAsync(tlsAuto)).ToString());
    }
    """, Harness: true),
    new("errors", "edge cases: exceptions and their messages", """
    foreach (var u in new[] { "http://images.test/close", "http://images.test/garbage", "https://secure.test/avatar", "http://gate.invalid/x" })
    {
        Mark("error " + u);
        try
        {
            Console.WriteLine($"{u} => {await u.IsImageUrlAsync()}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"{u} => {e.GetType().Name}: {e.Message}");
            for (var inner = e.InnerException; inner != null; inner = inner.InnerException)
            {
                Console.WriteLine($"  inner {inner.GetType().Name}: {inner.Message}");
            }
        }

        Mark("end");
    }
    """, Harness: true),
    new("timeouts", "edge cases: timeouts and cancellation", """
    var clock = System.Diagnostics.Stopwatch.StartNew();
    Mark("shared client, a server that never answers");
    try
    {
        Console.WriteLine(await "http://images.test/slow".IsImageUrlAsync());
    }
    catch (Exception e)
    {
        Console.WriteLine($"shared client: {e.GetType().Name} after {Math.Round(clock.Elapsed.TotalSeconds)} s: {e.Message}");
        for (var inner = e.InnerException; inner != null; inner = inner.InnerException)
        {
            Console.WriteLine($"  inner {inner.GetType().Name}: {inner.Message}");
        }
    }

    Mark("end");
    using var quick = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
    Mark("client with Timeout 1 s");
    clock.Restart();
    try
    {
        Console.WriteLine(await "http://images.test/slow".IsImageUrlAsync(quick));
    }
    catch (Exception e)
    {
        Console.WriteLine($"client with Timeout 1 s: {e.GetType().Name} after {Math.Round(clock.Elapsed.TotalSeconds)} s: {e.Message}");
    }

    Mark("end");
    using var cancelled = new CancellationTokenSource();
    cancelled.Cancel();
    await Row("cancelled token, http://images.test/avatar", async () => (await "http://images.test/avatar".IsImageUrlAsync(cancelled.Token)).ToString());
    await Row("cancelled token, http://images.test/a.png", async () => (await "http://images.test/a.png".IsImageUrlAsync(cancelled.Token)).ToString());
    """, Harness: true),
    new("versions-new", "versions: 2.0.0 side", """
    foreach (var (u, culture) in VersionInputs())
    {
        var before = Thread.CurrentThread.CurrentCulture;
        if (culture != null) Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(culture);
    #pragma warning disable CS0618
        await Row("obs|" + u + "|" + culture, () => Task.FromResult(u.IsImageUrl().ToString()));
    #pragma warning restore CS0618
        await Row("has|" + u + "|" + culture, () => Task.FromResult(u.HasImageExtension().ToString()));
        await Row("ask|" + u + "|" + culture, async () => (await u.IsImageUrlAsync()).ToString());
        Thread.CurrentThread.CurrentCulture = before;
    }
    """, Harness: true),
};

// ----- the pages' F# scripts, exactly as shown -----
var fsharp = new List<Snip>
{
    new("fs-first", "getting started: F# script", """
    #r "nuget: IsImageUrlDotNet, 2.0.0"
    open IsImageUrlDotNet

    printfn "%b" (ImageUrl.HasImageExtension "https://example.com/photos/cat.PNG?w=200")
    printfn "%b" ("photos/cat.png".HasImageExtension())

    let isImage =
        ImageUrl.IsImageUrlAsync("http://images.test/avatar")
        |> Async.AwaitTask
        |> Async.RunSynchronously

    printfn "%b" isImage
    """),
    new("fs-task", "recipes: F# task with your own client", """
    #r "nuget: IsImageUrlDotNet, 2.0.0"
    open System.Net.Http
    open IsImageUrlDotNet

    let check (client: HttpClient) (urls: string list) =
        task {
            for url in urls do
                let! answer = ImageUrl.IsImageUrlAsync(url, client)
                printfn "%s %b" url answer
        }

    let client = new HttpClient()
    (check client [ "http://images.test/avatar"; "http://images.test/about" ]).Wait()
    """),
    new("fs-obsolete", "versions: the obsolete call from F#", """
    #r "nuget: IsImageUrlDotNet, 2.0.0"
    open IsImageUrlDotNet

    printfn "%b" (IsImageUrlDotNetLib.IsImageUrl "image.png")
    """),
    new("fs-old-extension", "faq: F# and url.IsImageUrl()", """
    #r "nuget: IsImageUrlDotNet, 2.0.0"
    open IsImageUrlDotNet

    printfn "%b" ("image.png".IsImageUrl())
    """),
    new("fs-gate", "gate", """
    #r "nuget: IsImageUrlDotNet, 2.0.0"
    open IsImageUrlDotNet

    for url in [ "http://gate.invalid/fsi"; "https://gate.invalid/fsi" ] do
        try
            printfn "%s returned %b" url (ImageUrl.IsImageUrlAsync(url).GetAwaiter().GetResult())
        with e ->
            printfn "%s %s" url (e.GetType().Name)
    """, Harness: true),
};

// ----- the old version, 1.0.2, for Versions and upgrading -----
var old = new List<Snip>
{
    new("gate", "gate", """
    await Row("1.0.2 IsImageUrl http://gate.invalid/old", () => Task.FromResult("http://gate.invalid/old".IsImageUrl().ToString()));
    """, Harness: true),
    new("versions-old", "versions: 1.0.2 side", """
    foreach (var (u, culture) in VersionInputs())
    {
        var before = Thread.CurrentThread.CurrentCulture;
        if (culture != null) Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(culture);
        await Row("old|" + u + "|" + culture, () => Task.FromResult(u.IsImageUrl().ToString()));
        Thread.CurrentThread.CurrentCulture = before;
    }
    """, Harness: true),
};

var noFSharpCore = new List<Snip>
{
    new("first-use", "versions: 1.0.2 in a C# project without FSharp.Core", """
    try
    {
        Console.WriteLine("image.png".IsImageUrl());
    }
    catch (Exception e)
    {
        Console.WriteLine($"{e.GetType().Name}: {e.Message}");
    }
    """, Harness: true),
};

// ----- build the children (no proxy variables: restores reach nuget.org) -----
var buildEnv = new Dictionary<string, string?> { ["HTTP_PROXY"] = null, ["HTTPS_PROXY"] = null, ["ALL_PROXY"] = null, ["NO_PROXY"] = null };
var runEnv = new Dictionary<string, string?>
{
    ["HTTP_PROXY"] = proxyUrl,
    ["HTTPS_PROXY"] = proxyUrl,
    ["ALL_PROXY"] = null,
    ["NO_PROXY"] = null,
    ["WIKI_NETFX_PROXY"] = proxyUrl,
    ["WIKI_STANDIN_PORT"] = standIn.Port.ToString(),
    ["WIKI_STANDIN_THUMBPRINT"] = standIn.Thumbprint,
    ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
};
if (!windows)
{
    runEnv["http_proxy"] = proxyUrl;
    runEnv["https_proxy"] = proxyUrl;
    runEnv["no_proxy"] = null;
    buildEnv["http_proxy"] = null;
    buildEnv["https_proxy"] = null;
}

var hasNet8 = Run("dotnet", ["--list-runtimes"], buildEnv).Out.Contains("Microsoft.NETCore.App 8.");
var targets = new List<string> { "net10.0" };
if (windows) targets.Add("net48");
if (hasNet8) targets.Add("net8.0");

var children = new Dictionary<string, Child>();
foreach (var tfm in targets)
{
    children[tfm] = BuildChild("cases-" + tfm.Replace(".", ""), tfm, "IsImageUrlDotNet@2.0.0", csharp, []);
}

var oldChild = BuildChild("old-102", "net10.0", "IsImageUrlDotNet@1.0.2", old, ["FSharp.Core@10.1.401"]);
var bareChild = BuildChild("old-102-no-fsharp-core", "net10.0", "IsImageUrlDotNet@1.0.2", noFSharpCore, []);
Show("builds", string.Join("\n", children.Values.Append(oldChild).Append(bareChild).Select(c => $"{c.Name} ({c.Tfm}): {(c.Ok ? "built" : "FAILED\n" + c.Log)}")));

// dotnet fsi restores #r "nuget:" when a script runs: warm the cache once without the proxy, with no requests.
var fsxDir = Path.Combine(work, "fsx");
Directory.CreateDirectory(fsxDir);
File.WriteAllText(Path.Combine(fsxDir, "warm.fsx"), "#r \"nuget: IsImageUrlDotNet, 2.0.0\"\nprintfn \"restored\"\n");
var warm = Run("dotnet", ["fsi", "--quiet", Path.Combine(fsxDir, "warm.fsx")], buildEnv, fsxDir);
Show("f# restore (no proxy, no requests)", warm.Text);
standIn.Take();

// ----- the gate: before any case, on every route -----
var gateFailures = new List<string>();
foreach (var (tfm, child) in children)
{
    var (text, _) = RunRows(child, "gate");
    Show($"gate ({tfm})", text);
    CheckGate(tfm, text, 3);
}

var (oldGate, _) = RunRows(oldChild, "gate");
Show("gate (1.0.2 on net10.0)", oldGate);
CheckGate("1.0.2", oldGate, 1);
var fsGate = RunFsx(fsharp.Single(s => s.Id == "fs-gate"));
Show("gate (dotnet fsi)", fsGate.Text + "\n" + Heads(fsGate.Requests, false));
if (!(fsGate.Text.Contains("http://gate.invalid/fsi HttpRequestException") && fsGate.Text.Contains("https://gate.invalid/fsi HttpRequestException")
      && fsGate.Requests.Count >= 2 && fsGate.Requests.All(r => r.Contains("gate.invalid"))))
{
    gateFailures.Add("dotnet fsi");
}

if (gateFailures.Count > 0)
{
    Show("GATE FAILED", string.Join("\n", gateFailures) + "\nNo case ran.");
    return 1;
}

Show("gate result", "every route failed at the stand-in: no request left the machine");

foreach (var (tfm, child) in children)
{
    Show($"child info ({tfm})", RunCase(child, "info").Out);
}

Show("child info (1.0.2 on net10.0, FSharp.Core 10.1.401 added by hand)", "IsImageUrlDotNet 1.0.2 ships lib/net45 only; NU1701 at restore");

// ----- page snippets on net10.0, then the other targets compared -----
foreach (var snip in csharp.Where(s => !s.Harness))
{
    var main = Settle(snip, RunCase(children["net10.0"], snip.Id));
    Show(snip.Label, main.Out);
    Show(snip.Label + ": requests the stand-in received", Heads(main.Requests, true));
    foreach (var tfm in targets.Skip(1))
    {
        var other = Settle(snip, RunCase(children[tfm], snip.Id));
        var same = other.Out == main.Out;
        var sameHeads = Heads(other.Requests, true) == Heads(main.Requests, true);
        Show($"{snip.Label} ({tfm})",
            (same ? "same output as net10.0" : other.Out)
            + (other.CarriageReturns > 0 ? $"\n(printed {other.CarriageReturns} CR LF line endings, normalised to LF)" : "")
            + (sameHeads ? "\nsame requests as net10.0" : "\nrequests:\n" + Heads(other.Requests, true)));
    }
}

// ----- harness tables -----
foreach (var id in new[] { "has-table", "ask-table", "head", "redirects", "errors", "timeouts" })
{
    var snip = csharp.Single(s => s.Id == id);
    var (main, _) = RunRows(children["net10.0"], id);
    Show(snip.Label, main);
    foreach (var tfm in targets.Skip(1))
    {
        var (other, _) = RunRows(children[tfm], id);
        Show($"{snip.Label} ({tfm})", other == main ? "same as net10.0" : other);
    }
}

// ----- F# scripts -----
foreach (var snip in fsharp.Where(s => !s.Harness))
{
    var r = RunFsx(snip);
    Show(snip.Label, r.Text);
    Show(snip.Label + ": requests the stand-in received", Heads(r.Requests, false));
}

// ----- Versions and upgrading: 1.0.2 and 2.0.0 side by side -----
var (_, oldRows) = RunRows(oldChild, "versions-old");
var (_, newRows) = RunRows(children["net10.0"], "versions-new");
var table = new StringBuilder();
var sameAnswers = 0;
var sameRequests = 0;
var inputs = oldRows.Select(r => r.Key.Substring(4)).ToList();
foreach (var key in inputs)
{
    var parts = key.Split('|');
    var o = oldRows.Single(r => r.Key == "old|" + key);
    var n = newRows.Single(r => r.Key == "obs|" + key);
    var h = newRows.Single(r => r.Key == "has|" + key);
    var a = newRows.Single(r => r.Key == "ask|" + key);
    sameAnswers += o.Answer == n.Answer ? 1 : 0;
    sameRequests += string.Join("\n", o.Requests) == string.Join("\n", n.Requests) ? 1 : 0;
    table.AppendLine($"\"{parts[0]}\"{(parts[1].Length > 0 ? " under " + parts[1] : "")}");
    table.AppendLine($"  1.0.2 IsImageUrl         {o.Answer}{Count(o)}");
    table.AppendLine($"  2.0.0 IsImageUrl         {n.Answer}{Count(n)}");
    table.AppendLine($"  2.0.0 HasImageExtension  {h.Answer}{Count(h)}");
    table.AppendLine($"  2.0.0 IsImageUrlAsync    {a.Answer}{Count(a)}");
}

Show("versions: 1.0.2 and 2.0.0 side by side (net10.0)", table.ToString());
Show("versions: 2.0.0's IsImageUrl against 1.0.2 today", $"same answer {sameAnswers} of {inputs.Count}; same request heads {sameRequests} of {inputs.Count}");
Show(noFSharpCore[0].Label, RunCase(bareChild, "first-use").Out);

// ----- what C# callers see at compile time -----
var compileDir = Path.Combine(work, "compile-check");
Directory.CreateDirectory(compileDir);
var compileFile = Path.Combine(compileDir, "compile-check.cs");
File.WriteAllText(compileFile, """
    #:package IsImageUrlDotNet@2.0.0
    #:property PublishAot=false
    using IsImageUrlDotNet;

    var url = "http://images.test/avatar";
    Console.WriteLine(url.IsImageUrl());
    Console.WriteLine(await url.IsImageUrlAsync(default));
    """.Replace("\r\n", "\n"));
var compile = Run("dotnet", ["build", compileFile, "-tl:off", "-nologo", "-v:m"], buildEnv, compileDir);
Show("api reference: compiler messages for url.IsImageUrl() and url.IsImageUrlAsync(default)",
    string.Join("\n", compile.Out.Split('\n').Select(l => Regex.Match(l, @"\b(warning|error) (CS\d+): (.*?)( \[[^\]]*\])?$")).Where(m => m.Success).Select(m => $"{m.Groups[1].Value} {m.Groups[2].Value}: {m.Groups[3].Value}").Distinct())
    + $"\nbuild exit code {compile.Exit}");

// ----- the public surface, from the loaded net10.0 build and from each build's metadata -----
var surface = new StringBuilder();
foreach (var type in new[] { typeof(ImageUrl), typeof(IsImageUrlDotNetLib) })
{
    surface.AppendLine($"{(type.IsAbstract && type.IsSealed ? "static class" : "class")} {type.FullName}{(type.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute)) ? " [Extension]" : "")}");
    foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).OrderBy(p => p.Name))
    {
        surface.AppendLine($"  {TypeName(p.PropertyType)} {p.Name} {{ get; }}");
    }

    foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName).OrderBy(m => m.Name).ThenBy(m => m.GetParameters().Length))
    {
        var obsolete = m.GetCustomAttribute<ObsoleteAttribute>();
        surface.AppendLine($"  {TypeName(m.ReturnType)} {m.Name}({string.Join(", ", m.GetParameters().Select(p => TypeName(p.ParameterType) + " " + p.Name))})"
            + (m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute)) ? " [Extension]" : "")
            + (obsolete != null ? $" [Obsolete(\"{obsolete.Message}\", error: {obsolete.IsError.ToString().ToLowerInvariant()})]" : ""));
    }
}

Show("api reference: the public surface (net10.0 build, by reflection)", surface.ToString());

var builds = new StringBuilder();
string? firstSurface = null;
foreach (var dir in Directory.GetDirectories(Path.Combine(nugetRoot, "isimageurldotnet", "2.0.0", "lib")).OrderBy(d => d))
{
    var dll = Path.Combine(dir, "IsImageUrlDotNet.dll");
    using var pe = new PEReader(File.OpenRead(dll));
    var md = pe.GetMetadataReader();
    var names = new List<string>();
    foreach (var th in md.TypeDefinitions)
    {
        var t = md.GetTypeDefinition(th);
        if ((t.Attributes & System.Reflection.TypeAttributes.VisibilityMask) != System.Reflection.TypeAttributes.Public) continue;
        foreach (var mh in t.GetMethods())
        {
            var m = md.GetMethodDefinition(mh);
            if ((m.Attributes & System.Reflection.MethodAttributes.MemberAccessMask) == System.Reflection.MethodAttributes.Public)
            {
                var blob = md.GetBlobReader(m.Signature);
                var sig = blob.ReadSignatureHeader();
                if (sig.IsGeneric) blob.ReadCompressedInteger();
                names.Add($"{md.GetString(t.Name)}.{md.GetString(m.Name)}/{blob.ReadCompressedInteger()}");
            }
        }
    }

    var joined = string.Join(",", names.OrderBy(n => n));
    firstSurface ??= joined;
    var refs = md.AssemblyReferences.Select(r => md.GetAssemblyReference(r)).Select(r => $"{md.GetString(r.Name)} {r.Version}").OrderBy(r => r);
    builds.AppendLine($"lib/{Path.GetFileName(dir)}: {new FileInfo(dll).Length} bytes, {names.Count} public methods, {(joined == firstSurface ? "same public surface as the first" : "DIFFERENT public surface")}; references {string.Join(", ", refs)}");
}

Show("api reference: the three builds in the package", builds.ToString());
var hosts = standIn.HostsSeen();
Show("hosts the stand-in was asked for", string.Join("\n", hosts));
return 0;

// ===== helpers =====
Child BuildChild(string name, string tfm, string package, List<Snip> snips, string[] extraPackages)
{
    var dir = Path.Combine(work, "children", name);
    Directory.CreateDirectory(dir);
    var file = Path.Combine(dir, name + ".cs");
    File.WriteAllText(file, ChildSource(tfm, package, extraPackages, snips));
    var build = Run("dotnet", ["build", file, "-tl:off", "-nologo", "-v:m"], buildEnv, dir);
    var match = Regex.Matches(build.Out, @"-> (.+\.(dll|exe))\s*$", RegexOptions.Multiline).LastOrDefault();
    var output = match?.Groups[1].Value.Trim() ?? "";
    return new Child(name, tfm, build.Exit == 0 && output.Length > 0, output, build.Out + build.Err);
}

RunResult RunCase(Child child, string id)
{
    standIn.Take();
    var r = child.Tfm == "net48"
        ? Run(Path.ChangeExtension(child.Output, ".exe"), [id], runEnv, work)
        : Run("dotnet", [child.Output, id], runEnv, work);
    return r with { Requests = standIn.Take() };
}

(string Text, List<RowResult> Rows) RunRows(Child child, string id)
{
    var r = RunCase(child, id);
    var rows = new List<RowResult>();
    var text = new StringBuilder();
    var byKey = new Dictionary<string, List<string>>();
    string? current = null;
    foreach (var entry in r.Requests)
    {
        if (entry.StartsWith("MARK "))
        {
            current = entry.Substring(5) == "end" ? null : entry.Substring(5);
            if (current != null) byKey[current] = [];
        }
        else if (current != null)
        {
            byKey[current].Add(entry);
        }
    }

    foreach (var line in r.Out.Split('\n'))
    {
        if (line.StartsWith("ROW\t"))
        {
            var parts = line.Split('\t');
            var reqs = byKey.TryGetValue(parts[1], out var l) ? l : [];
            var row = new RowResult(parts[1], parts[2], reqs);
            rows.Add(row);
            text.AppendLine(parts[1].StartsWith("head:")
                ? $"{parts[1]} => {parts[2]}\n{Heads(reqs, true)}"
                : $"{parts[1]} => {parts[2]}{RequestLines(reqs)}");
        }
        else if (line.Length > 0)
        {
            text.AppendLine(line);
        }
    }

    var stray = r.Requests.Count(e => !e.StartsWith("MARK ")) - byKey.Values.Sum(v => v.Count);
    if (stray > 0) text.AppendLine($"({stray} requests outside any row)");
    if (r.Err.Length > 0) text.AppendLine("--- stderr\n" + r.Err);
    return (text.ToString().TrimEnd(), rows);
}

RunResult RunFsx(Snip snip)
{
    var file = Path.Combine(fsxDir, snip.Id + ".fsx");
    File.WriteAllText(file, snip.Code.Replace("\r\n", "\n") + "\n");
    standIn.Take();
    var r = Run("dotnet", ["fsi", "--quiet", file], runEnv, fsxDir);
    return r with { Requests = standIn.Take() };
}

void CheckGate(string route, string text, int expected)
{
    var failed = text.Split('\n').Count(l => l.Contains(" => HttpRequestException") || l.Contains(" => WebException"));
    var seen = Regex.Matches(text, @"\[(GET|CONNECT) [^\]]*gate\.invalid").Count;
    if (failed != expected || seen != expected)
    {
        gateFailures.Add($"{route}: {failed} of {expected} failed, {seen} of {expected} seen by the stand-in");
    }
}

// Requests a snippet sends at once arrive in any order: sort them so two runs print the same thing.
static RunResult Settle(Snip snip, RunResult r) => snip.Concurrent ? r with { Requests = [.. r.Requests.OrderBy(h => h, StringComparer.Ordinal)] } : r;

string Count(RowResult r) => r.Requests.Count == 0 ? "" : r.Requests.Count == 1 ? "  (1 request)" : $"  ({r.Requests.Count} requests)";

// Request lines in order; a run of the same line prints once with its count ("x 11").
static string RequestLines(List<string> reqs)
{
    if (reqs.Count == 0) return "   (no request)";
    var lines = reqs.Select(h => h.Split('\n')[0].Replace(" HTTP/1.1", "")).ToList();
    var runs = new List<string>();
    for (var i = 0; i < lines.Count;)
    {
        var j = i;
        while (j < lines.Count && lines[j] == lines[i]) j++;
        runs.Add(j - i > 1 ? $"{lines[i]} x {j - i}" : lines[i]);
        i = j;
    }

    return "   [" + string.Join("; ", runs) + "]";
}

static string Heads(List<string> reqs, bool full) =>
    reqs.Count == 0 ? "(no request)" : string.Join("\n", reqs.Select(h => full ? h : h.Split('\n')[0]));

static string TypeName(Type t)
{
    if (t == typeof(bool)) return "bool";
    if (t == typeof(string)) return "string";
    if (!t.IsGenericType) return t.Name;
    return t.Name.Split('`')[0] + "<" + string.Join(", ", t.GetGenericArguments().Select(TypeName)) + ">";
}

RunResult Run(string file, string[] arguments, Dictionary<string, string?> env, string? cwd = null)
{
    var info = new ProcessStartInfo(file)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8,
        WorkingDirectory = cwd ?? work,
    };
    foreach (var a in arguments) info.ArgumentList.Add(a);
    foreach (var (k, v) in env)
    {
        if (v is null) info.Environment.Remove(k); else info.Environment[k] = v;
    }

    using var p = Process.Start(info)!;
    var stderrTask = p.StandardError.ReadToEndAsync();
    var stdout = p.StandardOutput.ReadToEnd();
    var stderr = stderrTask.Result;
    p.WaitForExit();
    var crs = stdout.Count(c => c == '\r');
    return new RunResult(stdout.Replace("\r\n", "\n").TrimEnd(), stderr.Replace("\r\n", "\n").TrimEnd(), p.ExitCode, crs, []);
}

static string ChildSource(string tfm, string package, string[] extraPackages, List<Snip> snips)
{
    var usings = new SortedSet<string>(StringComparer.Ordinal)
    {
        "using System;", "using System.Collections.Generic;", "using System.Linq;", "using System.Net;", "using System.Net.Http;",
        "using System.Net.Sockets;", "using System.Reflection;", "using System.Text;", "using System.Threading;",
        "using System.Threading.Tasks;", "using IsImageUrlDotNet;",
    };
    var methods = new StringBuilder();
    var types = new StringBuilder();
    var cases = new StringBuilder();
    foreach (var s in snips)
    {
        var body = new List<string>();
        var inTypes = false;
        foreach (var line in s.Code.Replace("\r\n", "\n").Split('\n'))
        {
            if (!inTypes && body.Count == 0 && line.Trim().Length == 0) continue;
            if (!inTypes && body.Count == 0 && line.StartsWith("using ") && !line.StartsWith("using var ") && line.TrimEnd().EndsWith(";"))
            {
                usings.Add(line.Trim());
                continue;
            }

            if (!inTypes && (line.StartsWith("sealed class ") || line.StartsWith("class ") || line.StartsWith("record "))) inTypes = true;
            if (inTypes) types.AppendLine(line); else body.Add(line);
        }

        var method = "Case_" + s.Id.Replace('-', '_');
        cases.AppendLine($"    case \"{s.Id}\": await {method}(); break;");
        methods.AppendLine($"static async Task {method}()\n{{\n{string.Join("\n", body.Select(l => l.Length > 0 ? "    " + l : l)).TrimEnd()}\n}}\n");
    }

    var header = new StringBuilder();
    header.AppendLine($"#:package {package}");
    foreach (var extra in extraPackages) header.AppendLine($"#:package {extra}");
    header.AppendLine("#:property PublishAot=false");
    header.AppendLine("#:property LangVersion=latest");
    header.AppendLine("#:property NoWarn=CS1998");
    if (tfm != "net10.0") header.AppendLine($"#:property TargetFramework={tfm}");
    return (header + string.Join("\n", usings) + "\n\n" + $$"""
        // Generated by wiki-verify.cs: each page snippet is one method, run by name in its own process.
        Console.OutputEncoding = Encoding.UTF8;
        #if NETFRAMEWORK
        // .NET Framework ignores HTTP_PROXY: the harness sets the process's default proxy, as machine settings would.
        if (Environment.GetEnvironmentVariable("WIKI_NETFX_PROXY") is string netfxProxy) WebRequest.DefaultWebProxy = new WebProxy(netfxProxy);
        #endif
        switch (args[0])
        {
        {{cases.ToString().TrimEnd()}}
            default: Console.WriteLine("no case " + args[0]); break;
        }

        {{methods.ToString().TrimEnd()}}

        // ----- harness helpers (not on the pages) -----
        static int StandInPort() => int.Parse(Environment.GetEnvironmentVariable("WIKI_STANDIN_PORT") ?? "0");

        // Tells the stand-in, over a raw socket, which row the next requests belong to; returns once it has logged it.
        static void Mark(string key)
        {
            using var c = new TcpClient();
            c.Connect(IPAddress.Loopback, StandInPort());
            var s = c.GetStream();
            var b = Encoding.ASCII.GetBytes("MARK " + key + "\r\n\r\n");
            s.Write(b, 0, b.Length);
            try { s.Read(new byte[1], 0, 1); } catch (Exception) { }
        }

        static string Q(string? s) => s is null ? "null" : "\"" + s + "\"";

        static async Task Row(string key, Func<Task<string>> call)
        {
            Mark(key);
            string answer;
            try
            {
                answer = await call();
            }
            catch (Exception e)
            {
                answer = e is WebException w ? $"WebException ({w.Status})" : e.GetType().Name;
            }

            Mark("end");
            Console.WriteLine("ROW\t" + key + "\t" + answer);
        }

        // A client that trusts only the stand-in's throwaway certificate, for the https redirect rows.
        static HttpClient TrustingClient(bool autoRedirect)
        {
            var thumbprint = Environment.GetEnvironmentVariable("WIKI_STANDIN_THUMBPRINT");
            var handler = new HttpClientHandler { AllowAutoRedirect = autoRedirect };
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => cert != null && string.Equals(cert.GetCertHashString(), thumbprint, StringComparison.OrdinalIgnoreCase);
            return new HttpClient(handler);
        }

        static (string Url, string? Culture)[] VersionInputs() =>
        [
            ("image.png", null), ("x.webp", null), ("a", null), ("photos/cat.png?w=1", null), ("ftp://images.test/a.png", null),
            ("http://images.test/avatar", null), ("http://images.test/a.png?size=1", null), ("http://images.test/about", null),
            ("http://images.test/upper", null), ("http://images.test/x-imagefile", null), ("http://images.test/gone", null),
            ("x.GIF", "tr-TR"),
        ];

        {{types.ToString().TrimEnd()}}
        """).Replace("\r\n", "\n") + "\n";
}

record Snip(string Id, string Label, string Code, bool Harness = false, bool Concurrent = false);

record Child(string Name, string Tfm, bool Ok, string Output, string Log);

record RunResult(string Out, string Err, int Exit, int CarriageReturns, List<string> Requests)
{
    public string Text => Out + (Err.Length > 0 ? "\n--- stderr\n" + Err : "") + (Exit != 0 ? "\n--- exit " + Exit : "");
}

record RowResult(string Key, string Answer, List<string> Requests);

// The local stand-in: a proxy on 127.0.0.1 that answers fixed routes for images.test and other.test (plain http) and
// secure.test (CONNECT, then TLS with a throwaway self-signed certificate), and refuses every other host. It never
// opens a connection of its own. Every request head is logged in arrival order.
sealed class StandIn : IDisposable
{
    static readonly HashSet<string> PlainHosts = ["images.test", "other.test"];
    static readonly HashSet<string> TlsHosts = ["secure.test"];
    static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    readonly TcpListener listener = new(IPAddress.Loopback, 0);
    readonly List<string> log = [];
    readonly SortedSet<string> hosts = new(StringComparer.Ordinal);
    readonly object gate = new();
    readonly X509Certificate2 certificate;

    public StandIn()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=secure.test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("secure.test");
        request.CertificateExtensions.Add(san.Build());
        using var made = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        certificate = X509CertificateLoader.LoadPkcs12(made.Export(X509ContentType.Pfx), null);
        listener.Start();
        _ = Task.Run(AcceptLoop);
    }

    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;

    public string Thumbprint => certificate.Thumbprint;

    public List<string> Take()
    {
        lock (gate)
        {
            var copy = new List<string>(log);
            log.Clear();
            return copy;
        }
    }

    public List<string> HostsSeen()
    {
        lock (gate) return [.. hosts];
    }

    void Log(string entry, string? host)
    {
        lock (gate)
        {
            log.Add(entry);
            if (host != null) hosts.Add(host);
        }
    }

    async Task AcceptLoop()
    {
        while (true)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync();
            }
            catch (Exception)
            {
                return;
            }

            _ = Task.Run(() => Handle(client));
        }
    }

    async Task Handle(TcpClient client)
    {
        using var _ = client;
        try
        {
            var stream = client.GetStream();
            var head = await ReadHead(stream);
            if (head.Count == 0) return;
            if (head[0].StartsWith("MARK "))
            {
                Log(head[0], null);
                return;
            }

            var parts = head[0].Split(' ');
            var method = parts[0];
            var target = parts.Length > 1 ? parts[1] : "";
            if (method == "CONNECT")
            {
                var host = target.Split(':')[0];
                Log(string.Join("\n", head), host);
                if (!TlsHosts.Contains(host))
                {
                    await Write(stream, "HTTP/1.1 403 Forbidden\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                    return;
                }

                await Write(stream, "HTTP/1.1 200 Connection established\r\n\r\n");
                using var tls = new SslStream(stream);
                await tls.AuthenticateAsServerAsync(certificate);
                var inner = await ReadHead(tls);
                if (inner.Count == 0) return;
                Log(string.Join("\n", inner), null);
                var innerParts = inner[0].Split(' ');
                await Reply(tls, "https", innerParts[1].Split('?')[0], innerParts[0]);
                return;
            }

            if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
            {
                Log(string.Join("\n", head), "(not a proxy request)");
                return;
            }

            Log(string.Join("\n", head), uri.Host);
            if (uri.Scheme != "http" || !PlainHosts.Contains(uri.Host))
            {
                // Not HTTP, so the client fails at once. A closed connection would make HttpClient retry.
                await Write(stream, "REFUSED BY THE STAND-IN\r\n\r\n");
                return;
            }

            await Reply(stream, "http", uri.AbsolutePath, method);
        }
        catch (Exception)
        {
            // A client that gives up (a timeout, a refused certificate) ends here.
        }
    }

    static async Task Reply(Stream stream, string scheme, string path, string method)
    {
        switch (path)
        {
            case "/avatar": await Respond(stream, "200 OK", "image/png", Png, method); break;
            case "/about": await Respond(stream, "200 OK", "text/html; charset=utf-8", Encoding.ASCII.GetBytes("<html></html>"), method); break;
            case "/upper": await Respond(stream, "200 OK", "IMAGE/PNG", Png, method); break;
            case "/svg": await Respond(stream, "200 OK", "image/svg+xml", Encoding.ASCII.GetBytes("<svg/>"), method); break;
            case "/x-imagefile": await Respond(stream, "200 OK", "application/x-imagefile", Png, method); break;
            case "/octet": await Respond(stream, "200 OK", "application/octet-stream", Png, method); break;
            case "/no-type": await Respond(stream, "200 OK", null, Png, method); break;
            case "/gone": await Respond(stream, "404 Not Found", "image/png", Png, method); break;
            case "/error": await Respond(stream, "500 Internal Server Error", "text/plain", Encoding.ASCII.GetBytes("boom"), method); break;
            case "/empty": await Respond(stream, "204 No Content", "image/png", [], method); break;
            case "/photo": await Redirect(stream, "/avatar"); break;
            case "/to-other": await Redirect(stream, "http://other.test/avatar"); break;
            case "/loop": await Redirect(stream, "/loop"); break;
            case "/to-ftp": await Redirect(stream, "ftp://images.test/a.png"); break;
            case "/to-http": await Redirect(stream, "http://images.test/avatar"); break;
            case "/to-https": await Redirect(stream, "https://secure.test/avatar"); break;
            case "/slow": await Task.Delay(TimeSpan.FromSeconds(15)); break;
            case "/close": break;
            case "/garbage": await Write(stream, "NOT HTTP AT ALL\r\n\r\n"); break;
            default: await Respond(stream, "404 Not Found", "text/plain", Encoding.ASCII.GetBytes("no route"), method); break;
        }
    }

    static Task Redirect(Stream stream, string location) =>
        Write(stream, $"HTTP/1.1 302 Found\r\nLocation: {location}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");

    static async Task Respond(Stream stream, string status, string? contentType, byte[] body, string method)
    {
        var head = $"HTTP/1.1 {status}\r\n" + (contentType != null ? $"Content-Type: {contentType}\r\n" : "") + $"Content-Length: {body.Length}\r\nConnection: close\r\n\r\n";
        await Write(stream, head);
        if (method != "HEAD" && body.Length > 0) await stream.WriteAsync(body);
        await stream.FlushAsync();
    }

    static async Task Write(Stream stream, string text)
    {
        await stream.WriteAsync(Encoding.ASCII.GetBytes(text));
        await stream.FlushAsync();
    }

    static async Task<List<string>> ReadHead(Stream stream)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var buffer = new List<byte>();
        var one = new byte[1];
        while (true)
        {
            var n = await stream.ReadAsync(one, timeout.Token);
            if (n == 0) break;
            buffer.Add(one[0]);
            var c = buffer.Count;
            if (c >= 4 && buffer[c - 4] == 13 && buffer[c - 3] == 10 && buffer[c - 2] == 13 && buffer[c - 1] == 10) break;
        }

        return [.. Encoding.ASCII.GetString([.. buffer]).Split("\r\n", StringSplitOptions.RemoveEmptyEntries)];
    }

    public void Dispose()
    {
        listener.Stop();
        certificate.Dispose();
    }
}
