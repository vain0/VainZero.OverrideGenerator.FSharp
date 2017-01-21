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
  let ``test find`` =
    let body (query, expected) =
      test {
        let assemblies = AppDomain.CurrentDomain.GetAssemblies()
        let query = query |> TypeExpressionParser.tryParse |> Result.get
        let actual = TypeSearcherModule.find assemblies query
        do! actual |> Seq.toArray |> assertEquals expected
      }
    parameterize {
      case
        ( "IDisposable"
        , [| typeof<System.IDisposable> |]
        )
      case
        ( "IEnumerable<'x>"
        , [| typedefof<System.Collections.Generic.IEnumerable<_>> |]
        )
      case
        ( "IDictionary<T, U>"
        , [| typedefof<System.Collections.Generic.IDictionary<_, _>> |]
        )
      case
        ( "Collections.IEnumerable"
        , [| typeof<System.Collections.IEnumerable> |]
        )
      case
        ( "List<_>.Enumerator<_>" // TODO: It should match "List<_>.Enumerator".
        , [| typedefof<System.Collections.Generic.List<_>.Enumerator> |]
        )
      run body
    }
