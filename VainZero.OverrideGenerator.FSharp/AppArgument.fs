namespace VainZero.OverrideGenerator.FSharp.Console

open System
open Argu

type AppParameter =
  | [<Mandatory>]
    [<AltCommandLine("-t")>]
    Type
    of string
  | [<AltCommandLine("-r")>]
    Reference
    of string
  | ReceiverIdentifier
    of string
  | IndentWidth
    of int
  | Stub
    of string
with
  interface IArgParserTemplate with
    override this.Usage =
      match this with
      | Type _ ->
        "Specify the base type to inherit from. Can include type arguments like IEnumerable<T>; they're not validated."
      | Reference _ ->
        "Add a reference to a class library."
      | ReceiverIdentifier _ ->
        "Specify the identifier to bind the receiver. `this` by default."
      | IndentWidth _ ->
        "Specify the width of an indent. 2 by default."
      | Stub _ ->
        "Specify the stub definition. By default, an expresssion to raise an exception."

type AppArgument =
  {
    Type:
      string
    References:
      list<string>
    ReceiverIdentifier:
      string
    IndentWidth:
      int
    Stub:
      string
  }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AppArgument =
  let parser =
    ArgumentParser<AppParameter>()

  let parse (args: array<string>) =
    let result = parser.Parse(args, raiseOnUsage = true)
    let argument =
      {
        Type =
          result.GetResult(<@ Type @>)
        References =
          result.GetResults(<@ Reference @>)
        ReceiverIdentifier =
          result.GetResult(<@ ReceiverIdentifier @>, "this")
        IndentWidth =
          result.GetResult(<@ IndentWidth @>, 2)
        Stub =
          result.GetResult(<@ Stub @>, "System.NotImplementedException() |> raise")
      }
    argument
