namespace VainZero.OverrideGenerator.FSharp

open System
open System.IO
open System.Reflection
open Basis.Core
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

  let writeClass writes arguments (typ: Type) =
    async {
      match typ.GetMembers() |> Array.filter writes with
      | [||] ->
        ()
      | members ->
        return! writeMembers members
    }

  let write (writes: MemberInfo -> bool) arguments (typ: Type) =
    async {
      if typ.IsInterface then
        for (i, type') in typ.GetInterfaces() |> Array.append [| typ |] |> Seq.indexed do
          if i > 0 then
            do! writer.WriteLineAsync("")
          if type'.IsGenericTypeDefinition then
            let map =
              typ.GetGenericArguments()
              |> Seq.zip arguments
              |> Seq.map (fun (name, t) -> (t.Name, name))
              |> Map.ofSeq
            let parameterList =
              type'.GetGenericArguments()
              |> Seq.map (fun t -> map |> Map.tryFind t.Name |> Option.getOr "_")
              |> String.concat ", "
            let typeName =
              sprintf "%s<%s>" (type'.Name |> Str.takeWhile ((<>) '`')) parameterList
            do! writeInterface writes typeName type'
          else
            do! writeInterface writes type'.Name type'
      else
        return! writeClass writes arguments typ
    }

  member this.WriteAsync(typ, arguments, writes) =
    write writes arguments typ

  member this.Write(typ, arguments, writes) =
    this.WriteAsync(typ, arguments, writes) |> Async.RunSynchronously

  member this.Write(typ, arguments) =
    this.Write(typ, arguments, (fun _ -> true))

  member this.Write(typ: Type) =
    this.Write(typ, [||])
