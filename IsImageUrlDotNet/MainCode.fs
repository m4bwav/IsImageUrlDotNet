namespace IsImageUrlDotNet
open System.Net
open System
open System.IO


[<System.Runtime.CompilerServices.Extension>]
module IsImageUrlDotNetLib =   
    let ImageFileExtensions = ["png"; "jpg"; "gif"; "raw";"bmp"; "svg"; "gif"; "raw";]
    let NonImageFileExtensions = ["exe"; "pdf"; "html"; "htm"; "txt"; "mp3"; "wav"; "odp";]
    let private hasAnImageFileExtension(url:string) =
      if Path.HasExtension url = false then
         false
      else  
         let urlSplitList = url.ToLower().Split '.'
         let lastExtension = urlSplitList.[urlSplitList.Length - 1]
         List.contains lastExtension ImageFileExtensions

    let private hasAnNonImageFileExtension(url:string) =
      if Path.HasExtension url = false then
         false
      else  
         let urlSplitList = url.ToLower().Split '.'
         let lastExtension = urlSplitList.[urlSplitList.Length - 1]
         List.contains lastExtension NonImageFileExtensions

    let private requestUrlAndCheckIfImage(url:string) =
        let req = WebRequest.Create(Uri(url)) 
        use resp = req.GetResponse() 
        let contentType = resp.ContentType
        if contentType.Contains("text/html") then
           false
        else
            contentType.Contains("image")

    [<System.Runtime.CompilerServices.Extension>]   
    let IsImageUrl(opt:string) =                
      match opt with
        | null | "" -> false
        | url when Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute) = false -> false
        | url when hasAnImageFileExtension url -> true 
        | url when hasAnNonImageFileExtension url -> false 
        | _ -> requestUrlAndCheckIfImage opt         