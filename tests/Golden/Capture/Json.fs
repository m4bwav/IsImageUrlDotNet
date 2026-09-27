/// A small JSON writer, so the recording is the same text on every runtime (no serializer version in the way).
/// The golden test compiles this same file, so a case's JSON text can be compared exactly.
module Golden.Json

open System.Text

type Json =
    | JNull
    | JBool of bool
    | JNum of int
    | JStr of string
    | JArr of Json list
    | JObj of (string * Json) list

let private backslash = string (char 92)

let private escape (s: string) =
    let sb = StringBuilder()

    for c in s do
        match c with
        | '"' -> sb.Append(backslash).Append('"') |> ignore
        | c when c = char 92 -> sb.Append(backslash).Append(backslash) |> ignore
        | '\n' -> sb.Append(backslash).Append('n') |> ignore
        | '\r' -> sb.Append(backslash).Append('r') |> ignore
        | '\t' -> sb.Append(backslash).Append('t') |> ignore
        | c when c < ' ' || c = char 0x7f ->
            sb.Append(backslash).Append('u').Append((int c).ToString("x4")) |> ignore
        | c -> sb.Append(c) |> ignore

    sb.ToString()

let rec private write (sb: StringBuilder) (indent: int) (value: Json) =
    let pad n = String.replicate n "  "

    match value with
    | JNull -> sb.Append("null") |> ignore
    | JBool b -> sb.Append(if b then "true" else "false") |> ignore
    | JNum n -> sb.Append(n) |> ignore
    | JStr s -> sb.Append('"').Append(escape s).Append('"') |> ignore
    | JArr [] -> sb.Append("[]") |> ignore
    | JArr items ->
        sb.Append("[\n") |> ignore

        items
        |> List.iteri (fun i item ->
            sb.Append(pad (indent + 1)) |> ignore
            write sb (indent + 1) item
            sb.Append(if i < items.Length - 1 then ",\n" else "\n") |> ignore)

        sb.Append(pad indent).Append(']') |> ignore
    | JObj [] -> sb.Append("{}") |> ignore
    | JObj fields ->
        sb.Append("{\n") |> ignore

        fields
        |> List.iteri (fun i (k, v) ->
            sb.Append(pad (indent + 1)).Append('"').Append(escape k).Append("\": ") |> ignore
            write sb (indent + 1) v
            sb.Append(if i < fields.Length - 1 then ",\n" else "\n") |> ignore)

        sb.Append(pad indent).Append('}') |> ignore

/// Indented JSON with LF line endings.
let render (value: Json) =
    let sb = StringBuilder()
    write sb 0 value
    sb.ToString()
