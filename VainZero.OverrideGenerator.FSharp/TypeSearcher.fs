namespace VainZero.OverrideGenerator.FSharp

open System
open System.Reflection
open Basis.Core
open VainZero.Reflection

module TypeSearcherModule =
  type Error =
    | AssemblyLoadError
      of string * exn

  let matches (typeExpression: TypeExpression) (typ: Type) =
    let name = typ |> Type.rawName
    if name = typeExpression.Name then
      typ.GetGenericArguments().Length = typeExpression.Arguments.Length
    else
      false

open TypeSearcherModule

type TypeSearcher() =
  let appDomain =
    AppDomain.CreateDomain("VainZero.OverrideGenerator.FSharp.TypeSearcher")

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
          AssemblyLoadError (path, e) |> Failure
      let! assembly =
        try
          appDomain.Load(assemblyName) |> Success
        with
        | e ->
          AssemblyLoadError (path, e) |> Failure
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

  let tryFind (query: TypeExpression): Result<_, Error> =
    result {
      return
        assemblies ()
        |> Seq.collect (fun a -> a.GetTypes())
        |> Seq.filter (matches query)
    }

  let dispose () =
    AppDomain.Unload(appDomain)

  member this.LoadOrError(path) =
    tryLoad path

  member this.LoadOrError(paths) =
    tryLoadMany paths

  member this.FindOrError(query) =
    tryFind query

  member this.Dispose() =
    dispose ()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
