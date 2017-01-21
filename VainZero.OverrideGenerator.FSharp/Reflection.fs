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
