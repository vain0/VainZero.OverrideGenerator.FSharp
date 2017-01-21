namespace VainZero.Reflection

open System
open System.Reflection

module Type =
  let rawName (typ: Type) =
    if typ.IsGenericType || typ.IsGenericTypeDefinition then
      let length = typ.Name.IndexOf('`')
      if length < 0
      then typ.Name
      else typ.Name.Substring(0, length)
    else
      typ.Name

  let parseQualifiedName (fullName: string) =
    let namespaces =
      fullName.Split('.')
    let qualifiedClassName =
      namespaces |> Array.last
    let namespaces =
      namespaces.[0..(namespaces.Length - 2)]
    let classes =
      qualifiedClassName.Split('+')
    let classes =
      classes.[0..(classes.Length - 2)]
    (namespaces, classes)

  /// N1.N2.C1+C2+C3`1 -> ([|"N1"; "N2"|], [|"C1"; "C2"|])
  let qualifier (typ: Type) =
    parseQualifiedName typ.FullName
