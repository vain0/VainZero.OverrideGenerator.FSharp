namespace VainZero.OverrideGenerator.FSharp.Console

open System
open VainZero.OverrideGenerator.FSharp

module Program =
  [<EntryPoint>]
  let main argv =
    try
      let argument = AppArgument.parse argv
      let generator = OverrideGenerator(argument.Type)
      for reference in argument.References do
        generator.Load(reference)
      let content = generator.Generate()
      Console.WriteLine(content)
      0
    with
    | e ->
      Console.Error.WriteLine(AppArgument.parser.PrintUsage(programName = argv.[0]))
      -1
