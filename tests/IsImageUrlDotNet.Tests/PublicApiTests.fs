module IsImageUrlDotNet.Tests.PublicApiTests

open System
open System.IO
open NUnit.Framework
open IsImageUrlDotNet

[<Test>]
let ``every public member of 1.0.2 is still there, with its parameter names and extension attributes`` () =
    let old =
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "PublicApi-1.0.2.txt"))
        |> Array.filter (fun l -> l <> "")

    let current = PublicApi.lines typeof<ImageUrl>.Assembly |> Set.ofList
    let missing = old |> Array.filter (fun l -> not (current.Contains l))
    Assert.That(old, Is.Not.Empty)
    Assert.That(missing, Is.Empty, "missing from 2.x:\n" + String.Join("\n", missing))
