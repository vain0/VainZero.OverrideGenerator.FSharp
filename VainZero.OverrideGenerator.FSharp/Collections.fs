namespace VainZero.Collections

open System.Collections.Generic

[<RequireQualifiedAccess>]
module Array =
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
