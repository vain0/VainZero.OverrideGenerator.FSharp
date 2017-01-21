namespace VainZero.OverrideGenerator.FSharp

open System
open System.Reflection
open Basis.Core

module TypeSearcherModule =
  type Error =
    | QueryParseError
      of TypeQueryParser.Error
    | AssemblyLoadError
      of string * exn

  let matches (result: TypeQueryParser.Result) (typ: Type) =
    let name = typ.Name |> Str.takeWhile ((<>) '`')
    name = result.Name
    && typ.GenericTypeArguments.Length = result.TypeArguments.Length

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

  let tryFind (typeName: string): Result<_, Error> =
    result {
      let! result =
        typeName |> TypeQueryParser.tryParse
        |> Result.mapFailure QueryParseError
      return
        assemblies ()
        |> Seq.collect (fun a -> a.GetTypes())
        |> Seq.filter (matches result)
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
