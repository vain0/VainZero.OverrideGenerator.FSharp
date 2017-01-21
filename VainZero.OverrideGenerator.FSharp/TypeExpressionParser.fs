namespace VainZero.OverrideGenerator.FSharp

open System
open System.Text.RegularExpressions
open Basis.Core
open FParsec
open VainZero.Collections

type TypeExpression =
  | VariableTypeExpression
    of string
  | AppliedTypeExpression
    of AppliedTypeExpression
  | QualifiedTypeExpression
    of QualifiedTypeExpression
with
  override this.ToString() =
    match this with
    | VariableTypeExpression name ->
      "'" + name
    | AppliedTypeExpression applied ->
      applied |> string
    | QualifiedTypeExpression (qualifier, last) ->
      (qualifier |> Array.map (fun q -> string q + ".") |> String.concat "")
      + string last

and AppliedTypeExpression =
  {
    Name:
      string
    Arguments:
      array<TypeExpression>
  }
with
  override this.ToString() =
    match this.Arguments with
    | [||] ->
      this.Name
    | arguments ->
      sprintf "%s<%s>"
        this.Name
        (arguments |> Array.map string |> String.concat ", ")

and QualifiedTypeExpression =
  array<AppliedTypeExpression> * AppliedTypeExpression

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TypeExpressionParser =
  type Error =
    string

  module private Parsers =
    type Parser<'x> =
      Parser<'x, unit>

    let skipSpaceParser: Parser<unit> =
      skipMany (skipAnyOf [' '; '\t'])

    let identifierParser: Parser<string> =
      parse {
        let! headChar = pchar '_' <|> asciiLetter
        let! tailChars = manyChars (pchar '_' <|> asciiLetter <|> digit)
        return string headChar + tailChars
      }

    let (typeExpressionParser: Parser<TypeExpression>, typeExpressionParserRef) =
      createParserForwardedToRef ()

    let variableTypeExpressionParser =
      parse {
        do! skipChar '\''
        return! identifierParser
      }

    let appliedTypeExpressionParser =
      parse {
        let! name = identifierParser
        let! arguments =
          parse {
            do! skipSpaceParser
            return!
                between
                (skipChar '<' .>> skipSpaceParser) (skipChar '>')
                (sepBy1
                  (typeExpressionParser .>> skipSpaceParser)
                  (skipChar ',' .>> skipSpaceParser))
          }
          |> attempt
          |> opt
        return
          {
            Name =
              name
            Arguments =
              arguments
              |> Option.getOr []
              |> List.toArray
          }
      }

    let qualifiedTypeExpressionParser =
      sepBy1 appliedTypeExpressionParser (skipChar '.')
      |>>
        (fun expressions ->
          expressions
          |> List.toArray
          |> Array.tryDecomposeLast
          |> Option.get
        )

    typeExpressionParserRef :=
      attempt
        (variableTypeExpressionParser |>> VariableTypeExpression)
      <|>
        (qualifiedTypeExpressionParser |>> QualifiedTypeExpression)

    let queryParser: Parser<_> =
      parse {
        do! skipSpaceParser
        let! expression =
          qualifiedTypeExpressionParser
        do! skipSpaceParser
        do! eof
        return expression
      }

  let tryParse (query: string): Result<QualifiedTypeExpression, Error> =
    match runParserOnString Parsers.queryParser () "Query" query with
    | Success (expression, (), _) ->
      Basis.Core.Success expression
    | Failure (message, _, ()) ->
      Basis.Core.Failure message
