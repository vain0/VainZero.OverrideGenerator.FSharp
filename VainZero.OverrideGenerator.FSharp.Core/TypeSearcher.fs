namespace VainZero.OverrideGenerator.FSharp

open System
open System.Reflection
open Basis.Core
open VainZero.Collections
open VainZero.Reflection

module TypeSearcherModule =
  type Error =
    | AssemblyLoadError
      of string * exn

  let matches ((suffix, pattern): QualifiedTypeExpression) (typ: Type) =
    if
      (typ |> Type.rawName) = pattern.Name
      && typ.GetGenericArguments().Length = pattern.Arguments.Length
    then
      let (namespaces, classes) =
        Type.qualifier typ
      let qualifier =
        Array.append namespaces classes
      if suffix.Length <= qualifier.Length then
        seq {
          for i in 1..suffix.Length do
            let actual = qualifier.[qualifier.Length - i]
            let expected = suffix.[suffix.Length - i]
            let expectedName =
              if expected.Arguments.Length = 0
              then expected.Name
              else sprintf "%s`%d" expected.Name expected.Arguments.Length
            yield actual = expectedName
        } |> Seq.forall id
      else
        false
    else
      false

  let find (assemblies: seq<Assembly>) query =
    assemblies
    |> Seq.collect (fun a -> a.GetTypes())
    |> Seq.filter (matches query)

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

  let find query =
    find (assemblies ()) query

  let dispose () =
    AppDomain.Unload(appDomain)

  member this.LoadOrError(path) =
    tryLoad path

  member this.LoadOrError(paths) =
    tryLoadMany paths

  member this.Find(query) =
    find query

  member this.Dispose() =
    dispose ()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
