namespace VainZero.OverrideGenerator.FSharp.UnitTest

open System
open System.IO
open System.Text
open Basis.Core
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open VainZero.IO
open VainZero.OverrideGenerator.FSharp

module TypeSearcherTest =
  let ``test FindOrError matching cases`` =
    let body (query, expected) =
      test {
        use searcher = new TypeSearcher()
        match searcher.FindOrError(query) with
        | Success actual ->
          do! actual |> Seq.toArray |> assertEquals expected
        | Failure error ->
          do! fail (sprintf "Error: %O" error)
      }
    parameterize {
      case
        ( "IDisposable"
        , [| typeof<System.IDisposable> |]
        )
      case
        ( "IEnumerable<T>"
        , [| typedefof<System.Collections.Generic.IEnumerable<_>> |]
        )
      run body
    }
