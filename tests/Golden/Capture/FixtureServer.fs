/// The local fixture server for the golden capture and the golden test (the test compiles this same file).
///
/// It listens on 127.0.0.1 at a free port and is set as the HTTP proxy, so every http:// request any case makes
/// arrives here in absolute form ("GET http://fixture.test/image-png HTTP/1.1") and nothing leaves the machine; the
/// cases use .test host names (RFC 6761), which never resolve, so a request that bypassed the proxy would fail rather
/// than reach the internet. https:// arrives as CONNECT and is refused with 403. Each request's head (request line
/// and headers, in the order sent) is recorded, and the case takes the list after its call.
module Golden.FixtureServer

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading

type private Reply =
    | Respond of status: string * headers: string list * body: byte[]
    | CloseWithoutReply
    | Garbage

let private png = [| 0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy |]
let private text (s: string) = Encoding.ASCII.GetBytes s

let private ok contentType body =
    Respond("200 OK", [ "Content-Type: " + contentType ], body)

let private redirect status location =
    Respond(status, [ "Location: " + location; "Content-Type: text/html" ], text "moved")

/// Routes by host and path (the query string is ignored). Anything else is 404 text/plain.
let private route (host: string) (path: string) =
    match host, path with
    | _, "/" -> ok "text/html; charset=utf-8" (text "<html></html>")
    | _, "/image-png" -> ok "image/png" png
    | _, "/image-jpeg" -> ok "image/jpeg" png
    | _, "/image-gif" -> ok "image/gif" png
    | _, "/image-webp" -> ok "image/webp" png
    | _, "/image-svg" -> ok "image/svg+xml" (text "<svg/>")
    | _, "/image-upper" -> ok "IMAGE/PNG" png
    | _, "/image-params" -> ok "image/png; charset=binary" png
    | _, "/html" -> ok "text/html; charset=utf-8" (text "<html></html>")
    | _, "/html-upper" -> ok "TEXT/HTML" (text "<html></html>")
    | _, "/html-and-image" -> ok "text/html; profile=image" (text "<html></html>")
    | _, "/json" -> ok "application/json" (text "{}")
    | _, "/octet" -> ok "application/octet-stream" png
    | _, "/pdf" -> ok "application/pdf" (text "%PDF-1.4")
    | _, "/x-imagefile" -> ok "application/x-imagefile" png
    | _, "/text-image" -> ok "text/plain; name=image.png" (text "image")
    | _, "/no-type" -> Respond("200 OK", [], png)
    | _, "/empty-type" -> Respond("200 OK", [ "Content-Type: " ], png)
    | _, "/not-found-image" -> Respond("404 Not Found", [ "Content-Type: image/png" ], png)
    | _, "/server-error" -> Respond("500 Internal Server Error", [ "Content-Type: text/plain" ], text "boom")
    | _, "/no-content-image" -> Respond("204 No Content", [ "Content-Type: image/png" ], [||])
    | _, "/redirect-image" -> redirect "302 Found" "http://fixture.test/image-png"
    | _, "/redirect-html" -> redirect "301 Moved Permanently" "http://fixture.test/html"
    | _, "/redirect-other-host" -> redirect "302 Found" "http://other.test/image-png"
    | _, "/redirect-relative" -> redirect "302 Found" "/image-png"
    | _, "/redirect-https" -> redirect "302 Found" "https://fixture.test/image-png"
    | _, "/redirect-loop" -> redirect "302 Found" "http://fixture.test/redirect-loop"
    | _, "/redirect-no-location" -> Respond("302 Found", [ "Content-Type: image/png" ], png)
    | _, "/close" -> CloseWithoutReply
    | _, "/garbage" -> Garbage
    | _ -> Respond("404 Not Found", [ "Content-Type: text/plain" ], text "no route")

type Server() =
    let listener = new TcpListener(IPAddress.Loopback, 0)
    let seen = ResizeArray<string list>()
    let gate = obj ()
    let mutable running = true

    let readHead (stream: Stream) =
        // Reads bytes up to the blank line that ends the request head; the cases send no request bodies.
        let buffer = ResizeArray<byte>()
        let mutable finished = false

        while not finished do
            let b = stream.ReadByte()

            if b < 0 then
                finished <- true
            else
                buffer.Add(byte b)
                let n = buffer.Count

                if n >= 4 && buffer.[n - 4] = 13uy && buffer.[n - 3] = 10uy && buffer.[n - 2] = 13uy && buffer.[n - 1] = 10uy then
                    finished <- true

        Encoding.ASCII.GetString(buffer.ToArray()).Split([| "\r\n" |], StringSplitOptions.RemoveEmptyEntries)
        |> List.ofArray

    let handle (client: TcpClient) =
        use client = client
        use stream = client.GetStream()
        stream.ReadTimeout <- 10000
        let head = readHead stream

        if not head.IsEmpty then
            lock gate (fun () -> seen.Add head)
            let parts = head.Head.Split(' ')
            let meth, target = parts.[0], (if parts.Length > 1 then parts.[1] else "")

            let host, path =
                if meth = "CONNECT" then
                    target, ""
                elif target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) then
                    let u = Uri(target)
                    u.Host, u.AbsolutePath
                else
                    let hostHeader =
                        head
                        |> List.tryFind (fun h -> h.StartsWith("Host:", StringComparison.OrdinalIgnoreCase))
                        |> Option.map (fun h -> h.Substring(5).Trim())
                        |> Option.defaultValue ""

                    hostHeader, (target.Split('?').[0])

            let reply =
                if meth = "CONNECT" then
                    Respond("403 Forbidden", [], [||])
                else
                    route host path

            match reply with
            | CloseWithoutReply -> ()
            | Garbage ->
                let bytes = text "NOT HTTP AT ALL\r\n\r\n"
                stream.Write(bytes, 0, bytes.Length)
            | Respond (status, headers, body) ->
                let lines =
                    [ yield "HTTP/1.1 " + status
                      yield! headers
                      yield "Content-Length: " + string body.Length
                      yield "Connection: close"
                      yield ""
                      yield "" ]

                let headBytes = text (String.Join("\r\n", lines))
                stream.Write(headBytes, 0, headBytes.Length)

                if meth <> "HEAD" then
                    stream.Write(body, 0, body.Length)

                stream.Flush()

    let loop () =
        while running do
            try
                let client = listener.AcceptTcpClient()

                let t =
                    Thread((fun () ->
                        try
                            handle client
                        with _ ->
                            ()),
                           IsBackground = true)

                t.Start()
            with _ ->
                ()

    do
        listener.Start()
        Thread(loop, IsBackground = true).Start()

    /// The port the server listens on at 127.0.0.1.
    member _.Port = (listener.LocalEndpoint :?> IPEndPoint).Port

    /// The request heads seen since the last call, oldest first; clears the list.
    member _.Take() =
        // A request's head is recorded before its reply is written, so after a call returns its requests are here.
        lock gate (fun () ->
            let list = List.ofSeq seen
            seen.Clear()
            list)

    interface IDisposable with
        member _.Dispose() =
            running <- false
            listener.Stop()
