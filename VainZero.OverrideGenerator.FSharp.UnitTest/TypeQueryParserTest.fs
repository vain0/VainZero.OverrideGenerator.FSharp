namespace VainZero.OverrideGenerator.FSharp.UnitTest

open Basis.Core
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open VainZero.OverrideGenerator.FSharp

module TypeQueryParserTest =
  let ``test parse`` =
    let body (input, path, name, arguments) =
      test {
        let actual = input |> TypeQueryParser.tryParse |> Result.get
        do! actual.Path |> assertEquals path
        do! actual.Name |> assertEquals name
        do! actual.TypeParameters |> assertEquals arguments
      }
    parameterize {
      case
        ( "IEnumerable"
        , [||]
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
      (*
      case
        ( "IDictionary<IDictionary<X, Y>, IDictionary<Z, W>>"
        , [||]
        , "IDictionary"
        , [|"IDictionary<X, Y>"; "IDictionary<Z, W>"|]
        )
      //*)
      run body
    }
