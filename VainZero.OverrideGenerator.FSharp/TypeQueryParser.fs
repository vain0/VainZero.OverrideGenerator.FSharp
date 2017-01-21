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
      TypeParameters:
        array<string>
    }

  let tryParse (input: string): Result<_, Error> =
    result {
      let (name, typeParameters) =
        // TODO: improve
        if input.Contains("<") && input.EndsWith(">") then
          let (name, rest) =
            input |> Str.take (input.Length - 1) |> Str.split2 "<"
          let typeParameters =
            rest |> Str.splitBy "," |> Array.map Str.trim
          (name, typeParameters)
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
          TypeParameters =
            typeParameters
        }
    }
