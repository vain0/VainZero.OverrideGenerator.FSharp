namespace VainZero.OverrideGenerator.FSharp

open System
open System.IO
open System.Reflection
open Basis.Core
open VainZero.IO

module OverrideGeneratorModule =
  type Error =
    | NoMatchingType
    | FailedLoad
      of string * exn

  let isAbstract (memberInfo: MemberInfo) =
    match memberInfo.MemberType with
    | MemberTypes.Method ->
      let methodInfo = memberInfo :?> MethodInfo
      methodInfo.IsAbstract
    | MemberTypes.Property ->
      let propertyInfo = memberInfo :?> PropertyInfo
      let isAbstract (methodInfo: MethodInfo) =
        methodInfo |> isNull |> not && methodInfo.IsAbstract
      propertyInfo.GetMethod |> isAbstract || propertyInfo.SetMethod |> isAbstract
    | _ ->
      false

  let isNotSpecialMethod (memberInfo: MemberInfo) =
    match memberInfo.MemberType with
    | MemberTypes.Method ->
      let methodInfo = memberInfo :?> MethodInfo
      methodInfo.IsSpecialName |> not
    | _ ->
      true

open OverrideGeneratorModule

type OverrideGenerator() =
  let appDomain =
    AppDomain.CreateDomain("VainZero.OverrideGenerator.FSharp")

  let explicitlyLoadedAssemblies =
    ResizeArray<_>()

  let assemblies () =
    seq {
      yield! explicitlyLoadedAssemblies
      yield! appDomain.GetAssemblies()
    }

  let tryLoad (path: string) =
    result {
      let! assemblyName =
        try
          AssemblyName.GetAssemblyName(path) |> Success
        with
        | e ->
          FailedLoad (path, e) |> Failure
      let! assembly =
        try
          appDomain.Load(assemblyName) |> Success
        with
        | e ->
          FailedLoad (path, e) |> Failure
      explicitlyLoadedAssemblies.Add(assembly)
      return ()
    }

  let tryLoadMany paths =
    paths |> Seq.fold
      (fun result path ->
        match result with
        | Success () ->
          tryLoad path
        | Failure error ->
          error |> Failure
      ) (Success ())

  let selectType choose (typeName: string) =
    let result =
      typeName |> TypeParser.parse
    let types =
      assemblies ()
      |> Seq.collect (fun a -> a.GetTypes())
      |> Seq.filter
        (fun typ ->
          typ.Name = result.Name
          && typ.GenericTypeArguments.Length = result.TypeArguments.Length
        )
      |> Seq.toArray
    match types with
    | [||] ->
      NoMatchingType |> Failure
    | [|typ|] ->
      typ |> Success
    | types ->
      match types |> choose with
      | Some typ ->
        typ |> Success
      | None ->
        NoMatchingType |> Failure

  let tryGenerate (writer: OverrideWriter) typeName choose =
    result {
      let! typ =
        selectType choose typeName
      let writes memberInfo =
        isAbstract memberInfo && isNotSpecialMethod memberInfo
      return writer.WriteAsync(typ, typeName, writes)
    }

  let dispose () =
    AppDomain.Unload(appDomain)

  member this.LoadOrError(path) =
    tryLoad path

  member this.LoadOrError(paths) =
    tryLoadMany paths

  member this.GenerateOrError(writer, typeName, choose) =
    tryGenerate writer typeName choose

  member this.Dispose() =
    dispose ()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
