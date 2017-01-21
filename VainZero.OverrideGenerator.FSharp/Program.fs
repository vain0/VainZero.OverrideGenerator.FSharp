namespace VainZero.OverrideGenerator.FSharp.Console

open System
open System.IO
open Basis.Core
open VainZero.IO
open VainZero.OverrideGenerator.FSharp

type Error =
  | ArgumentParseError
  | TypeSearcherError
    of TypeSearcherModule.Error
  | OverrideGeneratorError
    of OverrideGeneratorModule.Error

module Program =
  let tryArgument argv =
    try
      AppArgument.parse argv |> Success
    with
    | e ->
      Failure ArgumentParseError

  let run argv =
    result {
      let! argument = tryArgument argv
      use searcher = new TypeSearcher()
      let generator = OverrideGenerator(searcher)
      do!
        searcher.LoadOrError(argument.References)
        |> Result.mapFailure TypeSearcherError
      let structuralWriter =
        StructuralTextWriter(Console.Out, argument.IndentWidth)
      let overrideWriter =
        OverrideWriter(structuralWriter, argument.ReceiverIdentifier, argument.Stub)
      let! write =
        generator.GenerateOrError(overrideWriter, argument.Type)
        |> Result.mapFailure OverrideGeneratorError
      do write |> Async.RunSynchronously
      return ()
    }

  let handleError (writer: TextWriter) =
    let rec loop =
      function
      | ArgumentParseError ->
        writer.WriteLine(AppArgument.parser.PrintUsage())
      | TypeSearcherError error ->
        match error with
        | TypeSearcherModule.AssemblyLoadError (path, e) ->
          writer.WriteLine(sprintf "Failed to load an assembly: %s" path)
          writer.WriteLine(sprintf "%A" e)
      | OverrideGeneratorError error ->
        match error with
        | OverrideGeneratorModule.TypeQueryParseError message ->
          writer.WriteLine("Invalid query.")
          writer.WriteLine(message)
        | OverrideGeneratorModule.TypeSearcherError error ->
          error |> TypeSearcherError |> loop
        | OverrideGeneratorModule.NoMatchingType ->
          writer.WriteLine(sprintf "No matching type.")
    loop

  [<EntryPoint>]
  let main argv =
    match run argv with
    | Success () ->
      0
    | Failure error ->
      handleError Console.Error error
      -1
