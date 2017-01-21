namespace VainZero.Reflection.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open VainZero.Reflection

module TypeTest =
  let ``test qualifier`` =
    let body (source, namespaces, classes) =
      test {
        let (actualNamespaces, actualClasses) =
          Type.parseQualifiedName source
        do! actualNamespaces |> assertEquals namespaces
        do! actualClasses |> assertEquals classes
      }
    parameterize {
      case ("X", [||], [||])
      case ("C1+X", [||], [|"C1"|])
      case ("C1+C2+X", [||], [|"C1"; "C2"|])
      case ("N1.X", [|"N1"|], [||])
      case ("N1.N2.X", [|"N1"; "N2"|], [||])
      case ("N1.N2.C1+C2+X", [|"N1"; "N2"|], [|"C1"; "C2"|])
      run body
    }
