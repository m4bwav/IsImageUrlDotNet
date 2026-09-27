/// One line per public type and member of an assembly, with parameter names and the extension attribute, so a
/// renamed parameter or a lost extension form shows up as a missing line. PublicApi-1.0.2.txt was written by this
/// same code, compiled in a scratch project against the published 1.0.2 DLL.
module IsImageUrlDotNet.Tests.PublicApi

open System
open System.Reflection
open System.Runtime.CompilerServices

let private ext (m: MemberInfo) =
    if m.IsDefined(typeof<ExtensionAttribute>, false) then
        " [Extension]"
    else
        ""

let private flags =
    BindingFlags.Public
    ||| BindingFlags.Static
    ||| BindingFlags.Instance
    ||| BindingFlags.DeclaredOnly

let lines (assembly: Assembly) =
    [
        for t in assembly.GetExportedTypes() |> Array.sortBy (fun t -> t.FullName) do
            yield sprintf "type %s%s" t.FullName (ext t)

            for p in t.GetProperties flags |> Array.sortBy (fun p -> p.Name) do
                yield sprintf "property %s.%s : %O" t.FullName p.Name p.PropertyType

            for m in
                t.GetMethods flags
                |> Array.filter (fun m -> not m.IsSpecialName)
                |> Array.sortBy (fun m -> m.ToString()) do
                let parameters =
                    m.GetParameters()
                    |> Array.map (fun p -> sprintf "%O %s" p.ParameterType p.Name)
                    |> String.concat ", "

                yield sprintf "method %s.%s(%s) : %O%s" t.FullName m.Name parameters m.ReturnType (ext m)
    ]
    |> List.distinct
