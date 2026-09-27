/// Every recorded case. The capture compiles this file against the published IsImageUrlDotNet 1.0.2; the golden test
/// compiles the same file against the new build, so both ask exactly the same questions in the same order.
///
/// Each case records the method, the culture, the arguments (as written, with <work> and <port> tokens), the result
/// ({"$throws": "Type: message"} with the WebException status and inner exception when it throws) and every request
/// head the fixture server saw during the call.
module Golden.Cases

open System
open System.Globalization
open System.IO
open System.Net
open System.Threading
open IsImageUrlDotNet
open Golden.Json

type Context =
    { Server: FixtureServer.Server
      /// A scratch folder for the file: URL cases; replaced by <work> in the recording.
      Work: string }

let private normalize (ctx: Context) (s: string) =
    let port = string ctx.Server.Port
    let work = ctx.Work.TrimEnd('/', char 92)
    let workUri = Uri(work + string Path.DirectorySeparatorChar).AbsoluteUri.TrimEnd('/')

    // The work folder first: it can sit under the current directory.
    let cwd = Environment.CurrentDirectory.TrimEnd('/', char 92)

    s
        .Replace("\r\n", "\n")
        .Replace(workUri, "file://<work>")
        .Replace(work, "<work>")
        .Replace(work.Replace(char 92, '/'), "<work>")
        .Replace(cwd, "<cwd>")
        .Replace(":" + port, ":<port>")

let rec private describe (ctx: Context) (e: exn) =
    let fields =
        [ yield "$throws", JStr(e.GetType().Name + ": " + normalize ctx e.Message)
          match e with
          | :? WebException as w -> yield "status", JStr(string w.Status)
          | _ -> ()
          if not (isNull e.InnerException) then
              yield "inner", describe ctx e.InnerException ]

    JObj fields

let private record (ctx: Context) (meth: string) (args: Json list) (call: unit -> Json) =
    ctx.Server.Take() |> ignore

    let result =
        try
            call ()
        with e ->
            describe ctx e

    let requests =
        ctx.Server.Take()
        |> List.map (fun head -> JArr(head |> List.map (fun line -> JStr(normalize ctx line))))

    JObj
        [ "method", JStr meth
          "culture", JStr CultureInfo.CurrentCulture.Name
          "args", JArr args
          "result", result
          "requests", JArr requests ]

