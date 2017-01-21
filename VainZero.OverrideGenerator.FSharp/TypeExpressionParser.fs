namespace VainZero.OverrideGenerator.FSharp

open System
open System.Text.RegularExpressions
open Basis.Core
open FParsec

type TypeExpression =
  {
    Qualifier:
      array<string>
    Name:
      string
    Arguments:
      array<TypeExpression>
  }
with
  override this.ToString() =
    let fullName =
      (this.Qualifier |> Array.map (fun q -> q + ".") |> String.concat "")
      + this.Name
    match this.Arguments with
    | [||] ->
      fullName
    | arguments ->
      sprintf "%s<%s>"
        fullName
        (arguments |> Array.map string |> String.concat ", ")

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TypeQueryParser =
  type Error =
    string

  module private Parsers =
    type Parser<'x> =
      Parser<'x, unit>

    let skipSpaceParser: Parser<unit> =
      skipMany (anyOf [' '; '\t'])

    let identifierParser: Parser<string> =
      parse {
        let! headChar = pchar '_' <|> asciiLetter
        let! tailChars = manyChars (pchar '_' <|> asciiLetter <|> digit)
        return string headChar + tailChars
      }

    let (typeExpressionParser: Parser<TypeExpression>, typeExpressionParserRef) =
      createParserForwardedToRef ()

    typeExpressionParserRef :=
      parse {
        let! names = sepBy1 identifierParser (skipChar '.')
        let name = names |> List.last
        let qualifier = names |> List.take (List.length names - 1)
        let! arguments =
          parse {
            do! skipSpaceParser
            return!
                between
                (skipChar '<') (skipChar '>')
                (sepBy1
                  (skipSpaceParser >>. typeExpressionParser)
                  (skipSpaceParser >>. skipChar ','))
          }
          |> attempt
          |> opt
        return
          {
            Qualifier =
              qualifier |> List.toArray
            Name =
              name
            Arguments =
              arguments
              |> Option.getOr []
              |> List.toArray
          }
      }

    let queryParser: Parser<_> =
      parse {
        do! skipSpaceParser
        let! expression =
          typeExpressionParser
        do! skipSpaceParser
        do! eof
        return expression
      }

  let tryParse (query: string): Result<TypeExpression, Error> =
    match runParserOnString Parsers.queryParser () "Query" query with
    | Success (expression, (), _) ->
      Basis.Core.Success expression
    | Failure (message, _, ()) ->
      Basis.Core.Failure message
