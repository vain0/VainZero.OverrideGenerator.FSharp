# OverrideGenerator for `F#`
Generates `override` stubs in F# syntax.

[Download the latest binary.](https://github.com/vain0/VainZero.OverrideGenerator.FSharp/releases/latest)

## Usage
### Basic
```
fsoverride -t type-name-to-inherit-from
```

For example, try:

```
fsoverride -t IEnumerable
```

and get:

```fsharp
interface IEnumerable with
  override this.GetEnumerator() =
    System.NotImplementedException() |> raise
```

The type name can contain namespace path and type arguments. Namespace path matches the suffix of full namaspace path. For example, ``-t "Generic.IEnumerable<int>"`` generates:

```fsharp
interface IEnumerable with
  override this.GetEnumerator() =
    System.NotImplementedException() |> raise

interface IEnumerable<int> with
  override this.GetEnumerator() =
    System.NotImplementedException() |> raise
```

### References
To generate stubs for the type defined in an assembly, specify ``-r`` parameter. For example, try:

```
fsoverride -r Argu.dll -t IArgParserTemplate
```

and get:

```fsharp
interface IArgParserTemplate with
  override this.Usage =
    System.NotImplementedException() |> raise
```

where `IArgParserTemplate` is a type defined in `Argu.dll`.

### Help command
```
fsoverride --help
```

## License
[MIT License](LICENSE.md)
