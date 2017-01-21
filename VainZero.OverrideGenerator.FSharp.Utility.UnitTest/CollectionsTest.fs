namespace VainZero.Collections.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open VainZero.Collections

module ArrayTest =
  let ``test endsWith true`` =
    let body (xs, suffix) =
      test {
        do! xs |> Array.endsWith suffix |> assertPred
      }
    parameterize {
      case ([||], [||])
      case ([|0|], [||])
      case ([|0|], [|0|])
      case ([|0; 1; 2|], [|2|])
      case ([|0; 1; 2|], [|1; 2|])
      run body
    }

  let ``test endsWith false`` =
    let body (xs, suffix) =
      test {
        do! xs |> Array.endsWith suffix |> not |> assertPred
      }
    parameterize {
      case ([||], [|0|])
      case ([|0|], [|1; 0|])
      case ([|0; 1; 2|], [|0; 1|])
      case ([|0; 1; 2|], [|0; 2|])
      run body
    }
