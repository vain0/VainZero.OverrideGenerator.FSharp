namespace VainZero.OverrideGenerator.FSharp.UnitTest

open System
open System.IO
open System.Text
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open VainZero.IO
open VainZero.OverrideGenerator.FSharp

module OverrideWriterTest =
  type IEmpty =
    interface
    end

  type IParameterlessMethod =
    abstract F: unit -> unit
    
  type IParameterMethod =
    abstract F: int -> int

  type INamedParameterMethod =
    abstract F: n: int * r: double -> int

  type IReadOnlyProperty =
    abstract X: int

  type IWriteOnlyProperty =
    abstract X: int with set

  type IProperty =
    abstract X: int with get, set

  type ITwoMembers =
    abstract F: n: int -> int
    abstract X: double

  type IGeneric1<'x> =
    abstract X: 'x

  type IGeneric2<'x, 'y> =
    abstract F: 'x -> 'y

  let receiver = "self"
  let stub = "todo ()"
  let indentWidth = 4

  let seed () =
    let stringBuilder = StringBuilder()
    let stringWriter = new StringWriter(stringBuilder)
    let structuralWriter = StructuralTextWriter(stringWriter, indentWidth)
    let writer = OverrideWriter(structuralWriter, receiver, stub)
    (writer, (fun () -> string stringBuilder))

  let ``test Write non-generic types`` =
    let body (typ, expected) =
      test {
        let (writer, get) = seed ()
        writer.Write(typ, [||], OverrideGeneratorModule.isNotSpecialMethod)
        do! get () |> assertEquals expected
      }
    parameterize {
      case
        ( typeof<IEmpty>
        , """interface IEmpty
"""
        )
      case
        ( typeof<IParameterlessMethod>
        , """interface IParameterlessMethod with
    override self.F() =
        todo ()
"""
        )
      case
        ( typeof<IParameterMethod>
        , """interface IParameterMethod with
    override self.F(arg1) =
        todo ()
"""
        )
      case
        ( typeof<INamedParameterMethod>
        , """interface INamedParameterMethod with
    override self.F(n, r) =
        todo ()
"""
        )
      case
        ( typeof<IReadOnlyProperty>
        , """interface IReadOnlyProperty with
    override self.X =
        todo ()
"""
        )
      case
        ( typeof<IWriteOnlyProperty>
        , """interface IWriteOnlyProperty with
    override self.X
        with set value =
            todo ()
"""
        )
      case
        ( typeof<IProperty>
        , """interface IProperty with
    override self.X
        with get () =
            todo ()
        and set value =
            todo ()
"""
        )
      case
        ( typeof<ITwoMembers>
        , """interface ITwoMembers with
    override self.F(n) =
        todo ()

    override self.X =
        todo ()
"""
        )
      run body
    }

  let ``test Write generic types`` =
    let body (typ, arguments, expected) =
      test {
        let (writer, get) = seed ()
        writer.Write(typ, arguments, OverrideGeneratorModule.isNotSpecialMethod)
        do! get () |> assertEquals expected
      }
    parameterize {
      case
        ( typedefof<IGeneric1<_>>
        , [|"'x"|]
        , """interface IGeneric1<'x> with
    override self.X =
        todo ()
"""
        )
      case
        ( typedefof<IGeneric2<_, _>>
        , [|"'x"; "'y"|]
        , """interface IGeneric2<'x, 'y> with
    override self.F(arg1) =
        todo ()
"""
        )
      run body
    }
