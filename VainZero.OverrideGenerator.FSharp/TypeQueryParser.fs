namespace VainZero.OverrideGenerator.FSharp

open System
open System.Text.RegularExpressions
open Basis.Core

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TypeQueryParser =
  type Error = unit

  type Result =
    {
      Input:
        string
      Path:
        array<string>
      Name:
        string
      TypeArguments:
        array<string>
    }

  let tryParse (input: string): Result<_, Error> =
    result {
      let (name, typeArguments) =
        // TODO: improve
        if input.Contains("<") && input.EndsWith(">") then
          let (name, rest) =
            input |> Str.take (input.Length - 1) |> Str.split2 "<"
          let typeArguments =
            rest |> Str.splitBy "," |> Array.map Str.trim
          (name, typeArguments)
        else
          (input, [||])
      return
        {
          Input =
            input
          Path =
            [||]
          Name =
            name
          TypeArguments =
            typeArguments
        }
    }
