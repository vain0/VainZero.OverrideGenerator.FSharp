namespace VainZero.OverrideGenerator.FSharp

open System
open System.IO
open System.Reflection
open Basis.Core
open VainZero.IO

module OverrideGeneratorModule =
  type Error =
    | TypeQueryParseError
      of TypeQueryParser.Error
    | TypeSearcherError
      of TypeSearcherModule.Error
    | NoMatchingType

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

type OverrideGenerator(searcher: TypeSearcher) =
  let writes memberInfo =
    isAbstract memberInfo && isNotSpecialMethod memberInfo

  let tryGenerate (writer: OverrideWriter) query =
    result {
      let! query =
        query |> TypeQueryParser.tryParse
        |> Result.mapFailure TypeQueryParseError
      let! types =
        searcher.FindOrError(query)
        |> Result.mapFailure TypeSearcherError
      match types |> Seq.tryHead with
      | Some typ ->
        return writer.WriteAsync(typ, query.TypeParameters, writes)
      | None ->
        return! Failure NoMatchingType
    }

  member this.GenerateOrError(writer, typeName) =
    tryGenerate writer typeName
