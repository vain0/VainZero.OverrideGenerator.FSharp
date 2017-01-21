namespace VainZero

open System
open System.IO
open Basis.Core

[<Sealed>]
type StructuralTextWriter(writer: TextWriter, indentWidth: int) =
  let indent = ref 0

  let indentLength () =
    !indent * indentWidth

  let createIndent () =
    String.replicate (indentLength ()) " "

  member this.Indent =
    indent

  member this.IndentLength =
    indentLength ()

  member this.AddIndent() =
    indent |> incr
    { new IDisposable with
        override this.Dispose() =
          indent |> decr
    }

  member this.WriteLineAsync(text) =
    async {
      let indent = createIndent ()
      for line in text |> Str.splitBy Environment.NewLine do
        do! writer.WriteAsync(indent) |> Async.AwaitTask
        do! writer.WriteLineAsync(line) |> Async.AwaitTask
    }
