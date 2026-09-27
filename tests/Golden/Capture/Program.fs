/// Golden capture entry point: starts the fixture server, makes it the proxy for every WebRequest, runs Cases.run
/// against the published 1.0.2 and writes the recording to stdout. See Capture.fsproj for how to run it.
module Golden.Program

open System
open System.Globalization
open System.IO
open System.Net
open System.Runtime.InteropServices
open System.Security.Cryptography
open System.Threading
open IsImageUrlDotNet
open Golden.Json

let private sha256 (path: string) =
    use sha = SHA256.Create()
    use stream = File.OpenRead path
    BitConverter.ToString(sha.ComputeHash stream).Replace("-", "").ToLowerInvariant()

let private os () =
    if RuntimeInformation.IsOSPlatform OSPlatform.Windows then "windows"
    elif RuntimeInformation.IsOSPlatform OSPlatform.OSX then "macos"
    else "linux"

[<EntryPoint>]
let main _ =
    // Exception messages in English on every machine; the culture per case is set by Cases.run.
    Thread.CurrentThread.CurrentUICulture <- CultureInfo("en-US")
    CultureInfo.DefaultThreadCurrentUICulture <- CultureInfo("en-US")

    let work = Path.Combine(AppContext.BaseDirectory, "golden-work")

    if Directory.Exists work then
        Directory.Delete(work, true)

    Directory.CreateDirectory work |> ignore

    use server = new FixtureServer.Server()
    WebRequest.DefaultWebProxy <- WebProxy(sprintf "http://127.0.0.1:%d" server.Port)

    let cases = Cases.run { Server = server; Work = work }
    // FSharp.Core is the assembly of the list type; the library is the assembly of the list's declaring module.
    let fsharpCore = IsImageUrlDotNetLib.ImageFileExtensions.GetType().Assembly
    let isImage = Type.GetType("IsImageUrlDotNet.IsImageUrlDotNetLib, IsImageUrlDotNet").Assembly

    let doc =
        JObj
            [ "package", JStr "IsImageUrlDotNet@1.0.2"
              "assembly", JStr(isImage.GetName().Name + " " + string (isImage.GetName().Version))
              "assemblySha256", JStr(sha256 isImage.Location)
              "fsharpCore", JStr(string (fsharpCore.GetName().Version))
              "runtime", JStr RuntimeInformation.FrameworkDescription
              "os", JStr(os ())
              "captured", JStr(DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
              "note",
              JStr
                  "Golden outputs of the published IsImageUrlDotNet 1.0.2 (lib/net45), recorded by tests/Golden/Capture against the local fixture server in FixtureServer.fs. Never edit; never regenerate from new code."
              "cases", JArr cases ]

    let out = Console.OpenStandardOutput()
    let bytes = Text.UTF8Encoding(false).GetBytes(render doc + "\n")
    out.Write(bytes, 0, bytes.Length)
    out.Flush()
    0
