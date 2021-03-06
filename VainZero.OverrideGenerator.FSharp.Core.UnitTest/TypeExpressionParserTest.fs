﻿namespace VainZero.OverrideGenerator.FSharp.UnitTest

open Basis.Core
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open VainZero.OverrideGenerator.FSharp

module TypeExpressionParserTest =
  let ``test tryParse success`` =
    let body (input, qualifier, name, arguments) =
      test {
        let (suffix, actual) = input |> TypeExpressionParser.tryParse |> Result.get
        do! suffix |> assertEquals qualifier
        do! actual.Name |> assertEquals name
        do! actual.Arguments |> Array.map string |> assertEquals arguments
      }
    parameterize {
      case
        ( "IEnumerable"
        , [||]
        , "IEnumerable"
        , [||]
        )
      case
        ( "System.Collections.IEnumerable"
        , [|"System"; "Collections"|]
          |> Array.map (fun n -> { Name = n; Arguments = [||] })
        , "IEnumerable"
        , [||]
        )
      case
        ( "IEnumerable<T>"
        , [||]
        , "IEnumerable"
        , [|"T"|]
        )
      case
        ( "IEnumerable<'x>"
        , [||]
        , "IEnumerable"
        , [|"'x"|]
        )
      case
        ( "System.Collections.Generic.IEnumerable<T>"
        , [|"System"; "Collections"; "Generic"|]
          |> Array.map (fun n -> { Name = n; Arguments = [||] })
        , "IEnumerable"
        , [|"T"|]
        )
      case
        ( "IEnumerable<IEnumerable<T>>"
        , [||]
        , "IEnumerable"
        , [|"IEnumerable<T>"|]
        )
      case
        ( "IDictionary<T, U>"
        , [||]
        , "IDictionary"
        , [|"T"; "U"|]
        )
      case
        ( "IDictionary<IDictionary<X, Y>, IDictionary<Z, W>>"
        , [||]
        , "IDictionary"
        , [|"IDictionary<X, Y>"; "IDictionary<Z, W>"|]
        )
      case
        ( " IDictionary < IDictionary < X , Y > , IEnumerable < Z > > "
        , [||]
        , "IDictionary"
        , [|"IDictionary<X, Y>"; "IEnumerable<Z>"|]
        )
      run body
    }
