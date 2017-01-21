# OverrideGenerator for F#
Generates `override` stubs in F# syntax.

## Usage
### Basic
```
VainZero.OverrideGenerator.FSharp.exe -t type-name-to-inherit-from
```

For example, try:

```
VainZero.OverrideGenerator.FSharp.exe -t IEnumerable
``

and get:

```
interface IEnumerable with
  override this.GetEnumerator() =
    System.NotImplementedException() |> raise
```

The type name can contain namespace path and type arguments. Namespace path matches the suffix of full namaspace path. For example, ``-t "Generic.IEnumerable<int>"`` generates:

```
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
VainZero.OverrideGenerator.FSharp.exe -r Argu.dll -t IArgParserTemplate
```

and get:

```
interface IArgParserTemplate with
  override this.Usage =
    System.NotImplementedException() |> raise
```

where `IArgParserTemplate` is a type defined in `Argu.dll`.

### Help command
```
VainZero.OverrideGenerator.FSharp.exe --help
```

## License
[MIT License](LICENSE.md)
