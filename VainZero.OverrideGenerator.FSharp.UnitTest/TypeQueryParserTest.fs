﻿namespace VainZero.OverrideGenerator.FSharp.UnitTest

open Basis.Core
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open VainZero.OverrideGenerator.FSharp

module TypeQueryParserTest =
  let ``test tryParse success`` =
    let body (input, qualifier, name, arguments) =
      test {
        let actual = input |> TypeQueryParser.tryParse |> Result.get
        do! actual.Qualifier |> assertEquals qualifier
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
        ( "System.Collections.Generic.IEnumerable<T>"
        , [|"System"; "Collections"; "Generic"|]
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
      run body
    }
