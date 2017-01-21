namespace VainZero.IO

open System
open System.IO

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

  member this.WriteLineAsync(text: string) =
    async {
      let indent = createIndent ()
      for line in text.Split([|Environment.NewLine|], StringSplitOptions.None) do
        match line with
        | null | "" ->
          do! writer.WriteLineAsync("") |> Async.AwaitTask
        | line ->
          do! writer.WriteAsync(indent) |> Async.AwaitTask
          do! writer.WriteLineAsync(line) |> Async.AwaitTask
    }
