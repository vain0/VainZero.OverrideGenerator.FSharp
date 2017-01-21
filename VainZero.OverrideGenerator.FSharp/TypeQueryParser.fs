namespace VainZero.OverrideGenerator.FSharp

open System
open System.Text.RegularExpressions
open Basis.Core

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TypeQueryParser =
  type Error = unit

  type Result =
    {
      Query:
        string
      Path:
        array<string>
      Name:
        string
      TypeParameters:
        array<string>
    }

  let tryParse (query: string): Result<_, Error> =
    result {
      let (name, typeParameters) =
        // TODO: improve
        if query.Contains("<") && query.EndsWith(">") then
          let (name, rest) =
            query |> Str.take (query.Length - 1) |> Str.split2 "<"
          let typeParameters =
            rest |> Str.splitBy "," |> Array.map Str.trim
          (name, typeParameters)
        else
          (query, [||])
      return
        {
          Query =
            query
          Path =
            [||]
          Name =
            name
          TypeParameters =
            typeParameters
        }
    }
