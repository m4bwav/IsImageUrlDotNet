/// The golden test: runs the capture's own Cases.fs against the new build and compares every case, as JSON text, with
/// the recording of the published 1.0.2 for this runtime and OS (tests/Golden/1.0.2.<runtime>-<os>.json).
/// The recordings never change. When a case differs, fix src/; never edit a recording.
module IsImageUrlDotNet.GoldenTests.GoldenTests

open System
open System.Globalization
open System.IO
open System.Net
open System.Runtime.InteropServices
open System.Threading
open NUnit.Framework
open Golden.Json

let private recordingName () =
    let os =
        if RuntimeInformation.IsOSPlatform OSPlatform.Windows then
            "windows"
        elif RuntimeInformation.IsOSPlatform OSPlatform.OSX then
            "macos"
        else
            "linux"

    if RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.Ordinal) then
        "1.0.2.net48-" + os + ".json"
    else
        "1.0.2.net10.0-" + os + ".json"

/// Splits the "cases" array of a document written by Golden.Json.render into one text per case.
let private caseTexts (document: string) =
    let marker = "\"cases\": [\n"
    let start = document.IndexOf(marker, StringComparison.Ordinal)

    if start < 0 then
        failwith "no cases array"

    let lines = document.Substring(start + marker.Length).Split('\n')
    let cases = ResizeArray<string>()
    let current = ResizeArray<string>()

    for line in lines do
        if line = "    {" && current.Count > 0 then
            cases.Add(String.Join("\n", current))
            current.Clear()

        if line.StartsWith("    ", StringComparison.Ordinal) then
            current.Add(if line = "    }," then "    }" else line)

    if current.Count > 0 then
        cases.Add(String.Join("\n", current))

    List.ofSeq cases

/// The one table of deliberate differences between 1.0.2 and 2.x. Each entry names the case it applies to and why.
/// D1 promises no behaviour differences; the only entry is about the machine the test runs on, not the library.
let private exceptions: (string * (string -> bool) * (string -> string)) list =
    [ // file:///x on Windows resolves against the current drive, and 1.0.2's message names it. Both Windows
        // recordings were made on D: (this repository's clone, and GitHub's windows-latest workspace); a clone on
        // another drive gets the same answer with its own drive letter.
        "file:///nonexistent-isimageurl/file names the current drive",
        (fun (case: string) -> case.Contains "\"file:///nonexistent-isimageurl/file\""),
        (fun (case: string) ->
            let drive = Path.GetPathRoot(Environment.CurrentDirectory).Substring(0, 1)
            case.Replace("'" + drive + ":\\\\nonexistent-isimageurl", "'D:\\\\nonexistent-isimageurl"))
    ]

let private applyExceptions (case: string) =
    exceptions
    |> List.fold (fun (text: string) (_, applies, change) -> if applies text then change text else text) case

let private cultureOfFirstCase (case: string) =
    let key = "\"culture\": \""
    let start = case.IndexOf(key, StringComparison.Ordinal) + key.Length
    case.Substring(start, case.IndexOf('"', start) - start)

[<Test>]
let ``IsImageUrl and the lists give 1.0.2's recorded answers and requests on this runtime`` () =
    let name = recordingName ()
    let path = Path.Combine(AppContext.BaseDirectory, "Golden", name)
    Assert.That(File.Exists path, Is.True, "no recording " + name)
    let expected = caseTexts (File.ReadAllText path)

    let thread = Thread.CurrentThread

    let oldCulture, oldUiCulture, oldProxy =
        thread.CurrentCulture, thread.CurrentUICulture, WebRequest.DefaultWebProxy
    // As in the capture's Program.fs: English exception messages; the ambient culture the recording was made under.
    thread.CurrentUICulture <- CultureInfo("en-US")
    thread.CurrentCulture <- CultureInfo(cultureOfFirstCase expected.Head)
    let work = Path.Combine(AppContext.BaseDirectory, "golden-work")

    if Directory.Exists work then
        Directory.Delete(work, true)

    Directory.CreateDirectory work |> ignore

    try
        use server = new Golden.FixtureServer.Server()
        WebRequest.DefaultWebProxy <- WebProxy(sprintf "http://127.0.0.1:%d" server.Port)
        let cases = Golden.Cases.run { Server = server; Work = work }

        let actual =
            caseTexts (render (JObj [ "cases", JArr cases ]) + "\n")
            |> List.map applyExceptions

        Assert.That(actual.Length, Is.EqualTo expected.Length, "number of cases")

        let differing =
            List.zip expected actual
            |> List.indexed
            |> List.filter (fun (_, (e, a)) -> e <> a)

        let report =
            differing
            |> List.truncate 5
            |> List.map (fun (i, (e, a)) -> sprintf "case %d\n--- 1.0.2 (%s)\n%s\n--- 2.x\n%s" i name e a)
            |> String.concat "\n\n"

        Assert.That(
            differing.Length,
            Is.EqualTo 0,
            sprintf "%d cases differ from %s\n\n%s" differing.Length name report
        )
    finally
        thread.CurrentCulture <- oldCulture
        thread.CurrentUICulture <- oldUiCulture
        WebRequest.DefaultWebProxy <- oldProxy
