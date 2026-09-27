namespace IsImageUrlDotNet

// WebRequest is obsolete on .NET 6+ (SYSLIB0014). IsImageUrl keeps it on purpose: 1.0.2's answers, including its
// exceptions and the requests it sends, come from WebRequest, and the golden recordings hold 2.x to them.
#nowarn "44"

open System
open System.IO
open System.Net

/// The 1.0.2 API, kept exactly: the same answers on each runtime as 1.0.2 gave, bugs included
/// (tests/Golden/1.0.2.*.json). New code should use <see cref="T:IsImageUrlDotNet.ImageUrl" />.
[<System.Runtime.CompilerServices.Extension>]
module IsImageUrlDotNetLib =
    /// The extensions IsImageUrl treats as images without a request (1.0.2's list, unchanged).
    let ImageFileExtensions =
        [ "png"; "jpg"; "gif"; "raw"; "bmp"; "svg"; "jpeg"; "psd" ]

    /// The extensions IsImageUrl treats as not images without a request (1.0.2's list, unchanged).
    let NonImageFileExtensions =
        [ "exe"; "pdf"; "html"; "htm"; "txt"; "mp3"; "wav"; "odp" ]

    // The code below is 1.0.2's, statement for statement. Do not "fix" it: every fix changes a recorded answer.
    let private hasAFileExtensionInList (fileExtensionList: string list) (url: string) =
        if Path.HasExtension url = false then
            false
        else
            let urlSplitList = url.ToLower().Split '.'
            let lastExtension = urlSplitList.[urlSplitList.Length - 1]
            List.contains lastExtension fileExtensionList

    let private hasAnImageFileExtension (url: string) =
        hasAFileExtensionInList ImageFileExtensions url

    let private hasAnNonImageFileExtension (url: string) =
        hasAFileExtensionInList NonImageFileExtensions url

    let private requestUrlAndCheckIfImage (url: string) =
        let req = WebRequest.Create(Uri(url))
        use resp = req.GetResponse()
        let contentType = resp.ContentType

        if contentType.Contains("text/html") then
            false
        else
            contentType.Contains("image")

    /// 1.0.2's answer: true when the text after the last dot is an image extension, false when it is a non-image
    /// extension or the string is not a well-formed URI, and otherwise a GET through WebRequest whose Content-Type
    /// contains "image". Throws for most undecidable input, follows redirects to any host and reads file: URLs.
    [<System.Runtime.CompilerServices.Extension>]
    [<Obsolete("IsImageUrl keeps 1.0.2's answers, bugs included. Use ImageUrl.HasImageExtension (offline) or ImageUrl.IsImageUrlAsync (http and https only).")>]
    let IsImageUrl (opt: string | null) =
        match opt with
        | null
        | "" -> false
        | url when Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute) = false -> false
        | url when hasAnImageFileExtension url -> true
        | url when hasAnNonImageFileExtension url -> false
        | _ -> requestUrlAndCheckIfImage opt
