﻿namespace VainZero.OverrideGenerator.FSharp

open System
open System.IO
open System.Reflection
open Basis.Core
open VainZero.IO

module OverrideGeneratorModule =
  type Error =
    | TypeQueryParseError
      of TypeExpressionParser.Error
    | TypeSearcherError
      of TypeSearcherModule.Error
    | NoMatchingType

  let canOverride (memberInfo: MemberInfo) =
    let canOverride (methodInfo: MethodInfo) =
      methodInfo |> isNull |> not
      && (methodInfo.IsAbstract || methodInfo.IsVirtual)
    match memberInfo.MemberType with
    | MemberTypes.Method ->
      (memberInfo :?> MethodInfo) |> canOverride
    | MemberTypes.Property ->
      let propertyInfo = memberInfo :?> PropertyInfo
      propertyInfo.GetMethod |> canOverride || propertyInfo.SetMethod |> canOverride
    | _ ->
      false

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
  let writes overridesDefault memberInfo =
    isNotSpecialMethod memberInfo
    && (if overridesDefault then canOverride else isAbstract) memberInfo

  let tryGenerate (writer: OverrideWriter) overridesDefault query =
    result {
      let! query =
        query |> TypeExpressionParser.tryParse
        |> Result.mapFailure TypeQueryParseError
      let types =
        searcher.Find(query)
      match types |> Seq.tryHead with
      | Some typ ->
        let arguments = (query |> snd).Arguments |> Array.map string
        return writer.WriteAsync(typ, arguments, writes overridesDefault)
      | None ->
        return! Failure NoMatchingType
    }

  member this.GenerateOrError(writer, overridesDefault, query) =
    tryGenerate writer overridesDefault query
