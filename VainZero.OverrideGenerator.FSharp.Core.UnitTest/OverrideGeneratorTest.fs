namespace VainZero.OverrideGenerator.FSharp.UnitTest

open System
open System.Collections.Generic
open System.IO
open System.Text
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open VainZero.IO
open VainZero.OverrideGenerator.FSharp

module OverrideGeneratorTest =
  [<AbstractClass>]
  type Base() =
    abstract AbstractProperty: int
    abstract AbstractMethod: unit -> int

    abstract VirtualMethod: unit -> unit
    default this.VirtualMethod() = ()

  type Derived() =
    inherit Base()

    override this.AbstractProperty = 0

    override this.AbstractMethod() = 0

  let objectMembers =
    [|"ToString"; "Equals"; "GetHashCode"|]

  let ``test canOverride`` =
    let body (typ: Type, expected) =
      test {
        let actual =
          typ.GetMembers()
          |> Array.filter
            (fun mi ->
              OverrideGeneratorModule.isNotSpecialMethod mi
              && OverrideGeneratorModule.canOverride mi
              && (objectMembers |> Array.contains mi.Name |> not)
            )
          |> Array.map (fun mi -> mi.Name)
        do! actual |> assertEquals expected
      }
    parameterize {
      case
        ( typeof<Base>
        , [| "AbstractMethod"; "VirtualMethod"; "AbstractProperty" |]
        )
      case
        ( typeof<Derived>
        , [| "AbstractMethod"; "VirtualMethod"; "AbstractProperty" |]
        )
      run body
    }

  let ``test isAbstract`` =
    let body (typ: Type, expected) =
      test {
        let actual =
          typ.GetMembers()
          |> Array.filter
            (fun mi ->
              OverrideGeneratorModule.isNotSpecialMethod mi
              && OverrideGeneratorModule.isAbstract mi
              && (objectMembers |> Array.contains mi.Name |> not)
            )
          |> Array.map (fun mi -> mi.Name)
        do! actual |> assertEquals expected
      }
    parameterize {
      case
        ( typeof<Base>
        , [| "AbstractMethod"; "AbstractProperty" |]
        )
      case
        ( typeof<Derived>
        , [||]
        )
      run body
    }
