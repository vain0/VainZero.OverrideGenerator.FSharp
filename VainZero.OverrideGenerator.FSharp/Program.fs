namespace VainZero.OverrideGenerator.FSharp.Console

open System
open System.IO
open Basis.Core
open VainZero.IO
open VainZero.OverrideGenerator.FSharp

type Error =
  | ArgumentParseError
  | OverrideGeneratorError
    of OverrideGeneratorModule.Error

module Program =
  let tryArgument argv =
    try
      AppArgument.parse argv |> Success
    with
    | e ->
      Failure ArgumentParseError

  let choose types =
    types |> Seq.tryHead

  let run argv =
    result {
      let! argument = tryArgument argv
      let generator = new OverrideGenerator()
      do!
        generator.LoadOrError(argument.References)
        |> Result.mapFailure OverrideGeneratorError
      let structuralWriter =
        StructuralTextWriter(Console.Out, argument.IndentWidth)
      let overrideWriter =
        OverrideWriter(structuralWriter, argument.ReceiverIdentifier, argument.Stub)
      let! write =
        generator.GenerateOrError(overrideWriter, argument.Type, choose)
        |> Result.mapFailure OverrideGeneratorError
      do write |> Async.RunSynchronously
      return ()
    }

  let handleError (writer: TextWriter) error =
    match error with
    | ArgumentParseError ->
      writer.WriteLine(AppArgument.parser.PrintUsage())
    | OverrideGeneratorError error ->
      match error with
      | OverrideGeneratorModule.NoMatchingType ->
        writer.WriteLine(sprintf "No matching type.")
      | OverrideGeneratorModule.FailedLoad (path, e) ->
        writer.WriteLine(sprintf "Failed to load an assembly: %s" path)
        writer.WriteLine(sprintf "%A" e)

  [<EntryPoint>]
  let main argv =
    match run argv with
    | Success () ->
      0
    | Failure error ->
      handleError Console.Error error
      -1
