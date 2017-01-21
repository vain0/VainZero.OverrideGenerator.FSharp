namespace VainZero.OverrideGenerator.FSharp

open System
open System.IO
open System.Reflection
open VainZero.IO

[<Sealed>]
type OverrideWriter(writer: StructuralTextWriter, receiver, stub) =
  let writeStub () =
    writer.WriteLineAsync(stub)

  let writeProperty (propertyInfo: PropertyInfo) =
    async {
      match (propertyInfo.GetMethod, propertyInfo.SetMethod) with
      | (null, null) -> ()
      | (getter, null) ->
        do!
          writer.WriteLineAsync
            (sprintf "override %s.%s =" receiver propertyInfo.Name)
        use indenting = writer.AddIndent()
        return! writeStub ()
      | (null, setter) ->
        do!
          writer.WriteLineAsync
            (sprintf "override %s.%s" receiver propertyInfo.Name)
        use indenting = writer.AddIndent()
        do!
          writer.WriteLineAsync
            (sprintf "with set value =")
        use indenting = writer.AddIndent()
        return! writeStub ()
      | (getter, setter) ->
        do!
          writer.WriteLineAsync
            (sprintf "override %s.%s" receiver propertyInfo.Name)
        use indenting = writer.AddIndent()
        do!
          async {
            do! writer.WriteLineAsync("with get () =")
            use indenting = writer.AddIndent()
            do! writeStub ()
          }
        return!
          async {
            do! writer.WriteLineAsync("and set value =")
            use indenting = writer.AddIndent()
            do! writeStub ()
          }
    }

  let writeMethod (methodInfo: MethodInfo) =
    async {
      let parameterList =
        methodInfo.GetParameters()
        |> Array.mapi
          (fun i parameter ->
            match parameter.Name with
            | null -> sprintf "arg%d" (i + 1)
            | name -> name
          )
        |> String.concat ", "
      let declaration =
        sprintf "override %s.%s(%s) =" receiver methodInfo.Name parameterList
      do! writer.WriteLineAsync(declaration)
      use indenting = writer.AddIndent()
      do! writeStub ()
    }

  let writeMember (memberInfo: MemberInfo) =
    match memberInfo with
    | :? MethodInfo as methodInfo ->
      writeMethod methodInfo
    | :? PropertyInfo as propertyInfo ->
      writeProperty propertyInfo
    | _ ->
      async { () }

  let writeMembers (members: seq<MemberInfo>) =
    async {
      for (index, mem) in members |> Seq.indexed do
        if index > 0 then
          do! writer.WriteLineAsync("")
        do! writeMember mem
    }

  let write (typ: Type) (typeName: string) (writes: MemberInfo -> bool) =
    async {
      let members =
        typ.GetMembers() |> Array.filter writes
      if typ.IsInterface then
        match members with
        | [||] ->
          do! writer.WriteLineAsync(sprintf "interface %s" typeName)
        | members ->
          do! writer.WriteLineAsync(sprintf "interface %s with" typeName)
          use indenting = writer.AddIndent()
          return! writeMembers members
      else
        match members with
        | [||] ->
          ()
        | members ->
          return! writeMembers members
    }

  member this.WriteAsync(typ, typeName, writes) =
    write typ typeName writes

  member this.Write(typ, typeName, writes) =
    this.WriteAsync(typ, typeName, writes) |> Async.RunSynchronously

  member this.Write(typ, typeName) =
    this.Write(typ, typeName, (fun _ -> true))

  member this.Write(typ: Type) =
    this.Write(typ, typ.Name)