let private withCulture (name: string) (f: unit -> 'T) =
    let thread = Thread.CurrentThread
    let old = thread.CurrentCulture
    thread.CurrentCulture <- CultureInfo(name)

    try
        f ()
    finally
        thread.CurrentCulture <- old

/// Inputs for IsImageUrl, each with the text recorded for it (only long inputs differ from their value).
let private inputs (ctx: Context) : (string * string) list =
    let same (s: string) = s, s
    let port = string ctx.Server.Port
    let fileUri (name: string) = Uri(Path.Combine(ctx.Work, name)).AbsoluteUri

    [ // nothing, whitespace, bare words
      yield null, null
      yield! List.map same [ ""; " "; "\t"; "a"; "png"; ".png" ]
      // relative names by extension: the lists, their case, and extensions in neither list
      yield!
          List.map
              same
              [ "image.png"; "IMAGE.PNG"; "photo.JPG"; "x.jpeg"; "x.gif"; "x.bmp"; "x.svg"; "x.raw"; "x.psd"
                "x.exe"; "x.pdf"; "x.html"; "x.htm"; "x.txt"; "x.mp3"; "x.wav"; "x.odp"
                "x.webp"; "x.tiff"; "x.ico"; "x.avif"; "x.zip"; "x.png.exe"; "x.exe.png"; "a.b/c.png"; "relative/path" ]
      // absolute http URLs decided by extension alone (no request)
      yield!
          List.map
              same
              [ "http://fixture.test/a.png"; "HTTP://FIXTURE.TEST/A.PNG"; "http://fixture.test/a.pdf"
                "http://fixture.test/.png"; "https://fixture.test/a.png"; "http://fixture.test/dir/a.jpeg" ]
      // extensions the lists do not decide: query, fragment, trailing dot, a dotted folder, unknown extensions
      yield!
          List.map
              same
              [ "http://fixture.test/a.png?size=1"; "http://fixture.test/a.png#top"; "http://fixture.test/a.pdf?x=1"
                "http://fixture.test/a.png."; "http://fixture.test/dir.png/file"; "http://fixture.test/photo.webp"
                "http://fixture.test"; "http://fixture.test/" ]
      // content types answered by the fixture server
      yield!
          [ "image-png"; "image-jpeg"; "image-gif"; "image-webp"; "image-svg"; "image-upper"; "image-params"
            "html"; "html-upper"; "html-and-image"; "json"; "octet"; "pdf"; "x-imagefile"; "text-image"
            "no-type"; "empty-type"; "not-found-image"; "server-error"; "no-content-image"
            "redirect-image"; "redirect-html"; "redirect-other-host"; "redirect-relative"; "redirect-https"
            "redirect-loop"; "redirect-no-location"; "close"; "garbage" ]
          |> List.map (fun p -> same ("http://fixture.test/" + p))
      // request shape: query, credentials in the URL, an explicit port, https, loopback (never proxied on .NET Framework)
      yield!
          List.map
              same
              [ "http://fixture.test/image-png?q=1"; "http://user:secret@fixture.test/image-png"
                "http://fixture.test:8080/image-png"; "https://fixture.test/image-png" ]
      yield "http://127.0.0.1:" + port + "/image-png", "http://127.0.0.1:<port>/image-png"
      yield "http://localhost:" + port + "/html", "http://localhost:<port>/html"
      // other schemes and path-like strings (127.0.0.1 or nothing, so no name is ever resolved)
      yield!
          List.map
              same
              [ "ftp://127.0.0.1:1/file"; "file:///nonexistent-isimageurl/file"; "mailto:someone@fixture.test"
                "data:image/png;base64,iVBORw0KGgo="; "urn:isbn:0451450523"; "javascript:alert(1)"
                "//127.0.0.1/share/file"; "/nonexistent-isimageurl/file"; "C:" + string (char 92) + "temp" + string (char 92) + "x.png" ]
      yield fileUri "fixture-file", "file://<work>/fixture-file"
      yield fileUri "missing-file", "file://<work>/missing-file"
      // characters: spaces, control characters, non-ASCII, escapes, path-invalid characters
      yield!
          List.map
              same
              [ "http://fixture.test/a b.png"; " http://fixture.test/a.png"; "http://fixture.test/a.png "
                "http://fixture.test/a.png\n"; "http://fixture.test/ü.png"; "http://fixture.test/%E2%82%AC.png"
                "http://fixture.test/a|b.png"; "a|b.png"; "a<b.png"; "a\"b.png"; "http://fixture.test/😀.png" ]
      // long inputs
      yield "http://fixture.test/" + String('a', 300) + ".png", "http://fixture.test/<a x 300>.png"
      yield "http://fixture.test/" + String('a', 70000) + ".png", "http://fixture.test/<a x 70000>.png"
      yield "http://fixture.test/" + String('a', 70000), "http://fixture.test/<a x 70000>" ]

/// Inputs whose answer depends on the culture: ToLower() under tr-TR maps "I" to dotless "ı".
let private cultureInputs =
    [ "x.GIF"; "IMAGE.PNG"; "photo.JPG"; "http://fixture.test/A.GIF"; "http://fixture.test/FILE.PDF"; "http://fixture.test/image-png" ]

let private str (s: string) = if isNull s then JNull else JStr s

/// Runs every case and returns the recordings in order.
let run (ctx: Context) : Json list =
    File.WriteAllText(Path.Combine(ctx.Work, "fixture-file"), "not an image")
    let isImage (url: string) () = JBool(IsImageUrlDotNetLib.IsImageUrl url)

    [ yield
          record ctx "ImageFileExtensions" [] (fun () ->
              JArr(IsImageUrlDotNetLib.ImageFileExtensions |> List.map JStr))
      yield
          record ctx "NonImageFileExtensions" [] (fun () ->
              JArr(IsImageUrlDotNetLib.NonImageFileExtensions |> List.map JStr))
      yield
          record ctx "ImageFileExtensions type" [] (fun () ->
              let t = IsImageUrlDotNetLib.ImageFileExtensions.GetType()
              JStr(t.Namespace + "." + t.Name))

      for value, shown in inputs ctx do
          yield withCulture "en-US" (fun () -> record ctx "IsImageUrl" [ str shown ] (isImage value))

      for value in cultureInputs do
          yield withCulture "tr-TR" (fun () -> record ctx "IsImageUrl" [ str value ] (isImage value))

      // The C# extension-method form ("...".IsImageUrl()). F# callers cannot use it: the F# compiler reads an
      // F# assembly's own signature, where IsImageUrl is a module function, and ignores the extension attributes.
      // So the attributes C# needs are recorded here by reflection.
      yield
          record ctx "extension attributes" [] (fun () ->
              let t = Type.GetType("IsImageUrlDotNet.IsImageUrlDotNetLib, IsImageUrlDotNet")
              let ext = typeof<System.Runtime.CompilerServices.ExtensionAttribute>
              let m = t.GetMethod("IsImageUrl", [| typeof<string> |])

              JObj
                  [ "type", JBool(t.IsDefined(ext, false))
                    "method", JBool(m.IsDefined(ext, false))
                    "assembly", JBool(t.Assembly.IsDefined(ext, false)) ]) ]
