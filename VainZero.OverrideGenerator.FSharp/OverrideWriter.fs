namespace VainZero.OverrideGenerator.FSharp

open System
open System.IO
open System.Reflection
open Basis.Core
open VainZero.IO
open VainZero.Reflection

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

  let typeName arguments (typ: Type) =
    if typ.IsGenericTypeDefinition
    then sprintf "%s<%s>" typ.Name (arguments |> String.concat ", ")
    else typ.Name

  let writeInterface writes typeName (typ: Type) =
    async {
      match typ.GetMembers() |> Array.filter writes with
      | [||] ->
        do! writer.WriteLineAsync(sprintf "interface %s" typeName)
      | members ->
        do! writer.WriteLineAsync(sprintf "interface %s with" typeName)
        use indenting = writer.AddIndent()
        return! writeMembers members
    }

  let writeInterfaceAll writes arguments (typ: Type) =
    async {
      let interfaces = Array.append (typ.GetInterfaces()) [| typ |] |> Seq.indexed
      let map =
        typ.GetGenericArguments()
        |> Seq.zip arguments
        |> Seq.map (fun (name, t) -> (t.Name, name))
        |> Map.ofSeq
      let rec typeName (t: Type) =
        if t.IsGenericParameter then
          map |> Map.tryFind t.Name |> Option.getOr "_"
        else if t.IsGenericType || t.IsGenericTypeDefinition then
          let parameters =
            t.GetGenericArguments() |> Seq.map typeName |> String.concat ", "
          sprintf "%s<%s>" (t |> Type.rawName) parameters
        else
          t.Name
      for (i, superType) in interfaces do
        if i > 0 then
          do! writer.WriteLineAsync("")
        do! writeInterface writes (superType |> typeName) superType
    }

  let writeClass writes arguments (typ: Type) =
    match typ.GetMembers() |> Array.filter writes with
    | [||] ->
      async { () }
    | members ->
      writeMembers members

  let write (writes: MemberInfo -> bool) arguments (typ: Type) =
    if typ.IsInterface then
      writeInterfaceAll writes arguments typ
    else
      writeClass writes arguments typ

  member this.WriteAsync(typ, arguments, writes) =
    write writes arguments typ

  member this.Write(typ, arguments, writes) =
    this.WriteAsync(typ, arguments, writes) |> Async.RunSynchronously

  member this.Write(typ, arguments) =
    this.Write(typ, arguments, (fun _ -> true))

  member this.Write(typ: Type) =
    this.Write(typ, [||])
