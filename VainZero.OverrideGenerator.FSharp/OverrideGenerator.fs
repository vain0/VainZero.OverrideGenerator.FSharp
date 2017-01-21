namespace VainZero.OverrideGenerator.FSharp

open System
open System.IO
open System.Reflection
open Basis.Core
open VainZero

module OverrideGeneratorModule =
  type Error =
    | NoMatchingType
    | FailedLoad
      of exn

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
          FailedLoad e |> Failure
      let! assembly =
        try
          appDomain.Load(assemblyName) |> Success
        with
        | e ->
          FailedLoad e |> Failure
      explicitlyLoadedAssemblies.Add(assembly)
      return ()
    }

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

  let tryGenerate textWriter typeName choose =
    result {
      let writer = OverrideWriter(textWriter)
      let! typ = selectType choose typeName
      return writer.WriteAsync(typ, typeName, isAbstract)
    }

  let dispose () =
    AppDomain.Unload(appDomain)

  member this.LoadOrError(path) =
    tryLoad path

  member this.GenerateOrError(textWriter, typeName, choose) =
    tryGenerate textWriter typeName choose

  member this.Dispose() =
    dispose ()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
