namespace VainZero.Collections

open System.Collections.Generic

[<RequireQualifiedAccess>]
module Array =
  let tryDecomposeLast (this: array<'x>) =
    if this.Length = 0 then
      None
    else
      Some (this.[0..(this.Length - 2)], this.[this.Length - 1])

  let endsWith (suffix: array<'x>) (this: array<'x>): bool =
    if suffix.Length <= this.Length then
      let comparer = EqualityComparer.Default
      seq {
        for i in 1..suffix.Length do
          yield comparer.Equals(suffix.[suffix.Length - i], this.[this.Length - i])
      }
      |> Seq.forall id
    else
      false
